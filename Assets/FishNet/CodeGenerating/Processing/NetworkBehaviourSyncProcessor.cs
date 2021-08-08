﻿using FishNet.CodeGenerating.Helping;
using FishNet.CodeGenerating.Helping.Extension;
using FishNet.Object.Synchronizing;
using FishNet.Object.Synchronizing.Internal;
using FishNet.Serializing;
using FishNet.Transporting;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using UnityEngine;

namespace FishNet.CodeGenerating.Processing
{
    internal class NetworkBehaviourSyncProcessor
    {
        #region Misc.
        /// <summary>
        /// Last instruction to read a sync type.
        /// </summary>
        private Instruction _lastReadInstruction = null;
        /// <summary>
        /// Objects created during this process. Used primarily to skip changing references within objects.
        /// </summary>
        private List<object> _createdSyncTypeMethodDefinitions = new List<object>();
        #endregion

        #region Const.
        private const string SYNCHANDLER_PREFIX = "syncHandler_";
        private const string ACCESSOR_PREFIX = "sync___";
        private const string SETSYNCINDEX_METHOD_NAME = "SetSyncIndexInternal";
        private const string SYNCOBJECT_INITIALIZEINSTANCE_METHOD_NAME = "InitializeSyncObjectInstanceInternal";
        #endregion

        /// <summary>
        /// Processes SyncVars and Objects.
        /// </summary>
        /// <param name="typeDef"></param>
        /// <param name="diagnostics"></param>
        internal bool Process( TypeDefinition typeDef, List<(SyncType, ProcessedSync)> allProcessedSyncs)
        {
            bool modified = false;

            _createdSyncTypeMethodDefinitions.Clear();
            _lastReadInstruction = null;

            /* Use a for loop because fields are added as they're processed.
             * A foreach would through collection modified error. */
            for (int i = 0; i < typeDef.Fields.Count; i++)
            {
                FieldDefinition fieldDef = typeDef.Fields[i];
                SyncType st = GetSyncType(fieldDef);
                //Not a sync type field.
                if (st == SyncType.Unset)
                    continue;

                if (st == SyncType.Variable)
                    TryCreateSyncVar(allProcessedSyncs.Count, allProcessedSyncs, typeDef, fieldDef);
                else if (st == SyncType.List)
                    TryCreateSyncList(allProcessedSyncs.Count, allProcessedSyncs, typeDef, fieldDef);

                modified = true;
            }

            return modified;
        }

        /// <summary>
        /// Gets SyncType fieldDef is.
        /// </summary>
        /// <param name="fieldDef"></param>
        /// <param name="diagnostics"></param>
        /// <returns></returns>
        private SyncType GetSyncType(FieldDefinition fieldDef)
        {
            if (fieldDef.FieldType.Resolve().ImplementsInterface<ISyncType>())
            {
                if (fieldDef.FieldType.Name == typeof(SyncList<>).Name)
                    return SyncType.List;
            }
            //Sync attribute.
            CustomAttribute syncAttribute = GetSyncVarAttribute(fieldDef);
            if (syncAttribute != null)
                return SyncType.Variable;

            //Fall through.
            return SyncType.Unset;
        }

        /// <summary>
        /// Tries to create a SyncList.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <param name="syncTypeCount"></param>
        /// <param name="allProcessedSyncs"></param>
        /// <param name="typeDef"></param>
        /// <param name="fieldDef"></param>
        /// <param name="diagnostics"></param>
        private void TryCreateSyncList( int syncTypeCount, List<(SyncType, ProcessedSync)> allProcessedSyncs, TypeDefinition typeDef, FieldDefinition originalFieldDef)
        {
            bool error;
            CustomAttribute syncAttribute = GetSyncObjectAttribute(originalFieldDef, out error);
            if (error)
                return;

            //Make sure type can be serialized.
            GenericInstanceType tmpGenerinstanceType = originalFieldDef.FieldType as GenericInstanceType;
            //this returns the correct data type, eg SyncList<int> would return int.
            TypeReference dataTypeRef = tmpGenerinstanceType.GenericArguments[0];
            System.Type monoType = dataTypeRef.GetMonoType();

            bool canSerialize = CodegenSession.GeneralHelper.HasSerializerAndDeserializer(dataTypeRef, true);
            if (!canSerialize)
            {
                CodegenSession.Diagnostics.AddError($"SyncObject {originalFieldDef.Name} data type {monoType.FullName} does not support serialization. Use a supported type or create a custom serializer.");
                return;
            }    

            bool result = InitializeSyncObject(syncTypeCount, typeDef, originalFieldDef, monoType, syncAttribute);
            if (result)
                allProcessedSyncs.Add((SyncType.List, null));
        }

        /// <summary>
        /// Tries to create a SyncVar.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <param name="syncTypeCount"></param>
        /// <param name="typeDef"></param>
        /// <param name="diagnostics"></param>
        private void TryCreateSyncVar( int syncTypeCount, List<(SyncType, ProcessedSync)> allProcessedSyncs, TypeDefinition typeDef, FieldDefinition fieldDef)
        {
            CustomAttribute syncAttribute = GetSyncVarAttribute(fieldDef);
            if (syncAttribute == null)
            {
                CodegenSession.Diagnostics.AddError($"Tried to create SyncVar for {fieldDef.FullName} but syncAttribute is null.");
                return;
            }

            MethodReference accessorSetValueMethodRef;
            MethodReference accessorGetValueMethodRef;
            bool created = CreateSyncVar(syncTypeCount, typeDef, fieldDef, syncAttribute, out accessorSetValueMethodRef, out accessorGetValueMethodRef);
            if (created)
            {
                FieldReference originalFieldRef = CodegenSession.Module.ImportReference(fieldDef);
                allProcessedSyncs.Add((SyncType.Variable, new ProcessedSync(originalFieldRef, accessorSetValueMethodRef, accessorGetValueMethodRef)));
            }
        }


