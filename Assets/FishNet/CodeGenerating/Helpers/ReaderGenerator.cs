using System;
using System.Collections.Generic;
using Mono.Cecil;
using FishNet.Serializing;
using Mono.Cecil.Cil;
using FishNet.CodeGenerating.Helping.Extension;
using Unity.CompilationPipeline.Common.Diagnostics;
using FishNet.Object;

namespace FishNet.CodeGenerating.Helping
{
    internal static class ReaderGenerator
    {

        #region Relfection references.
        private static Dictionary<TypeReference, ListMethodReferences> _cachedListMethodRefs = new Dictionary<TypeReference, ListMethodReferences>();
        #endregion

        #region Misc.
        private static ModuleDefinition _moduleDef;
        #endregion

        #region Const.
        public const string GENERATED_CLASS_NAME = WriterGenerator.GENERATED_CLASS_NAME;
        public const TypeAttributes GENERATED_TYPE_ATTRIBUTES = WriterGenerator.GENERATED_TYPE_ATTRIBUTES;
        private const string READ_PREFIX = "Read___";
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
        /// Generates a reader for objectTypeReference if one does not already exist. 
        /// </summary>
        /// <param name="objectTypeRef"></param>
        /// <returns></returns>
        internal static MethodReference CreateReader(TypeReference objectTypeRef, List<DiagnosticMessage> diagnostics)
        {
            MethodReference methodRefResult = null;
            TypeDefinition objectTypeDef;

            SerializerType serializerType = GeneratorHelper.GetSerializerType(objectTypeRef, false, out objectTypeDef, diagnostics);
            if (serializerType != SerializerType.Invalid)
            {
                //Array.
                if (serializerType == SerializerType.Array)
                {
                    TypeReference elementType = objectTypeRef.GetElementType();
                    methodRefResult = CreateCollectionReaderMethodDefinition(objectTypeRef, elementType, diagnostics);
                }
                //Enum.
                else if (serializerType == SerializerType.Enum)
                {
                    methodRefResult = CreateEnumWriterMethodDefinition(objectTypeRef, diagnostics);
                }
                //List.
                else if (serializerType == SerializerType.List)
                {
                    GenericInstanceType genericInstance = (GenericInstanceType)objectTypeRef;
                    TypeReference elementType = genericInstance.GenericArguments[0];
                    methodRefResult = CreateCollectionReaderMethodDefinition(objectTypeRef, elementType, diagnostics);
                }
                //NetworkBehaviour.
                else if (serializerType == SerializerType.NetworkBehaviour)
                {
                    methodRefResult = GetNetworkBehaviourReaderMethodReference(objectTypeRef);
                }
                //Class or struct.
                else if (serializerType == SerializerType.ClassOrStruct)
                {
                    methodRefResult = CreateClassOrStructReaderMethodDefinition(objectTypeRef, diagnostics);
                }
            }

            //If was created.
            if (methodRefResult != null)
            {
                ReaderHelper.AddReaderMethod(objectTypeRef, methodRefResult, false, true);
            }

            return methodRefResult;
        }

        /// <summary>
        /// Generates a reader for objectTypeReference if one does not already exist.
        /// </summary>
        /// <param name="objectTypeRef"></param>
        /// <returns></returns>
        private static MethodDefinition CreateEnumWriterMethodDefinition(TypeReference objectTypeRef, List<DiagnosticMessage> diagnostics)
        {
            MethodDefinition createdReaderMethodDef = CreateStaticReaderStubMethodDefinition(objectTypeRef);
            ILProcessor processor = createdReaderMethodDef.Body.GetILProcessor();
            //Get type reference for enum type. eg byte int
            TypeDefinition objectTypeDef = objectTypeRef.Resolve();
            TypeReference underlyingTypeRef = objectTypeDef.GetEnumUnderlyingTypeReference();
            //Get read method for underlying type.
            MethodReference readMethodRef = ReaderHelper.GetOrCreateFavoredReadMethodReference(underlyingTypeRef, true, diagnostics);
            if (readMethodRef == null)
                return null;

            ParameterDefinition readerParameterDef = createdReaderMethodDef.Parameters[0];
            //reader.ReadXXX().
            processor.Emit(OpCodes.Ldarg, readerParameterDef);
            processor.Emit(OpCodes.Call, readMethodRef);

            processor.Emit(OpCodes.Ret);
            return createdReaderMethodDef;
        }


