#if !UNITY_2020_2_OR_NEWER
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Assembly = System.Reflection.Assembly;
using ILPPInterface = FishNet.CodeGenerating.ILCore.Pre2020.ILPostProcessor;


namespace FishNet.CodeGenerating.ILCore.Pre2020
{

    internal static class ILPostProcessProgram
    {
        private static ILPostProcessor[] _ilPostProcessors;

        [InitializeOnLoadMethod]
        private static void OnInitializeOnLoad()
        {
            CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;
            _ilPostProcessors = FindAllPostProcessors();
        }

        private static ILPostProcessor[] FindAllPostProcessors()
        {
            TypeCache.TypeCollection typesDerivedFrom = TypeCache.GetTypesDerivedFrom<ILPostProcessor>();
            List<ILPostProcessor> localILPostProcessors = new List<ILPostProcessor>(typesDerivedFrom.Count);

            foreach (Type typeCollection in typesDerivedFrom)
            {
                try
                {
                    localILPostProcessors.Add((ILPostProcessor)Activator.CreateInstance(typeCollection));
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Could not create {nameof(ILPostProcessor)} ({typeCollection.FullName}):{Environment.NewLine}{exception.StackTrace}");
                }
            }

            // Default sort by type full name
            localILPostProcessors.Sort((left, right) => string.Compare(left.GetType().FullName, right.GetType().FullName, StringComparison.Ordinal));

            return localILPostProcessors.ToArray();
        }

