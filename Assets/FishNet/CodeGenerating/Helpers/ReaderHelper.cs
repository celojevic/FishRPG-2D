using System;
using System.Collections.Generic;
using Mono.Cecil;
using FishNet.Serializing;
using Mono.Cecil.Cil;
using System.Reflection;
using FishNet.Connection;
using Unity.CompilationPipeline.Common.Diagnostics;
using FishNet.Serializing.Helping;

namespace FishNet.CodeGenerating.Helping
{
    internal static class ReaderHelper
    {
        #region Reflection references.
        internal static TypeReference PooledReader_TypeRef;
        internal static TypeReference NetworkConnection_TypeRef;
        internal static MethodReference PooledReader_ReadNetworkBehaviour_MethodRef;
        private static readonly Dictionary<TypeReference, MethodReference> _instancedReaderMethods = new Dictionary<TypeReference, MethodReference>(new TypeReferenceComparer());
        private static readonly Dictionary<TypeReference, MethodReference> _staticReaderMethods = new Dictionary<TypeReference, MethodReference>(new TypeReferenceComparer());
        private static HashSet<TypeReference> _autoPackedMethods = new HashSet<TypeReference>(new TypeReferenceComparer());
        private static MethodReference Reader_ReadPackedWhole_MethodRef;
        #endregion

        #region Misc.
        private static ModuleDefinition _moduleDef;
        #endregion

        #region Const.
        internal const string READ_PREFIX = "Read";
        #endregion

        /// <summary>
        /// Imports references needed by this helper.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <returns></returns>
        internal static bool ImportReferences(ModuleDefinition moduleDef, List<DiagnosticMessage> diagnostics)
        {
            _autoPackedMethods.Clear();
            ClearReaderMethods();

            _moduleDef = moduleDef;
            PooledReader_TypeRef = moduleDef.ImportReference(typeof(PooledReader));
            NetworkConnection_TypeRef = moduleDef.ImportReference(typeof(NetworkConnection));

            Type pooledReaderType = typeof(PooledReader);

            foreach (MethodInfo methodInfo in pooledReaderType.GetMethods())
            {
                /* Special methods. */
                //ReadPackedWhole.
                if (methodInfo.Name == nameof(PooledReader.ReadPackedWhole))
                {
                    Reader_ReadPackedWhole_MethodRef = moduleDef.ImportReference(methodInfo);
                    continue;
                }

                //Custom attribute to specify certain methods to ignore.
                bool ignored = false;
                foreach (CustomAttributeData item in methodInfo.CustomAttributes)
                {
                    if (item.AttributeType == typeof(CodegenIgnoreAttribute))
                    {
                        ignored = true;
                        break;
                    }
                }
                if (ignored)
                    continue;

                //Generic methods are not supported.
                if (methodInfo.IsGenericMethod)
                    continue;
                //Not long enough to be a write method.
                if (methodInfo.Name.Length < READ_PREFIX.Length)
                    continue;
                //Method name doesn't start with writePrefix.
                if (methodInfo.Name.Substring(0, READ_PREFIX.Length) != READ_PREFIX)
                    continue;
                ParameterInfo[] parameterInfos = methodInfo.GetParameters();
                //Can have at most one parameter for packing.
                if (parameterInfos.Length > 1)
                    continue;
                //If has one parameter make sure it's a packing type.
                bool autoPackMethod = false;
                if (parameterInfos.Length == 1)
                {
                    autoPackMethod = (parameterInfos[0].ParameterType == typeof(AutoPackType));
                    if (!autoPackMethod)
                        continue;
                }

                /* TypeReference for the return type
                 * of the read method. */
                TypeReference typeRef = moduleDef.ImportReference(methodInfo.ReturnType);
                MethodReference methodRef = moduleDef.ImportReference(methodInfo);

                /* If here all checks pass. */
                AddReaderMethod(typeRef, methodRef, true, true);
                if (autoPackMethod)
                    _autoPackedMethods.Add(typeRef);
            }

            Type readerExtensionsType = typeof(ReaderExtensions);

            foreach (MethodInfo methodInfo in readerExtensionsType.GetMethods())
            {
                //Custom attribute to specify certain methods to ignore.
                bool ignored = false;
                foreach (CustomAttributeData item in methodInfo.CustomAttributes)
                {
                    if (item.AttributeType == typeof(CodegenIgnoreAttribute))
                    {
                        ignored = true;
                        break;
                    }
                }
                if (ignored)
                    continue;

                //Generic methods are not supported.
                if (methodInfo.IsGenericMethod)
                    continue;
                //Not static.
                if (!methodInfo.IsStatic)
                    continue;
                //Not long enough to be a write method.
                if (methodInfo.Name.Length < READ_PREFIX.Length)
                    continue;
                //Method name doesn't start with writePrefix.
                if (methodInfo.Name.Substring(0, READ_PREFIX.Length) != READ_PREFIX)
                    continue;
                ParameterInfo[] parameterInfos = methodInfo.GetParameters();
                //Can have at most one parameter for packing.
                if (parameterInfos.Length > 2)
                    continue;
                //If has 2 parameters make sure it's a packing type.
                bool autoPackMethod = false;
                if (parameterInfos.Length == 2)
                {
                    autoPackMethod = (parameterInfos[1].ParameterType == typeof(AutoPackType));
                    if (!autoPackMethod)
                        continue;
                }

                /* TypeReference for the return type
                 * of the read method. */
                TypeReference typeRef = moduleDef.ImportReference(methodInfo.ReturnType);
                MethodReference methodRef = moduleDef.ImportReference(methodInfo);

                /* If here all checks pass. */
                AddReaderMethod(typeRef, methodRef, false, true);
            }


            return true;
        }


