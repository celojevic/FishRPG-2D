﻿using FishNet.CodeGenerating.Helping.Extension;
using FishNet.Serializing;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using Unity.CompilationPipeline.Common.Diagnostics;
using UnityEngine;

namespace FishNet.CodeGenerating.Helping
{

    internal class GenericReaderHelper
    {

        #region Reflection references.
        private TypeReference _genericReaderTypeRef;
        private TypeReference _readerTypeRef;
        private MethodReference _readGetSetMethodRef;
        private MethodReference _readAutoPackGetSetMethodRef;
        private TypeReference _functionT2TypeRef;
        private TypeReference _functionT3TypeRef;
        private MethodReference _functionT2ConstructorMethodRef;
        private MethodReference _functionT3ConstructorMethodRef;
        private TypeDefinition _generatedReaderWriterClassTypeDef;
        private MethodDefinition _generatedReaderWriterOnLoadMethodDef;
        private TypeReference _autoPackTypeRef;
        #endregion

        #region Misc.
        /// <summary>
        /// TypeReferences which have already had delegates made for.
        /// </summary>
        private HashSet<TypeReference> _delegatedTypes = new HashSet<TypeReference>();
        #endregion

        #region Const.
        internal const string FIRSTINITIALIZE_METHOD_NAME = GenericWriterHelper.FIRSTINITIALIZE_METHOD_NAME;
        internal const MethodAttributes FIRSTINITIALIZE_METHOD_ATTRIBUTES = GenericWriterHelper.FIRSTINITIALIZE_METHOD_ATTRIBUTES;
        #endregion

        /// <summary>
        /// Imports references needed by this helper.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <returns></returns>
        internal bool ImportReferences()
        {
            _genericReaderTypeRef = CodegenSession.Module.ImportReference(typeof(GenericReader<>));
            _readerTypeRef = CodegenSession.Module.ImportReference(typeof(Reader));
            _functionT2TypeRef = CodegenSession.Module.ImportReference(typeof(Func<,>));
            _functionT3TypeRef = CodegenSession.Module.ImportReference(typeof(Func<,,>));
            _functionT2ConstructorMethodRef = CodegenSession.Module.ImportReference(typeof(Func<,>).GetConstructors()[0]);
            _functionT3ConstructorMethodRef = CodegenSession.Module.ImportReference(typeof(Func<,,>).GetConstructors()[0]);

            _autoPackTypeRef = CodegenSession.Module.ImportReference(typeof(AutoPackType));

            System.Reflection.PropertyInfo writePropertyInfo;
            writePropertyInfo = typeof(GenericReader<>).GetProperty(nameof(GenericReader<int>.Read));
            _readGetSetMethodRef = CodegenSession.Module.ImportReference(writePropertyInfo.GetSetMethod());
            writePropertyInfo = typeof(GenericReader<>).GetProperty(nameof(GenericReader<int>.ReadAutoPack));
            _readAutoPackGetSetMethodRef = CodegenSession.Module.ImportReference(writePropertyInfo.GetSetMethod());

            return true;
        }

        /// <summary>
        /// Creates a Read delegate for readMethodRef and places it within the generated reader/writer constructor.
        /// </summary>
        /// <param name="readMethodRef"></param>
        /// <param name="diagnostics"></param>
        internal void CreateReadDelegate(MethodReference readMethodRef)
        {
            bool created;
            /* If class for generated reader/writers isn't known yet.
            * It's possible this is the case if the entry being added
            * now is the first entry. That would mean the class was just
            * generated. */
            if (_generatedReaderWriterClassTypeDef == null)
                _generatedReaderWriterClassTypeDef = CodegenSession.GeneralHelper.GetOrCreateClass(out _, ReaderGenerator.GENERATED_TYPE_ATTRIBUTES, ReaderGenerator.GENERATED_CLASS_NAME, null);
            /* If constructor isn't set then try to get or create it
             * and also add it to methods if were created. */
            if (_generatedReaderWriterOnLoadMethodDef == null)
            {
                _generatedReaderWriterOnLoadMethodDef = CodegenSession.GeneralHelper.GetOrCreateMethod(_generatedReaderWriterClassTypeDef, out created, FIRSTINITIALIZE_METHOD_ATTRIBUTES, FIRSTINITIALIZE_METHOD_NAME, CodegenSession.Module.TypeSystem.Void);
                if (created)
                    CodegenSession.GeneralHelper.CreateRuntimeInitializeOnLoadMethodAttribute(_generatedReaderWriterOnLoadMethodDef);
            }
            //Check if ret already exist, if so remove it; ret will be added on again in this method.
            if (_generatedReaderWriterOnLoadMethodDef.Body.Instructions.Count != 0)
            {
                int lastIndex = (_generatedReaderWriterOnLoadMethodDef.Body.Instructions.Count - 1);
                if (_generatedReaderWriterOnLoadMethodDef.Body.Instructions[lastIndex].OpCode == OpCodes.Ret)
                    _generatedReaderWriterOnLoadMethodDef.Body.Instructions.RemoveAt(lastIndex);
            }
            //Check if already exist.
            ILProcessor processor = _generatedReaderWriterOnLoadMethodDef.Body.GetILProcessor();
            TypeReference dataTypeRef = readMethodRef.ReturnType;
            if (_delegatedTypes.Contains(dataTypeRef))
            {
                CodegenSession.LogError($"Generic read already created for {dataTypeRef.FullName}.");
                return;
            }
            else
            {
                _delegatedTypes.Add(dataTypeRef);
            }

            //Create a Func<Reader, T> delegate 
            processor.Emit(OpCodes.Ldnull);
            processor.Emit(OpCodes.Ldftn, readMethodRef);

            GenericInstanceType functionGenericInstance;
            MethodReference functionConstructorInstanceMethodRef;
            bool isAutoPacked = CodegenSession.ReaderHelper.IsAutoPackedType(dataTypeRef);

            //Generate for autopacktype.
            if (isAutoPacked)
            {
                functionGenericInstance = _functionT3TypeRef.MakeGenericInstanceType(_readerTypeRef,_autoPackTypeRef, dataTypeRef);
                functionConstructorInstanceMethodRef = _functionT3ConstructorMethodRef.MakeHostInstanceGeneric(functionGenericInstance);
            }
            //Not autopacked.
            else
            {
                functionGenericInstance = _functionT2TypeRef.MakeGenericInstanceType(_readerTypeRef, dataTypeRef);
                functionConstructorInstanceMethodRef = _functionT2ConstructorMethodRef.MakeHostInstanceGeneric(functionGenericInstance);
            }

            processor.Emit(OpCodes.Newobj, functionConstructorInstanceMethodRef);

            //Call delegate to GeneratedReader<T>.Read
            GenericInstanceType genericInstance = _genericReaderTypeRef.MakeGenericInstanceType(dataTypeRef);
            MethodReference genericReaderMethodRef = (isAutoPacked) ?
                _readAutoPackGetSetMethodRef.MakeHostInstanceGeneric(genericInstance) :
                _readGetSetMethodRef.MakeHostInstanceGeneric(genericInstance);
            processor.Emit(OpCodes.Call, genericReaderMethodRef);

            processor.Emit(OpCodes.Ret);
        }


    }
}