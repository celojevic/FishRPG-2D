using FishNet.CodeGenerating.Helping.Extension;
using FishNet.CodeGenerating.ILCore;
using FishNet.Object.Helping;
using FishNet.Serializing;
using FishNet.Serializing.Helping;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using Unity.CompilationPipeline.Common.Diagnostics;
using UnityEngine;

namespace FishNet.CodeGenerating.Helping
{
    internal static class GeneralHelper
    {
        #region Reflection references.
        internal static MethodReference Debug_LogWarning_MethodRef;
        private static MethodReference EqualityComparer_Default_MethodRef;
        internal static MethodReference IsServer_MethodRef = null;
        internal static MethodReference IsClient_MethodRef = null;
        internal static MethodReference NetworkObject_Deinitializing_MethodRef = null;
        private static Dictionary<Type, TypeReference> _importedTypeReferences = new Dictionary<Type, TypeReference>();
        private static Dictionary<FieldDefinition, FieldReference> _importedFieldReferences = new Dictionary<FieldDefinition, FieldReference>();
        private static Dictionary<Type, GenericInstanceMethod> _equalityComparerMethodReferences = new Dictionary<Type, GenericInstanceMethod>();
        private static string NonSerialized_Attribute_FullName;
        private static string Single_FullName;
        #endregion

        #region Misc.
        private static ModuleDefinition _moduleDef;
        #endregion        
        internal static bool ImportReferences(ModuleDefinition moduleDef)
        {
            _importedFieldReferences.Clear();
            _importedTypeReferences.Clear();
            _equalityComparerMethodReferences.Clear();

            _moduleDef = moduleDef;

            NonSerialized_Attribute_FullName = typeof(NonSerializedAttribute).FullName;
            Single_FullName = typeof(float).FullName;

            Type comparers = typeof(Comparers);
            EqualityComparer_Default_MethodRef = moduleDef.ImportReference<Comparers>(x => Comparers.EqualityCompare<object>(default, default));

            Type debugType = typeof(UnityEngine.Debug);
            foreach (System.Reflection.MethodInfo methodInfo in debugType.GetMethods())
            {
                if (methodInfo.Name == nameof(UnityEngine.Debug.LogWarning) && methodInfo.GetParameters().Length == 1)
                    Debug_LogWarning_MethodRef = moduleDef.ImportReference(methodInfo);
            }

            Type codegenHelper = typeof(CodegenHelper);
            foreach (System.Reflection.MethodInfo methodInfo in codegenHelper.GetMethods())
            {
                if (methodInfo.Name == nameof(CodegenHelper.NetworkObject_Deinitializing))
                    NetworkObject_Deinitializing_MethodRef = _moduleDef.ImportReference(methodInfo);
                else if (methodInfo.Name == nameof(CodegenHelper.IsClient))
                    IsClient_MethodRef = _moduleDef.ImportReference(methodInfo);
                else if (methodInfo.Name == nameof(CodegenHelper.IsServer))
                    IsServer_MethodRef = _moduleDef.ImportReference(methodInfo);
            }

            return true;
        }

        /// <summary>
        /// Gets the equality comparerer method for type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static GenericInstanceMethod GetEqualityComparer(Type type)
        {
            GenericInstanceMethod result;
            if (_equalityComparerMethodReferences.TryGetValue(type, out result))
            {
                return result;
            }
            else
            {
                result = new GenericInstanceMethod(EqualityComparer_Default_MethodRef.GetElementMethod());
                result.GenericArguments.Add(GetTypeReference(type));
                _equalityComparerMethodReferences.Add(type, result);
            }

            return result;
        }

        /// <summary>
        /// Creates the RuntimeInitializeOnLoadMethod attribute for a method.
        /// </summary>
        internal static void CreateRuntimeInitializeOnLoadMethodAttribute(MethodDefinition methodDef)
        {
            TypeReference attTypeRef = GetTypeReference(typeof(RuntimeInitializeOnLoadMethodAttribute));
            foreach (CustomAttribute item in methodDef.CustomAttributes)
            {
                //Already exist.
                if (item.AttributeType.FullName == attTypeRef.FullName)
                    return;
            }

            MethodDefinition constructorMethodDef = attTypeRef.ResolveDefaultPublicConstructor();
            MethodReference constructorMethodRef = _moduleDef.ImportReference(constructorMethodDef);
            CustomAttribute ca = new CustomAttribute(constructorMethodRef);
            methodDef.CustomAttributes.Add(ca);
        }

