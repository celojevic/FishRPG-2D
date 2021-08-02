using System;
using System.Collections.Generic;
using Mono.Cecil;
using FishNet.Serializing;
using Mono.Cecil.Cil;
using System.Reflection;
using FishNet.Object;
using Unity.CompilationPipeline.Common.Diagnostics;
using FishNet.Serializing.Helping;
using UnityEngine;

namespace FishNet.CodeGenerating.Helping
{
    internal static class WriterHelper
    {
        #region Reflection references.
        private static MethodReference WriterPool_GetWriter_MethodRef;
        private static MethodReference Writer_WritePackedWhole_MethodRef;
        internal static TypeReference PooledWriter_TypeRef;
        internal static readonly Dictionary<TypeReference, MethodReference> _instancedWriterMethods = new Dictionary<TypeReference, MethodReference>(new TypeReferenceComparer());
        private static readonly Dictionary<TypeReference, MethodReference> _staticWriterMethods = new Dictionary<TypeReference, MethodReference>(new TypeReferenceComparer());
        private static HashSet<TypeReference> _autoPackedMethods = new HashSet<TypeReference>(new TypeReferenceComparer());
        private static MethodReference PooledWriter_Dispose_MethodRef;
        internal static TypeReference NetworkBehaviour_TypeRef;
        #endregion

        #region Misc.
        private static ModuleDefinition _moduleDef;
        #endregion

        #region Const.
        internal const string WRITE_PREFIX = "Write";
        #endregion

        /// <summary>
        /// Imports references needed by this helper.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <returns></returns>
        internal static bool ImportReferences(ModuleDefinition moduleDef, List<DiagnosticMessage> diagnostics)
        {
            _autoPackedMethods.Clear();
            ClearWriterMethods();

            _moduleDef = moduleDef;
            PooledWriter_TypeRef = moduleDef.ImportReference(typeof(PooledWriter));
            NetworkBehaviour_TypeRef = moduleDef.ImportReference(typeof(NetworkBehaviour));

            //WriterPool.GetWriter
            Type writerPoolType = typeof(WriterPool);
            foreach (var methodInfo in writerPoolType.GetMethods())
            {
                if (methodInfo.Name == nameof(WriterPool.GetWriter))
                    WriterPool_GetWriter_MethodRef = moduleDef.ImportReference(methodInfo);
            }

            Type pooledWriterType = typeof(PooledWriter);
            foreach (MethodInfo methodInfo in pooledWriterType.GetMethods())
            {
                /* Special methods. */
                //Write.Dispose.
                if (methodInfo.Name == nameof(PooledWriter.Dispose))
                {
                    PooledWriter_Dispose_MethodRef = moduleDef.ImportReference(methodInfo);
                    continue;
                }
                //WritePackedWhole.
                if (methodInfo.Name == nameof(PooledWriter.WritePackedWhole))
                {
                    Writer_WritePackedWhole_MethodRef = moduleDef.ImportReference(methodInfo);
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
                if (methodInfo.Name.Length < WRITE_PREFIX.Length)
                    continue;
                //Method name doesn't start with writePrefix.
                if (methodInfo.Name.Substring(0, WRITE_PREFIX.Length) != WRITE_PREFIX)
                    continue;
                ParameterInfo[] parameterInfos = methodInfo.GetParameters();
                /* No parameters or more than 2 parameters. Most Write methods
                 * will have only 1 parameter but some will have 2 if
                 * there is a pack option. */
                if (parameterInfos.Length < 1 || parameterInfos.Length > 2)
                    continue;
                /* If two parameters make sure the second parameter
                 * is a pack parameter. */
                bool autoPackMethod = false;
                if (parameterInfos.Length == 2)
                {
                    autoPackMethod = (parameterInfos[1].ParameterType == typeof(AutoPackType));
                    if (!autoPackMethod)
                        continue;
                }
                //First parameter is generic; these are not supported.
                if (parameterInfos[0].ParameterType.IsGenericParameter)
                    continue;

                /* TypeReference for the first parameter in the write method.
                 * The first parameter will always be the type written. */
                TypeReference typeRef = moduleDef.ImportReference(parameterInfos[0].ParameterType);
                /* If here all checks pass. */
                MethodReference methodRef = moduleDef.ImportReference(methodInfo);
                AddWriterMethod(typeRef, methodRef, true, true);
                if (autoPackMethod)
                    _autoPackedMethods.Add(typeRef);
            }

            Type writerExtensionsType = typeof(WriterExtensions);
            foreach (MethodInfo methodInfo in writerExtensionsType.GetMethods())
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
                if (methodInfo.Name.Length < WRITE_PREFIX.Length)
                    continue;
                //Method name doesn't start with writePrefix.
                if (methodInfo.Name.Substring(0, WRITE_PREFIX.Length) != WRITE_PREFIX)
                    continue;
                ParameterInfo[] parameterInfos = methodInfo.GetParameters();
                /* No parameters or more than 3 parameters. Most extension Write methods
                 * will have only 2 parameter but some will have 3 if
                 * there is a pack option. */
                if (parameterInfos.Length < 2 || parameterInfos.Length > 3)
                    continue;
                /* If 3 parameters make sure the 3rd parameter
                 * is a pack parameter. */
                bool autoPackMethod = false;
                if (parameterInfos.Length == 3)
                {
                    autoPackMethod = (parameterInfos[1].ParameterType == typeof(AutoPackType));
                    if (!autoPackMethod)
                        continue;
                }
                //First parameter is generic; these are not supported.
                if (parameterInfos[0].ParameterType.IsGenericParameter)
                    continue;

                /* TypeReference for the second parameter in the write method.
                 * The first parameter will always be the type written. */
                TypeReference typeRef = moduleDef.ImportReference(parameterInfos[1].ParameterType);
                /* If here all checks pass. */
                MethodReference methodRef = moduleDef.ImportReference(methodInfo);
                AddWriterMethod(typeRef, methodRef, false, true);
            }

            return true;
        }

