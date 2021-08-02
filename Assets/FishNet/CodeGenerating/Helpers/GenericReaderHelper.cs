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

    internal static class GenericReaderHelper
    {

        #region Reflection references.
        private static ModuleDefinition _moduleDef;
        private static TypeReference _genericReaderTypeRef;
        private static TypeReference _readerTypeRef;
        private static MethodReference _readGetSetMethodRef;
        private static TypeReference _functionTypeRef;
        private static MethodReference _functionConstructorMethodRef;
        private static TypeDefinition _generatedReaderWriterClassTypeDef;
        private static MethodDefinition _generatedReaderWriterConstructorMethodDef;
        #endregion

        #region Misc.
        /// <summary>
        /// TypeReferences which have already had delegates made for.
        /// </summary>
        private static HashSet<TypeReference> _delegatedTypes = new HashSet<TypeReference>();
        #endregion

        /// <summary>
        /// Imports references needed by this helper.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <returns></returns>
        internal static bool ImportReferences(ModuleDefinition moduleDef)
        {
            _moduleDef = moduleDef;
            _genericReaderTypeRef = _moduleDef.ImportReference(typeof(GenericReader<>));
            _readerTypeRef = _moduleDef.ImportReference(typeof(Reader));
            _functionTypeRef = _moduleDef.ImportReference(typeof(Func<,>));
            _functionConstructorMethodRef = _moduleDef.ImportReference(typeof(Func<,>).GetConstructors()[0]);

            System.Reflection.PropertyInfo writePropertyInfo = typeof(GenericReader<>).GetProperty(nameof(GenericReader<int>.Read));
            _readGetSetMethodRef = _moduleDef.ImportReference(writePropertyInfo.GetSetMethod());

            return true;
        }

        /// <summary>
        /// Creates a Read delegate for readMethodRef and places it within the generated reader/writer static constructor.
        /// </summary>
        /// <param name="readMethodRef"></param>
        /// <param name="diagnostics"></param>
        internal static void CreateReadDelegate(MethodReference readMethodRef, List<DiagnosticMessage> diagnostics)
        {
            /* If class for generated reader/writers isn't known yet.
            * It's possible this is the case if the entry being added
            * now is the first entry. That would mean the class was just
            * generated. */
            if (_generatedReaderWriterClassTypeDef == null)
                _generatedReaderWriterClassTypeDef = GeneralHelper.GetOrCreateClass(_moduleDef, out _,ReaderGenerator.GENERATED_TYPE_ATTRIBUTES, ReaderGenerator.GENERATED_CLASS_NAME, null);

            /* If constructor isn't set then try to get or create it
             * and also add it to methods if were created. */
            if (_generatedReaderWriterConstructorMethodDef == null)
            {
                bool created;
                _generatedReaderWriterConstructorMethodDef = GeneralHelper.GetOrCreateConstructor(_generatedReaderWriterClassTypeDef, out created, diagnostics, true);
                if (created)
                {                    _generatedReaderWriterClassTypeDef.Methods.Add(_generatedReaderWriterConstructorMethodDef);
                    GeneralHelper.CreateRuntimeInitializeOnLoadMethodAttribute(_generatedReaderWriterConstructorMethodDef);
                } 
            }

            //Check if ret already exist, if so remove it; ret will be added on again in this method.
            if (_generatedReaderWriterConstructorMethodDef.Body.Instructions.Count != 0)
            {
                int lastIndex = (_generatedReaderWriterConstructorMethodDef.Body.Instructions.Count - 1);
                if (_generatedReaderWriterConstructorMethodDef.Body.Instructions[lastIndex].OpCode == OpCodes.Ret)
                    _generatedReaderWriterConstructorMethodDef.Body.Instructions.RemoveAt(lastIndex);
            }
             
            ILProcessor processor = _generatedReaderWriterConstructorMethodDef.Body.GetILProcessor();
            TypeReference dataType = readMethodRef.ReturnType;
            if (_delegatedTypes.Contains(dataType))
            {
                diagnostics.AddError($"Generic read already created for {dataType.FullName}.");
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