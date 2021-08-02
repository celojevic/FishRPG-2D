
using FishNet.CodeGenerating.Helping;
using FishNet.CodeGenerating.Helping.Extension;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using Unity.CompilationPipeline.Common.Diagnostics;

namespace FishNet.CodeGenerating.Processing
{
    internal static class NetworkBehaviourQolAttributeProcessor
    {

        internal static void Process(TypeDefinition typeDef, List<DiagnosticMessage> diagnostics)
        {
            foreach (MethodDefinition methodDef in typeDef.Methods)
            {
                //Has RPC attribute, doesn't quality for a quality of life attribute.
                if (NetworkBehaviourRpcProcessor.GetRpcAttribute(methodDef, out _, diagnostics) != null)
                    continue;

                QolAttributeType qolType;
                CustomAttribute qolAttribute = GetQOLAttribute(methodDef, out qolType, diagnostics);
                if (qolAttribute == null)
                    continue;

                /* This is a one time check to make sure the qolType is
                 * a supported value. Multiple methods beyond this rely on the
                 * value being supported. Rather than check in each method a
                 * single check is performed here. */
                if (qolType != QolAttributeType.Server && qolType != QolAttributeType.Client)
                {
                    diagnostics.AddError($"QolAttributeType of {qolType.ToString()} is unhandled.");
                    continue;
                }

                CreateAttributeMethod(methodDef, qolAttribute, qolType, diagnostics);
            }
        }

        /// <summary>
        /// Returns the RPC attribute on a method, if one exist. Otherwise returns null.
        /// </summary>
        /// <param name="methodDef"></param>
        /// <param name="rpcType"></param>
        /// <returns></returns>
        private static CustomAttribute GetQOLAttribute(MethodDefinition methodDef, out QolAttributeType qolType, List<DiagnosticMessage> diagnostics)
        {
            CustomAttribute foundAttribute = null;
            qolType = QolAttributeType.None;
            //Becomes true if an error occurred during this process.
            bool error = false;

            foreach (CustomAttribute customAttribute in methodDef.CustomAttributes)
            {
                QolAttributeType thisQolType = AttributeHelper.GetQolAttributeType(customAttribute.AttributeType.FullName);
                if (thisQolType != QolAttributeType.None)
                {
                    //A qol attribute already exist.
                    if (foundAttribute != null)
                    {
                        diagnostics.AddError($"{methodDef.Name} {thisQolType.ToString()} method cannot have multiple quality of life attributes.");
                        error = true;
                    }
                    //Static method.
                    if (methodDef.IsStatic)
                    {
                        diagnostics.AddError($"{methodDef.Name} {thisQolType.ToString()} method cannot be static.");
                        error = true;
                    }
                    //Abstract method.
                    if (methodDef.IsAbstract)
                    {
                        diagnostics.AddError($"{methodDef.Name} {thisQolType.ToString()} method cannot be abstract.");
                        error = true;
                    }

                    //If all checks passed.
                    if (!error)
                    {
                        foundAttribute = customAttribute;
                        qolType = thisQolType;
                    }
                }
            }

            //If an error occurred then reset results.
            if (error)
            {
                foundAttribute = null;
                qolType = QolAttributeType.None;
            }

            return foundAttribute;
        }

        /// <summary>
        /// Modifies the specified method to use QolType.
        /// </summary>
        /// <param name="originalMethodDef"></param>
        /// <param name="qolAttribute"></param>
        /// <param name="qolType"></param>
        /// <param name="diagnostics"></param>
        private static void CreateAttributeMethod(MethodDefinition methodDef, CustomAttribute qolAttribute, QolAttributeType qolType, List<DiagnosticMessage> diagnostics)
        {
            bool warn = qolAttribute.GetField("Warn", false);

            ILProcessor processor = methodDef.Body.GetILProcessor();

            if (qolType == QolAttributeType.Client)
                ObjectHelper.CreateIsClientCheck(processor, methodDef, warn, true);
            else if (qolType == QolAttributeType.Server)
                ObjectHelper.CreateIsServerCheck(processor, methodDef, warn,  true);
        }

    }
}