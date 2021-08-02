
using FishNet.CodeGenerating.Helping;
using FishNet.CodeGenerating.Helping.Extension;
using FishNet.Object;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;
using Unity.CompilationPipeline.Common.Diagnostics;
using UnityEngine;

namespace FishNet.CodeGenerating.Processing
{
    internal static class NetworkBehaviourProcessor
    {
        #region Misc.
        /// <summary>
        /// Methods modified or iterated during weaving.
        /// </summary>
        internal static List<MethodDefinition> ModifiedMethodDefinitions = new List<MethodDefinition>();
        #endregion

        #region Const.
        internal const string NETWORKINITIALIZE_INTERNAL_NAME = "NetworkInitialize___Internal";
        private static MethodAttributes PUBLIC_VIRTUAL_ATTRIBUTES = (MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);
        #endregion

        internal static void Process(ModuleDefinition moduleDef, TypeDefinition firstTypeDef, TypeDefinition typeDef, ref int allRpcCount, List<(SyncType, ProcessedSync)> allProcessedSyncs, HashSet<string> allProcessedCallbacks, List<DiagnosticMessage> diagnostics)
        {
            //Disallow nested network behaviours.
            if (typeDef.NestedTypes
                .Where(t => t.IsSubclassOf(ObjectHelper.NetworkBehaviour_FullName))
                .ToList().Count > 0)
            {
                diagnostics.AddError($"{typeDef.FullName} contains nested NetworkBehaviours. These are not supported.");
                return;
            }

            //First typeDef to process. Will always be child most.
            if (firstTypeDef == null)
            {
                firstTypeDef = typeDef;

                if (!MakeAwakeMethodsPublicVirtual(typeDef, diagnostics))
                    return;
                MethodDefinition childMostAwakeMethodDef = GetChildMostAwakeMethodDefinition(typeDef);
                List<MethodDefinition> networkInitializeMethodDefs = CreateNetworkInitializeMethodDefinitions(typeDef);
                CreateNetworkInitializeBaseCalls(typeDef, networkInitializeMethodDefs, 0);
                CreateFirstNetworkInitializeCall(typeDef, childMostAwakeMethodDef, networkInitializeMethodDefs[0]);
            }

            NetworkBehaviourQolAttributeProcessor.Process(typeDef, diagnostics);
            NetworkBehaviourRpcProcessor.Process(typeDef, ref allRpcCount, diagnostics);
            NetworkBehaviourCallbackProcessor.Process(firstTypeDef, typeDef, allProcessedCallbacks, diagnostics);
            NetworkBehaviourSyncProcessor.Process(moduleDef, typeDef, allProcessedSyncs, diagnostics);

            //Also process base types.
            if (typeDef.NonNetworkBehaviourBaseType())
                Process(moduleDef, firstTypeDef, typeDef.BaseType.Resolve(), ref allRpcCount, allProcessedSyncs, allProcessedCallbacks, diagnostics);

            List<MethodDefinition> modifiableMethods = GetModifiableMethods(typeDef);
            NetworkBehaviourSyncProcessor.ReplaceGetSets(modifiableMethods, allProcessedSyncs, diagnostics);
        }


        /// <summary>
        /// Gets the top-most parent away method.
        /// </summary>
        /// <param name="typeDef"></param>
        /// <returns></returns>
        private static MethodDefinition GetChildMostAwakeMethodDefinition(TypeDefinition typeDef)
        {
            while (typeDef != null)
            {
                MethodDefinition methodDef = typeDef.GetMethod(ObjectHelper.AWAKE_METHOD_NAME);
                if (methodDef != null)
                    return methodDef;

                if (typeDef.NonNetworkBehaviourBaseType())
                    typeDef = typeDef.BaseType.Resolve();
                else
                    typeDef = null;
            }

            //Fall through.
            return null;
        }