        /// <summary>
        /// Creates generic write delegates for all currently known write types.
        /// </summary>
        internal static void CreateGenericDelegates(List<DiagnosticMessage> diagnostics)
        {
            /* Only write statics. This will include extensions and generated. */
            foreach (KeyValuePair<TypeReference, MethodReference> item in _staticWriterMethods)
                GenericWriterHelper.CreateWriteDelegate(item.Value, diagnostics);
        }

        /// <summary>
        /// Returns if typeRef has a serializer.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <returns></returns>
        internal static bool HasSerializer(TypeReference typeRef, List<DiagnosticMessage> diagnostics, bool createMissing)
        {
            bool result = (GetInstancedWriteMethodReference(typeRef) != null) ||
                (GetStaticWriteMethodReference(typeRef) != null);

            if (!result && createMissing)
            {
                if (!GeneralHelper.HasNonSerializableAttribute(typeRef.Resolve()))
                {
                    MethodReference methodRef = WriterGenerator.CreateWriter(typeRef, diagnostics);
                    result = (methodRef != null);
                }
            }

            return result;
        }


        #region GetWriterMethodReference.
        /// <summary>
        /// Returns the MethodReference for typeRef.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <returns></returns>
        internal static MethodReference GetInstancedWriteMethodReference(TypeReference typeRef)
        {
            _instancedWriterMethods.TryGetValue(typeRef, out MethodReference methodRef);
            return methodRef;
        }
        /// <summary>
        /// Returns the MethodReference for typeRef.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <returns></returns>
        internal static MethodReference GetStaticWriteMethodReference(TypeReference typeRef)
        {
            _staticWriterMethods.TryGetValue(typeRef, out MethodReference methodRef);
            return methodRef;
        }
        /// <summary>
        /// Returns the MethodReference for typeRef favoring instanced or static.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <param name="favorInstanced"></param>
        /// <returns></returns>
        internal static MethodReference GetFavoredWriteMethodReference(TypeReference typeRef, bool favorInstanced)
        {
            MethodReference result;
            if (favorInstanced)
            {
                result = GetInstancedWriteMethodReference(typeRef);
                if (result == null)
                    result = GetStaticWriteMethodReference(typeRef);
            }
            else
            {
                result = GetStaticWriteMethodReference(typeRef);
                if (result == null)
                    result = GetInstancedWriteMethodReference(typeRef);
            }

            return result;
        }
        /// <summary>
        /// Gets the write MethodRef for typeRef, or tries to create it if not present.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <returns></returns>
        internal static MethodReference GetOrCreateFavoredWriteMethodReference(TypeReference typeRef, bool favorInstanced, List<DiagnosticMessage> diagnostics)
        {
            //Try to get existing writer, if not present make one.
            MethodReference writeMethodRef = GetFavoredWriteMethodReference(typeRef, favorInstanced);
            if (writeMethodRef == null)
                writeMethodRef = WriterGenerator.CreateWriter(typeRef, diagnostics);
            if (writeMethodRef == null)
                diagnostics.AddError($"Could not create serializer for {typeRef.FullName}.");

            return writeMethodRef;
        }
        #endregion