        /// <summary>
        /// Gets the default AutoPackType to use for typeRef.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <returns></returns>
        internal static AutoPackType GetDefaultAutoPackType(TypeReference typeRef)
        {
            //Singles are defauled to unpacked.
            if (typeRef.FullName == Single_FullName)
                return AutoPackType.Unpacked;
            else
                return AutoPackType.Packed;
        }

        /// <summary>
        /// Gets a class within moduleDef or creates and returns the class if it does not already exist.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <returns></returns>
        internal static TypeDefinition GetOrCreateClass(ModuleDefinition moduleDef, out bool created, TypeAttributes typeAttr, string className, TypeReference baseTypeRef)
        {
            TypeDefinition type = moduleDef.GetClass(className);
            if (type != null)
            {
                created = false;
                return type;
            }
            else
            {
                created = true;
                type = new TypeDefinition(FishNetILPP.RUNTIME_ASSEMBLY_NAME, className,
                    typeAttr, moduleDef.ImportReference(typeof(object)));
                //Add base class if specified.
                if (baseTypeRef != null)
                    type.BaseType = moduleDef.ImportReference(baseTypeRef);

                moduleDef.Types.Add(type);
                return type;
            }
        }

        #region HasNonSerializableAttribute
        /// <summary>
        /// Returns if fieldDef has a NonSerialized attribute.
        /// </summary>
        /// <param name="fieldDef"></param>
        /// <returns></returns>
        internal static bool HasNonSerializableAttribute(FieldDefinition fieldDef)
        {
            foreach (CustomAttribute customAttribute in fieldDef.CustomAttributes)
            {
                if (customAttribute.AttributeType.FullName == NonSerialized_Attribute_FullName)
                    return true;
            }

            //Fall through, no matches.
            return false;
        }
        /// <summary>
        /// Returns if typeDef has a NonSerialized attribute.
        /// </summary>
        /// <param name="typeDef"></param>
        /// <returns></returns>
        internal static bool HasNonSerializableAttribute(TypeDefinition typeDef)
        {
            foreach (CustomAttribute customAttribute in typeDef.CustomAttributes)
            {
                if (customAttribute.AttributeType.FullName == NonSerialized_Attribute_FullName)
                    return true;
            }

            //Fall through, no matches.
            return false;
        }
        #endregion

        /// <summary>
        /// Gets a TypeReference for a type.
        /// </summary>
        /// <param name="type"></param>
        internal static TypeReference GetTypeReference(Type type)
        {
            TypeReference result;
            if (!_importedTypeReferences.TryGetValue(type, out result))
            {
                result = _moduleDef.ImportReference(type);
                _importedTypeReferences.Add(type, result);
            }

            return result;
        }

        /// <summary>
        /// Gets a FieldReference for a type.
        /// </summary>
        /// <param name="type"></param>
        internal static FieldReference GetFieldReference(FieldDefinition fieldDef)
        {
            FieldReference result;
            if (!_importedFieldReferences.TryGetValue(fieldDef, out result))
            {
                result = _moduleDef.ImportReference(fieldDef);
                _importedFieldReferences.Add(fieldDef, result);
            }

            return result;
        }

        /// <summary>
        /// Gets the current static constructor for typeDef, or makes a new one if constructor doesn't exist.
        /// </summary>
        /// <param name="typeDef"></param>
        /// <returns></returns>
        internal static MethodDefinition GetOrCreateConstructor(TypeDefinition typeDef, out bool created, List<DiagnosticMessage> diagnostics, bool makeStatic)
        {
            // find static constructor
            MethodDefinition cctorMethodDef = typeDef.GetMethod(".cctor");
            if (cctorMethodDef == null)
                cctorMethodDef = typeDef.GetMethod(".ctor");

            //Static constructor already exist.
            if (cctorMethodDef != null)
            {
                created = false;
                /* If there is OpCodes.Ret at the end then remove it.
                 * This OpCode will be added on again later. */
                if (cctorMethodDef.Body.Instructions.Count != 0)
                {
                    Instruction lastInst = cctorMethodDef.Body.Instructions[cctorMethodDef.Body.Instructions.Count - 1];
                    if (lastInst.OpCode == OpCodes.Ret)
                    {
                        cctorMethodDef.Body.Instructions.RemoveAt(cctorMethodDef.Body.Instructions.Count - 1);
                    }
                    else
                    {
                        diagnostics.AddError($"{typeDef.Name} has invalid class constructor");
                        return null;
                    }
                }
            }
            //Static constructor does not exist yet.
            else
            {
                created = true;
                MethodAttributes methodAttr = (Mono.Cecil.MethodAttributes.HideBySig |
                        Mono.Cecil.MethodAttributes.SpecialName |
                        Mono.Cecil.MethodAttributes.RTSpecialName);
                if (makeStatic)
                    methodAttr |= Mono.Cecil.MethodAttributes.Static;

                //Create a static constructor.
                cctorMethodDef = new MethodDefinition(".cctor", methodAttr,
                        typeDef.Module.TypeSystem.Void
                        );
            }

            return cctorMethodDef;
        }

