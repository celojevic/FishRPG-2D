using System.IO;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using UnityEngine;
using FishNet.CodeGenerating.Helping;
using FishNet.CodeGenerating.Processing;
using FishNet.CodeGenerating.Helping.Extension;

#if UNITY_2020_2_OR_NEWER
using ILPPInterface = Unity.CompilationPipeline.Common.ILPostProcessing.ILPostProcessor;
#else
using ILPPInterface = FishNet.CodeGenerating.ILCore.Pre2020.ILPostProcessor;
#endif

using FishNet.Broadcast;

namespace FishNet.CodeGenerating.ILCore
{
    public class FishNetILPP : ILPPInterface
    {
        #region Const.
        internal const string RUNTIME_ASSEMBLY_NAME = "FishNet";
        #endregion

#if UNITY_2020_2_OR_NEWER
        public override bool WillProcess(ICompiledAssembly compiledAssembly) => WillProcessCommon(compiledAssembly);
        public override ILPPInterface GetInstance() => this;
        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            AssemblyDefinition assemblyDef = ILCoreHelper.GetAssemblyDefinition(compiledAssembly);
            if (assemblyDef == null)
                return null;

            List<DiagnosticMessage> diagnostics = new List<DiagnosticMessage>();
            if (diagnostics.Count > 0)
            {
                ILCoreHelper.PrintDiagnostics(diagnostics, compiledAssembly, "FishNetILPP");
                return null;
            }
            else
            {
                //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                //sw.Restart();
                ILPostProcessResult result = ProcessCommon(assemblyDef, assemblyDef.MainModule, diagnostics);
                return result; 
            }
        }

#else
        public override string GetClassName() => "FishNetILPP";
        public override bool WillProcess(ICompiledAssembly compiledAssembly, ModuleDefinition moduleDef, List<DiagnosticMessage> diagnostics) => WillProcessCommon(compiledAssembly);
        public override ILPostProcessResult Process(AssemblyDefinition assemblyDef, ModuleDefinition moduleDef, List<DiagnosticMessage> diagnostics) => ProcessCommon(assemblyDef, moduleDef, diagnostics);
#endif

        private ILPostProcessResult ProcessCommon(AssemblyDefinition assemblyDef, ModuleDefinition moduleDef, List<DiagnosticMessage> diagnostics)
        {
            if (!ImportReferences(moduleDef, diagnostics))
                Debug.LogError($"Could not import references for {moduleDef.Name}.");
            CreateDeclaredDelegates(moduleDef, diagnostics);
            CreateDeclaredSerializers(moduleDef, diagnostics);
            CreateIBroadcast(moduleDef, diagnostics);
            CreateNetworkBehaviours(moduleDef, diagnostics);
            CreateGenericReadWriteDelegates(moduleDef, diagnostics);

            return GetPostProcessResult(assemblyDef, diagnostics);
        }

        /// <summary>
        /// Creates delegates for user declared serializers.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <param name="diagnostics"></param>
        private void CreateDeclaredDelegates(ModuleDefinition moduleDef, List<DiagnosticMessage> diagnostics)
        {
            TypeAttributes readWriteExtensionTypeAttr = (TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract);
            List<TypeDefinition> allTypeDefs = moduleDef.Types.ToList();
            foreach (TypeDefinition td in allTypeDefs)
            {
                if (td.Attributes.HasFlag(readWriteExtensionTypeAttr))
                    CustomSerializerProcessor.CreateDelegates(td, moduleDef, diagnostics);
            }
        }

        /// <summary>
        /// Creates serializers for custom types within user declared serializers.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <param name="diagnostics"></param>
        private void CreateDeclaredSerializers(ModuleDefinition moduleDef, List<DiagnosticMessage> diagnostics)
        {
            TypeAttributes readWriteExtensionTypeAttr = (TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract);
            List<TypeDefinition> allTypeDefs = moduleDef.Types.ToList();
            foreach (TypeDefinition td in allTypeDefs)
            {
                if (td.Attributes.HasFlag(readWriteExtensionTypeAttr))
                    CustomSerializerProcessor.CreateSerializers(td, moduleDef, diagnostics);
            }
        }

