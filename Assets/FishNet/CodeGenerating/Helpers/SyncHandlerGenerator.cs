using FishNet.CodeGenerating.Helping.Extension;
using FishNet.Object.Synchronizing;
using FishNet.Object.Synchronizing.Internal;
using FishNet.Serializing;
using FishNet.Serializing.Helping;
using FishNet.Transporting;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.CompilationPipeline.Common.Diagnostics;

namespace FishNet.CodeGenerating.Helping
{
    internal static class SyncHandlerGenerator
    {
        #region Types.
        internal class CreatedSyncType
        {
            public TypeDefinition StubClassTypeDefinition;
            public MethodReference GetValueMethodReference;
            public MethodReference SetValueMethodReference;
            public MethodReference GetPreviousClientValueMethodReference;
            public MethodReference ReadMethodReference;
            public MethodReference ConstructorMethodReference;
            public CreatedSyncType(TypeDefinition stubClassTypeDef, MethodReference getMethodRef, MethodReference setMethodRef, MethodReference getPreviousMethodRef, MethodReference readMethodRef, MethodReference constructorMethodRef)
            {
                StubClassTypeDefinition = stubClassTypeDef;
                GetValueMethodReference = getMethodRef;
                SetValueMethodReference = setMethodRef;
                GetPreviousClientValueMethodReference = getPreviousMethodRef;
                ReadMethodReference = readMethodRef;
                ConstructorMethodReference = constructorMethodRef;
            }
        }

        #endregion

        #region Relfection references.
        internal static Dictionary<TypeDefinition, CreatedSyncType> CreatedSyncTypes = new Dictionary<TypeDefinition, CreatedSyncType>(new TypeDefinitionComparer());
        private static TypeReference SyncBase_TypeRef;
        private static MethodReference _typedComparerMethodRef;
        internal static MethodReference SyncBase_SetSyncIndex_MethodRef;
        #endregion

        #region Misc.
        private static ModuleDefinition _moduleDef;
        #endregion

        #region Const.
        public const Mono.Cecil.TypeAttributes SYNCSTUB_TYPE_ATTRIBUTES = (Mono.Cecil.TypeAttributes.BeforeFieldInit |
            Mono.Cecil.TypeAttributes.Class | Mono.Cecil.TypeAttributes.AnsiClass | Mono.Cecil.TypeAttributes.Public |
            Mono.Cecil.TypeAttributes.AutoClass);
        private const string SYNCSTUB_CLASS_PREFIX = "SyncHandler";
        #endregion

        /// <summary>
        /// Imports references needed by this helper.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <returns></returns>
        internal static bool ImportReferences(ModuleDefinition moduleDef)
        {
            _moduleDef = moduleDef;
            Type syncBaseType = typeof(SyncBase);
            SyncBase_TypeRef = _moduleDef.ImportReference(syncBaseType);

            foreach (MethodInfo methodInfo in syncBaseType.GetMethods())
            {
                if (methodInfo.Name == nameof(SyncBase.SetSyncIndexInternal))
                    SyncBase_SetSyncIndex_MethodRef = moduleDef.ImportReference(methodInfo);
            }

            return true;
        }