        /// <summary>
        /// Creates a read for a class type which inherits NetworkBehaviour.
        /// </summary>
        /// <param name="classTypeRef"></param>
        /// <returns></returns>
        private static MethodReference GetNetworkBehaviourReaderMethodReference(TypeReference classTypeRef)
        {
            MethodDefinition createdReaderMethodDef = CreateStaticReaderStubMethodDefinition(classTypeRef);
            ILProcessor processor = createdReaderMethodDef.Body.GetILProcessor();
            TypeReference networkBehaviourTypeRef = GeneralHelper.GetTypeReference(typeof(NetworkBehaviour));

            processor.Emit(OpCodes.Ldarg_0);
            //  processor.Emit<Reader>(OpCodes.Call, (reader) => reader.ReadNetworkBehaviour());
            processor.Emit(OpCodes.Call, ReaderHelper.GetFavoredReadMethodReference(networkBehaviourTypeRef, true));
            processor.Emit(OpCodes.Castclass, classTypeRef);
            processor.Emit(OpCodes.Ret);
            return createdReaderMethodDef;
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
                    if (methodInfo.Name == "Add")
                        add = _moduleDef.ImportReference(methodInfo);
                    else if (methodInfo.Name == "get_Item")
                        item = _moduleDef.ImportReference(methodInfo);
                }


                if (add == null || item == null)
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
        /// Create a reader for an array or list.
        /// </summary>
        private static MethodDefinition CreateCollectionReaderMethodDefinition(TypeReference objectTypeRef, TypeReference elementTypeRef, List<DiagnosticMessage> diagnostics)
        {
            MethodDefinition createdReaderMethodDef = CreateStaticReaderStubMethodDefinition(objectTypeRef);
            /* Try to get instanced first for collection element type, if it doesn't exist then try to
             * get/or make a static one. */
            MethodReference readMethodRef = ReaderHelper.GetOrCreateFavoredReadMethodReference(elementTypeRef, true, diagnostics);
            if (readMethodRef == null)
                return null;

            ILProcessor processor = createdReaderMethodDef.Body.GetILProcessor();

            //True if array, false if list.
            bool isArray = objectTypeRef.IsArray;

            ListMethodReferences lstMethodRefs = null;
            //If not array get methodRefs needed to create a list writer.
            if (!isArray)
            {
                lstMethodRefs = GetListMethodReferences(elementTypeRef, diagnostics);
                if (lstMethodRefs == null)
                    return null;

            }

            ParameterDefinition readerParameterDef = createdReaderMethodDef.Parameters[0];

            VariableDefinition sizeVariableDef = GeneralHelper.CreateVariable(createdReaderMethodDef, typeof(int));
            //Load packed whole value into sizeVariableDef, exit if null indicator.
            ReaderHelper.CreateRetOnNull(processor, readerParameterDef, sizeVariableDef, false);

            //Make local variable of array type.
            VariableDefinition collectionVariableDef = GeneralHelper.CreateVariable(createdReaderMethodDef, objectTypeRef);
            //Create new array/list of size.
            processor.Emit(OpCodes.Ldloc, sizeVariableDef);
            if (isArray)
            {
                processor.Emit(OpCodes.Newarr, elementTypeRef);
            }
            else
            {
                System.Reflection.ConstructorInfo lstConstructor = lstMethodRefs.ListType.GetConstructors()[1];
                MethodReference constructorMethodRef = _moduleDef.ImportReference(lstConstructor);
                processor.Emit(OpCodes.Newobj, constructorMethodRef);
            }
            //Store new object of arr/list into collection variable.
            processor.Emit(OpCodes.Stloc, collectionVariableDef);

            VariableDefinition loopIndex = GeneralHelper.CreateVariable(createdReaderMethodDef, typeof(int));
            Instruction loopComparer = processor.Create(OpCodes.Ldloc, loopIndex);

            //int i = 0
            processor.Emit(OpCodes.Ldc_I4_0);
            processor.Emit(OpCodes.Stloc, loopIndex);
            processor.Emit(OpCodes.Br_S, loopComparer);

            //Loop content.
            //Collection[index]
            Instruction contentStart = processor.Create(OpCodes.Ldloc, collectionVariableDef);
            processor.Append(contentStart);
            /* Only arrays load the index since we are setting to that index.
             * List call lst.Add */
            if (isArray)
                processor.Emit(OpCodes.Ldloc, loopIndex);
            //Collection[index] = reader.
            processor.Emit(OpCodes.Ldarg, readerParameterDef);
            //Pass in AutoPackType default.
            if (ReaderHelper.IsAutoPackedType(elementTypeRef))
            {
                AutoPackType packType = GeneralHelper.GetDefaultAutoPackType(elementTypeRef);
                processor.Emit(OpCodes.Ldc_I4, (int)packType);
            }
            //Collection[index] = reader.ReadType().
            processor.Emit(OpCodes.Call, readMethodRef);
            //Set value to collection.
            if (isArray)
                processor.Emit(OpCodes.Stelem_Any, elementTypeRef);
            else
                processor.Emit(OpCodes.Callvirt, lstMethodRefs.Add_MethodRef);

            //i++
            processor.Emit(OpCodes.Ldloc, loopIndex);
            processor.Emit(OpCodes.Ldc_I4_1);
            processor.Emit(OpCodes.Add);
            processor.Emit(OpCodes.Stloc, loopIndex);
            //if i < length jmp to content start.
            processor.Append(loopComparer); //if i < size
            processor.Emit(OpCodes.Ldloc, sizeVariableDef);
            processor.Emit(OpCodes.Blt_S, contentStart);

            processor.Emit(OpCodes.Ldloc, collectionVariableDef);
            processor.Emit(OpCodes.Ret);

            return createdReaderMethodDef;
        }

