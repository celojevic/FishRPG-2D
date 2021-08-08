using FishNet.CodeGenerating.Helping.Extension;
using FishNet.Serializing;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using Unity.CompilationPipeline.Common.Diagnostics;

namespace FishNet.CodeGenerating.Helping
{

    internal class GenericReaderHelper
    {

        #region Reflection references.
        private TypeReference _genericReaderTypeRef;
        private TypeReference _readerTypeRef;
        private MethodReference _readGetSetMethodRef;
        private TypeReference _functionTypeRef;
        private MethodReference _functionConstructorMethodRef;
        private TypeDefinition _generatedReaderWriterClassTypeDef;
        private MethodDefinition _generatedReaderWriterOnLoadMethodDef;
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
            _functionTypeRef = CodegenSession.Module.ImportReference(typeof(Func<,>));
            _functionConstructorMethodRef = CodegenSession.Module.ImportReference(typeof(Func<,>).GetConstructors()[0]);

            System.Reflection.PropertyInfo writePropertyInfo = typeof(GenericReader<>).GetProperty(nameof(GenericReader<int>.Read));
            _readGetSetMethodRef = CodegenSession.Module.ImportReference(writePropertyInfo.GetSetMethod());

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

            ILProcessor processor = _generatedReaderWriterOnLoadMethodDef.Body.GetILProcessor();
            TypeReference dataType = readMethodRef.ReturnType;
            if (_delegatedTypes.Contains(dataType))
            {
                CodegenSession.Diagnostics.AddError($"Generic read already created for {dataType.FullName}.");
                return;
            }
            else
            {
                _delegatedTypes.Add(dataType);
            }
            //Create a Func<Reader, T> delegate 
            processor.Emit(OpCodes.Ldnull);
            processor.Emit(OpCodes.Ldftn, readMethodRef);
            GenericInstanceType functionGenericInstance = _functionTypeRef.MakeGenericInstanceType(_readerTypeRef, dataType);
            MethodReference functionConstructorInstanceMethodRef = _functionConstructorMethodRef.MakeHostInstanceGeneric(functionGenericInstance);
            processor.Emit(OpCodes.Newobj, functionConstructorInstanceMethodRef);

            //Call delegate to GeneratedReader<T>.Read
            GenericInstanceType genericInstance = _genericReaderTypeRef.MakeGenericInstanceType(dataType);
            MethodReference specializedField = _readGetSetMethodRef.MakeHostInstanceGeneric(genericInstance);
            processor.Emit(OpCodes.Call, specializedField);

            processor.Emit(OpCodes.Ret);
        }


    }
}