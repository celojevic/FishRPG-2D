using System;
using System.Collections.Generic;
using Mono.Cecil;
using FishNet.Serializing;
using Mono.Cecil.Cil;
using FishNet.CodeGenerating.Helping.Extension;
using Unity.CompilationPipeline.Common.Diagnostics;
using UnityEngine;

namespace FishNet.CodeGenerating.Helping
{
    internal static class WriterGenerator
    {


        #region Reflection references.
        private static Dictionary<TypeReference, ListMethodReferences> _cachedListMethodRefs = new Dictionary<TypeReference, ListMethodReferences>();
        #endregion

        #region Misc.
        private static ModuleDefinition _moduleDef;
        #endregion

        #region Const.
        public const string GENERATED_CLASS_NAME = "GeneratedReadersAndWriters";
        public const TypeAttributes GENERATED_TYPE_ATTRIBUTES = (TypeAttributes.BeforeFieldInit | TypeAttributes.Class | TypeAttributes.AnsiClass |
            TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.Abstract | TypeAttributes.Sealed);
        private const string WRITE_PREFIX = "Write___";
        #endregion

        /// <summary>
        /// Imports references needed by this helper.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <returns></returns>
        internal static bool ImportReferences(ModuleDefinition moduleDef)
        {
            _cachedListMethodRefs.Clear();

            _moduleDef = moduleDef;
            return true;
        }

        /// <summary>
        /// Generates a writer for objectTypeReference if one does not already exist.
        /// </summary>
        /// <param name="objectTypeRef"></param>
        /// <returns></returns>
        internal static MethodReference CreateWriter(TypeReference objectTypeRef, List<DiagnosticMessage> diagnostics)
        {
            MethodReference methodRefResult = null;
            TypeDefinition objectTypeDef;
            SerializerType serializerType = GeneratorHelper.GetSerializerType(objectTypeRef, true, out objectTypeDef, diagnostics);
            if (serializerType != SerializerType.Invalid)
            {
                //Array.
                if (serializerType == SerializerType.Array)
                {
                    TypeReference elementType = objectTypeRef.GetElementType();
                    methodRefResult = CreateCollectionWriterMethodDefinition(objectTypeRef, elementType, diagnostics);
                }
                //Enum.
                else if (serializerType == SerializerType.Enum)
                {
                    methodRefResult = CreateEnumWriterMethodDefinition(objectTypeRef, diagnostics);
                }
                //List.
                else if (serializerType == SerializerType.List)
                {
                    GenericInstanceType genericInstanceType = (GenericInstanceType)objectTypeRef;
                    TypeReference elementType = genericInstanceType.GenericArguments[0];
                    methodRefResult = CreateCollectionWriterMethodDefinition(objectTypeRef, elementType, diagnostics);
                }
                //NetworkBehaviour.
                else if (serializerType == SerializerType.NetworkBehaviour)
                {
                    methodRefResult = CreateNetworkBehaviourWriterMethodReference(objectTypeDef, diagnostics);
                }
                //Class or struct.
                else if (serializerType == SerializerType.ClassOrStruct)
                {
                    methodRefResult = CreateClassOrStructWriterMethodDefinition(objectTypeRef, diagnostics);
                }
            }

            //If was created.
            if (methodRefResult != null)
                WriterHelper.AddWriterMethod(objectTypeRef, methodRefResult, false, true);

            return methodRefResult;
        }



