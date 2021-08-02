
using FishNet.CodeGenerating.Helping;
using FishNet.CodeGenerating.Helping.Extension;
using FishNet.Serializing;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using Unity.CompilationPipeline.Common.Diagnostics;

namespace FishNet.CodeGenerating.Processing
{
    internal static class CustomSerializerProcessor
    {

        #region Types.
        internal enum ExtensionType
        {
            None,
            Write,
            Read
        }

        #endregion

        internal static void CreateDelegates(TypeDefinition typeDef, ModuleDefinition moduleDef, List<DiagnosticMessage> diagnostics)
        {
            /* Find all declared methods and register delegates to them.
             * After they are all registered create any custom writers
             * needed to complete the declared methods. It's important to
             * make generated writers after so that a generated method
             * isn't made for a type when the user has already made a declared one. */
            foreach (MethodDefinition methodDef in typeDef.Methods)
            {
                ExtensionType extensionType = GetExtensionType(methodDef, diagnostics);
                if (extensionType == ExtensionType.None)
                    continue;

                MethodReference methodRef = moduleDef.ImportReference(methodDef);
                if (extensionType == ExtensionType.Write)
                {
                    WriterHelper.AddWriterMethod(methodRef.Parameters[1].ParameterType, methodRef, false, true);
                }
                else if (extensionType == ExtensionType.Read)
                {
                    ReaderHelper.AddReaderMethod(methodRef.ReturnType, methodRef, false, true);
                }
            }
        }

        /// <summary>
        /// Creates serializers for any custom types for declared methods.
        /// </summary>
        /// <param name="declaredMethods"></param>
        /// <param name="moduleDef"></param>
        internal static void CreateSerializers(TypeDefinition typeDef, ModuleDefinition moduleDef, List<DiagnosticMessage> diagnostics)
        {
            List<(MethodDefinition, ExtensionType)> declaredMethods = new List<(MethodDefinition, ExtensionType)>();
            /* Go through all custom serializers again and see if 
             * they use any types that the user didn't make a serializer for
             * and that there isn't a built-in type for. Create serializers
             * for these types. */
            foreach (MethodDefinition methodDef in typeDef.Methods)
            {
                ExtensionType extensionType = GetExtensionType(methodDef, diagnostics);
                if (extensionType == ExtensionType.None)
                    continue;

                declaredMethods.Add((methodDef, extensionType));
            }
            //Now that all declared are loaded see if any of them need generated serializers.
            foreach ((MethodDefinition methodDef, ExtensionType extensionType) in declaredMethods)
                CreateSerializers(extensionType, moduleDef, methodDef, diagnostics);
        }


        /// <summary>
        /// Creates a custom serializer for any types not handled within users declared.
        /// </summary>
        /// <param name="extensionType"></param>
        /// <param name="moduleDef"></param>
        /// <param name="methodDef"></param>
        /// <param name="diagnostics"></param>
        private static void CreateSerializers(ExtensionType extensionType, ModuleDefinition moduleDef, MethodDefinition methodDef, List<DiagnosticMessage> diagnostics)
        {
            for (int i = 0; i < methodDef.Body.Instructions.Count; i++)
                CheckToModifyInstructions(extensionType, moduleDef, methodDef, ref i, diagnostics);
        }

        /// <summary>
        /// Checks if instructions need to be modified and does so.
        /// </summary>
        /// <param name="methodDef"></param>
        /// <param name="instructionIndex"></param>
        private static void CheckToModifyInstructions(ExtensionType extensionType, ModuleDefinition moduleDef, MethodDefinition methodDef, ref int instructionIndex, List<DiagnosticMessage> diagnostics)
        {
            Instruction instruction = methodDef.Body.Instructions[instructionIndex];
            //Fields.
            if (instruction.OpCode == OpCodes.Ldsfld || instruction.OpCode == OpCodes.Ldfld)
                CheckFieldReferenceInstruction(extensionType, moduleDef, methodDef, ref instructionIndex, diagnostics);
            //Method calls.
            else if (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt)
                CheckCallInstruction(extensionType, moduleDef, methodDef, ref instructionIndex, (MethodReference)instruction.Operand, diagnostics);
        }


        /// <summary>
        /// Checks if a reader or writer must be generated for a field type.
        /// </summary>
        /// <param name="methodDef"></param>
        /// <param name="instructionIndex"></param>
        private static void CheckFieldReferenceInstruction(ExtensionType extensionType, ModuleDefinition moduleDef, MethodDefinition methodDef, ref int instructionIndex, List<DiagnosticMessage> diagnostics)
        {
            Instruction instruction = methodDef.Body.Instructions[instructionIndex];
            FieldReference field = (FieldReference)instruction.Operand;
            TypeReference type = field.DeclaringType;

            if (type.IsType(typeof(GenericWriter<>)) || type.IsType(typeof(GenericReader<>)) && type.IsGenericInstance)
            {
                GenericInstanceType typeGenericInst = (GenericInstanceType)type;
                TypeReference parameterType = typeGenericInst.GenericArguments[0];

                CreateReaderOrWriter(extensionType, moduleDef, methodDef, ref instructionIndex, parameterType, diagnostics);
            }
        }


