using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.IO;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace FishNet.CodeGenerating.ILCore
{
    internal static class ILCoreHelper
    {

        /// <summary>
        /// Returns AssembleDefinition for compiledAssembly.
        /// </summary>
        /// <param name="compiledAssembly"></param>
        /// <returns></returns>
        internal static AssemblyDefinition GetAssemblyDefinition(ICompiledAssembly compiledAssembly)
        {
            var assemblyResolver = new PostProcessorAssemblyResolver(compiledAssembly);
            var readerParameters = new ReaderParameters
            {
                SymbolStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData),
                SymbolReaderProvider = new PortablePdbReaderProvider(),
                AssemblyResolver = assemblyResolver,
                ReflectionImporterProvider = new PostProcessorReflectionImporterProvider(),
                ReadingMode = ReadingMode.Immediate
            };

            var assemblyDefinition = AssemblyDefinition.ReadAssembly(new MemoryStream(compiledAssembly.InMemoryAssembly.PeData), readerParameters);
            //Allows us to resolve inside FishNet assembly, such as for components.
            assemblyResolver.AddAssemblyDefinitionBeingOperatedOn(assemblyDefinition);

            return assemblyDefinition;
        }

        /// <summary>
        /// Prints out diagnostic results.
        /// </summary>
        /// <param name="diagnostics"></param>
        internal static void PrintDiagnostics(List<DiagnosticMessage> diagnostics, ICompiledAssembly compiledAssembly, string ilppName)
        {
            //Print out any diagnostics.
            if (diagnostics.Count > 0)
            {
                UnityEngine.Debug.LogError($"{ilppName} failed to run on {compiledAssembly.Name}");

                foreach (DiagnosticMessage message in diagnostics)
                {
                    switch (message.DiagnosticType)
                    {
                        case DiagnosticType.Error:
                            UnityEngine.Debug.LogError($"{message.MessageData}");
                            break;
                        case DiagnosticType.Warning:
                            UnityEngine.Debug.LogWarning($"{message.MessageData}");
                            break;
                    }
                }
            }
        }

    }

}