        /// <summary>
        /// Creaters serializers and calls for IBroadcast.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <param name="diagnostics"></param>
        private void CreateIBroadcast(ModuleDefinition moduleDef, List<DiagnosticMessage> diagnostics)
        {
            HashSet<TypeDefinition> typeDefs = new HashSet<TypeDefinition>();
            foreach (TypeDefinition td in moduleDef.Types)
            {
                TypeDefinition climbTypeDef = td;
                while (climbTypeDef != null)
                {
                    /* Check initial class as well all types within
                     * the class. Then check all of it's base classes. */
                    if (climbTypeDef.ImplementsInterface<IBroadcast>())
                        typeDefs.Add(climbTypeDef);

                    //Add nested. Only going to go a single layer deep.
                    foreach (TypeDefinition nestedTypeDef in td.NestedTypes)
                    {
                        if (nestedTypeDef.ImplementsInterface<IBroadcast>())
                            typeDefs.Add(nestedTypeDef);
                    }

                    //Climb up base classes.
                    if (climbTypeDef.BaseType != null)
                        climbTypeDef = climbTypeDef.BaseType.Resolve();
                    else
                        climbTypeDef = null;
                }
            }

            //Create reader/writers for found typeDefs.
            foreach (TypeDefinition td in typeDefs)
            {
                TypeReference typeRef = moduleDef.ImportReference(td);

                bool canSerialize = GeneralHelper.HasSerializerAndDeserializer(typeRef, true, diagnostics);
                if (!canSerialize)
                    diagnostics.AddError($"Broadcast {td.Name} does not support serialization. Use a supported type or create a custom serializer.");
            }
        }

        /// <summary>
        /// Creates NetworkBehaviour changes.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <param name="diagnostics"></param>
        private void CreateNetworkBehaviours(ModuleDefinition moduleDef, List<DiagnosticMessage> diagnostics)
        {
            //Get all network behaviours to process.
            List<TypeDefinition> networkBehaviourTypeDefs = moduleDef.Types
                .Where(td => td.IsSubclassOf(ObjectHelper.NetworkBehaviour_FullName))
                .ToList();

            /* Remove any networkbehaviour typedefs which are inherited by
             * another networkbehaviour typedef. When a networkbehaviour typedef
             * is processed so are all of the inherited types. */
            for (int i = 0; i < networkBehaviourTypeDefs.Count; i++)
            {
                int entriesRemoved = 0;

                List<TypeDefinition> tdSubClasses = new List<TypeDefinition>();
                TypeDefinition tdClimb = networkBehaviourTypeDefs[i].BaseType.Resolve();
                while (tdClimb != null)
                {
                    tdSubClasses.Add(tdClimb);
                    if (tdClimb.NonNetworkBehaviourBaseType())
                        tdClimb = tdClimb.BaseType.Resolve();
                    else
                        tdClimb = null;
                }
                //No base types to compare.
                if (tdSubClasses.Count == 0)
                    continue;
                //Try to remove every subclass.
                foreach (TypeDefinition tdSub in tdSubClasses)
                {
                    if (networkBehaviourTypeDefs.Remove(tdSub))
                        entriesRemoved++;
                }
                //Subtract entries removed from i since theyre now gone.
                i -= entriesRemoved;
            }

            /* This needs to persist because it holds SyncHandler
             * references for each SyncType. Those
             * SyncHandlers are re-used if there are multiple SyncTypes
             * using the same Type. */
            List<(SyncType, ProcessedSync)> allProcessedSyncs = new List<(SyncType, ProcessedSync)>();

            HashSet<string> allProcessedCallbacks = new HashSet<string>();

            foreach (TypeDefinition typeDef in networkBehaviourTypeDefs)
            {
                moduleDef.ImportReference(typeDef);
                //RPCs are per networkbehaviour + hierarchy and need to be reset.
                int allRpcCount = 0;
                //Callbacks are per networkbehaviour + hierarchy as well.
                allProcessedCallbacks.Clear();
                NetworkBehaviourProcessor.Process(moduleDef, null, typeDef, ref allRpcCount, allProcessedSyncs, allProcessedCallbacks, diagnostics);

                //Register rpc count on each script that inherits from network behaviour.
                TypeDefinition climbTypeDef = typeDef;
                while (climbTypeDef != null)
                {
                    NetworkBehaviourProcessor.CreateRegisterRpcCount(climbTypeDef, allRpcCount, diagnostics);
                    if (climbTypeDef.BaseType != null && climbTypeDef.BaseType.FullName != ObjectHelper.NetworkBehaviour_FullName)
                        climbTypeDef = climbTypeDef.BaseType.Resolve();
                    else
                        climbTypeDef = null;
                }
            }
        }

