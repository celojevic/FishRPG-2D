﻿using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.CompilationPipeline.Common.Diagnostics;

namespace FishNet.CodeGenerating.Helping
{
    public static class Diagnostics
    {
        public static void AddError(this List<DiagnosticMessage> diagnostics, string message)
        {
            diagnostics.AddError((SequencePoint)null, message);
        }

        public static void AddError(this List<DiagnosticMessage> diagnostics, MethodDefinition methodDef, string message)
        {
            diagnostics.AddError(methodDef.DebugInformation.SequencePoints.FirstOrDefault(), message);
        }

        public static void AddError(this List<DiagnosticMessage> diagnostics, SequencePoint sequencePoint, string message)
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