        /// <summary>
        /// Creates a return of boolean type.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="result"></param>
        internal static void CreateRetBoolean(ILProcessor processor, bool result)
        {
            OpCode code = (result) ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0;
            processor.Emit(code);
            processor.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Creates a debug warning and returns the starting instruction.
        /// </summary>
        /// <param name="processor"></param>
        internal static List<Instruction> CreateDebugWarningInstructions(ILProcessor processor, string message)
        {
            List<Instruction> instructions = new List<Instruction>();
            instructions.Add(processor.Create(OpCodes.Ldstr, message));
            instructions.Add(processor.Create(OpCodes.Call, Debug_LogWarning_MethodRef));
            return instructions;
        }

        ///// <summary>
        ///// Creates a debug warning appends the instructions.
        ///// </summary>
        ///// <param name="processor"></param>
        //internal static void CreateDebugWarning(ILProcessor processor, FieldDefinition fieldDef)
        //{
        //    processor.Emit(OpCodes.Ldfld, fieldDef); 
        //    TypeDefinition td = fieldDef.FieldType.Resolve();
        //    _moduleDef.ImportReference(td);
        //    processor.Emit(OpCodes.Box, td);
        //    processor.Emit(OpCodes.Call, Debug_LogWarning_MethodRef);
        //}


        /// <summary>
        /// Creates a debug warning appends the instructions.
        /// </summary>
        /// <param name="processor"></param>
        internal static void CreateDebugWarning(ILProcessor processor, string message)
        {
            processor.Emit(OpCodes.Ldstr, message);
            processor.Emit(OpCodes.Call, Debug_LogWarning_MethodRef);
        }

        #region CreateVariable / CreateParameter.
        /// <summary>
        /// Creates a parameter within methodDef and returns it's ParameterDefinition.
        /// </summary>
        /// <param name="methodDef"></param>
        /// <param name="parameterTypeRef"></param>
        /// <returns></returns>
        internal static ParameterDefinition CreateParameter(MethodDefinition methodDef, TypeDefinition parameterTypeDef, string name = "", ParameterAttributes attributes = ParameterAttributes.None)
        {
            TypeReference typeRef = methodDef.Module.ImportReference(parameterTypeDef);
            return CreateParameter(methodDef, typeRef, name, attributes);
        }
        /// <summary>
        /// Creates a parameter within methodDef and returns it's ParameterDefinition.
        /// </summary>
        /// <param name="methodDef"></param>
        /// <param name="parameterTypeRef"></param>
        /// <returns></returns>
        internal static ParameterDefinition CreateParameter(MethodDefinition methodDef, TypeReference parameterTypeRef, string name = "", ParameterAttributes attributes = ParameterAttributes.None)
        {
            ParameterDefinition parameterDef = new ParameterDefinition(name, attributes, parameterTypeRef);
            methodDef.Parameters.Add(parameterDef);
            return parameterDef;
        }
        /// <summary>
        /// Creates a parameter within methodDef and returns it's ParameterDefinition.
        /// </summary>
        /// <param name="methodDef"></param>
        /// <param name="parameterTypeRef"></param>
        /// <returns></returns>
        internal static ParameterDefinition CreateParameter(MethodDefinition methodDef, Type parameterType, string name = "", ParameterAttributes attributes = ParameterAttributes.None)
        {
            return CreateParameter(methodDef, GetTypeReference(parameterType), name, attributes);
        }
        /// <summary>
        /// Creates a variable type within the body and returns it's VariableDef.
        /// </summary>
        /// <param name="methodDef"></param>
        /// <param name="variableTypeRef"></param>
        /// <returns></returns>
        internal static VariableDefinition CreateVariable(MethodDefinition methodDef, TypeReference variableTypeRef)
        {
            VariableDefinition variableDef = new VariableDefinition(variableTypeRef);
            methodDef.Body.Variables.Add(variableDef);
            return variableDef;
        }
        /// Creates a variable type within the body and returns it's VariableDef.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="methodDef"></param>
        /// <param name="variableTypeRef"></param>
        /// <returns></returns>
        internal static VariableDefinition CreateVariable(MethodDefinition methodDef, Type variableType)
        {
            return CreateVariable(methodDef, GetTypeReference(variableType));
        }
        #endregion

        #region SetVariableDef.
        /// <summary>
        /// Initializes variableDef as a new object or collection of typeDef.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="variableDef"></param>
        /// <param name="typeDef"></param>
        internal static void SetVariableDefinitionFromObject(ILProcessor processor, VariableDefinition variableDef, TypeDefinition typeDef, List<DiagnosticMessage> diagnostics)
        {
            TypeReference type = variableDef.VariableType;
            if (type.IsValueType)
            {
                // structs are created with Initobj
                processor.Emit(OpCodes.Ldloca, variableDef);
                processor.Emit(OpCodes.Initobj, type);
            }
            else if (typeDef.IsDerivedFrom<UnityEngine.ScriptableObject>())
            {
                MethodReference createScriptableObjectInstance = processor.Body.Method.Module.ImportReference(() => UnityEngine.ScriptableObject.CreateInstance<UnityEngine.ScriptableObject>());
                GenericInstanceMethod genericInstanceMethod = new GenericInstanceMethod(createScriptableObjectInstance.GetElementMethod());
                genericInstanceMethod.GenericArguments.Add(type);
                processor.Emit(OpCodes.Call, genericInstanceMethod);
                processor.Emit(OpCodes.Stloc, variableDef);
            }
            else
            {
                MethodDefinition constructorMethodDef = type.ResolveDefaultPublicConstructor();
                if (constructorMethodDef == null)
                {
                    diagnostics.AddError($"{type.Name} can't be deserialized because a default constructor could not be found. Create a default constructor or a custom serializer/deserializer.");
                    return;
                }

                MethodReference constructorMethodRef = processor.Body.Method.Module.ImportReference(constructorMethodDef);
                processor.Emit(OpCodes.Newobj, constructorMethodRef);
                processor.Emit(OpCodes.Stloc, variableDef);
            }
        }

        /// <summary>
        /// Assigns value to a VariableDef.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="variableDef"></param>
        /// <param name="value"></param>
        internal static void SetVariableDefinitionFromInt(ILProcessor processor, VariableDefinition variableDef, int value)
        {
            processor.Emit(OpCodes.Ldc_I4, value);
            processor.Emit(OpCodes.Stloc, variableDef);
        }
        /// <summary>
        /// Assigns value to a VariableDef.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="variableDef"></param>
        /// <param name="value"></param>
        internal static void SetVariableDefinitionFromParameter(ILProcessor processor, VariableDefinition variableDef, ParameterDefinition value)
        {
            processor.Emit(OpCodes.Ldarg, value);
            processor.Emit(OpCodes.Stloc, variableDef);
        }
        #endregion.

        /// <summary>
        /// Returns if an instruction is a call to a method.
        /// </summary>
        /// <param name="instruction"></param>
        /// <param name="calledMethod"></param>
        /// <returns></returns>
        internal static bool IsCallToMethod(Instruction instruction, out MethodDefinition calledMethod)
        {
            if (instruction.OpCode == OpCodes.Call && instruction.Operand is MethodDefinition method)
            {
                calledMethod = method;
                return true;
            }
            else
            {
                calledMethod = null;
                return false;
            }
        }


        /// <summary>
        /// Returns if a serializer and deserializer exist for typeRef. 
        /// </summary>
        /// <param name="typeRef"></param>
        /// <param name="create">True to create if missing.</param>
        /// <returns></returns>
        internal static bool HasSerializerAndDeserializer(TypeReference typeRef, bool create, List<DiagnosticMessage> diagnostics)
        {
            //Can be serialized/deserialized.
            bool hasWriter = WriterHelper.HasSerializer(typeRef, diagnostics, create);
            bool hasReader = ReaderHelper.HasDeserializer(typeRef, diagnostics, create);

            return (hasWriter && hasReader);
        }
    }
}