        /// <summary>
        /// Adds typeRef, methodDef to InstancedWriterMethods.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <param name="methodRef"></param>
        /// <param name="useAdd"></param>
        internal static void AddWriterMethod(TypeReference typeRef, MethodReference methodRef, bool instanced, bool useAdd)
        {
            Dictionary<TypeReference, MethodReference> dict = (instanced) ?
                _instancedWriterMethods : _staticWriterMethods;

            if (useAdd)
                dict.Add(typeRef, methodRef);
            else
                dict[typeRef] = methodRef;
        }

        /// <summary>
        /// Clears cached writer methods.
        /// </summary>
        private static void ClearWriterMethods()
        {
            _instancedWriterMethods.Clear();
            _staticWriterMethods.Clear();
        }

        /// <summary>
        /// Creates a PooledWriter within the body/ and returns its variable index.
        /// EG: PooledWriter writer = WriterPool.GetWriter();
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="methodDef"></param>
        /// <returns></returns>
        internal static VariableDefinition CreatePooledWriter(ILProcessor processor, MethodDefinition methodDef)
        {
            VariableDefinition pooledWriterVariableDef = GeneralHelper.CreateVariable(methodDef, PooledWriter_TypeRef);
            //Get a pooled writer from WriterPool and assign it to added PooledWriter.
            processor.Emit(OpCodes.Call, WriterPool_GetWriter_MethodRef);
            processor.Emit(OpCodes.Stloc, pooledWriterVariableDef);
            return pooledWriterVariableDef;
        }

