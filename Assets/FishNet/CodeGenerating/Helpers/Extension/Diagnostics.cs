using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.CompilationPipeline.Common.Diagnostics;

namespace FishNet.CodeGenerating.Helping
{
    internal static class Diagnostics
    {
        internal static void AddError(this List<DiagnosticMessage> diagnostics, string message)
        {
            CodegenSession.Diagnostics.AddError((SequencePoint)null, message);
        }

        internal static void AddError(this List<DiagnosticMessage> diagnostics, MethodDefinition methodDef, string message)
        {
            CodegenSession.Diagnostics.AddError(methodDef.DebugInformation.SequencePoints.FirstOrDefault(), message);
        }

        internal static void AddError(this List<DiagnosticMessage> diagnostics, SequencePoint sequencePoint, string message)
        {
            diagnostics.Add(new DiagnosticMessage
            {
                DiagnosticType = DiagnosticType.Error,
                File = sequencePoint?.Document.Url.Replace($"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}", ""),
                Line = sequencePoint?.StartLine ?? 0,
                Column = sequencePoint?.StartColumn ?? 0,
                MessageData = $" - {message}"
            });
        }

    }
}