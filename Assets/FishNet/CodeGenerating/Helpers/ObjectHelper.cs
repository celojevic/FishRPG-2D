using FishNet.Broadcast;
using FishNet.CodeGenerating.Helping.Extension;
using FishNet.CodeGenerating.Processing;
using FishNet.Object;
using FishNet.Object.Helping;
using FishNet.Object.Synchronizing;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace FishNet.CodeGenerating.Helping
{
    internal class ObjectHelper
    {
        #region Reflection references.
        internal string NetworkBehaviour_FullName;
        internal string IBroadcast_FullName;
        internal string SyncList_FullName;
        private MethodReference NetworkBehaviour_CreateServerRpcDelegate_MethodRef;
        private MethodReference NetworkBehaviour_CreateObserversRpcDelegate_MethodRef;
        private MethodReference NetworkBehaviour_CreateTargetRpcDelegate_MethodRef;
        private MethodReference Networkbehaviour_ServerRpcDelegateDelegateConstructor_MethodRef;
        private MethodReference Networkbehaviour_ClientRpcDelegateDelegateConstructor_MethodRef;
        private MethodReference NetworkBehaviour_SendServerRpc_MethodRef;
        private MethodReference NetworkBehaviour_SendObserversRpc_MethodRef;
        private MethodReference NetworkBehaviour_SendTargetRpc_MethodRef;
        private MethodReference NetworkBehaviour_IsClient_MethodRef;
        private MethodReference NetworkBehaviour_IsServer_MethodRef;
        private MethodReference NetworkBehaviour_IsHost_MethodRef;
        private MethodReference NetworkBehaviour_IsOwner_MethodRef;
        private MethodReference NetworkBehaviour_CompareOwner_MethodRef;
        internal MethodReference NetworkBehaviour_Owner_MethodRef;
        private MethodReference NetworkBehaviour_OwnerIsValid_MethodRef;
        internal MethodReference NetworkBehaviour_ReadSyncVarInternal_MethodRef;
        internal MethodReference NetworkBehaviour_UsingOnStartServer_MethodRef;
        internal MethodReference NetworkBehaviour_UsingOnStopServerInternal_MethodRef;
        internal MethodReference NetworkBehaviour_UsingOnOwnershipServerInternal_MethodRef;
        internal MethodReference NetworkBehaviour_UsingOnSpawnServerInternal_MethodRef;
        internal MethodReference NetworkBehaviour_UsingOnDespawnServerInternal_MethodRef;
        internal MethodReference NetworkBehaviour_UsingOnStartClientInternal_MethodRef;
        internal MethodReference NetworkBehaviour_UsingOnStopClientInternal_MethodRef;
        internal MethodReference NetworkBehaviour_UsingOnOwnershipClientInternal_MethodRef;
        internal MethodReference NetworkBehaviour_SetRpcMethodCountInternal_MethodRef;
        #endregion

        #region Const.
        internal const string AWAKE_METHOD_NAME = "Awake";
        #endregion

        internal bool ImportReferences()
        {
            Type networkBehaviourType = typeof(NetworkBehaviour);
            NetworkBehaviour_FullName = networkBehaviourType.FullName;
            CodegenSession.Module.ImportReference(networkBehaviourType);

            Type ibroadcastType = typeof(IBroadcast);
            CodegenSession.Module.ImportReference(ibroadcastType);
            IBroadcast_FullName = ibroadcastType.FullName;

            Type syncListType = typeof(SyncList<>);
            CodegenSession.Module.ImportReference(syncListType);
            SyncList_FullName = syncListType.FullName;

            //ServerRpcDelegate and ClientRpcDelegate constructors.
            Networkbehaviour_ServerRpcDelegateDelegateConstructor_MethodRef = CodegenSession.Module.ImportReference(typeof(ServerRpcDelegate).GetConstructors().First());
            Networkbehaviour_ClientRpcDelegateDelegateConstructor_MethodRef = CodegenSession.Module.ImportReference(typeof(ClientRpcDelegate).GetConstructors().First());

            foreach (MethodInfo methodInfo in networkBehaviourType.GetMethods((BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)))
            {
                //CreateDelegates.
                if (methodInfo.Name == nameof(NetworkBehaviour.CreateServerRpcDelegateInternal))
                    NetworkBehaviour_CreateServerRpcDelegate_MethodRef = CodegenSession.Module.ImportReference(methodInfo);
                else if (methodInfo.Name == nameof(NetworkBehaviour.CreateObserversRpcDelegateInternal))
                    NetworkBehaviour_CreateObserversRpcDelegate_MethodRef = CodegenSession.Module.ImportReference(methodInfo);
                else if (methodInfo.Name == nameof(NetworkBehaviour.CreateTargetRpcDelegateInternal))
                    NetworkBehaviour_CreateTargetRpcDelegate_MethodRef = CodegenSession.Module.ImportReference(methodInfo);
                //SendRpcs.
                else if (methodInfo.Name == nameof(NetworkBehaviour.SendServerRpc))
                    NetworkBehaviour_SendServerRpc_MethodRef = CodegenSession.Module.ImportReference(methodInfo);
                else if (methodInfo.Name == nameof(NetworkBehaviour.SendObserversRpc))
                    NetworkBehaviour_SendObserversRpc_MethodRef = CodegenSession.Module.ImportReference(methodInfo);
                else if (methodInfo.Name == nameof(NetworkBehaviour.SendTargetRpc))
                    NetworkBehaviour_SendTargetRpc_MethodRef = CodegenSession.Module.ImportReference(methodInfo);
                //NetworkObject/NetworkBehaviour Callbacks.
                else if (methodInfo.Name == NetworkBehaviourCallbackProcessor.USING_ONSTARTSERVER_INTERNAL_NAME)
                    NetworkBehaviour_UsingOnStartServer_MethodRef = CodegenSession.Module.ImportReference(methodInfo);

                else if (methodInfo.Name == NetworkBehaviourCallbackProcessor.USING_ONSTOPSERVER_INTERNAL_NAME)
                    NetworkBehaviour_UsingOnStopServerInternal_MethodRef = CodegenSession.Module.ImportReference(methodInfo);

                else if (methodInfo.Name == NetworkBehaviourCallbackProcessor.USING_ONOWNERSHIPSERVER_INTERNAL_NAME)
                    NetworkBehaviour_UsingOnOwnershipServerInternal_MethodRef = CodegenSession.Module.ImportReference(methodInfo);

                else if (methodInfo.Name == NetworkBehaviourCallbackProcessor.USING_ONSPAWNSERVER_INTERNAL_NAME)
                    NetworkBehaviour_UsingOnSpawnServerInternal_MethodRef = CodegenSession.Module.ImportReference(methodInfo);

                else if (methodInfo.Name == NetworkBehaviourCallbackProcessor.USING_ONDESPAWNSERVER_INTERNAL_NAME)
                    NetworkBehaviour_UsingOnDespawnServerInternal_MethodRef = CodegenSession.Module.ImportReference(methodInfo);

                else if (methodInfo.Name == NetworkBehaviourCallbackProcessor.USING_ONSTARTCLIENT_INTERNAL_NAME)
                    NetworkBehaviour_UsingOnStartClientInternal_MethodRef = CodegenSession.Module.ImportReference(methodInfo);

                else if (methodInfo.Name == NetworkBehaviourCallbackProcessor.USING_ONSTOPCLIENT_INTERNAL_NAME)
                    NetworkBehaviour_UsingOnStopClientInternal_MethodRef = CodegenSession.Module.ImportReference(methodInfo);

                else if (methodInfo.Name == NetworkBehaviourCallbackProcessor.USING_ONOWNERSHIPCLIENT_INTERNAL_NAME)
                    NetworkBehaviour_UsingOnOwnershipClientInternal_MethodRef = CodegenSession.Module.ImportReference(methodInfo);
                //Misc.
                else if (methodInfo.Name == nameof(NetworkBehaviour.CompareOwner))
                    NetworkBehaviour_CompareOwner_MethodRef = CodegenSession.Module.ImportReference(methodInfo);
                else if (methodInfo.Name == nameof(NetworkBehaviour.ReadSyncVarInternal))
                    NetworkBehaviour_ReadSyncVarInternal_MethodRef = CodegenSession.Module.ImportReference(methodInfo);
                else if (methodInfo.Name == nameof(NetworkBehaviour.SetRpcMethodCountInternal))
                    NetworkBehaviour_SetRpcMethodCountInternal_MethodRef = CodegenSession.Module.ImportReference(methodInfo);
            }

            foreach (PropertyInfo propertyInfo in networkBehaviourType.GetProperties())
            {
                //Server/Client states.
                if (propertyInfo.Name == nameof(NetworkBehaviour.IsClient))
                    NetworkBehaviour_IsClient_MethodRef = CodegenSession.Module.ImportReference(propertyInfo.GetMethod);
                else if (propertyInfo.Name == nameof(NetworkBehaviour.IsServer))
                    NetworkBehaviour_IsServer_MethodRef = CodegenSession.Module.ImportReference(propertyInfo.GetMethod);
                else if (propertyInfo.Name == nameof(NetworkBehaviour.IsHost))
                    NetworkBehaviour_IsHost_MethodRef = CodegenSession.Module.ImportReference(propertyInfo.GetMethod);
                else if (propertyInfo.Name == nameof(NetworkBehaviour.IsOwner))
                    NetworkBehaviour_IsOwner_MethodRef = CodegenSession.Module.ImportReference(propertyInfo.GetMethod);
                //Owner.
                else if (propertyInfo.Name == nameof(NetworkBehaviour.Owner))
                    NetworkBehaviour_Owner_MethodRef = CodegenSession.Module.ImportReference(propertyInfo.GetMethod);
                else if (propertyInfo.Name == nameof(NetworkBehaviour.OwnerIsValid))
                    NetworkBehaviour_OwnerIsValid_MethodRef = CodegenSession.Module.ImportReference(propertyInfo.GetMethod);
            }

            return true;
        }

        /// <summary>
        /// Returnsthe child most Awake by iterating up childMostTypeDef.
        /// </summary>
        /// <param name="childMostTypeDef"></param>
        /// <param name="created"></param>
        /// <returns></returns>
        internal MethodDefinition GetAwakeMethodDefinition(TypeDefinition typeDef)
        {
            return typeDef.GetMethod(AWAKE_METHOD_NAME);
        }

        /// <summary>
        /// Creates a RPC delegate for rpcType.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="originalMethodDef"></param>
        /// <param name="readerMethodDef"></param>
        /// <param name="rpcType"></param>
        internal void CreateRpcDelegate(ILProcessor processor, MethodDefinition originalMethodDef, MethodDefinition readerMethodDef, RpcType rpcType, int allRpcCount)
        {
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldc_I4, allRpcCount);
            /* Create delegate and call NetworkBehaviour method. */
            processor.Emit(OpCodes.Ldnull);
            processor.Emit(OpCodes.Ldftn, readerMethodDef);
            //Server.
            if (rpcType == RpcType.Server)
            {
                processor.Emit(OpCodes.Newobj, Networkbehaviour_ServerRpcDelegateDelegateConstructor_MethodRef);
                processor.Emit(OpCodes.Call, NetworkBehaviour_CreateServerRpcDelegate_MethodRef);
            }
            //Observers.
            else if (rpcType == RpcType.Observers)
            {
                processor.Emit(OpCodes.Newobj, Networkbehaviour_ClientRpcDelegateDelegateConstructor_MethodRef);
                processor.Emit(OpCodes.Call, NetworkBehaviour_CreateObserversRpcDelegate_MethodRef);
            }
            //Target
            else if (rpcType == RpcType.Target)
            {
                processor.Emit(OpCodes.Newobj, Networkbehaviour_ClientRpcDelegateDelegateConstructor_MethodRef);
                processor.Emit(OpCodes.Call, NetworkBehaviour_CreateTargetRpcDelegate_MethodRef);
            }
        }

        /// <summary>
        /// Creates a call to SendServerRpc on NetworkBehaviour.
        /// </summary>
        /// <param name="writerVariableDef"></param>
        /// <param name="channel"></param>
        internal void CreateSendServerRpc(ILProcessor processor, int methodHash, VariableDefinition writerVariableDef, VariableDefinition channelVariableDef)
        {
            CreateSendRpcCommon(processor, methodHash, writerVariableDef, channelVariableDef);
            //Call NetworkBehaviour.
            processor.Emit(OpCodes.Call, NetworkBehaviour_SendServerRpc_MethodRef);
        }

        /// <summary>
        /// Creates a call to SendObserversRpc on NetworkBehaviour.
        /// </summary>
        /// <param name="writerVariableDef"></param>
        /// <param name="channel"></param>
        internal void CreateSendObserversRpc(ILProcessor processor, int methodHash, VariableDefinition writerVariableDef, VariableDefinition channelVariableDef)
        {
            CreateSendRpcCommon(processor, methodHash, writerVariableDef, channelVariableDef);
            //Call NetworkBehaviour.
            processor.Emit(OpCodes.Call, NetworkBehaviour_SendObserversRpc_MethodRef);
        }
        /// <summary>
        /// Creates a call to SendTargetRpc on NetworkBehaviour.
        /// </summary>
        /// <param name="writerVariableDef"></param>
        internal void CreateSendTargetRpc(ILProcessor processor, int methodHash, VariableDefinition writerVariableDef, VariableDefinition channelVariableDef, ParameterDefinition connectionParameterDef)
        {
            CreateSendRpcCommon(processor, methodHash, writerVariableDef, channelVariableDef);
            //Reference to NetworkConnection.
            processor.Emit(OpCodes.Ldarg, connectionParameterDef);
            //Call NetworkBehaviour.
            processor.Emit(OpCodes.Call, NetworkBehaviour_SendTargetRpc_MethodRef);
        }

        /// <summary>
        /// Writes common properties that all SendRpc methods use.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="methodHash"></param>
        /// <param name="writerVariableDef"></param>
        /// <param name="channelVariableDef"></param>
        private void CreateSendRpcCommon(ILProcessor processor, int methodHash, VariableDefinition writerVariableDef, VariableDefinition channelVariableDef)
        {
            processor.Emit(OpCodes.Ldarg_0); // argument: this
            //Hash argument. 
            processor.Emit(OpCodes.Ldc_I4, methodHash);
            //reference to PooledWriter.
            processor.Emit(OpCodes.Ldloc, writerVariableDef);
            //reference to Channel.
            processor.Emit(OpCodes.Ldloc, channelVariableDef);
        }
        /// <summary>
        /// Creates exit method condition if local client is not owner.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="asServer"></param>
        /// <param name="warn"></param>
        internal void CreateLocalClientIsOwnerCheck(ILProcessor processor, bool warn)
        {
            /* This is placed after the if check.
             * Should the if check pass then code
             * jumps to this instruction. */
            Instruction endIf = processor.Create(OpCodes.Nop);

            processor.Emit(OpCodes.Ldarg_0); //argument: this
            //If !base.IsOwner endIf.
            processor.Emit(OpCodes.Call, NetworkBehaviour_IsOwner_MethodRef);
            processor.Emit(OpCodes.Brtrue, endIf);
            //If warning then also append warning text.
            if (warn)
                CodegenSession.GeneralHelper.CreateDebugWarning(processor, "Cannot complete action because you are not the owner of this object.");
            //Return block.
            processor.Emit(OpCodes.Ret);

            //After if statement, jumped to when successful check.
            processor.Append(endIf);
        }
        /// <summary>
        /// Creates exit method condition if remote client is not owner.
        /// </summary>
        /// <param name="processor"></param>
        internal void CreateRemoteClientIsOwnerCheck(ILProcessor processor, ParameterDefinition connectionParameterDef)
        {
            /* This is placed after the if check.
             * Should the if check pass then code
             * jumps to this instruction. */
            Instruction endIf = processor.Create(OpCodes.Nop);

            processor.Emit(OpCodes.Ldarg_0); //argument: this
            //If !base.IsOwner endIf.
            processor.Emit(OpCodes.Ldarg, connectionParameterDef);
            processor.Emit(OpCodes.Call, NetworkBehaviour_CompareOwner_MethodRef);
            processor.Emit(OpCodes.Brtrue, endIf);
            //Return block.
            processor.Emit(OpCodes.Ret);

            //After if statement, jumped to when successful check.
            processor.Append(endIf);
        }

        /// <summary>
        /// Creates exit method condition if not client.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="retInstruction"></param>
        /// <param name="warn"></param>
        internal void CreateIsClientCheck(ILProcessor processor, MethodDefinition methodDef, bool warn, bool insertFirst)
        {
            /* This is placed after the if check.
             * Should the if check pass then code
             * jumps to this instruction. */
            Instruction endIf = processor.Create(OpCodes.Nop);

            List<Instruction> instructions = new List<Instruction>();
            instructions.Add(processor.Create(OpCodes.Ldarg_0)); //argument: this
            //If (!base.IsClient)
            instructions.Add(processor.Create(OpCodes.Call, NetworkBehaviour_IsClient_MethodRef));
            instructions.Add(processor.Create(OpCodes.Brtrue, endIf));
            //If warning then also append warning text.
            if (warn)
                instructions.AddRange(
                    CodegenSession.GeneralHelper.CreateDebugWarningInstructions(processor, "Cannot complete action because client is not active. This may also occur if the object is not yet initialized or if it does not contain a NetworkObject component.")
                    );
            //Add return.
            instructions.AddRange(CreateReturnDefault(processor, methodDef));
            //After if statement, jumped to when successful check.
            instructions.Add(endIf);

            if (insertFirst)
            {
                processor.InsertFirst(instructions);
            }
            else
            {
                foreach (Instruction inst in instructions)
                    processor.Append(inst);
            }
        }


        /// <summary>
        /// Creates exit method condition if not server.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="retInstruction"></param>
        /// <param name="warn"></param>
        internal void CreateIsServerCheck(ILProcessor processor, MethodDefinition methodDef, bool warn, bool insertFirst)
        {
            /* This is placed after the if check.
            * Should the if check pass then code
            * jumps to this instruction. */
            Instruction endIf = processor.Create(OpCodes.Nop);

            List<Instruction> instructions = new List<Instruction>();
            instructions.Add(processor.Create(OpCodes.Ldarg_0)); //argument: this
            //If (!base.IsServer)
            instructions.Add(processor.Create(OpCodes.Call, NetworkBehaviour_IsServer_MethodRef));
            instructions.Add(processor.Create(OpCodes.Brtrue, endIf));
            //If warning then also append warning text.
            if (warn)
                instructions.AddRange(
                    CodegenSession.GeneralHelper.CreateDebugWarningInstructions(processor, "Cannot complete action because server is not active. This may also occur if the object is not yet initialized or if it does not contain a NetworkObject component.")
                    );
            //Add return.
            instructions.AddRange(CreateReturnDefault(processor, methodDef));
            //After if statement, jumped to when successful check.
            instructions.Add(endIf);

            if (insertFirst)
            {
                processor.InsertFirst(instructions);
            }
            else
            {
                foreach (Instruction inst in instructions)
                    processor.Append(inst);
            }
        }

        /// <summary>
        /// Creates a return using the ReturnType for methodDef.
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="methodDef"></param>
        /// <returns></returns>
        private List<Instruction> CreateReturnDefault(ILProcessor processor, MethodDefinition methodDef)
        {
            List<Instruction> instructions = new List<Instruction>();
            //If requires a value return.
            if (methodDef.ReturnType != methodDef.Module.TypeSystem.Void)
            {
                //Import type first.
                methodDef.Module.ImportReference(methodDef.ReturnType);
                VariableDefinition vd = CodegenSession.GeneralHelper.CreateVariable(methodDef, methodDef.ReturnType);
                instructions.Add(processor.Create(OpCodes.Ldloca_S, vd));
                instructions.Add(processor.Create(OpCodes.Initobj, vd.VariableType));
                instructions.Add(processor.Create(OpCodes.Ldloc, vd));
            }
            instructions.Add(processor.Create(OpCodes.Ret));

            return instructions;
        }
    }
}