        /// <summary>
        /// Creates a reader method for a struct or class objectTypeRef.
        /// </summary>
        /// <param name="objectTypeRef"></param>
        /// <returns></returns>
        private static MethodDefinition CreateClassOrStructReaderMethodDefinition(TypeReference objectTypeRef, List<DiagnosticMessage> diagnostics)
        {
            MethodDefinition createdReaderMethodDef = CreateStaticReaderStubMethodDefinition(objectTypeRef);
            TypeDefinition objectTypeDef = objectTypeRef.Resolve();
            ILProcessor processor = createdReaderMethodDef.Body.GetILProcessor();

            ParameterDefinition readerParameterDef = createdReaderMethodDef.Parameters[0];
            // create local for return value
            VariableDefinition objectVariableDef = GeneralHelper.CreateVariable(createdReaderMethodDef, objectTypeRef);

            //If not a value type create a return null check.
            if (!objectTypeDef.IsValueType)
            {
                VariableDefinition nullVariableDef = GeneralHelper.CreateVariable(createdReaderMethodDef, typeof(bool));
                //Load packed whole value into sizeVariableDef, exit if null indicator.
                ReaderHelper.CreateRetOnNull(processor, readerParameterDef, nullVariableDef, true);
            }

            /* If here then not null. */
            //Make a new instance of object type and set to objectVariableDef.
            GeneralHelper.SetVariableDefinitionFromObject(processor, objectVariableDef, objectTypeDef, diagnostics);
            if (!ReadFields(processor, readerParameterDef, objectVariableDef, objectTypeRef, diagnostics))
                return null;

            //Load result and return it.
            processor.Emit(OpCodes.Ldloc, objectVariableDef);
            processor.Emit(OpCodes.Ret);

            return createdReaderMethodDef;
        }

        /// <summary>
        /// Reads all fields of objectTypeRef.
        /// </summary>  
        private static bool ReadFields(ILProcessor processor, ParameterDefinition readerParameterDef, VariableDefinition objectVariableDef, TypeReference objectTypeRef, List<DiagnosticMessage> diagnostics)
        {

            foreach (FieldDefinition fieldDef in objectTypeRef.FindAllPublicFields())
            {
                MethodReference readMethodRef = ReaderHelper.GetOrCreateFavoredReadMethodReference(fieldDef.FieldType, true, diagnostics);
                //Not all fields will support reading, such as NonSerialized ones.
                if (readMethodRef == null)
                    continue;

                ReaderHelper.CreateReadNoCreateMissing(processor, readerParameterDef, objectVariableDef, fieldDef, diagnostics);
            }

            return true;
        }


        /// <summary>
        /// Creates the stub for a new reader method.
        /// </summary>
        /// <param name="objectTypeRef"></param>
        /// <returns></returns>
        private static MethodDefinition CreateStaticReaderStubMethodDefinition(TypeReference objectTypeRef)
        {
            string methodName = $"{READ_PREFIX}{objectTypeRef.FullName}";
            // create new reader for this type
            TypeDefinition readerTypeDef = GeneralHelper.GetOrCreateClass(_moduleDef, out _, GENERATED_TYPE_ATTRIBUTES, GENERATED_CLASS_NAME, null);
            MethodDefinition readerMethodDef = readerTypeDef.AddMethod(methodName,
                    MethodAttributes.Public |
                    MethodAttributes.Static |
                    MethodAttributes.HideBySig,
                    objectTypeRef);

            GeneralHelper.CreateParameter(readerMethodDef, ReaderHelper.PooledReader_TypeRef, "pooledReader");
            readerMethodDef.Body.InitLocals = true;

            return readerMethodDef;
        }


    }
}