        /// <summary>
        /// Creates an 'InitializeNetwork' method which is called by the childmost class to initialize scripts on Awake.
        /// </summary>
        /// <param name="firstTypeDef"></param>
        /// <returns></returns>
        private static List<MethodDefinition> CreateNetworkInitializeMethodDefinitions(TypeDefinition firstTypeDef)
        {
            List<MethodDefinition> methodDefs = new List<MethodDefinition>();

            TypeDefinition typeDef = firstTypeDef;
            while (typeDef != null)
            {
                //Create new public virtual method and add it to typedef.
                MethodDefinition md = new MethodDefinition(NETWORKINITIALIZE_INTERNAL_NAME,
                    PUBLIC_VIRTUAL_ATTRIBUTES,
                    typeDef.Module.TypeSystem.Void);
                typeDef.Methods.Add(md);
                //Emit ret into new method.
                ILProcessor processor = md.Body.GetILProcessor();
                processor.Emit(OpCodes.Ret);
                //Add to created method refs
                methodDefs.Add(md);

                if (typeDef.NonNetworkBehaviourBaseType())
                    typeDef = typeDef.BaseType.Resolve();
                else
                    typeDef = null;
            }

            return methodDefs;
        }

        /// <summary>
        /// Creates Awake method for and all parents of typeDef using the parentMostAwakeMethodDef as a template.
        /// </summary>
        /// <param name="typeDef"></param>
        /// <param name="parentMostAwakeMethodDef"></param>
        /// <returns>True if successful.</returns>
        private static bool MakeAwakeMethodsPublicVirtual(TypeDefinition typeDef, List<DiagnosticMessage> diagnostics)
        {
            while (typeDef != null)
            {
                MethodDefinition tmpMd = typeDef.GetMethod(ObjectHelper.AWAKE_METHOD_NAME);
                if (tmpMd != null)
                {
                    if (tmpMd.ReturnType != typeDef.Module.TypeSystem.Void)
                    {
                        diagnostics.AddError($"IEnumerator Awake methods are not supported within NetworkBehaviours.");
                        return false;
                    }
                    tmpMd.Attributes = PUBLIC_VIRTUAL_ATTRIBUTES;
                }

                if (typeDef.NonNetworkBehaviourBaseType())
                    typeDef = typeDef.BaseType.Resolve();
                else
                    typeDef = null;
            }

            return true;
        }

        /// <summary>
        /// Makes all NetworkInitialize methods within typeDef call their inherited siblings.
        /// </summary>
        /// <param name="typeDef"></param>
        internal static void CreateNetworkInitializeBaseCalls(TypeDefinition typeDef, List<MethodDefinition> networkInitializeMethodDefs, int lstIndex)
        {
            //On last method, nothing up to call.
            if (lstIndex >= (networkInitializeMethodDefs.Count - 1))
                return;

            MethodDefinition md = networkInitializeMethodDefs[lstIndex];
            //MethodRef to call for base call.
            MethodReference mr = typeDef.Module.ImportReference(networkInitializeMethodDefs[lstIndex + 1]);

            ILProcessor processor = md.Body.GetILProcessor();
            //Create instructions for base call.
            List<Instruction> instructions = new List<Instruction>();
            instructions.Add(processor.Create(OpCodes.Ldarg_0)); //this.
            instructions.Add(processor.Create(OpCodes.Call, mr));

            processor.InsertFirst(instructions);

            //Call again with typeDef from next method.
            if (typeDef.NonNetworkBehaviourBaseType())
                CreateNetworkInitializeBaseCalls(typeDef.BaseType.Resolve(), networkInitializeMethodDefs, lstIndex + 1);
        }


