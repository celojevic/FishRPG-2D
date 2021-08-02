using FishNet.CodeGenerating.Helping.Extension;
using FishNet.Object;
using Mono.Cecil;
using System.Collections.Generic;
using Unity.CompilationPipeline.Common.Diagnostics;
using UnityEngine;

namespace FishNet.CodeGenerating.Helping
{


    internal static class GeneratorHelper
    {
        /// <summary>
        /// Gets what objectTypeRef will be serialized as.
        /// </summary>
        /// <param name="objectTypeRef"></param>
        /// <param name="writer"></param>
        /// <param name="objectTypeDef"></param>
        /// <param name="diagnostics"></param>
        /// <returns></returns>
        internal static SerializerType GetSerializerType(TypeReference objectTypeRef, bool writer, out TypeDefinition objectTypeDef, List<DiagnosticMessage> diagnostics)
        {
            string errorPrefix = (writer) ? "CreateWrite: " : "CreateRead: ";
            objectTypeDef = null;

            /* Check if already has a serializer. */
            if (writer)
            {
                if (WriterHelper.GetFavoredWriteMethodReference(objectTypeRef, true) != null)
                {
                    diagnostics.AddError($"Writer already exist for {objectTypeRef.FullName}.");
                    return SerializerType.Invalid;
                }
            }
            else
            {
                if (ReaderHelper.GetFavoredReadMethodReference(objectTypeRef, true) != null)
                {
                    diagnostics.AddError($"Reader already exist for {objectTypeRef.FullName}.");
                    return SerializerType.Invalid;
                }
            }
            
            objectTypeDef = objectTypeRef.Resolve();
            //Invalid typeDef.
            if (objectTypeDef == null)
            {
                diagnostics.AddError($"{errorPrefix}{objectTypeDef.FullName} could not be resolved.");
                return SerializerType.Invalid;
            }
            //By reference.            
            if (objectTypeRef.IsByReference)
            {
                diagnostics.AddError($"{errorPrefix}Cannot pass {objectTypeRef.Name} by reference");
                return SerializerType.Invalid;
            }
            /* Arrays have to be processed first because it's possible for them to meet other conditions
             * below and be processed wrong. */
            else if (objectTypeRef.IsArray)
            {
                if (objectTypeRef.IsMultidimensionalArray())
                {
                    diagnostics.AddError($"{errorPrefix}{objectTypeRef.Name} is an unsupported type. Multidimensional arrays are not supported");
                    return SerializerType.Invalid;
                }
                else
                {
                    return SerializerType.Array;
                }
            }
            //Enum.
            else if (objectTypeDef.IsEnum)
            {
                return SerializerType.Enum;
            }
            //if (variableDefinition.Is(typeof(ArraySegment<>)))
            //{
            //    return GenerateArraySegmentReadFunc(objectTypeRef);
            //}
            else if (objectTypeDef.Is(typeof(List<>)))
            {
                return SerializerType.List;
            }
            else if (objectTypeDef.IsDerivedFrom<NetworkBehaviour>())
            {
                return SerializerType.NetworkBehaviour;
            }
            //Invalid type. This must be called after trying to generate everything but class.
            else if (!GeneratorHelper.IsValidSerializeType(objectTypeDef, diagnostics))
            {
                return SerializerType.Invalid;
            }
            //If here then the only type left is struct or class.
            else if ((!objectTypeDef.IsPrimitive && (objectTypeDef.IsClass || objectTypeDef.IsValueType)))
            {
                return SerializerType.ClassOrStruct;
            }
            //Unknown type.
            else
            {
                diagnostics.AddError($"{errorPrefix}{objectTypeRef.Name} is an unsupported type. Mostly because we don't know what the heck it is. Please let us know so we can fix this.");
                return SerializerType.Invalid;
            }
        }


        /// <summary>
        /// Returns if objectTypeRef is an invalid type, which cannot be serialized.
        /// </summary>
        /// <param name="objectTypeDef"></param>
        /// <returns></returns> 
        private static bool IsValidSerializeType(TypeDefinition objectTypeDef, List<DiagnosticMessage> diagnostics)
        {
            string errorText = $"{objectTypeDef.Name} is not a supported type. Use a supported type or provide a custom serializer";
            //Unable to determine type, cannot generate for.
            if (objectTypeDef == null)
            {
                diagnostics.AddError(errorText);
                return false;
            }
            //Component.
            if (objectTypeDef.IsDerivedFrom<UnityEngine.Component>())
            {
                diagnostics.AddError(errorText);
                return false;
            }
            //Unity Object.
            if (objectTypeDef.Is(typeof(UnityEngine.Object)))
            {
                diagnostics.AddError(errorText);
                return false;
            }
            //ScriptableObject.
            if (objectTypeDef.Is(typeof(UnityEngine.ScriptableObject)))
            {
                diagnostics.AddError(errorText);
                return false;
            }
            //Has generic parameters.
            if (objectTypeDef.HasGenericParameters)
            {
                diagnostics.AddError(errorText);
                return false;
            }
            //Is an interface.
            if (objectTypeDef.IsInterface)
            {
                diagnostics.AddError(errorText);
                return false;
            }
            //Is abstract.
            if (objectTypeDef.IsAbstract)
            {
                diagnostics.AddError(errorText);
                return false;
            }

            //If here type is valid.
            return true;
        }


    }


}