        /// <summary>
        /// Creates generic write delegates for all currently known write types.
        /// </summary>
        internal static void CreateGenericDelegates(List<DiagnosticMessage> diagnostics)
        {
            /* Only write statics. This will include extensions and generated. */
            foreach (KeyValuePair<TypeReference, MethodReference> item in _staticReaderMethods)
                GenericReaderHelper.CreateReadDelegate(item.Value, diagnostics);
        }


        /// <summary>
        /// Returns if typeRef has a deserializer.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <param name="createMissing"></param>
        /// <returns></returns>
        internal static bool HasDeserializer(TypeReference typeRef, List<DiagnosticMessage> diagnostics, bool createMissing)
        {
            bool result = (GetInstancedReadMethodReference(typeRef) != null) ||
                (GetStaticReadMethodReference(typeRef) != null);

            if (!result && createMissing)
            {
                if (!GeneralHelper.HasNonSerializableAttribute(typeRef.Resolve()))
                {
                    MethodReference methodRef = ReaderGenerator.CreateReader(typeRef, diagnostics);
                    result = (methodRef != null);
                }
            }

            return result;
        }


        /// <summary>
        /// Returns if typeRef supports auto packing.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <returns></returns>
        internal static bool IsAutoPackedType(TypeReference typeRef)
        {
            return _autoPackedMethods.Contains(typeRef);
        }
        /// <summary>
        /// Creates a null check on the first argument and returns a null object if result indicates to do so.
        /// </summary>
        internal static void CreateRetOnNull(ILProcessor processor, ParameterDefinition readerParameterDef, VariableDefinition resultVariableDef, bool useBool)
        {
            Instruction endIf = processor.Create(OpCodes.Nop);

            if (useBool)
                CreateReadBool(processor, readerParameterDef, resultVariableDef);
            else
                CreateReadPackedWhole(processor, readerParameterDef, resultVariableDef);

            //If (true or == -1) jmp to endIf.
            processor.Emit(OpCodes.Ldloc, resultVariableDef);
            if (useBool)
            {
                processor.Emit(OpCodes.Brfalse, endIf);
            }
            else
            {
                //-1
                processor.Emit(OpCodes.Ldc_I4_M1);
                processor.Emit(OpCodes.Bne_Un_S, endIf);
            }
            //Insert null.
            processor.Emit(OpCodes.Ldnull);
            //Exit method.
            processor.Emit(OpCodes.Ret);
            //End of if check.
            processor.Append(endIf);
        }