        /// <summary>
        /// Adds a write for a NetworkBehaviour class type to WriterMethods.
        /// </summary>
        /// <param name="classTypeRef"></param>
        private static MethodDefinition CreateNetworkBehaviourWriterMethodReference(TypeReference objectTypeRef, List<DiagnosticMessage> diagnostics)
        {
            //All NetworkBehaviour types will simply WriteNetworkBehaviour/ReadNetworkBehaviour.
            //Create static generated reader/writer class. This class holds all generated reader/writers.
            GeneralHelper.GetOrCreateClass(_moduleDef, out _, GENERATED_TYPE_ATTRIBUTES, GENERATED_CLASS_NAME, null);

            MethodDefinition createdWriterMethodDef = CreateStaticWriterStubMethodDefinition(objectTypeRef);
            ILProcessor processor = createdWriterMethodDef.Body.GetILProcessor();
            MethodReference writeMethodRef = WriterHelper.GetOrCreateFavoredWriteMethodReference(WriterHelper.NetworkBehaviour_TypeRef, true, diagnostics);
            //Get parameters for method.
            ParameterDefinition writerParameterDef = createdWriterMethodDef.Parameters[0];
            ParameterDefinition classParameterDef = createdWriterMethodDef.Parameters[1];

            //Load parameters as arguments.
            processor.Emit(OpCodes.Ldarg, writerParameterDef);
            processor.Emit(OpCodes.Ldarg, classParameterDef);
            //writer.WriteNetworkBehaviour(arg1);
            processor.Emit(OpCodes.Call, writeMethodRef);

            processor.Emit(OpCodes.Ret);

            return createdWriterMethodDef;
        }

        /// <summary> 
        /// Gets the length of a collection and writes the value to a variable.
        /// </summary>
        private static void CreateCollectionLength(ILProcessor processor, ParameterDefinition collectionParameterDef, VariableDefinition storeVariableDef)
        {
            processor.Emit(OpCodes.Ldarg, collectionParameterDef);
            processor.Emit(OpCodes.Ldlen);
            processor.Emit(OpCodes.Conv_I4);
            processor.Emit(OpCodes.Stloc, storeVariableDef);
        }

        /// <summary>
        /// Returns common list references for list type of elementTypeRef.
        /// </summary>
        /// <param name="elementTypeRef"></param>
        /// <returns></returns>
        private static ListMethodReferences GetListMethodReferences(TypeReference elementTypeRef, List<DiagnosticMessage> diagnostics)
        {
            ListMethodReferences result;
            //If found return result.
            if (_cachedListMethodRefs.TryGetValue(elementTypeRef, out result))
            {
                return result;
            }
            //Otherwise make a new entry.
            else
            {
                Type elementMonoType = elementTypeRef.GetMonoType();
                if (elementMonoType == null)
                {
                    diagnostics.AddError($"Mono Type could not be found for {elementMonoType.FullName}.");
                    return null;
                }
                Type constructedListType = typeof(List<>).MakeGenericType(elementMonoType);

                MethodReference add = null;
                MethodReference item = null;
                foreach (System.Reflection.MethodInfo methodInfo in constructedListType.GetMethods())
                {
                    if (methodInfo.Name == "get_Item")
                        item = _moduleDef.ImportReference(methodInfo);
                }


                if (item == null)
                {
                    diagnostics.AddError($"Count or Item property could not be found for {elementMonoType.FullName}.");
                    return null;
                }

                ListMethodReferences lmr = new ListMethodReferences(constructedListType, item, add);
                _cachedListMethodRefs.Add(elementTypeRef, lmr);
                return lmr;
            }
        }



        /// <summary>
        /// Creates a writer for a class or struct of objectTypeRef.
        /// </summary>
        /// <param name="objectTypeRef"></param>
        /// <returns></returns>
        private static MethodDefinition CreateClassOrStructWriterMethodDefinition(TypeReference objectTypeRef, List<DiagnosticMessage> diagnostics)
        {
            /*Stubs generate Method(Writer writer, T value). */
            MethodDefinition createdWriterMethodDef = CreateStaticWriterStubMethodDefinition(objectTypeRef);
            ILProcessor processor = createdWriterMethodDef.Body.GetILProcessor();

            //If not a value type then add a null check.
            if (!objectTypeRef.Resolve().IsValueType)
            {
                ParameterDefinition readerParameterDef = createdWriterMethodDef.Parameters[0];
                WriterHelper.CreateRetOnNull(processor, readerParameterDef, createdWriterMethodDef.Parameters[1], true, diagnostics);
                //Code will only execute here and below if not null.
                WriterHelper.CreateWriteBool(processor, readerParameterDef, false);
            }

            //Write all fields for the class or struct.
            ParameterDefinition valueParameterDef = createdWriterMethodDef.Parameters[1];
            if (!WriteFields(processor, valueParameterDef, objectTypeRef, diagnostics))
                return null;

            processor.Emit(OpCodes.Ret);
            return createdWriterMethodDef;
        }