        private static void OnCompilationFinished(string targetAssembly, CompilerMessage[] messages)
        {
            //If any of the compile messages are errors.
            if (messages.Length > 0)
            {
                if (messages.Any(msg => msg.type == CompilerMessageType.Error))
                    return;
            }
            //Don't run on editor assemblies.
            if (targetAssembly.Contains("-Editor") || targetAssembly.Contains(".Editor"))
                return;
            //Don't run on Unity assemblies, but allow CodeGen ones.
            if ((targetAssembly.Contains("com.unity") || Path.GetFileName(targetAssembly).StartsWith("Unity")) && !targetAssembly.Contains(".CodeGen"))
                return;

            //Where assembles are built.
            string outputDirectory = $"{Application.dataPath}/../{Path.GetDirectoryName(targetAssembly)}";
            //Assembly path for UnityEngine.
            string unityEngineAssemblyPath = string.Empty;
            //Assembly path for fishnet.
            string fishNetRuntimeAssemblyPath = string.Empty;
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            //True if assembly uses fishnet.
            bool usesFishNet = false;
            bool foundThisAssembly = false;

            List<string> depenencyPaths = new List<string>();
            foreach (Assembly assembly in assemblies)
            {
                // Find the assembly currently being compiled from domain assembly list and check if it's using FishNet.
                if (assembly.GetName().Name == Path.GetFileNameWithoutExtension(targetAssembly))
                {
                    foundThisAssembly = true;
                    foreach (System.Reflection.AssemblyName dependency in assembly.GetReferencedAssemblies())
                    {
                        // Since this assembly is already loaded in the domain this is a no-op and returns the
                        // already loaded assembly
                        depenencyPaths.Add(Assembly.Load(dependency).Location);
                        if (dependency.Name.Contains(FishNetILPP.RUNTIME_ASSEMBLY_NAME))
                            usesFishNet = true;
                    }
                }

                try
                {
                    //Set assembly paths.
                    if (assembly.Location.Contains("UnityEngine.CoreModule"))
                        unityEngineAssemblyPath = assembly.Location;
                    if (assembly.Location.Contains(FishNetILPP.RUNTIME_ASSEMBLY_NAME))
                        fishNetRuntimeAssemblyPath = assembly.Location;
                }
                catch (NotSupportedException)
                {
                    // in memory assembly, can't get location
                }
            }

            if (!foundThisAssembly)
            {
                // Target assembly not found in current domain, trying to load it to check references 
                // will lead to trouble in the build pipeline, so lets assume it should go to weaver.
                // Add all assemblies in current domain to dependency list since there could be a 
                // dependency lurking there (there might be generated assemblies so ignore file not found exceptions).
                // (can happen in runtime test framework on editor platform and when doing full library reimport)
                foreach (Assembly assembly in assemblies)
                {
                    try
                    {
                        if (!(assembly.ManifestModule is System.Reflection.Emit.ModuleBuilder))
                            depenencyPaths.Add(Assembly.Load(assembly.GetName().Name).Location);
                    }
                    catch (FileNotFoundException)
                    {
                    }
                }

                usesFishNet = true;
            }

            //If assembly doesn't use fishnet.
            if (!usesFishNet)
            {
                //Check if assembly is fishnet itself.
                usesFishNet = targetAssembly.Contains(FishNetILPP.RUNTIME_ASSEMBLY_NAME);
            }

            if (!usesFishNet)
            {
                return;
            }
            if (string.IsNullOrEmpty(unityEngineAssemblyPath))
            {
                Debug.LogError("Failed to find UnityEngine assembly");
                return;
            }
            if (string.IsNullOrEmpty(fishNetRuntimeAssemblyPath))
            {
                Debug.LogError("Failed to find FishNet runtime assembly");
                return;
            }

            string assemblyPathName = Path.GetFileName(targetAssembly);
            ILPostProcessCompiledAssembly compiledAssembly = new ILPostProcessCompiledAssembly(assemblyPathName, depenencyPaths.ToArray(), null, outputDirectory);

            //Get assembly definition to read.
            AssemblyDefinition assemblyDef = ILCoreHelper.GetAssemblyDefinition(compiledAssembly);
            if (assemblyDef == null)
            {
                Debug.LogError($"Cannot read assembly definition: {compiledAssembly.Name}");
                return;
            }
            ModuleDefinition moduleDef = assemblyDef.MainModule;
            if (moduleDef == null)
            {
                Debug.LogError($"Cannot get the main module for {assemblyDef.Name}.");
                return;
            }

            List<DiagnosticMessage> diagnostics = new List<DiagnosticMessage>();

            //True if anything was processed.
            bool anyProcessed = false;

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Restart();
            foreach (ILPostProcessor pp in _ilPostProcessors)
            {
                string ppName = pp.GetClassName();
                diagnostics.Clear();

                //Result of il post process.
                ILPostProcessResult result = null;
                
                if (pp.WillProcess(compiledAssembly, moduleDef, diagnostics))
                {
                    anyProcessed = true;
                    result = pp.Process(assemblyDef, moduleDef, diagnostics);
                }

                //Print out any diagnostics.
                if (diagnostics.Count > 0)
                {
                    ILCoreHelper.PrintDiagnostics(diagnostics, compiledAssembly, pp.GetType().Name);
                    continue;
                }

                //Write any assembly changes.
                if (result != null)
                    WriteAssembly(result.InMemoryAssembly, outputDirectory, assemblyPathName);
            }

            if (anyProcessed)
                Debug.Log($"Completed code generation in {sw.ElapsedMilliseconds} milliseconds.");
        }

        /// <summary>
        /// Writes any changes to an assembly.
        /// </summary>
        /// <param name="inMemoryAssembly"></param>
        /// <param name="outputPath"></param>
        /// <param name="assName"></param>
        private static void WriteAssembly(InMemoryAssembly inMemoryAssembly, string outputPath, string assName)
        {
            if (inMemoryAssembly == null)
                throw new ArgumentException("InMemoryAssembly has never been accessed or modified");

            var asmPath = Path.Combine(outputPath, assName);
            var pdbFileName = $"{Path.GetFileNameWithoutExtension(assName)}.pdb";
            var pdbPath = Path.Combine(outputPath, pdbFileName);

            File.WriteAllBytes(asmPath, inMemoryAssembly.PeData);
            File.WriteAllBytes(pdbPath, inMemoryAssembly.PdbData);
        }
    }
}
#endif
