
#if !UNITY_2020_2_OR_NEWER
namespace FishNet.CodeGenerating.ILCore.Pre2020
{
    using Unity.CompilationPipeline.Common.ILPostProcessing;
    using ILPPInterface = Unity.CompilationPipeline.Common.ILPostProcessing.ILPostProcessor;
    /* // There is a behaviour difference between 2019.4 and 2020+ codegen
    // that essentially does checking on the existence of ILPP vs if a CodeGen assembly
    // is present. So in order to make sure ILPP runs properly in 2019.4 from a clean
    // import of the project we add this dummy ILPP which forces the callback to made
    // and meets the internal ScriptCompilation pipeline requirements */

    internal sealed class CodegenFixILPP : ILPPInterface
    {
        public override ILPPInterface GetInstance() => this;
        public override ILPostProcessResult Process(Unity.CompilationPipeline.Common.ILPostProcessing.ICompiledAssembly compiledAssembly) => null;
        public override bool WillProcess(ICompiledAssembly compiledAssembly) => false;
    }


}
#endif