        /// <summary>
        /// Find all fields in type and write them
        /// </summary>
        /// <param name="objectTypeRef"></param>
        /// <param name="processor"></param>
        /// <returns>false if fail</returns>
        private static bool WriteFields(ILProcessor processor, ParameterDefinition valueParameterDef, TypeReference objectTypeRef, List<DiagnosticMessage> diagnostics)
        {
            foreach (FieldDefinition fieldDef in objectTypeRef.FindAllPublicFields())
            {
                MethodReference writeMethodRef = WriterHelper.GetOrCreateFavoredWriteMethodReference(fieldDef.FieldType, true, diagnostics);
                //Not all fields will support writing, such as NonSerialized ones.
                if (writeMethodRef == null)
                    continue;

                WriterHelper.CreateWrite(processor, valueParameterDef, fieldDef, writeMethodRef, diagnostics);
            }

            return true;
        }


        /// <summary>
        /// Creates a writer for an enum.
        /// </summary>
        /// <param name="enumTypeRef"></param>
        /// <returns></returns>
        private static MethodDefinition CreateEnumWriterMethodDefinition(TypeReference enumTypeRef, List<DiagnosticMessage> diagnostics)
        {
            MethodDefinition createdWriterMethodDef = CreateStaticWriterStubMethodDefinition(enumTypeRef);
            ILProcessor processor = createdWriterMethodDef.Body.GetILProcessor();

            //Element type for enum. EG: byte int ect
            TypeReference elementTypeRef = enumTypeRef.Resolve().GetEnumUnderlyingTypeReference();
            //Method to write that type.
            MethodReference underlyingWriterMethodRef = WriterHelper.GetOrCreateFavoredWriteMethodReference(elementTypeRef, true, diagnostics);
            if (underlyingWriterMethodRef == null)
                return null;

            ParameterDefinition writerParameterDef = createdWriterMethodDef.Parameters[0];
            ParameterDefinition valueParameterDef = createdWriterMethodDef.Parameters[1];
            //Push writer and value into call.
            processor.Emit(OpCodes.Ldarg, writerParameterDef);
            processor.Emit(OpCodes.Ldarg, valueParameterDef);
            //writer.WriteXXX(value)
            processor.Emit(OpCodes.Call, underlyingWriterMethodRef);

            processor.Emit(OpCodes.Ret);
            return createdWriterMethodDef;
        }