        /// <summary>
        /// Returns the syncvar attribute on a method, if one exist. Otherwise returns null.
        /// </summary>
        /// <param name="fieldDef"></param>
        /// <returns></returns>
        private CustomAttribute GetSyncObjectAttribute(FieldDefinition fieldDef, out bool error)
        {
            CustomAttribute foundAttribute = null;
            //Becomes true if an error occurred during this process.
            error = false;

            foreach (CustomAttribute customAttribute in fieldDef.CustomAttributes)
            {
                if (!CodegenSession.AttributeHelper.IsSyncObjectAttribute(customAttribute.AttributeType.FullName))
                    continue;

                //A syncvar attribute already exist.
                if (foundAttribute != null)
                {
                    CodegenSession.Diagnostics.AddError($"{fieldDef.Name} cannot have multiple SyncObject attributes.");
                    error = true;
                }
                //Static.
                if (fieldDef.IsStatic)
                {
                    CodegenSession.Diagnostics.AddError($"{fieldDef.Name} SyncObject cannot be static.");
                    error = true;
                }
                //Generic.
                if (fieldDef.FieldType.IsGenericParameter)
                {
                    CodegenSession.Diagnostics.AddError($"{fieldDef.Name} SyncObject cannot be be generic.");
                    error = true;
                }

                //If all checks passed.
                if (!error)
                    foundAttribute = customAttribute;
            }

            //If an error occurred then reset results.
            if (error)
                foundAttribute = null;

            return foundAttribute;
        }


        /// <summary>
        /// Returns the syncvar attribute on a method, if one exist. Otherwise returns null.
        /// </summary>
        /// <param name="fieldDef"></param>
        /// <returns></returns>
        private CustomAttribute GetSyncVarAttribute(FieldDefinition fieldDef)
        {
            CustomAttribute foundAttribute = null;
            //Becomes true if an error occurred during this process.
            bool error = false;

            foreach (CustomAttribute customAttribute in fieldDef.CustomAttributes)
            {
                if (!CodegenSession.AttributeHelper.IsSyncVarAttribute(customAttribute.AttributeType.FullName))
                    continue;

                //A syncvar attribute already exist.
                if (foundAttribute != null)
                {
                    CodegenSession.Diagnostics.AddError($"{fieldDef.Name} cannot have multiple SyncVar attributes.");
                    error = true;
                }
                //Static.
                if (fieldDef.IsStatic)
                {
                    CodegenSession.Diagnostics.AddError($"{fieldDef.Name} SyncVar cannot be static.");
                    error = true;
                }
                //Generic.
                if (fieldDef.FieldType.IsGenericParameter)
                {
                    CodegenSession.Diagnostics.AddError($"{fieldDef.Name} SyncVar cannot be be generic.");
                    error = true;
                }

                //If all checks passed.
                if (!error)
                    foundAttribute = customAttribute;
            }

            if (foundAttribute != null)
            {
                bool canSerialize = CodegenSession.GeneralHelper.HasSerializerAndDeserializer(fieldDef.FieldType, true);
                if (!canSerialize)
                {
                    CodegenSession.Diagnostics.AddError($"SyncVar {fieldDef.FullName} field type {fieldDef.FieldType.FullName} does not support serialization. Use a supported type or create a custom serializer.");
                    error = true;
                }
            }

            //If an error occurred then reset results.
            if (error)
                foundAttribute = null;

            return foundAttribute;
        }

        /// <summary>
        /// Creates a syncVar class for the user's syncvar.
        /// </summary>
        /// <param name="originalFieldDef"></param>
        /// <param name="syncTypeAttribute"></param>
        /// <returns></returns>
        private bool CreateSyncVar( int syncTypeCount, TypeDefinition typeDef, FieldDefinition originalFieldDef, CustomAttribute syncTypeAttribute, out MethodReference accessorSetValueMethodRef, out MethodReference accessorGetValueMethodRef)
        {
            accessorGetValueMethodRef = null;
            accessorSetValueMethodRef = null;
            MethodReference syncHandlerGetValueMethodRef;
            MethodReference syncHandlerSetValueMethodRef;
            MethodReference syncHandlerGetPreviousClientValueMethodRef;
            MethodReference syncHandlerReadMethodRef;
            FieldDefinition createdSyncHandlerFieldDef = CreateSyncVarFieldDefinition(typeDef, originalFieldDef,
                out syncHandlerGetValueMethodRef, out syncHandlerSetValueMethodRef, out syncHandlerGetPreviousClientValueMethodRef, out syncHandlerReadMethodRef);

            if (createdSyncHandlerFieldDef != null)
            {
                MethodReference hookMethodRef = GetSyncVarHookMethodReference(typeDef, originalFieldDef, syncTypeAttribute);

                //If accessor was made add it's methods to createdSyncTypeObjects.
                if (CreateSyncVarAccessor(originalFieldDef, createdSyncHandlerFieldDef, syncHandlerSetValueMethodRef,
                    syncHandlerGetPreviousClientValueMethodRef, out accessorGetValueMethodRef, out accessorSetValueMethodRef,
                    hookMethodRef) != null)
                {
                    _createdSyncTypeMethodDefinitions.Add(accessorGetValueMethodRef.Resolve());
                    _createdSyncTypeMethodDefinitions.Add(accessorSetValueMethodRef.Resolve());
                }

                InitializeSyncHandler(syncTypeCount, createdSyncHandlerFieldDef, typeDef, originalFieldDef, syncTypeAttribute);

                MethodDefinition syncHandlerReadMethodDef = CreateSyncHandlerRead(typeDef, syncTypeCount, originalFieldDef, createdSyncHandlerFieldDef, accessorSetValueMethodRef);
                if (syncHandlerReadMethodDef != null)
                    _createdSyncTypeMethodDefinitions.Add(syncHandlerReadMethodDef);

                return true;
            }
            else
            {
                return false;
            }

        }