        /// <summary>
        /// Gets the syncstub class for typeRef.
        /// </summary>
        /// <param name="dataTypeRef"></param>
        /// <returns></returns>
        internal static TypeDefinition GetOrCreateSyncHandler(TypeReference dataTypeRef, out MethodReference syncHandlerGetValueMethodRef,
            out MethodReference syncHandlerSetValueMethodRef, out MethodReference syncHandlerGetPreviousClientValueMethodRef,
            out MethodReference syncHandlerReadMethodRef, List<DiagnosticMessage> diagnostics)
        {
            syncHandlerSetValueMethodRef = null;
            syncHandlerGetValueMethodRef = null;
            syncHandlerGetPreviousClientValueMethodRef = null;
            syncHandlerReadMethodRef = null;

            TypeDefinition dataTypeDef = dataTypeRef.Resolve();

            bool created;
            TypeDefinition syncClassTypeDef = GeneralHelper.GetOrCreateClass(_moduleDef, out created, SYNCSTUB_TYPE_ATTRIBUTES,
                $"{SYNCSTUB_CLASS_PREFIX}{dataTypeRef.Name}", SyncBase_TypeRef);

            if (!created)
            {
                CreatedSyncType createdSyncStub;
                if (CreatedSyncTypes.TryGetValue(dataTypeDef, out createdSyncStub))
                {
                    syncHandlerGetValueMethodRef = createdSyncStub.GetValueMethodReference;
                    syncHandlerSetValueMethodRef = createdSyncStub.SetValueMethodReference;
                    syncHandlerGetPreviousClientValueMethodRef = createdSyncStub.GetPreviousClientValueMethodReference;
                    syncHandlerReadMethodRef = createdSyncStub.ReadMethodReference;
                }
                else
                {
                    diagnostics.AddError($"Found created class for sync type {dataTypeRef.FullName} but was unable to find cached class data.");
                    return null;
                }
            }
            //If was created then it must be completed with fields, methods, ect.
            else
            {
                /* Create comparer method reference for type. */
                Type dataMonoType = dataTypeRef.GetMonoType();
                if (dataMonoType == null)
                    return null;

                _moduleDef.ImportReference(dataTypeRef.Resolve());
                MethodInfo comparerGenericMethodInfo = typeof(Comparers).GetMethod(nameof(Comparers.EqualityCompare));
                //Get method for Comparer.EqualityCompare<Type>
                MethodInfo genericEqualityComparer = comparerGenericMethodInfo.MakeGenericMethod(dataMonoType);
                _typedComparerMethodRef = _moduleDef.ImportReference(genericEqualityComparer);

                TypeDefinition syncBaseTypeDef = SyncBase_TypeRef.Resolve();
                /* Required references. */

                //Methods.
                MethodReference baseReadMethodRef = null;
                MethodReference baseResetMethodRef = null;
                MethodReference baseWriteMethodRef = null;
                MethodReference baseDirtyMethodRef = null;
                MethodReference baseInitializeInstanceInternalMethodRef = null;
                foreach (MethodDefinition methodDef in syncBaseTypeDef.Methods)
                {
                    if (methodDef.Name == nameof(SyncBase.Read))
                        baseReadMethodRef = _moduleDef.ImportReference(methodDef);
                    else if (methodDef.Name == nameof(SyncBase.Reset))
                        baseResetMethodRef = _moduleDef.ImportReference(methodDef);
                    else if (methodDef.Name == nameof(SyncBase.Write))
                        baseWriteMethodRef = _moduleDef.ImportReference(methodDef);
                    else if (methodDef.Name == nameof(SyncBase.Dirty))
                        baseDirtyMethodRef = _moduleDef.ImportReference(methodDef);
                    else if (methodDef.Name == nameof(SyncBase.InitializeInstanceInternal))
                        baseInitializeInstanceInternalMethodRef = _moduleDef.ImportReference(methodDef);

                }
                //Fields
                FieldReference baseNetworkBehaviourFieldRef = null;
                foreach (FieldDefinition fieldDef in syncBaseTypeDef.Fields)
                {
                    if (fieldDef.Name == nameof(SyncBase.NetworkBehaviour))
                        baseNetworkBehaviourFieldRef = _moduleDef.ImportReference(fieldDef);
                }

                /* Adding fields to class. */
                //PreviousClientValue.
                FieldDefinition previousClientValueFieldDef = new FieldDefinition("_previousClientValue", Mono.Cecil.FieldAttributes.Private, dataTypeRef);
                syncClassTypeDef.Fields.Add(previousClientValueFieldDef);
                //InitializedValue.
                FieldDefinition initializeValueFieldDef = new FieldDefinition("_initializeValue", Mono.Cecil.FieldAttributes.Private, dataTypeRef);
                syncClassTypeDef.Fields.Add(initializeValueFieldDef);
                //Value.
                FieldDefinition valueFieldDef = new FieldDefinition("_value", Mono.Cecil.FieldAttributes.Private, dataTypeRef);
                syncClassTypeDef.Fields.Add(valueFieldDef);

                MethodDefinition tmpMd;
                tmpMd = CreateSyncHandlerConstructor(syncClassTypeDef, dataTypeRef.Resolve(), previousClientValueFieldDef, initializeValueFieldDef, valueFieldDef, baseInitializeInstanceInternalMethodRef, diagnostics);
                MethodReference syncHandlerConstructorMethodRef = _moduleDef.ImportReference(tmpMd);

                tmpMd = CreateSetValueMethodDefinition(syncClassTypeDef, valueFieldDef, previousClientValueFieldDef, baseNetworkBehaviourFieldRef, baseDirtyMethodRef, dataTypeRef);
                syncHandlerSetValueMethodRef = _moduleDef.ImportReference(tmpMd);

                tmpMd = CreateReadMethodDefinition(syncClassTypeDef, syncHandlerSetValueMethodRef, baseReadMethodRef, dataTypeRef, diagnostics);
                syncHandlerReadMethodRef = _moduleDef.ImportReference(tmpMd);

                tmpMd = CreateWriteMethodDefinition(syncClassTypeDef, valueFieldDef, baseWriteMethodRef, dataTypeRef, diagnostics);
                MethodReference writeMethodRef = _moduleDef.ImportReference(tmpMd);

                CreateWriteIfChangedMethodDefinition(syncClassTypeDef, writeMethodRef, valueFieldDef, initializeValueFieldDef);

                tmpMd = CreateGetValueMethodDefinition(syncClassTypeDef, valueFieldDef, dataTypeRef);
                syncHandlerGetValueMethodRef = _moduleDef.ImportReference(tmpMd);

                tmpMd = CreateGetPreviousClientValueMethodDefinition(syncClassTypeDef, previousClientValueFieldDef, dataTypeRef);
                syncHandlerGetPreviousClientValueMethodRef = _moduleDef.ImportReference(tmpMd);

                CreateResetMethodDefinition(syncClassTypeDef, initializeValueFieldDef, valueFieldDef, baseResetMethodRef);

                CreatedSyncTypes.Add(dataTypeDef, new CreatedSyncType(syncBaseTypeDef, syncHandlerGetValueMethodRef,
                    syncHandlerSetValueMethodRef, syncHandlerGetPreviousClientValueMethodRef, syncHandlerReadMethodRef, syncHandlerConstructorMethodRef));
            }

            return syncClassTypeDef;
        }


