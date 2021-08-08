﻿using FishNet.CodeGenerating.Helping.Extension;
using FishNet.CodeGenerating.ILCore;
using FishNet.Serializing;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CompilationPipeline.Common.Diagnostics;
using UnityEngine;
using UnityEngine.Scripting;

namespace FishNet.CodeGenerating.Helping
{

    internal class GenericWriterHelper
    {

        #region Reflection references.
        private TypeReference _genericRWriterTypeRef;
        private TypeReference _writerTypeRef;
        private MethodReference _writeGetSetMethodRef;
        private TypeReference _actionTypeRef;
        private MethodReference _actionConstructorMethodRef;
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
        internal const string FIRSTINITIALIZE_METHOD_NAME = "FirstInitialize";
        internal const MethodAttributes FIRSTINITIALIZE_METHOD_ATTRIBUTES = MethodAttributes.Static;
        #endregion

        /// <summary>
        /// Imports references needed by this helper.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <returns></returns>
        internal bool ImportReferences()
        {
            _genericRWriterTypeRef = CodegenSession.Module.ImportReference(typeof(GenericWriter<>));
            _writerTypeRef = CodegenSession.Module.ImportReference(typeof(Writer));
            _actionTypeRef = CodegenSession.Module.ImportReference(typeof(Action<,>));
            _actionConstructorMethodRef = CodegenSession.Module.ImportReference(typeof(Action<,>).GetConstructors()[0]);

            System.Reflection.PropertyInfo writePropertyInfo = typeof(GenericWriter<>).GetProperty(nameof(GenericWriter<int>.Write));
            _writeGetSetMethodRef = CodegenSession.Module.ImportReference(writePropertyInfo.GetSetMethod());

            return true;
        }

        /// <summary>
        /// Creates a variant of an instanced write method.
        /// </summary>
        /// <param name="writeMethodRef"></param>
        /// <param name="diagnostics"></param>
        internal void CreateInstancedStaticWrite(MethodReference writeMethodRef)
        {
            if (_generatedReaderWriterClassTypeDef == null)
                _generatedReaderWriterClassTypeDef = CodegenSession.GeneralHelper.GetOrCreateClass(out _, WriterGenerator.GENERATED_TYPE_ATTRIBUTES, WriterGenerator.GENERATED_CLASS_NAME, null);

            MethodDefinition writeMethodDef = writeMethodRef.Resolve();
            MethodDefinition createdMethodDef = new MethodDefinition($"Static___{writeMethodRef.Name}",
                (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig),
                _generatedReaderWriterClassTypeDef.Module.TypeSystem.Void);
            _generatedReaderWriterClassTypeDef.Methods.Add(createdMethodDef);

            TypeReference extensionAttributeTypeRef = CodegenSession.Module.ImportReference(typeof(System.Runtime.CompilerServices.ExtensionAttribute));
            MethodDefinition constructor = extensionAttributeTypeRef.Resolve().GetConstructors().First();

            MethodReference extensionAttributeConstructorMethodRef = CodegenSession.Module.ImportReference(constructor);
            CustomAttribute extensionCustomAttribute = new CustomAttribute(extensionAttributeConstructorMethodRef);
            createdMethodDef.CustomAttributes.Add(extensionCustomAttribute);

            /* Add parameters to new method. */
            //First add extension.
            ParameterDefinition extensionParameterDef = CodegenSession.GeneralHelper.CreateParameter(createdMethodDef, typeof(PooledWriter), "pooledWriter", ParameterAttributes.None);
            //Then other types.
            ParameterDefinition[] remainingParameterDefs = new ParameterDefinition[writeMethodDef.Parameters.Count];
            for (int i = 0; i < writeMethodDef.Parameters.Count; i++)
            {
                remainingParameterDefs[i] = CodegenSession.GeneralHelper.CreateParameter(createdMethodDef, writeMethodDef.Parameters[i].ParameterType);
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
        /// Creates a Write delegate for writeMethodRef and places it within the generated reader/writer constructor.
        /// </summary>
        /// <param name="writeMethodRef"></param>
        /// <param name="diagnostics"></param>
        internal void CreateWriteDelegate(MethodReference writeMethodRef)
        {
            /* If class for generated reader/writers isn't known yet.
            * It's possible this is the case if the entry being added
            * now is the first entry. That would mean the class was just
            * generated. */
            bool created;

            if (_generatedReaderWriterClassTypeDef == null)
                _generatedReaderWriterClassTypeDef = CodegenSession.GeneralHelper.GetOrCreateClass(out created, WriterGenerator.GENERATED_TYPE_ATTRIBUTES, WriterGenerator.GENERATED_CLASS_NAME, null);

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
            TypeReference dataType;
            //Static methods will have the data type as the second parameter (1).
            if (writeMethodRef.Resolve().Attributes.HasFlag(MethodAttributes.Static))
                dataType = writeMethodRef.Parameters[1].ParameterType;
            else
                dataType = writeMethodRef.Parameters[0].ParameterType;

            if (_delegatedTypes.Contains(dataType))
            {
                CodegenSession.Diagnostics.AddError($"Generic write already created for {dataType.FullName}.");
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