        /// <summary>
        /// Creates or gets a SyncType class for originalFieldDef.
        /// </summary>
        /// <param name="typeDef"></param>
        /// <param name="originalFieldDef"></param>
        /// <param name="syncVarAttribute"></param>
        /// <param name="createdSetValueMethodRef"></param>
        /// <param name="syncHandlerGetValueMethodRef"></param>
        /// <param name="diagnostics"></param>
        /// <returns></returns>  
        private FieldDefinition CreateSyncVarFieldDefinition(TypeDefinition typeDef, FieldDefinition originalFieldDef, out MethodReference syncHandlerGetValueMethodRef, out MethodReference syncHandlerSetValueMethodRef, out MethodReference syncHandlerGetPreviousClientValueMethodRef, out MethodReference syncHandlerReadMethodRef)
        {
            //Get class stub used.
            TypeDefinition syncStubTypeDef = CodegenSession.SyncHandlerGenerator.GetOrCreateSyncHandler(originalFieldDef.FieldType, out syncHandlerGetValueMethodRef, out syncHandlerSetValueMethodRef, out syncHandlerGetPreviousClientValueMethodRef, out syncHandlerReadMethodRef);
            if (syncStubTypeDef == null)
                return null;

            FieldDefinition createdFieldDef = new FieldDefinition($"{SYNCHANDLER_PREFIX}{originalFieldDef.Name}", originalFieldDef.Attributes, syncStubTypeDef);
            if (createdFieldDef == null)
            {
                CodegenSession.Diagnostics.AddError($"Could not create field for Sync type {originalFieldDef.FieldType.FullName}, name of {originalFieldDef.Name}.");
                return null;
            }

            typeDef.Fields.Add(createdFieldDef);
            return createdFieldDef;
        }

        /// <summary>
        /// Validates and gets the hook MethodReference for a SyncVar if available.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <param name="typeDef"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        private MethodReference GetSyncVarHookMethodReference( TypeDefinition typeDef, FieldDefinition originalFieldDef, CustomAttribute attribute)
        {
            string hook = attribute.GetField("OnChange", string.Empty);
            //No hook is specified.
            if (string.IsNullOrEmpty(hook))
                return null;

            MethodDefinition md = typeDef.GetMethod(hook);

            if (md != null)
            {
                string incorrectParametersMsg = $"OnChange method for {originalFieldDef.FullName} must contain 3 parameters in order of {originalFieldDef.FieldType.Name} oldValue, {originalFieldDef.FieldType.Name} newValue, {CodegenSession.Module.TypeSystem.Boolean} asServer.";
                //Not correct number of parameters.
                if (md.Parameters.Count != 3)
                {
                    CodegenSession.Diagnostics.AddError(incorrectParametersMsg);
                    return null;
                }
                //One or more parameters are wrong.
                //Not correct number of parameters.
                if (md.Parameters[0].ParameterType != originalFieldDef.FieldType ||
                    md.Parameters[1].ParameterType != originalFieldDef.FieldType ||
                    md.Parameters[2].ParameterType != CodegenSession.Module.TypeSystem.Boolean)
                {
                    CodegenSession.Diagnostics.AddError(incorrectParametersMsg);
                    return null;
                }

                //If here everything checks out, return a method reference to hook method.
                return CodegenSession.Module.ImportReference(md);
            }
            //Hook specified but no method found.
            else
            {
                CodegenSession.Diagnostics.AddError($"Could not find method name {hook} for SyncType {originalFieldDef.FullName}.");
                return null;
            }
        }