        /// <summary>
        /// Gets the current static constructor for typeDef, or makes a new one if constructor doesn't exist.
        /// </summary>
        /// <param name="typeDef"></param>
        /// <returns></returns>
        internal static MethodDefinition CreateSyncHandlerConstructor(TypeDefinition typeDef, TypeDefinition valueTypeDef,
            FieldDefinition previousClientValueFieldDef, FieldDefinition initializeValueFieldDef,
            FieldDefinition valueFieldDef, MethodReference baseInitializeInstanceMethodRef, List<DiagnosticMessage> diagnostics)
        {
            Mono.Cecil.MethodAttributes methodAttr = (Mono.Cecil.MethodAttributes.HideBySig |
                    Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.SpecialName |
                    Mono.Cecil.MethodAttributes.RTSpecialName);

            //Create constructor.
            MethodDefinition createdMethodDef = new MethodDefinition(".ctor", methodAttr,
                    typeDef.Module.TypeSystem.Void
                    );
            typeDef.Methods.Add(createdMethodDef);

            createdMethodDef.Body.InitLocals = true;

            //Add parameters.
            ParameterDefinition writePermissionsParameterDef = GeneralHelper.CreateParameter(createdMethodDef, typeof(WritePermission));
            ParameterDefinition readPermissionsParameterDef = GeneralHelper.CreateParameter(createdMethodDef, typeof(ReadPermission));
            ParameterDefinition sendTickIntervalParameterDef = GeneralHelper.CreateParameter(createdMethodDef, typeof(float));
            ParameterDefinition channelParameterDef = GeneralHelper.CreateParameter(createdMethodDef, typeof(Channel));
            ParameterDefinition initialValueParameterDef = GeneralHelper.CreateParameter(createdMethodDef, valueTypeDef);

            ILProcessor processor = createdMethodDef.Body.GetILProcessor();

            //Set initial values.
            //Previous values.
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldarg, initialValueParameterDef);
            processor.Emit(OpCodes.Stfld, previousClientValueFieldDef);
            //Initialize.
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldarg, initialValueParameterDef);
            processor.Emit(OpCodes.Stfld, initializeValueFieldDef);
            //Value.
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldarg, initialValueParameterDef);
            processor.Emit(OpCodes.Stfld, valueFieldDef);

            //Call base initialize with parameters passed in.
            processor.Emit(OpCodes.Ldarg_0); //this.
            processor.Emit(OpCodes.Ldarg, writePermissionsParameterDef);
            processor.Emit(OpCodes.Ldarg, readPermissionsParameterDef);
            processor.Emit(OpCodes.Ldarg, sendTickIntervalParameterDef);
            processor.Emit(OpCodes.Ldarg, channelParameterDef);
            processor.Emit(OpCodes.Ldc_I4_0); //false bool for IsSyncObject.
            processor.Emit(OpCodes.Call, baseInitializeInstanceMethodRef);

            processor.Emit(OpCodes.Ret);

            return createdMethodDef;
        }


        /// <summary>
        /// Creates private SetValue method.
        /// </summary>
        /// <param name="createdClassTypeDef"></param>
        /// <param name="dataTypeRef"></param>
        private static MethodDefinition CreateSetValueMethodDefinition(TypeDefinition createdClassTypeDef, FieldDefinition valueFieldDef,
            FieldDefinition previousClientValueFieldDef, FieldReference baseNetworkBehaviourFieldRef, MethodReference baseDirtyMethodRef, 
            TypeReference dataTypeRef)
        {
            MethodDefinition createdMethodDef = new MethodDefinition("SetValue",
                (Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.HideBySig),
                _moduleDef.TypeSystem.Boolean);
            createdClassTypeDef.Methods.Add(createdMethodDef);

            ParameterDefinition nextValueParameterDef = GeneralHelper.CreateParameter(createdMethodDef, dataTypeRef, "nextValue");
            ParameterDefinition asServerParameterDef = GeneralHelper.CreateParameter(createdMethodDef, typeof(bool), "asServer");

            ILProcessor processor = createdMethodDef.Body.GetILProcessor();
            createdMethodDef.Body.InitLocals = true;

            //True, for return response.
            Instruction endMethodFalseInst = processor.Create(OpCodes.Nop);
            Instruction endMethodTrueInst = processor.Create(OpCodes.Nop);

            /* If deinitializing then exit method. */
            //if (base.NetworkBehaviourDeinitializing) return.
            processor.Emit(OpCodes.Ldarg_0); //base.
            processor.Emit(OpCodes.Ldfld, baseNetworkBehaviourFieldRef);
            processor.Emit(OpCodes.Call, GeneralHelper.NetworkObject_Deinitializing_MethodRef);
            processor.Emit(OpCodes.Brtrue, endMethodFalseInst);

            //bool isServer = Helper.IsServer(base.NetworkBehaviour)
            VariableDefinition isServerVariableDef = GeneralHelper.CreateVariable(createdMethodDef, typeof(bool));
            CreateCallBaseNetworkBehaviour(processor, baseNetworkBehaviourFieldRef);
            processor.Emit(OpCodes.Call, GeneralHelper.IsServer_MethodRef);
            processor.Emit(OpCodes.Stloc, isServerVariableDef);
            //bool isClient = Helper.IsClient(base.NetworkBehaviour)
            VariableDefinition isClientVariableDef = GeneralHelper.CreateVariable(createdMethodDef, typeof(bool));
            CreateCallBaseNetworkBehaviour(processor, baseNetworkBehaviourFieldRef);
            processor.Emit(OpCodes.Call, GeneralHelper.IsClient_MethodRef);
            processor.Emit(OpCodes.Stloc, isClientVariableDef);


            Instruction beginClientChecksInst = processor.Create(OpCodes.Nop);

            /* As Server condition. */
            //If asServer / else jump to IsClient check.
            processor.Emit(OpCodes.Ldarg, asServerParameterDef);
            processor.Emit(OpCodes.Brfalse_S, beginClientChecksInst);
            //IsServer check.
            Instruction serverCanProcessLogicInst = processor.Create(OpCodes.Nop);
            processor.Emit(OpCodes.Ldloc, isServerVariableDef);
            processor.Emit(OpCodes.Brtrue_S, serverCanProcessLogicInst);
            //Debug and exit if server isn't active.
            GeneralHelper.CreateDebugWarning(processor, $"Sync value cannot be set when server is not active.");
            GeneralHelper.CreateRetBoolean(processor, false);
            //Server logic.
            processor.Append(serverCanProcessLogicInst);
            //Return false if unchanged.
            CreateRetFalseIfUnchanged(processor, valueFieldDef, nextValueParameterDef);
            //_value = nextValue.
            processor.Emit(OpCodes.Ldarg_0); //this.
            processor.Emit(OpCodes.Ldarg, nextValueParameterDef);
            processor.Emit(OpCodes.Stfld, valueFieldDef);
            //Dirty.
            processor.Emit(OpCodes.Ldarg_0); //base.
            processor.Emit(OpCodes.Call, baseDirtyMethodRef);
            processor.Emit(OpCodes.Br, endMethodTrueInst);

            /* !AsServer condition. (setting as client)*/

            //IsClient check.
            processor.Append(beginClientChecksInst);
            processor.Emit(OpCodes.Ldloc, isClientVariableDef);
            Instruction clientCanProcessLogicInst = processor.Create(OpCodes.Nop);
            processor.Emit(OpCodes.Brtrue_S, clientCanProcessLogicInst);
            //Debug and exit if client isn't active.
            GeneralHelper.CreateDebugWarning(processor, $"Sync value cannot be set when client is not active.");
            GeneralHelper.CreateRetBoolean(processor, false);
            //Client logic.
            processor.Append(clientCanProcessLogicInst);

            Instruction endEqualityCheckInst = processor.Create(OpCodes.Nop);
            //Return false if unchanged. Only checked if also not server.
            processor.Emit(OpCodes.Ldloc, isServerVariableDef);
            processor.Emit(OpCodes.Brtrue, endEqualityCheckInst);
            CreateRetFalseIfUnchanged(processor, previousClientValueFieldDef, nextValueParameterDef);
            processor.Append(endEqualityCheckInst);

            /* Set the previous client value no matter what.
             * The new value will only be set if not also server,
             * as the current value on server shouldn't be overwritten
             * with the latest client received. */
            //_previousClientValue = _value;
            processor.Emit(OpCodes.Ldarg_0); //this.
            processor.Emit(OpCodes.Ldarg_0); //this. one for each field.
            processor.Emit(OpCodes.Ldfld, valueFieldDef);
            processor.Emit(OpCodes.Stfld, previousClientValueFieldDef);

            /* As mentioned only set value if not also server.
             * Server value shouldn't be overwritten by client. */
            //_value = nextValue.
            Instruction isServerUpdateValueEndIfInst = processor.Create(OpCodes.Nop);
            processor.Emit(OpCodes.Ldloc, isServerVariableDef);
            processor.Emit(OpCodes.Brtrue, isServerUpdateValueEndIfInst);
            processor.Emit(OpCodes.Ldarg_0); //this.
            processor.Emit(OpCodes.Ldarg, nextValueParameterDef);
            processor.Emit(OpCodes.Stfld, valueFieldDef);
            processor.Append(isServerUpdateValueEndIfInst);

            processor.Append(endMethodTrueInst);
            //Return true at end of !asServer. Will arrive if all checks pass.
            GeneralHelper.CreateRetBoolean(processor, true);

            //End of method return.
            processor.Append(endMethodFalseInst);
            GeneralHelper.CreateRetBoolean(processor, false);

            return createdMethodDef;
        }


        /// <summary>
        /// Creates Read method.
        /// </summary>
        /// <param name="createdClassTypeDef"></param>
        /// <param name="valueFieldDef"></param>
        /// <param name="dataTypeRef"></param>
        private static MethodDefinition CreateReadMethodDefinition(TypeDefinition createdClassTypeDef, MethodReference setValueMethodRef, MethodReference baseReadMethodRef, TypeReference dataTypeRef, List<DiagnosticMessage> diagnostics)
        {
            MethodDefinition createdMethodDef = new MethodDefinition("Read",
                (Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.HideBySig | Mono.Cecil.MethodAttributes.Virtual),
                _moduleDef.TypeSystem.Boolean);
            createdClassTypeDef.Methods.Add(createdMethodDef);

            ParameterDefinition readerParameterDef = GeneralHelper.CreateParameter(createdMethodDef, ReaderHelper.PooledReader_TypeRef);

            ILProcessor processor = createdMethodDef.Body.GetILProcessor();
            createdMethodDef.Body.InitLocals = true;
            //MethodReference baseReadMethodRef = _moduleDef.ImportReference(baseReadMethodDef);

            //base.Read(pooledReader);
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldarg, readerParameterDef);
            processor.Emit(OpCodes.Call, baseReadMethodRef);

            VariableDefinition newValue = GeneralHelper.CreateVariable(createdMethodDef, dataTypeRef);
            MethodReference readTypeMethodRef = ReaderHelper.GetOrCreateFavoredReadMethodReference(dataTypeRef, true, diagnostics);

            //value = reader.ReadXXXXX
            processor.Emit(OpCodes.Ldarg, readerParameterDef);
            if (ReaderHelper.IsAutoPackedType(dataTypeRef))
                processor.Emit(OpCodes.Ldc_I4_1); //AutoPackType.Packed
            processor.Emit(OpCodes.Callvirt, readTypeMethodRef);
            processor.Emit(OpCodes.Stloc, newValue);

            //SetValue(newValue, false);
            processor.Emit(OpCodes.Ldarg_0); //this.
            processor.Emit(OpCodes.Ldloc, newValue);
            processor.Emit(OpCodes.Ldc_I4_0); //false boolean - !asServer.
            processor.Emit(OpCodes.Call, setValueMethodRef);

            processor.Emit(OpCodes.Ret);

            return createdMethodDef;
        }


        /// <summary>
        /// Creates Write method.
        /// </summary>
        /// <param name="createdClassTypeDef"></param>
        /// <param name="dataTypeRef"></param>
        private static MethodDefinition CreateWriteMethodDefinition(TypeDefinition createdClassTypeDef, FieldDefinition valueFieldDef, MethodReference baseWriteMethodDef, TypeReference dataTypeRef, List<DiagnosticMessage> diagnostics)
        {
            MethodDefinition createdMethodDef = new MethodDefinition("Write",
                (Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.HideBySig | Mono.Cecil.MethodAttributes.Virtual),
                _moduleDef.TypeSystem.Void);
            createdClassTypeDef.Methods.Add(createdMethodDef);

            //PooledWriter parameter.
            ParameterDefinition writerParameterDef = GeneralHelper.CreateParameter(createdMethodDef, WriterHelper.PooledWriter_TypeRef);
            //resetSyncTime parameter.
            ParameterDefinition resetSyncTimeParameterDef = GeneralHelper.CreateParameter(createdMethodDef, typeof(bool), "", (Mono.Cecil.ParameterAttributes.HasDefault | Mono.Cecil.ParameterAttributes.Optional));
            resetSyncTimeParameterDef.Constant = (bool)true;

            ILProcessor processor = createdMethodDef.Body.GetILProcessor();
            createdMethodDef.Body.InitLocals = true;
            MethodReference baseWriteMethodRef = _moduleDef.ImportReference(baseWriteMethodDef);

            //base.Write(writer, bool);
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldarg, writerParameterDef);
            processor.Emit(OpCodes.Ldarg, resetSyncTimeParameterDef);
            processor.Emit(OpCodes.Call, baseWriteMethodRef);

            //Write value.
            MethodReference writeMethodRef = WriterHelper.GetOrCreateFavoredWriteMethodReference(dataTypeRef, true, diagnostics);

            processor.Emit(OpCodes.Ldarg, writerParameterDef);
            processor.Emit(OpCodes.Ldarg_0); //this.
            processor.Emit(OpCodes.Ldfld, valueFieldDef);
            //If an auto pack method then insert default value.
            if (WriterHelper.IsAutoPackedType(valueFieldDef.FieldType))
            {
                AutoPackType packType = GeneralHelper.GetDefaultAutoPackType(valueFieldDef.FieldType);
                processor.Emit(OpCodes.Ldc_I4, (int)packType);
            }
            processor.Emit(OpCodes.Call, writeMethodRef);

            //WriterHelper.CreateWrite(processor, writerParameterDef, valueFieldDef, writeMethodRef, diagnostics);

            processor.Emit(OpCodes.Ret);

            return createdMethodDef;
        }


        /// <summary>
        /// Creates WriteIfChanged method.
        /// </summary>
        /// <param name="createdClassTypeDef"></param>
        /// <param name="syncTypeRef"></param>
        private static void CreateWriteIfChangedMethodDefinition(TypeDefinition createdClassTypeDef, MethodReference writeMethodRef, FieldDefinition valueFieldDef, FieldDefinition initialValueFieldDef)
        {
            MethodDefinition createdMethodDef = new MethodDefinition("WriteIfChanged",
                (Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.HideBySig | Mono.Cecil.MethodAttributes.Virtual),
                _moduleDef.TypeSystem.Void);
            createdClassTypeDef.Methods.Add(createdMethodDef);

            //PooledWriter parameter.
            ParameterDefinition writerParameterDef = GeneralHelper.CreateParameter(createdMethodDef, WriterHelper.PooledWriter_TypeRef);

            ILProcessor processor = createdMethodDef.Body.GetILProcessor();
            createdMethodDef.Body.InitLocals = true;

            //Exit early if unchanged
            CreateRetIfUnchanged(processor, valueFieldDef, initialValueFieldDef);

            //Write(pooledWriter, false);
            processor.Emit(OpCodes.Ldarg_0); //this.
            processor.Emit(OpCodes.Ldarg, writerParameterDef);
            processor.Emit(OpCodes.Ldc_I4_0); //false boolean.
            processor.Emit(OpCodes.Call, writeMethodRef);

            processor.Emit(OpCodes.Ret);
        }


        /// <summary>
        /// Creates GetValue method.
        /// </summary>
        /// <param name="createdClassTypeDef"></param>
        /// <param name="valueFieldDef"></param>
        /// <param name="dataTypeRef"></param>
        private static MethodDefinition CreateGetValueMethodDefinition(TypeDefinition createdClassTypeDef, FieldDefinition valueFieldDef, TypeReference dataTypeRef)
        {
            MethodDefinition createdMethodDef = new MethodDefinition("GetValue", (Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.HideBySig),
                dataTypeRef);
            createdClassTypeDef.Methods.Add(createdMethodDef);

            ILProcessor processor = createdMethodDef.Body.GetILProcessor();
            createdMethodDef.Body.InitLocals = true;
            //return Value.
            processor.Emit(OpCodes.Ldarg_0); //this.
            processor.Emit(OpCodes.Ldfld, valueFieldDef);
            processor.Emit(OpCodes.Ret);

            return createdMethodDef;
        }



        /// <summary>
        /// Creates GetPreviousValue method.
        /// </summary>
        /// <param name="createdClassTypeDef"></param>
        /// <param name="previousClientValueFieldDef"></param>
        /// <param name="dataTypeRef"></param>
        private static MethodDefinition CreateGetPreviousClientValueMethodDefinition(TypeDefinition createdClassTypeDef, FieldDefinition previousClientValueFieldDef, TypeReference dataTypeRef)
        {
            MethodDefinition createdMethodDef = new MethodDefinition("GetPreviousClientValue", (Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.HideBySig),
                dataTypeRef);
            createdClassTypeDef.Methods.Add(createdMethodDef);

            ILProcessor processor = createdMethodDef.Body.GetILProcessor();
            createdMethodDef.Body.InitLocals = true;

            processor.Emit(OpCodes.Ldarg_0); //this.
            processor.Emit(OpCodes.Ldfld, previousClientValueFieldDef);
            processor.Emit(OpCodes.Ret);

            return createdMethodDef;
        }


        /// <summary>
        /// Creates Reset method.
        /// </summary>
        /// <param name="createdClassTypeDef"></param>
        /// <param name="initializedValueFieldDef"></param>
        /// <param name="valueFieldDef"></param>
        private static void CreateResetMethodDefinition(TypeDefinition createdClassTypeDef, FieldDefinition initializedValueFieldDef, FieldDefinition valueFieldDef, MethodReference baseResetMethodRef)
        {
            MethodDefinition createdMethodDef = new MethodDefinition("Reset",
                (Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.HideBySig | Mono.Cecil.MethodAttributes.Virtual),
                _moduleDef.TypeSystem.Void);
            createdClassTypeDef.Methods.Add(createdMethodDef);

            ILProcessor processor = createdMethodDef.Body.GetILProcessor();
            createdMethodDef.Body.InitLocals = true;

            /*_value = _initializedValue; */
            processor.Emit(OpCodes.Ldarg_0); //this.
            processor.Emit(OpCodes.Ldarg_0); //this. (one for each field.
            processor.Emit(OpCodes.Ldfld, initializedValueFieldDef);
            processor.Emit(OpCodes.Stfld, valueFieldDef);

            processor.Emit(OpCodes.Ldarg_0); //this.
            processor.Emit(OpCodes.Call, baseResetMethodRef);

            processor.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Creates a ret of false if compared value is unchanged from current.
        /// </summary>
        private static void CreateRetFalseIfUnchanged(ILProcessor processor, FieldDefinition valueFieldDef, object nextValueDef)
        {
            Instruction endIfInst = processor.Create(OpCodes.Nop);
            //If (Comparer.EqualityCompare(_value, _initialValue)) return;
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldfld, valueFieldDef);
            //If comparing against another field.
            if (nextValueDef is FieldDefinition fd)
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Ldfld, fd);
            }
            //If comparing against a parameter.
            else if (nextValueDef is ParameterDefinition pd)
            {
                processor.Emit(OpCodes.Ldarg, pd);
            }
            processor.Emit(OpCodes.Call, _typedComparerMethodRef);
            processor.Emit(OpCodes.Brfalse, endIfInst);
            GeneralHelper.CreateRetBoolean(processor, false);
            processor.Append(endIfInst);
        }


        /// <summary>
        /// Creates a ret if compared value is unchanged from current.
        /// </summary>
        private static void CreateRetIfUnchanged(ILProcessor processor, FieldDefinition valueFieldDef, object nextValueDef)
        {
            Instruction endIfInst = processor.Create(OpCodes.Nop);
            //If (Comparer.EqualityCompare(_value, _initialValue)) return;
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldfld, valueFieldDef);
            //If comparing against another field.
            if (nextValueDef is FieldDefinition fd)
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Ldfld, fd);
            }
            //If comparing against a parameter.
            else if (nextValueDef is ParameterDefinition pd)
            {
                processor.Emit(OpCodes.Ldarg, pd);
            }
            processor.Emit(OpCodes.Call, _typedComparerMethodRef);
            processor.Emit(OpCodes.Brfalse, endIfInst);
            processor.Emit(OpCodes.Ret);
            processor.Append(endIfInst);
        }


        /// <summary>
        /// Creates a call to the base NetworkBehaviour.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="networkBehaviourFieldRef"></param>
        private static void CreateCallBaseNetworkBehaviour(ILProcessor processor, FieldReference networkBehaviourFieldRef)
        {
            processor.Emit(OpCodes.Ldarg_0); //this.
            processor.Emit(OpCodes.Ldfld, networkBehaviourFieldRef);
        }

    }


}