        /// <summary>
        /// Returns if assembly can be processed.
        /// </summary>
        /// <param name="compiledAssembly"></param>
        /// <returns></returns>
        private bool WillProcessCommon(ICompiledAssembly compiledAssembly)
        {
            List<string> disallowedModules = new List<string>()
            {
                "Unity.FishNet.CodeGen"
            };
            AssemblyDefinition assemblyDef = ILCoreHelper.GetAssemblyDefinition(compiledAssembly);
            if (assemblyDef == null)
            {
                Debug.LogError($"Cannot read assembly definition: {compiledAssembly.Name}");
                return false;
            }
            ModuleDefinition moduleDef = assemblyDef.MainModule;
            if (moduleDef == null)
            {
                Debug.LogError($"Cannot get the main module for {assemblyDef.Name}.");
                return false;
            }

            if (disallowedModules.Contains(moduleDef.Assembly.Name.Name))
                return false;

            bool referencesFishNet = compiledAssembly.References.Any(filePath => Path.GetFileNameWithoutExtension(filePath) == RUNTIME_ASSEMBLY_NAME);
            //If can process make sure needed references import.
            if (referencesFishNet)
            {
                List<DiagnosticMessage> diagnostics = new List<DiagnosticMessage>();
                if (!ImportReferences(moduleDef, diagnostics))
                {
                    Debug.LogError($"Could not import references for {moduleDef.Name}.");
                    referencesFishNet = false;
                }
            }

            return referencesFishNet;
        }

        /// <summary>
        /// Creates generic delegates for all read and write methods.
        /// </summary>
        /// <param name="moduleDef"></param>
        /// <param name="diagnostics"></param>
        private void CreateGenericReadWriteDelegates(ModuleDefinition moduleDef, List<DiagnosticMessage> diagnostics)
        {
            WriterHelper.CreateGenericDelegates(diagnostics);
            ReaderHelper.CreateGenericDelegates(diagnostics);

            /* This is breaking on imported references. Would like to get this working eventually
             * so I don't have to manually make static writers for included types. */
            //foreach (var item in WriterHelper._instancedWriterMethods.Values)
            //    GenericWriterHelper.CreateInstancedStaticWrite(item, diagnostics);
        }

        /// <summary>
        /// Returns results. To be called after all creates.
        /// </summary>
        /// <param name="diagnostics"></param>
        /// <returns></returns>
        private ILPostProcessResult GetPostProcessResult(AssemblyDefinition assemblyDef, List<DiagnosticMessage> diagnostics)
        {
            MemoryStream pe = new MemoryStream();
            MemoryStream pdb = new MemoryStream();
            WriterParameters writerParameters = new WriterParameters
            {
                SymbolWriterProvider = new PortablePdbWriterProvider(),
                SymbolStream = pdb,
                WriteSymbols = true
            };

            assemblyDef.Write(pe, writerParameters);
            return new ILPostProcessResult(new InMemoryAssembly(pe.ToArray(), pdb.ToArray()), diagnostics);
        }

        /// <summary>
        /// Imports references into all helpers.
        /// </summary>
        /// <param name="moduleDef"></param>
        private bool ImportReferences(ModuleDefinition moduleDef, List<DiagnosticMessage> diagnostics)
        {
            if (!ReaderHelper.ImportReferences(moduleDef, diagnostics))
                return false;
            if (!GeneralHelper.ImportReferences(moduleDef))
                return false;
            if (!WriterHelper.ImportReferences(moduleDef, diagnostics))
                return false;
            if (!TransportHelper.ImportReferences(moduleDef))
                return false;
            if (!ObjectHelper.ImportReferences(moduleDef))
                return false;
            if (!ConnectionHelper.ImportReferences(moduleDef))
                return false;
            if (!WriterGenerator.ImportReferences(moduleDef))
                return false;
            if (!ReaderGenerator.ImportReferences(moduleDef))
                return false;
            if (!GenericWriterHelper.ImportReferences(moduleDef))
                return false;
            if (!GenericReaderHelper.ImportReferences(moduleDef))
                return false;
            if (!SyncHandlerGenerator.ImportReferences(moduleDef))
                return false;

            return true;
        }


    }
}