        /// <summary>
        /// Makes all Awake methods within typeDef and base classes public and virtual.
        /// </summary>
        /// <param name="typeDef"></param>
        internal static void CreateFirstNetworkInitializeCall(TypeDefinition typeDef, MethodDefinition firstUserAwakeMethodDef, MethodDefinition firstNetworkInitializeMethodDef)
        {
            ILProcessor processor;
            //Get awake for current method.
            MethodDefinition thisAwakeMethodDef = typeDef.GetMethod(ObjectHelper.AWAKE_METHOD_NAME);
            bool created = false;

            //If no awake then make one.
            if (thisAwakeMethodDef == null)
            {
                created = true;

                thisAwakeMethodDef = new MethodDefinition(ObjectHelper.AWAKE_METHOD_NAME, PUBLIC_VIRTUAL_ATTRIBUTES,
                    typeDef.Module.TypeSystem.Void);
                thisAwakeMethodDef.Body.InitLocals = true;
                typeDef.Methods.Add(thisAwakeMethodDef);               

                processor = thisAwakeMethodDef.Body.GetILProcessor();
                processor.Emit(OpCodes.Ret);                                
            }

            //MethodRefs for networkinitialize and awake.
            MethodReference networkInitializeMethodRef = typeDef.Module.ImportReference(firstNetworkInitializeMethodDef);

            processor = thisAwakeMethodDef.Body.GetILProcessor();
            //Create instructions for base call.
            List<Instruction> instructions = new List<Instruction>();
            instructions.Add(processor.Create(OpCodes.Ldarg_0)); //this.
            instructions.Add(processor.Create(OpCodes.Call, networkInitializeMethodRef));

            /* If awake was created then make a call to the users
             * first awake. There's no reason to do this if awake
             * already existed because the user would have control
             * over making that call. */
            if (created && firstUserAwakeMethodDef != null)
            {
                MethodReference baseAwakeMethodRef = typeDef.Module.ImportReference(firstUserAwakeMethodDef);
                instructions.Add(processor.Create(OpCodes.Ldarg_0));//this.
                instructions.Add(processor.Create(OpCodes.Call, baseAwakeMethodRef));
            }

            processor.InsertFirst(instructions);
        }



        /// <summary>
        /// Creates a call to NetworkBehaviour to register RPC count.
        /// </summary>
        internal static void CreateRegisterRpcCount(TypeDefinition typeDef, int allRpcCount, List<DiagnosticMessage> diagnostics)
        {
            MethodDefinition methodDef = typeDef.GetMethod(NETWORKINITIALIZE_INTERNAL_NAME);

            //Insert at the beginning to ensure user code doesn't return out of it.
            ILProcessor processor = methodDef.Body.GetILProcessor();

            List<Instruction> instructions = new List<Instruction>();
            instructions.Add(processor.Create(OpCodes.Ldarg_0)); //this.
            instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)allRpcCount));
            instructions.Add(processor.Create(OpCodes.Call, ObjectHelper.NetworkBehaviour_SetRpcMethodCountInternal_MethodRef));


            //SetGivenName debug.
            //System.Type networkBehaviourType = typeof(NetworkBehaviour);
            //TypeReference trr = typeDef.module.ImportReference(networkBehaviourType);
            //MethodDefinition mrd = trr.Resolve().GetMethod("SetGivenName");
            //MethodReference mrr = typeDef.module.ImportReference(mrd);
            //instructions.Add(processor.Create(OpCodes.Ldarg_0));
            //instructions.Add(processor.Create(OpCodes.Ldstr, typeDef.Name));
            //instructions.Add(processor.Create(OpCodes.Call, mrr));


            processor.InsertFirst(instructions);
        }


        /// <summary>
        /// Returns methods which may be modified by code generation.
        /// </summary>
        /// <param name="typeDef"></param>
        /// <returns></returns>
        private static List<MethodDefinition> GetModifiableMethods(TypeDefinition typeDef)
        {
            List<MethodDefinition> results = new List<MethodDefinition>();
            //Typical methods.
            foreach (MethodDefinition methodDef in typeDef.Methods)
            {
                if (methodDef.Name == ".cctor")
                    continue;
                if (methodDef.IsConstructor)
                    continue;

                results.Add(methodDef);
            }
            //Accessors.
            foreach (PropertyDefinition propertyDef in typeDef.Properties)
            {
                if (propertyDef.GetMethod != null)
                    results.Add(propertyDef.GetMethod);
                if (propertyDef.SetMethod != null)
                    results.Add(propertyDef.SetMethod);                
            }

            return results;
        }

    }
}