        /// <summary>
        /// Creates accessor for a SyncVar.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <param name="originalFieldDef"></param>
        /// <param name="syncHandlerFieldDef"></param>
        /// <param name="syncHandlerSetValueMethodRef"></param>
        /// <param name="accessorGetValueMethodRef"></param>
        /// <param name="accessorSetValueMethodRef"></param>
        /// <param name="hookMethodRef"></param>
        /// <param name="diagnostics"></param>
        /// <returns></returns>
        private FieldDefinition CreateSyncVarAccessor( FieldDefinition originalFieldDef, FieldDefinition syncHandlerFieldDef, MethodReference syncHandlerSetValueMethodRef, MethodReference syncHandlerGetPreviousClientValueMethodRef, out MethodReference accessorGetValueMethodRef, out MethodReference accessorSetValueMethodRef, MethodReference hookMethodRef)
        {
            /* Create and add property definition. */
            PropertyDefinition createdPropertyDef = new PropertyDefinition($"SyncAccessor_{originalFieldDef.Name}", PropertyAttributes.None, originalFieldDef.FieldType);
            createdPropertyDef.DeclaringType = originalFieldDef.DeclaringType;
            //add the methods and property to the type.
            originalFieldDef.DeclaringType.Properties.Add(createdPropertyDef);

            ILProcessor processor;

            /* Get method for property definition. */
            MethodDefinition createdGetMethodDef = originalFieldDef.DeclaringType.AddMethod($"{ACCESSOR_PREFIX}get_value_{originalFieldDef.Name}", MethodAttributes.Public |
                    MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                    originalFieldDef.FieldType);
            createdGetMethodDef.SemanticsAttributes = MethodSemanticsAttributes.Getter;

            processor = createdGetMethodDef.Body.GetILProcessor();
            processor.Emit(OpCodes.Ldarg_0); //this.
            processor.Emit(OpCodes.Ldfld, originalFieldDef);
            processor.Emit(OpCodes.Ret);
            accessorGetValueMethodRef = CodegenSession.Module.ImportReference(createdGetMethodDef);
            //Add getter to properties.
            createdPropertyDef.GetMethod = createdGetMethodDef;

            /* Set method. */
            //Create the set method
            MethodDefinition createdSetMethodDef = originalFieldDef.DeclaringType.AddMethod($"{ACCESSOR_PREFIX}set_value_{originalFieldDef.Name}", MethodAttributes.Public |
                    MethodAttributes.SpecialName |
                    MethodAttributes.HideBySig);
            createdSetMethodDef.SemanticsAttributes = MethodSemanticsAttributes.Setter;

            ParameterDefinition valueParameterDef = CodegenSession.GeneralHelper.CreateParameter(createdSetMethodDef, originalFieldDef.FieldType, "value");
            ParameterDefinition asServerParameterDef = CodegenSession.GeneralHelper.CreateParameter(createdSetMethodDef, typeof(bool), "asServer");
            processor = createdSetMethodDef.Body.GetILProcessor();

            //Create previous value for hook. Only store it if hook is available.
            VariableDefinition prevValueVariableDef = CodegenSession.GeneralHelper.CreateVariable(createdSetMethodDef, valueParameterDef.ParameterType);
            if (hookMethodRef != null)
            {
                //If (asServer) previousValue = this.originalField.
                Instruction notAsServerPrevValueInst = processor.Create(OpCodes.Nop);
                Instruction notAsServerEndIfInst = processor.Create(OpCodes.Nop);
                processor.Emit(OpCodes.Ldarg, asServerParameterDef);
                processor.Emit(OpCodes.Brfalse, notAsServerPrevValueInst);
                processor.Emit(OpCodes.Ldarg_0); //this.
                processor.Emit(OpCodes.Ldfld, originalFieldDef);
                processor.Emit(OpCodes.Stloc, prevValueVariableDef);
                processor.Emit(OpCodes.Br, notAsServerEndIfInst);
                processor.Append(notAsServerPrevValueInst);

                //(else) -> If (!asServer)
                //If (!IsServer) previousValue = this.originalField.
                Instruction isServerInst = processor.Create(OpCodes.Nop);
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Call, CodegenSession.GeneralHelper.IsServer_MethodRef);
                processor.Emit(OpCodes.Brtrue, isServerInst);
                processor.Emit(OpCodes.Ldarg_0); //this.
                processor.Emit(OpCodes.Ldfld, originalFieldDef);
                processor.Emit(OpCodes.Stloc, prevValueVariableDef);
                processor.Emit(OpCodes.Br, notAsServerEndIfInst);
                //else else (isServer) previousValue = syncHnadler.GetPreviousClientValue().
                processor.Append(isServerInst);
                processor.Emit(OpCodes.Ldarg_0); //this.
                processor.Emit(OpCodes.Ldfld, syncHandlerFieldDef);
                processor.Emit(OpCodes.Call, syncHandlerGetPreviousClientValueMethodRef);
                processor.Emit(OpCodes.Stloc, prevValueVariableDef);
                processor.Append(notAsServerEndIfInst);
            }

            //if (!syncHandler_XXXX.SetValue(....) return
            Instruction endSetInst = processor.Create(OpCodes.Nop);
            processor.Emit(OpCodes.Ldarg_0); //this.
            processor.Emit(OpCodes.Ldfld, syncHandlerFieldDef);
            processor.Emit(OpCodes.Ldarg, valueParameterDef);
            processor.Emit(OpCodes.Ldarg, asServerParameterDef);
            processor.Emit(OpCodes.Callvirt, syncHandlerSetValueMethodRef);
            processor.Emit(OpCodes.Brfalse, endSetInst);

            //Assign to new value. _originalField = value;
            processor.Emit(OpCodes.Ldarg_0); //this.
            processor.Emit(OpCodes.Ldarg, valueParameterDef);
            processor.Emit(OpCodes.Stfld, originalFieldDef);