        /// <summary>
        /// Calls Dispose on a PooledWriter.
        /// EG: writer.Dispose();
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="writerDefinition"></param>
        internal static void DisposePooledWriter(ILProcessor processor, VariableDefinition writerDefinition)
        {
            processor.Emit(OpCodes.Ldloc, writerDefinition);
            processor.Emit(OpCodes.Callvirt, PooledWriter_Dispose_MethodRef);
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
        /// Creates a null check on the second argument using a boolean.
        /// </summary>
        internal static void CreateRetOnNull(ILProcessor processor, ParameterDefinition writerParameterDef, ParameterDefinition checkedParameterDef, bool useBool, List<DiagnosticMessage> diagnostics)
        {
            Instruction endIf = processor.Create(OpCodes.Nop);
            //If (value) jmp to endIf.
            processor.Emit(OpCodes.Ldarg, checkedParameterDef);
            processor.Emit(OpCodes.Brtrue, endIf);
            //writer.WriteBool / writer.WritePackedWhole
            if (useBool)
                CreateWriteBool(processor, writerParameterDef, true);
            else
                CreateWritePackedWhole(processor, writerParameterDef, -1);
            //Exit method.
            processor.Emit(OpCodes.Ret);
            //End of if check.
            processor.Append(endIf);
        }

        #region CreateWritePackWhole
        /// <summary>
        /// Creates a call to WritePackWhole with value.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="value"></param>
        internal static void CreateWritePackedWhole(ILProcessor processor, ParameterDefinition writerParameterDef, int value)
        {
            //Create local int and set it to value.
            VariableDefinition intVariableDef = GeneralHelper.CreateVariable(processor.Body.Method, typeof(int));
            GeneralHelper.SetVariableDefinitionFromInt(processor, intVariableDef, value);
            //Writer.
            processor.Emit(OpCodes.Ldarg, writerParameterDef);
            //Writer.WritePackedWhole(value).
            processor.Emit(OpCodes.Ldloc, intVariableDef);
            processor.Emit(OpCodes.Conv_U8);
            processor.Emit(OpCodes.Callvirt, Writer_WritePackedWhole_MethodRef);
        }
        /// <summary>
        /// Creates a call to WritePackWhole with value.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="value"></param>
        internal static void CreateWritePackedWhole(ILProcessor processor, ParameterDefinition writerParameterDef, VariableDefinition value)
        {
            //Writer.
            processor.Emit(OpCodes.Ldarg, writerParameterDef);
            //Writer.WritePackedWhole(value).
            processor.Emit(OpCodes.Ldloc, value);
            processor.Emit(OpCodes.Conv_U8);
            processor.Emit(OpCodes.Callvirt, Writer_WritePackedWhole_MethodRef);
        }
        #endregion

        /// <summary>
        /// Creates a call to WriteBoolean with value.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="writerParameterDef"></param>
        /// <param name="value"></param>
        internal static void CreateWriteBool(ILProcessor processor, ParameterDefinition writerParameterDef, bool value)
        {
            MethodReference writeBoolMethodRef = GetFavoredWriteMethodReference(GeneralHelper.GetTypeReference(typeof(bool)), true);
            processor.Emit(OpCodes.Ldarg, writerParameterDef);
            int intValue = (value) ? 1 : 0;
            processor.Emit(OpCodes.Ldc_I4, intValue);
            processor.Emit(OpCodes.Callvirt, writeBoolMethodRef);
        }

        /// <summary>
        /// Creates a Write call on a PooledWriter variable for parameterDef.
        /// EG: writer.WriteBool(xxxxx);
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="writerDef"></param>
        /// <param name="valueParameterDef"></param>
        internal static void CreateWrite(ILProcessor processor, object writerDef, ParameterDefinition valueParameterDef, MethodReference writeMethodRef, List<DiagnosticMessage> diagnostics)
        {
            if (writeMethodRef != null)
            {
                if (writerDef is VariableDefinition)
                {
                    processor.Emit(OpCodes.Ldloc, (VariableDefinition)writerDef);
                }
                else if (writerDef is ParameterDefinition)
                {
                    processor.Emit(OpCodes.Ldarg, (ParameterDefinition)writerDef);
                }
                else
                {
                    diagnostics.AddError($"{writerDef.GetType().FullName} is not a valid writerDef. Type must be VariableDefinition or ParameterDefinition.");
                    return;
                }
                processor.Emit(OpCodes.Ldarg, valueParameterDef);
                //If an auto pack method then insert default value.
                if (_autoPackedMethods.Contains(valueParameterDef.ParameterType))
                {
                    AutoPackType packType = GeneralHelper.GetDefaultAutoPackType(valueParameterDef.ParameterType);
                    processor.Emit(OpCodes.Ldc_I4, (int)packType);
                }
                processor.Emit(OpCodes.Call, writeMethodRef);
            }
            else
            {
                diagnostics.AddError($"Writer not found for {valueParameterDef.ParameterType.FullName}.");
            }
        }
        /// <summary>
        /// Creates a Write call to a static writer.
        /// EG: StaticClass.WriteBool(xxxxx);
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="fieldDef"></param>
        internal static void CreateWrite(ILProcessor processor, ParameterDefinition writerVariableDef, FieldDefinition fieldDef, MethodReference writeMethodRef, List<DiagnosticMessage> diagnostics)
        {
            if (writeMethodRef != null)
            {
                FieldReference fieldRef = GeneralHelper.GetFieldReference(fieldDef);
                processor.Emit(OpCodes.Ldarg_0); //this.
                processor.Emit(OpCodes.Ldarg, writerVariableDef);
                processor.Emit(OpCodes.Ldfld, fieldRef);
                //If an auto pack method then insert default value.
                if (_autoPackedMethods.Contains(fieldDef.FieldType))
                {
                    AutoPackType packType = GeneralHelper.GetDefaultAutoPackType(fieldDef.FieldType);
                    processor.Emit(OpCodes.Ldc_I4, (int)packType);
                }
                processor.Emit(OpCodes.Call, writeMethodRef);
            }
            else
            {
                diagnostics.AddError($"Writer not found for {fieldDef.FieldType.FullName}.");
            }
        }

    }

}