        /// <summary>
        /// Checks if a reader or writer must be generated for a call type.
        /// </summary>
        /// <param name="extensionType"></param>
        /// <param name="moduleDef"></param>
        /// <param name="methodDef"></param>
        /// <param name="instructionIndex"></param>
        /// <param name="method"></param>
        private static void CheckCallInstruction(ExtensionType extensionType, ModuleDefinition moduleDef, MethodDefinition methodDef, ref int instructionIndex, MethodReference method, List<DiagnosticMessage> diagnostics)
        {
            if (!method.IsGenericInstance)
                return;

            //True if call is to read/write.
            bool canCreate = (
                method.Is<Writer>(nameof(Writer.Write)) ||
                method.Is<Reader>(nameof(Reader.Read))
                );

            if (canCreate)
            {
                GenericInstanceMethod instanceMethod = (GenericInstanceMethod)method;
                TypeReference parameterType = instanceMethod.GenericArguments[0];
                if (parameterType.IsGenericParameter)
                    return;

                CreateReaderOrWriter(extensionType, moduleDef, methodDef, ref instructionIndex, parameterType, diagnostics);
            }
        }


        /// <summary>
        /// Creates a reader or writer for parameterType.
        /// </summary>
        /// <param name="extensionType"></param>
        /// <param name="methodDef"></param>
        /// <param name="instructionIndex"></param>
        /// <param name="parameterType"></param>
        private static void CreateReaderOrWriter(ExtensionType extensionType, ModuleDefinition moduleDef, MethodDefinition methodDef, ref int instructionIndex, TypeReference parameterType, List<DiagnosticMessage> diagnostics)
        {
            if (!parameterType.IsGenericParameter && parameterType.CanBeResolved())
            {
                TypeDefinition typeDefinition = parameterType.Resolve();
                //If class and not value type check for accessible constructor.
                if (typeDefinition.IsClass && !typeDefinition.IsValueType)
                {
                    MethodDefinition constructor = typeDefinition.GetMethod(".ctor");
                    //Constructor is inaccessible, cannot create serializer for type.
                    if (!constructor.IsPublic || !(constructor.IsAssembly && typeDefinition.Module == moduleDef))
                        return;
                }

                ILProcessor processor = methodDef.Body.GetILProcessor();

                //Find already existing read or write method.
                MethodReference createdMethodRef = (extensionType == ExtensionType.Write) ?
                    WriterHelper.GetFavoredWriteMethodReference(parameterType, true) :
                    ReaderHelper.GetFavoredReadMethodReference(parameterType, true);
                //If a created method already exist nothing further is required.
                if (createdMethodRef != null)
                {
                    //Replace call to generic with already made serializer.
                    Instruction newInstruction = processor.Create(OpCodes.Call, createdMethodRef);
                    methodDef.Body.Instructions[instructionIndex] = newInstruction;
                    return;
                }
                else
                {
                    createdMethodRef = (extensionType == ExtensionType.Write) ?
                        WriterGenerator.CreateWriter(parameterType, diagnostics) :
                        ReaderGenerator.CreateReader(parameterType, diagnostics);
                }

                //If method was created.
                if (createdMethodRef != null)
                {
                    /* If an autopack type then we have to inject the
                     * autopack above the new instruction. */
                    if (WriterHelper.IsAutoPackedType(parameterType))
                    {
                        AutoPackType packType = GeneralHelper.GetDefaultAutoPackType(parameterType);
                        Instruction autoPack = processor.Create(OpCodes.Ldc_I4, (int)packType);
                        methodDef.Body.Instructions.Insert(instructionIndex, autoPack);
                        instructionIndex++;
                    }
                    Instruction newInstruction = processor.Create(OpCodes.Call, createdMethodRef);
                    methodDef.Body.Instructions[instructionIndex] = newInstruction;
                }
            }
        }


        /// <summary>
        /// Returns the RPC attribute on a method, if one exist. Otherwise returns null.
        /// </summary>
        /// <param name="methodDef"></param>
        /// <returns></returns>
        private static ExtensionType GetExtensionType(MethodDefinition methodDef, List<DiagnosticMessage> diagnostics)
        {
            bool hasExtensionAttribute = methodDef.HasCustomAttribute<System.Runtime.CompilerServices.ExtensionAttribute>();
            if (!hasExtensionAttribute)
                return ExtensionType.None;

            bool write = (methodDef.ReturnType == methodDef.Module.TypeSystem.Void);
            string prefix = (write) ?
                WriterHelper.WRITE_PREFIX : ReaderHelper.READ_PREFIX;

            //Does not contain prefix.
            if (methodDef.Name.Length < prefix.Length || methodDef.Name.Substring(0, prefix.Length) != prefix)
                return ExtensionType.None;

            if (write && methodDef.Parameters.Count < 2)
            {
                diagnostics.AddError($"{methodDef.FullName} must have at least two parameters, the first being PooledWriter, and second value to write.");
                return ExtensionType.None;
            }
            else if (!write && methodDef.Parameters.Count < 1)
            {
                diagnostics.AddError($"{methodDef.FullName} must have at least one parameters, the first being PooledReader.");
                return ExtensionType.None;
            }

            return (write) ? ExtensionType.Write : ExtensionType.Read;
        }


    }
}