            //If hook exist then call it with value changes and asServer.
            if (hookMethodRef != null)
            {
                processor.Emit(OpCodes.Ldarg_0); //this.
                processor.Emit(OpCodes.Ldloc, prevValueVariableDef);
                processor.Emit(OpCodes.Ldarg_0); //this.
                processor.Emit(OpCodes.Ldfld, originalFieldDef);
                processor.Emit(OpCodes.Ldarg, asServerParameterDef);
                processor.Emit(OpCodes.Call, hookMethodRef);
            }

            processor.Append(endSetInst);
            processor.Emit(OpCodes.Ret);
            accessorSetValueMethodRef = CodegenSession.Module.ImportReference(createdSetMethodDef);
            //Add setter to properties.
            createdPropertyDef.SetMethod = createdSetMethodDef;

            return originalFieldDef;
        }

        /// <summary>
        /// Initializes the SyncHandler FieldDefinition.
        /// </summary>
        /// <param name="typeDef"></param>
        /// <param name="originalFieldDef"></param>
        /// <param name="attribute"></param>
        /// <param name="diagnostics"></param>
        internal bool InitializeSyncObject(int syncCount, TypeDefinition typeDef, FieldDefinition originalFieldDef, System.Type monoType, CustomAttribute attribute)
        {
            float sendRate = 0.1f;
            WritePermission writePermissions = WritePermission.ServerOnly;
            ReadPermission readPermissions = ReadPermission.Observers;
            Channel channel = Channel.Reliable;
            //If attribute isn't null then override values.
            if (attribute != null)
            {
                sendRate = attribute.GetField("SendRate", 0.1f);
                writePermissions = WritePermission.ServerOnly;
                readPermissions = attribute.GetField("ReadPermissions", ReadPermission.Observers);
                channel = Channel.Reliable; //attribute.GetField("Channel", Channel.Reliable);
            }

            MethodDefinition injectionMethodDef = typeDef.GetMethod(NetworkBehaviourProcessor.NETWORKINITIALIZE_INTERNAL_NAME);
            ILProcessor processor = injectionMethodDef.Body.GetILProcessor();

            List<Instruction> instructions = new List<Instruction>();

            //This import shouldn't be needed but cecil is stingy so rather be safe than sorry.
            typeDef.Module.ImportReference(monoType);
            //Get Type for SyncList of dataTypeRef. eg SyncList<int>.
            System.Type typedSyncClassType = typeof(SyncList<>).MakeGenericType(monoType);
            CodegenSession.Module.ImportReference(typedSyncClassType);

            /* Set sync index. */
            System.Reflection.MethodInfo setSyncIndexMethodInfo = typedSyncClassType.GetMethod(SETSYNCINDEX_METHOD_NAME);
            MethodReference setSyncIndexMethodRef = typeDef.Module.ImportReference(setSyncIndexMethodInfo);

            /* Initialize with attribute settings. */
            System.Reflection.MethodInfo initializeInstanceMethodInfo = typedSyncClassType.GetMethod("InitializeInstanceInternal");
            MethodReference initializeInstanceMethodRef = typeDef.Module.ImportReference(initializeInstanceMethodInfo);

            //Initialize with attribute settings.
            instructions.Add(processor.Create(OpCodes.Ldarg_0)); //this.
            instructions.Add(processor.Create(OpCodes.Ldfld, originalFieldDef));
            instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)writePermissions));
            instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)readPermissions));
            instructions.Add(processor.Create(OpCodes.Ldc_R4, sendRate));
            instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)channel));
            instructions.Add(processor.Create(OpCodes.Ldc_I4_1)); //true for syncObject.
            instructions.Add(processor.Create(OpCodes.Call, initializeInstanceMethodRef));

            //Set NetworkBehaviour and SyncIndex to use.
            instructions.Add(processor.Create(OpCodes.Ldarg_0)); //this.
            instructions.Add(processor.Create(OpCodes.Ldfld, originalFieldDef));
            instructions.Add(processor.Create(OpCodes.Ldarg_0)); //this again for NetworkBehaviour.
            instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)syncCount));
            instructions.Add(processor.Create(OpCodes.Callvirt, setSyncIndexMethodRef));

            processor.InsertFirst(instructions);

            return true;
        }


        /// <summary>
        /// Initializes the SyncHandler FieldDefinition.
        /// </summary>
        /// <param name="createdFieldDef"></param>
        /// <param name="typeDef"></param>
        /// <param name="originalFieldDef"></param>
        /// <param name="attribute"></param>
        /// <param name="diagnostics"></param>
        internal void InitializeSyncHandler(int syncCount, FieldDefinition createdFieldDef, TypeDefinition typeDef, FieldDefinition originalFieldDef, CustomAttribute attribute)
        {
            //Get all possible attributes.
            float sendRate = attribute.GetField("SendRate", 0.1f);
            WritePermission writePermissions = WritePermission.ServerOnly;
            ReadPermission readPermissions = attribute.GetField("ReadPermissions", ReadPermission.Observers);
            Channel channel = attribute.GetField("Channel", Channel.Reliable);

            MethodReference syncHandlerConstructorMethodRef;
            //Get constructor for syncType.
            if (CodegenSession.SyncHandlerGenerator.CreatedSyncTypes.TryGetValue(originalFieldDef.FieldType.Resolve(), out CreatedSyncType cst))
            {
                syncHandlerConstructorMethodRef = cst.ConstructorMethodReference;
            }
            else
            {
                CodegenSession.Diagnostics.AddError($"Created constructor not found for SyncType {originalFieldDef.DeclaringType}.");
                return;
            }

            MethodDefinition injectionMethodDef = typeDef.GetMethod(NetworkBehaviourProcessor.NETWORKINITIALIZE_INTERNAL_NAME);
            ILProcessor processor = injectionMethodDef.Body.GetILProcessor();

            List<Instruction> instructions = new List<Instruction>();
            //Initialize fieldDef with values from attribute.
            instructions.Add(processor.Create(OpCodes.Ldarg_0)); //this.
            instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)writePermissions));
            instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)readPermissions));
            instructions.Add(processor.Create(OpCodes.Ldc_R4, sendRate));
            instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)channel));
            instructions.Add(processor.Create(OpCodes.Ldarg_0)); //this.
            instructions.Add(processor.Create(OpCodes.Ldfld, originalFieldDef)); //initial value.
            instructions.Add(processor.Create(OpCodes.Newobj, syncHandlerConstructorMethodRef));
            instructions.Add(processor.Create(OpCodes.Stfld, createdFieldDef));
            //Set NetworkBehaviour and SyncIndex to use.
            instructions.Add(processor.Create(OpCodes.Ldarg_0)); //this.
            instructions.Add(processor.Create(OpCodes.Ldfld, createdFieldDef));
            instructions.Add(processor.Create(OpCodes.Ldarg_0)); //this again for NetworkBehaviour.
            instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)syncCount));
            instructions.Add(processor.Create(OpCodes.Callvirt, CodegenSession.SyncHandlerGenerator.SyncBase_SetSyncIndex_MethodRef));

            processor.InsertFirst(instructions);
            //Instruction firstInstruction = injectionMethodDef.Body.Instructions[0];
            //foreach (Instruction inst in instructions)
            //    processor.InsertBefore(firstInstruction, inst);
        }

        /// <summary>
        /// Replaces GetSets for methods which may use a SyncType.
        /// </summary>
        /// <param name="modifiableMethods"></param>
        /// <param name="processedSyncs"></param>
        internal void ReplaceGetSets(List<MethodDefinition> modifiableMethods, List<(SyncType, ProcessedSync)> processedSyncs)
        {
            //Build processed syncs into dictionary for quicker loookups.
            Dictionary<FieldReference, List<ProcessedSync>> processedLookup = new Dictionary<FieldReference, List<ProcessedSync>>();
            foreach ((SyncType st, ProcessedSync ps) in processedSyncs)
            {
                if (st != SyncType.Variable)
                    continue;

                List<ProcessedSync> result;
                if (!processedLookup.TryGetValue(ps.OriginalFieldReference, out result))
                {
                    result = new List<ProcessedSync>() { ps };
                    processedLookup.Add(ps.OriginalFieldReference, result);
                }

                result.Add(ps);
            }

            foreach (MethodDefinition methodDef in modifiableMethods)
                ReplaceGetSet(methodDef, processedLookup);
        }

        /// <summary>
        /// Replaces GetSets for a method which may use a SyncType.
        /// </summary>
        /// <param name="methodDef"></param>
        /// <param name="processedLookup"></param>
        private void ReplaceGetSet(MethodDefinition methodDef, Dictionary<FieldReference, List<ProcessedSync>> processedLookup)
        {
            if (methodDef == null)
            {
                CodegenSession.Diagnostics.AddError($"An object expecting value was null. Please try saving your script again.");
                return;
            }
            if (methodDef.IsAbstract)
                return;
            if (_createdSyncTypeMethodDefinitions.Contains(methodDef))
                return;
            if (methodDef.Name == NetworkBehaviourProcessor.NETWORKINITIALIZE_INTERNAL_NAME)
                return;

            for (int i = 0; i < methodDef.Body.Instructions.Count; i++)
            {
                Instruction inst = methodDef.Body.Instructions[i];

                /* Loading a field. (Getter) */
                if (inst.OpCode == OpCodes.Ldfld && inst.Operand is FieldReference opFieldld)
                {
                    FieldReference resolvedOpField = opFieldld.Resolve();
                    if (resolvedOpField == null)
                        resolvedOpField = opFieldld.DeclaringType.Resolve().GetField(opFieldld.Name);

                    ProcessGetField(methodDef, i, resolvedOpField, processedLookup);
                }

                /* Setting a field. (Setter) */
                else if (inst.OpCode == OpCodes.Stfld && inst.Operand is FieldReference opFieldst)
                {
                    FieldReference resolvedOpField = opFieldst.Resolve();
                    if (resolvedOpField == null)
                        resolvedOpField = opFieldst.DeclaringType.Resolve().GetField(opFieldst.Name);

                    ProcessSetField(methodDef, i, resolvedOpField, processedLookup);
                }

                /* Load address, reference field. */
                else if (inst.OpCode == OpCodes.Ldflda && inst.Operand is FieldReference opFieldlda)
                {
                    FieldReference resolvedOpField = opFieldlda.Resolve();
                    if (resolvedOpField == null)
                        resolvedOpField = opFieldlda.DeclaringType.Resolve().GetField(opFieldlda.Name);

                    ProcessAddressField(methodDef, i, resolvedOpField, processedLookup);
                }

            }
        }

        /// <summary>
        /// Replaces Gets for a method which may use a SyncType.
        /// </summary>
        /// <param name="methodDef"></param>
        /// <param name="instructionIndex"></param>
        /// <param name="resolvedOpField"></param>
        /// <param name="processedLookup"></param>
        private void ProcessGetField(MethodDefinition methodDef, int instructionIndex, FieldReference resolvedOpField, Dictionary<FieldReference, List<ProcessedSync>> processedLookup)
        {
            Instruction inst = methodDef.Body.Instructions[instructionIndex];

            //If was a replaced field.
            if (processedLookup.TryGetValue(resolvedOpField, out List<ProcessedSync> psLst))
            {
                ProcessedSync ps = GetProcessedSync(resolvedOpField, psLst);
                if (ps == null)
                    return;

                //Generic type.
                if (resolvedOpField.DeclaringType.IsGenericInstance || resolvedOpField.DeclaringType.HasGenericParameters)
                {
                    FieldReference newField = inst.Operand as FieldReference;
                    GenericInstanceType genericType = (GenericInstanceType)newField.DeclaringType;
                    inst.OpCode = OpCodes.Callvirt;
                    inst.Operand = ps.GetMethodReference.MakeHostInstanceGeneric(genericType);
                }
                //Strong type.
                else
                {
                    inst.OpCode = OpCodes.Call;
                    inst.Operand = ps.GetMethodReference;
                }
            }
        }


        /// <summary>
        /// Replaces Sets for a method which may use a SyncType.
        /// </summary>
        /// <param name="methodDef"></param>
        /// <param name="instructionIndex"></param>
        /// <param name="resolvedOpField"></param>
        /// <param name="processedLookup"></param>
        private void ProcessSetField(MethodDefinition methodDef, int instructionIndex, FieldReference resolvedOpField, Dictionary<FieldReference, List<ProcessedSync>> processedLookup)
        {
            //return;
            Instruction inst = methodDef.Body.Instructions[instructionIndex];
            //If was a replaced field.
            if (processedLookup.TryGetValue(resolvedOpField, out List<ProcessedSync> psLst))
            {
                ProcessedSync ps = GetProcessedSync(resolvedOpField, psLst);
                if (ps == null)
                    return;

                ILProcessor processor = methodDef.Body.GetILProcessor();
                //Generic type.
                if (resolvedOpField.DeclaringType.IsGenericInstance || resolvedOpField.DeclaringType.HasGenericParameters)
                {
                    //Pass in true for as server.
                    Instruction boolTrueInst = processor.Create(OpCodes.Ldc_I4_1);
                    methodDef.Body.Instructions.Insert(instructionIndex, boolTrueInst);

                    var newField = inst.Operand as FieldReference;
                    var genericType = (GenericInstanceType)newField.DeclaringType;
                    inst.OpCode = OpCodes.Callvirt;
                    inst.Operand = ps.SetMethodReference.MakeHostInstanceGeneric(genericType);
                }
                //Strong typed.
                else
                {
                    //Pass in true for as server.
                    Instruction boolTrueInst = processor.Create(OpCodes.Ldc_I4_1);
                    methodDef.Body.Instructions.Insert(instructionIndex, boolTrueInst);

                    inst.OpCode = OpCodes.Call;
                    inst.Operand = ps.SetMethodReference;
                }
            }
        }

        /// <summary>
        /// Replaces address Sets for a method which may use a SyncType.
        /// </summary>
        /// <param name="methodDef"></param>
        /// <param name="instructionIndex"></param>
        /// <param name="resolvedOpField"></param>
        /// <param name="processedLookup"></param>
        private void ProcessAddressField(MethodDefinition methodDef, int instructionIndex, FieldReference resolvedOpField, Dictionary<FieldReference, List<ProcessedSync>> processedLookup)
        {
            Instruction inst = methodDef.Body.Instructions[instructionIndex];
            //Check if next instruction is Initobj, which would be setting a new instance.
            Instruction nextInstr = inst.Next;
            if (nextInstr.OpCode == OpCodes.Initobj)
            {
                //If was a replaced field.
                if (processedLookup.TryGetValue(resolvedOpField, out List<ProcessedSync> psLst))
                {
                    ProcessedSync ps = GetProcessedSync(resolvedOpField, psLst);
                    if (ps == null)
                        return;

                    ILProcessor processor = methodDef.Body.GetILProcessor();

                    VariableDefinition tmpVariableDef = CodegenSession.GeneralHelper.CreateVariable(methodDef, resolvedOpField.FieldType);
                    processor.InsertBefore(inst, processor.Create(OpCodes.Ldloca, tmpVariableDef));
                    processor.InsertBefore(inst, processor.Create(OpCodes.Initobj, resolvedOpField.FieldType));
                    processor.InsertBefore(inst, processor.Create(OpCodes.Ldloc, tmpVariableDef));
                    Instruction newInstr = processor.Create(OpCodes.Call, ps.SetMethodReference);
                    processor.InsertBefore(inst, newInstr);

                    /* Pass in true for as server.
                     * The instruction index is 3 past ld. */
                    Instruction boolTrueInst = processor.Create(OpCodes.Ldc_I4_1);
                    methodDef.Body.Instructions.Insert(instructionIndex + 3, boolTrueInst);

                    processor.Remove(inst);
                    processor.Remove(nextInstr);
                }
            }
        }

        /// <summary>
        /// Creates a call to a SyncHandler.Read and sets value locally.
        /// </summary>
        /// <param name="typeDef"></param>
        /// <param name="syncIndex"></param>
        /// <param name="originalFieldDef"></param>
        /// <param name="syncHandlerGetValueMethodRef"></param>
        private MethodDefinition CreateSyncHandlerRead(TypeDefinition typeDef, int syncIndex, FieldDefinition originalFieldDef, FieldDefinition syncHandlerFieldDef, MethodReference accessorSetMethodRef)
        {
            Instruction jmpGoalInst;
            ILProcessor processor;

            //Get the read sync method, or create it if not present.
            MethodDefinition readSyncMethodDef = typeDef.GetMethod(CodegenSession.ObjectHelper.NetworkBehaviour_ReadSyncVarInternal_MethodRef.Name);
            if (readSyncMethodDef == null)
            {
                readSyncMethodDef = new MethodDefinition(CodegenSession.ObjectHelper.NetworkBehaviour_ReadSyncVarInternal_MethodRef.Name,
                (MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual),
                    typeDef.Module.TypeSystem.Void);

                CodegenSession.GeneralHelper.CreateParameter(readSyncMethodDef, typeof(PooledReader));
                CodegenSession.GeneralHelper.CreateParameter(readSyncMethodDef, typeof(byte));
                readSyncMethodDef.Body.InitLocals = true;

                processor = readSyncMethodDef.Body.GetILProcessor();
                processor.Emit(OpCodes.Ret);

                typeDef.Methods.Add(readSyncMethodDef);
            }
            //Already created. 
            else
            {
                processor = readSyncMethodDef.Body.GetILProcessor();
            }

            ParameterDefinition pooledReaderParameterDef = readSyncMethodDef.Parameters[0];
            ParameterDefinition indexParameterDef = readSyncMethodDef.Parameters[1];
            VariableDefinition nextValueVariableDef;
            List<Instruction> readInsts;

            /* Create a nop instruction placed at the first index of the method.
             * All instructions will be added before this, then the nop will be
             * removed afterwards. This ensures the newer instructions will
             * be above the previous. This let's the IL jump to a previously
             * created read instruction when the latest one fails conditions. */
            Instruction nopPlaceHolderInst = processor.Create(OpCodes.Nop);

            readSyncMethodDef.Body.Instructions.Insert(0, nopPlaceHolderInst);

            /* If there was a previously made read then set jmp goal to the first
             * condition for it. Otherwise set it to the last instruction, which would
             * be a ret. */
            jmpGoalInst = (_lastReadInstruction != null) ? _lastReadInstruction :
                readSyncMethodDef.Body.Instructions[readSyncMethodDef.Body.Instructions.Count - 1];

            //Check index first. if (index != syncIndex) return
            Instruction nextLastReadInstruction = processor.Create(OpCodes.Ldarg, indexParameterDef);
            processor.InsertBefore(jmpGoalInst, nextLastReadInstruction);
            processor.InsertBefore(jmpGoalInst, processor.Create(OpCodes.Ldc_I4, syncIndex));
            processor.InsertBefore(jmpGoalInst, processor.Create(OpCodes.Bne_Un, jmpGoalInst));
            //PooledReader.ReadXXXX()
            readInsts = CodegenSession.ReaderHelper.CreateReadInstructions(processor, readSyncMethodDef, pooledReaderParameterDef,
                 originalFieldDef.FieldType, out nextValueVariableDef);
            if (readInsts == null)
                return null;
            //Add each instruction from CreateRead.
            foreach (Instruction i in readInsts)
                processor.InsertBefore(jmpGoalInst, i);

            //Call accessor with new value and false for asServer
            processor.InsertBefore(jmpGoalInst, processor.Create(OpCodes.Ldarg_0)); //this.
            processor.InsertBefore(jmpGoalInst, processor.Create(OpCodes.Ldloc, nextValueVariableDef));
            processor.InsertBefore(jmpGoalInst, processor.Create(OpCodes.Ldc_I4_0));
            processor.InsertBefore(jmpGoalInst, processor.Create(OpCodes.Call, accessorSetMethodRef));

            processor.InsertBefore(jmpGoalInst, processor.Create(OpCodes.Ret));

            _lastReadInstruction = nextLastReadInstruction;
            processor.Remove(nopPlaceHolderInst);

            return readSyncMethodDef;
        }

        /// <summary>
        /// Returns the ProcessedSync entry for resolvedOpField.
        /// </summary>
        /// <param name="resolvedOpField"></param>
        /// <param name="psLst"></param>
        /// <returns></returns>
        private ProcessedSync GetProcessedSync(FieldReference resolvedOpField, List<ProcessedSync> psLst)
        {
            for (int i = 0; i < psLst.Count; i++)
            {
                if (psLst[i].OriginalFieldReference == resolvedOpField)
                    return psLst[i];
            }

            /* Fall through, not found. */
            CodegenSession.Diagnostics.AddError($"Unable to find user referenced field for {resolvedOpField.Name}.");
            return null;
        }
    }
}