        /// <summary>
        /// Creates a writer for a collection for elementTypeRef.
        /// </summary>
        private static MethodDefinition CreateCollectionWriterMethodDefinition(TypeReference objectTypeRef, TypeReference elementTypeRef, List<DiagnosticMessage> diagnostics)
        {
            MethodDefinition createdWriterMethodDef = CreateStaticWriterStubMethodDefinition(objectTypeRef);
            /* Try to get instanced first for collection element type, if it doesn't exist then try to
             * get/or make a static one. */
            MethodReference writeMethodRef = WriterHelper.GetOrCreateFavoredWriteMethodReference(elementTypeRef, true, diagnostics);
            if (writeMethodRef == null)
                return null;

            ILProcessor processor = createdWriterMethodDef.Body.GetILProcessor();

            ListMethodReferences lstMethodRefs = null;
            //True if array, false if list.
            bool isArray = createdWriterMethodDef.Parameters[1].ParameterType.IsArray;
            //If not array get methodRefs needed to create a list writer.
            if (!isArray)
            {
                lstMethodRefs = GetListMethodReferences(elementTypeRef, diagnostics);
                if (lstMethodRefs == null)
                    return null;
            }

            //Null instructions.
            WriterHelper.CreateRetOnNull(processor, createdWriterMethodDef.Parameters[0], createdWriterMethodDef.Parameters[1], false, diagnostics);

            //Write length. It only makes it this far if not null.
            //int length = arr[].Length.
            VariableDefinition sizeVariableDef = GeneralHelper.CreateVariable(createdWriterMethodDef, typeof(int));
            CreateCollectionLength(processor, createdWriterMethodDef.Parameters[1], sizeVariableDef);
            //writer.WritePackedWhole(length).
            WriterHelper.CreateWritePackedWhole(processor, createdWriterMethodDef.Parameters[0], sizeVariableDef);

            VariableDefinition loopIndex = GeneralHelper.CreateVariable(createdWriterMethodDef, typeof(int));
            Instruction loopComparer = processor.Create(OpCodes.Ldloc, loopIndex);

            //int i = 0
            processor.Emit(OpCodes.Ldc_I4_0);
            processor.Emit(OpCodes.Stloc, loopIndex);
            processor.Emit(OpCodes.Br_S, loopComparer);

            //Loop content.
            Instruction contentStart = processor.Create(OpCodes.Ldarg_0);
            processor.Append(contentStart);
            processor.Emit(OpCodes.Ldarg_1);
            processor.Emit(OpCodes.Ldloc, loopIndex);

            //Load array element.
            if (isArray)
            {
                if (elementTypeRef.IsValueType)
                    processor.Emit(OpCodes.Ldelem_Any, elementTypeRef);
                else
                    processor.Emit(OpCodes.Ldelem_Ref);
            }
            else
            {
                processor.Emit(OpCodes.Callvirt, lstMethodRefs.Item_MethodRef);
            }
            //If auto pack type then write default auto pack.
            if (WriterHelper.IsAutoPackedType(elementTypeRef))
            {
                AutoPackType packType = GeneralHelper.GetDefaultAutoPackType(elementTypeRef);
                processor.Emit(OpCodes.Ldc_I4, (int)packType);
            }
            //writer.Write
            processor.Emit(OpCodes.Callvirt, writeMethodRef);

            //i++
            processor.Emit(OpCodes.Ldloc, loopIndex);
            processor.Emit(OpCodes.Ldc_I4_1);
            processor.Emit(OpCodes.Add);
            processor.Emit(OpCodes.Stloc, loopIndex);
            //if i < length jmp to content start.
            processor.Append(loopComparer);  //if i < obj(size).
            processor.Emit(OpCodes.Ldloc, sizeVariableDef);
            processor.Emit(OpCodes.Blt_S, contentStart);

            processor.Emit(OpCodes.Ret);
            return createdWriterMethodDef;
        }


        /// <summary>
        /// Creates a method definition stub for objectTypeRef.
        /// </summary>
        /// <param name="objectTypeRef"></param>
        /// <returns></returns>
        private static MethodDefinition CreateStaticWriterStubMethodDefinition(TypeReference objectTypeRef)
        {
            string methodName = $"{WRITE_PREFIX}{objectTypeRef.FullName}";
            // create new writer for this type
            TypeDefinition writerTypeDef = GeneralHelper.GetOrCreateClass(_moduleDef, out _, GENERATED_TYPE_ATTRIBUTES, GENERATED_CLASS_NAME, null);

            MethodDefinition writerMethodDef = writerTypeDef.AddMethod(methodName,
                    MethodAttributes.Public |
                    MethodAttributes.Static |
                    MethodAttributes.HideBySig);

            GeneralHelper.CreateParameter(writerMethodDef, WriterHelper.PooledWriter_TypeRef, "pooledWriter");
            GeneralHelper.CreateParameter(writerMethodDef, objectTypeRef, "value");
            writerMethodDef.Body.InitLocals = true;

            return writerMethodDef;
        }



    }
}