        /// <summary>
        /// Creates a call to WriteBoolean with value.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="writerParameterDef"></param>
        /// <param name="value"></param>
        internal static void CreateReadBool(ILProcessor processor, ParameterDefinition readerParameterDef, VariableDefinition localBoolVariableDef)
        {
            MethodReference readBoolMethodRef = GetFavoredReadMethodReference(GeneralHelper.GetTypeReference(typeof(bool)), true);
            processor.Emit(OpCodes.Ldarg, readerParameterDef);
            processor.Emit(OpCodes.Callvirt, readBoolMethodRef);
            processor.Emit(OpCodes.Stloc, localBoolVariableDef);
        }
        ///// <summary>
        ///// Creates a call to WritePackWhole with value.
        ///// </summary>
        ///// <param name="processor"></param>
        ///// <param name="value"></param>
        //internal static void CreateWritePackedWhole(ILProcessor processor, ParameterDefinition writerParameterDef, int value)
        //{
        //    //Create local int and set it to value.
        //    VariableDefinition intVariableDef = GeneralHelper.CreateLocalVariable(processor.Body.Method, typeof(int));
        //    GeneralHelper.SetVariableDefinition(processor, intVariableDef, value);
        //    //Writer.
        //    processor.Emit(OpCodes.Ldarg, writerParameterDef);
        //    //Writer.WritePackedWhole(value).
        //    processor.Emit(OpCodes.Ldloc, intVariableDef);
        //    processor.Emit(OpCodes.Conv_U8);
        //    processor.Emit(OpCodes.Callvirt, Writer_WritePackedWhole_MethodRef);
        //}
        /// <summary>
        /// Creates a call to WritePackWhole with value.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="value"></param>
        internal static void CreateReadPackedWhole(ILProcessor processor, ParameterDefinition readerParameterDef, VariableDefinition resultVariableDef)
        {
            //Reader.
            processor.Emit(OpCodes.Ldarg, readerParameterDef);
            //Reader.ReadPackedWhole().
            processor.Emit(OpCodes.Callvirt, Reader_ReadPackedWhole_MethodRef);
            processor.Emit(OpCodes.Conv_I4);
            processor.Emit(OpCodes.Stloc, resultVariableDef);
        }


        #region GetReaderrMethodReference.
        /// <summary>
        /// Returns the MethodReference for typeRef.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <returns></returns>
        internal static MethodReference GetInstancedReadMethodReference(TypeReference typeRef)
        {
            _instancedReaderMethods.TryGetValue(typeRef, out MethodReference methodRef);
            return methodRef;
        }
        /// <summary>
        /// Returns the MethodReference for typeRef.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <returns></returns>
        internal static MethodReference GetStaticReadMethodReference(TypeReference typeRef)
        {
            _staticReaderMethods.TryGetValue(typeRef, out MethodReference methodRef);
            return methodRef;
        }
        /// <summary>
        /// Returns the MethodReference for typeRef favoring instanced or static. Tries to create reader if not present.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <param name="favorInstanced"></param>
        /// <returns></returns>
        internal static MethodReference GetFavoredReadMethodReference(TypeReference typeRef, bool favorInstanced)
        {
            MethodReference result;
            if (favorInstanced)
            {
                result = GetInstancedReadMethodReference(typeRef);
                if (result == null)
                    result = GetStaticReadMethodReference(typeRef);
            }
            else
            {
                result = GetStaticReadMethodReference(typeRef);
                if (result == null)
                    result = GetInstancedReadMethodReference(typeRef);
            }

            return result;
        }
        /// <summary>
        /// Returns the MethodReference for typeRef favoring instanced or static.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <param name="favorInstanced"></param>
        /// <returns></returns>
        internal static MethodReference GetOrCreateFavoredReadMethodReference(TypeReference typeRef, bool favorInstanced, List<DiagnosticMessage> diagnostics)
        {
            //Try to get existing writer, if not present make one.
            MethodReference readMethodRef = GetFavoredReadMethodReference(typeRef, favorInstanced);
            if (readMethodRef == null)
                readMethodRef = ReaderGenerator.CreateReader(typeRef, diagnostics);
            if (readMethodRef == null)
                diagnostics.AddError($"Could not create deserializer for {typeRef.FullName}.");

            return readMethodRef;
        }
        #endregion

        /// <summary>
        /// Adds typeRef, methodDef to instanced or static readerMethods.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <param name="methodRef"></param>
        /// <param name="useAdd"></param>
        internal static void AddReaderMethod(TypeReference typeRef, MethodReference methodRef, bool instanced, bool useAdd)
        {
            Dictionary<TypeReference, MethodReference> dict = (instanced) ?
                _instancedReaderMethods : _staticReaderMethods;

            if (useAdd)
                dict.Add(typeRef, methodRef);
            else
                dict[typeRef] = methodRef;
        }

