#if !UNITY_2020_2_OR_NEWER
using Mono.Cecil;
using System.Collections.Generic;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace FishNet.CodeGenerating.ILCore.Pre2020
{
    public abstract class ILPostProcessor
    {
        public abstract string GetClassName();
        public abstract bool WillProcess(ICompiledAssembly compiledAssembly, ModuleDefinition moduleDef, List<DiagnosticMessage> diagnostics);
        public abstract ILPostProcessResult Process(AssemblyDefinition assemblyDef, ModuleDefinition moduleDef, List<DiagnosticMessage> diagnostics);
    }
}
#endif