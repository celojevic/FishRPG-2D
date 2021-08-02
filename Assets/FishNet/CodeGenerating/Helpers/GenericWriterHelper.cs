using FishNet.CodeGenerating.Helping.Extension;
using FishNet.Serializing;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CompilationPipeline.Common.Diagnostics;
using UnityEngine;

namespace FishNet.CodeGenerating.Helping
{

    internal static class GenericWriterHelper
    {

        #region Reflection references.
        private static ModuleDefinition _moduleDef;
        private static TypeReference _genericRWriterTypeRef;
        private static TypeReference _writerTypeRef;
        private static MethodReference _writeGetSetMethodRef;
        private static TypeReference _actionTypeRef;
        private static MethodReference _actionConstructorMethodRef;
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
            _generatedReaderWriterConstructorMethodDef = null;
            _generatedReaderWriterClassTypeDef = null;
             
            _moduleDef = moduleDef;
            _genericRWriterTypeRef = _moduleDef.ImportReference(typeof(GenericWriter<>));
            _writerTypeRef = _moduleDef.ImportReference(typeof(Writer));
            _actionTypeRef = _moduleDef.ImportReference(typeof(Action<,>));
            _actionConstructorMethodRef = _moduleDef.ImportReference(typeof(Action<,>).GetConstructors()[0]);

            System.Reflection.PropertyInfo writePropertyInfo = typeof(GenericWriter<>).GetProperty(nameof(GenericWriter<int>.Write));
            _writeGetSetMethodRef = _moduleDef.ImportReference(writePropertyInfo.GetSetMethod());

            return true;
        }

        /// <summary>
        /// Creates a static variant of an instanced write method.
        /// </summary>
        /// <param name="writeMethodRef"></param>
        /// <param name="diagnostics"></param>
        internal static void CreateInstancedStaticWrite(MethodReference writeMethodRef, List<DiagnosticMessage> diagnostics)
        {
            if (_generatedReaderWriterClassTypeDef == null)
                _generatedReaderWriterClassTypeDef = GeneralHelper.GetOrCreateClass(_moduleDef, out _, WriterGenerator.GENERATED_TYPE_ATTRIBUTES, WriterGenerator.GENERATED_CLASS_NAME, null);
             
            MethodDefinition writeMethodDef = writeMethodRef.Resolve();
            MethodDefinition createdMethodDef = new MethodDefinition($"Static___{writeMethodRef.Name}",
                (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig),
                _generatedReaderWriterClassTypeDef.Module.TypeSystem.Void);
            _generatedReaderWriterClassTypeDef.Methods.Add(createdMethodDef);

            TypeReference extensionAttributeTypeRef = _moduleDef.ImportReference(typeof(System.Runtime.CompilerServices.ExtensionAttribute));
            MethodDefinition constructor = extensionAttributeTypeRef.Resolve().GetConstructors().First();
             
            MethodReference extensionAttributeConstructorMethodRef = _moduleDef.ImportReference(constructor);
            CustomAttribute extensionCustomAttribute = new CustomAttribute(extensionAttributeConstructorMethodRef);
            createdMethodDef.CustomAttributes.Add(extensionCustomAttribute);

            /* Add parameters to new method. */
            //First add extension.
            ParameterDefinition extensionParameterDef = GeneralHelper.CreateParameter(createdMethodDef, typeof(PooledWriter), "pooledWriter", ParameterAttributes.None);
            //Then other types.
            ParameterDefinition[] remainingParameterDefs = new ParameterDefinition[writeMethodDef.Parameters.Count];
            for (int i = 0; i < writeMethodDef.Parameters.Count; i++)
            {
                remainingParameterDefs[i] = GeneralHelper.CreateParameter(createdMethodDef, writeMethodDef.Parameters[i].ParameterType);
                _generatedReaderWriterClassTypeDef.Module.ImportReference(remainingParameterDefs[i].ParameterType.Resolve());
            } 

            ILProcessor processor = createdMethodDef.Body.GetILProcessor();
            //Load all parameters.
            foreach (ParameterDefinition pd in remainingParameterDefs)
                processor.Emit(OpCodes.Ldarg, pd);
            //Call instanced method.
            processor.Emit(OpCodes.Ldarg, extensionParameterDef);
            processor.Emit(OpCodes.Call, writeMethodRef);
            processor.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Creates a Write delegate for writeMethodRef and places it within the generated reader/writer static constructor.
        /// </summary>
        /// <param name="writeMethodRef"></param>
        /// <param name="diagnostics"></param>
        internal static void CreateWriteDelegate(MethodReference writeMethodRef, List<DiagnosticMessage> diagnostics)
        {
            /* If class for generated reader/writers isn't known yet.
            * It's possible this is the case if the entry being added
            * now is the first entry. That would mean the class was just
            * generated. */
            if (_generatedReaderWriterClassTypeDef == null)
                _generatedReaderWriterClassTypeDef = GeneralHelper.GetOrCreateClass(_moduleDef, out _, WriterGenerator.GENERATED_TYPE_ATTRIBUTES, WriterGenerator.GENERATED_CLASS_NAME, null);

            /* If constructor isn't set then try to get or create it
             * and also add it to methods if were created. */
            if (_generatedReaderWriterConstructorMethodDef == null)
            {
                bool created;
                _generatedReaderWriterConstructorMethodDef = GeneralHelper.GetOrCreateConstructor(_generatedReaderWriterClassTypeDef, out created, diagnostics, true);
                if (created)
                {
                    _generatedReaderWriterClassTypeDef.Methods.Add(_generatedReaderWriterConstructorMethodDef);
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
            TypeReference dataType;
            //Static methods will have the data type as the second parameter (1).
            if (writeMethodRef.Resolve().Attributes.HasFlag(MethodAttributes.Static))
                dataType = writeMethodRef.Parameters[1].ParameterType;
            else
                dataType = writeMethodRef.Parameters[0].ParameterType;

            if (_delegatedTypes.Contains(dataType))
            {
                diagnostics.AddError($"Generic write already created for {dataType.FullName}.");
                return;
            }
            else
            {
                _delegatedTypes.Add(dataType);
            }

            //Create a Action<Writer, T> delegate
            processor.Emit(OpCodes.Ldnull);
            processor.Emit(OpCodes.Ldftn, writeMethodRef);
            GenericInstanceType actionGenericInstance = _actionTypeRef.MakeGenericInstanceType(_writerTypeRef, dataType);
            MethodReference actionConstructorInstanceMethodRef = _actionConstructorMethodRef.MakeHostInstanceGeneric(actionGenericInstance);
            processor.Emit(OpCodes.Newobj, actionConstructorInstanceMethodRef);

            //Call delegate to GenericWriter<T>.Write
            GenericInstanceType genericInstance = _genericRWriterTypeRef.MakeGenericInstanceType(dataType);
            MethodReference genericRWriteMethodRef = _writeGetSetMethodRef.MakeHostInstanceGeneric(genericInstance);
            processor.Emit(OpCodes.Call, genericRWriteMethodRef);

            processor.Emit(OpCodes.Ret);
        }


    }
}