        /// <summary>
        /// Clears cached reader methods.
        /// </summary>
        private static void ClearReaderMethods()
        {
            _instancedReaderMethods.Clear();
            _staticReaderMethods.Clear();
        }

        /// <summary>
        /// Creates and returns a local variable after using a PooledReader reference to populate it.
        /// EG: bool bool1 = pooledReader.ReadBool();
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="methodDef"></param>
        /// <param name="readerParameterDef"></param>
        /// <param name="readTypeRef"></param>
        /// <returns></returns>
        internal static VariableDefinition CreateRead(ILProcessor processor, MethodDefinition methodDef, ParameterDefinition readerParameterDef, TypeReference readTypeRef, List<DiagnosticMessage> diagnostics)
        {
            VariableDefinition variableDef;
            List<Instruction> insts = CreateReadInstructions(processor, methodDef, readerParameterDef, readTypeRef, out variableDef, diagnostics);

            if (insts != null)
            {
                foreach (Instruction i in insts)
                    processor.Append(i);
            }

            return variableDef;
        }

        /// <summary>
        /// Creates read instructions returning instructions and outputing variable of read result.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="methodDef"></param>
        /// <param name="readerParameterDef"></param>
        /// <param name="readTypeRef"></param>
        /// <param name="diagnostics"></param>
        /// <returns></returns>
        internal static List<Instruction> CreateReadInstructions(ILProcessor processor, MethodDefinition methodDef, ParameterDefinition readerParameterDef, TypeReference readTypeRef, out VariableDefinition createdVariableDef, List<DiagnosticMessage> diagnostics)
        {
            List<Instruction> insts = new List<Instruction>();
            MethodReference readerMethodRef = GetFavoredReadMethodReference(readTypeRef, true);
            if (readerMethodRef != null)
            {
                //Make a local variable. 
                createdVariableDef = GeneralHelper.CreateVariable(methodDef, readTypeRef);
                //pooledReader.ReadBool();
                insts.Add(processor.Create(OpCodes.Ldarg, readerParameterDef));
                //If an auto pack method then insert default value.
                if (_autoPackedMethods.Contains(readTypeRef))
                {
                    AutoPackType packType = GeneralHelper.GetDefaultAutoPackType(readTypeRef);
                    insts.Add(processor.Create(OpCodes.Ldc_I4, (int)packType));
                }
                insts.Add(processor.Create(OpCodes.Callvirt, readerMethodRef));
                //Store into local variable.
                insts.Add(processor.Create(OpCodes.Stloc, createdVariableDef));
                return insts;
            }
            else
            {
                diagnostics.AddError("Reader not found for " + readTypeRef.ToString());
                createdVariableDef = null;
                return null;
            }
        }



        /// <summary>
        /// Creates a read call on a static method for FieldDef. Creates and returns a local variable after using a PooledReader reference to populate it.
        /// EG: bool bool1 = pooledReader.ReadBool();
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="readerParameterDef"></param>
        /// <returns></returns>
        internal static bool CreateReadNoCreateMissing(ILProcessor processor, ParameterDefinition readerParameterDef, VariableDefinition objectVariableDef, FieldDefinition fieldDef, List<DiagnosticMessage> diagnostics)
        {
            MethodReference readMethodRef = GetFavoredReadMethodReference(fieldDef.FieldType, true);
            if (readMethodRef != null)
            {
                /* How to load object instance. If it's a structure
                 * then it must be loaded by address. Otherwise if
                 * class Ldloc can be used. */
                OpCode loadOpCode = (objectVariableDef.VariableType.IsValueType) ?
                    OpCodes.Ldloca : OpCodes.Ldloc;

                processor.Emit(loadOpCode, objectVariableDef);
                //reader.
                processor.Emit(OpCodes.Ldarg, readerParameterDef);
                if (IsAutoPackedType(fieldDef.FieldType))
                {
                    AutoPackType packType = GeneralHelper.GetDefaultAutoPackType(fieldDef.FieldType);
                    processor.Emit(OpCodes.Ldc_I4, (int)packType);
                }
                //reader.ReadXXXX().
                processor.Emit(OpCodes.Call, readMethodRef);
                //obj.Field = result / reader.ReadXXXX().
                processor.Emit(OpCodes.Stfld, fieldDef);

                return true;
            }
            else
            {
                diagnostics.AddError($"Reader not found for {fieldDef.FullName}.");
                return false;
            }
        }
    }
}