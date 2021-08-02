using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FishNet.CodeGenerating.Helping.Extension
{

    internal static class TypeReferenceExtensions
    {
        /// <summary>
        /// Resolves the default constructor for typeRef.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <returns></returns>
        public static MethodDefinition ResolveDefaultPublicConstructor(this TypeReference typeRef)
        {
            foreach (MethodDefinition methodRef in typeRef.Resolve().Methods)
            {
                if (methodRef.IsConstructor && methodRef.Resolve().IsPublic && methodRef.Parameters.Count == 0)
                    return methodRef;
            }
            return null;
        }

        /// <summary>
        /// Resolves the default constructor for typeRef.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <returns></returns>
        public static MethodDefinition ResolveFirstPublicConstructor(this TypeReference typeRef)
        {
            foreach (MethodDefinition methodDef in typeRef.Resolve().Methods)
            {
                if (methodDef.IsConstructor && methodDef.Resolve().IsPublic)
                    return methodDef;
            }
            return null;
        }

        /// <summary>
        /// Gets mono Type from typeRef.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <returns></returns>
        private static Type GetMonoTypeInTypeRefAsm(this TypeReference typeRef)
        {
            if (typeRef == null)
            {
                Debug.LogError("TypeRef is null.");
                return null;
            }

            Type result = null;
            try
            {
                result = Type.GetType(typeRef.FullName + ", " + typeRef.Resolve().Module.Assembly.FullName);
            }
            catch { }
            finally
            {
                if (result == null)
                    Debug.LogError($"Unable to get Type for {typeRef.FullName}.");
            }

            return result;
        }
        /// <summary>
        /// Gets mono Type from typeRef.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <returns></returns>
        public static Type GetMonoType(this TypeReference typeRef)
        {
            if (typeRef == null)
            {
                Debug.LogError("TypeRef is null.");
                return null;
            }

            Type result = null;
            try
            {
                result = Type.GetType(typeRef.GetReflectionName(), true);
            }
            catch { }
            finally
            {
                if (result == null)
                    result = GetMonoTypeInTypeRefAsm(typeRef);
            }

            return result;
        }

        private static string GetReflectionName(this TypeReference type)
        {
            if (type.IsGenericInstance)
            {
                var genericInstance = (GenericInstanceType)type;
                return string.Format("{0}.{1}[{2}]", genericInstance.Namespace, type.Name, String.Join(",", genericInstance.GenericArguments.Select(p => p.GetReflectionName()).ToArray()));
            }
            return type.FullName;
        }

        /// <summary>
        /// Gets all public fields in typeRef and base type.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <returns></returns>
        public static IEnumerable<FieldDefinition> FindAllPublicFields(this TypeReference typeRef)
        {
            return FindAllPublicFields(typeRef.Resolve());
        }


        /// <summary>
        /// Finds public fields in type and base type
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        public static IEnumerable<FieldDefinition> FindAllPublicFields(this TypeDefinition typeDefinition)
        {
            while (typeDefinition != null)
            {
                foreach (FieldDefinition field in typeDefinition.Fields)
                {
                    if (field.IsStatic || field.IsPrivate)
                        continue;

                    if (field.IsNotSerialized)
                        continue;

                    yield return field;
                }

                try
                {
                    typeDefinition = typeDefinition.BaseType?.Resolve();
                }
                catch
                {
                    break;
                }
            }
        }


        /// <summary>
        /// Returns a method within the base type of typeRef.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public static MethodReference GetMethodInBaseType(this TypeReference typeRef, string methodName)
        {
            TypeDefinition typedef = typeRef.Resolve();
            TypeReference typeRefCopy = typeRef;
            while (typedef != null)
            {
                foreach (MethodDefinition md in typedef.Methods)
                {
                    if (md.Name == methodName)
                    {
                        return md;

                        //MethodReference method = md;
                        //if (typeRefCopy.IsGenericInstance)
                        //{
                        //    var baseTypeInstance = (GenericInstanceType)typeRef;
                        //    method = method.MakeHostInstanceGeneric(baseTypeInstance);
                        //}

                        //return method;
                    }
                }

                try
                {
                    TypeReference parent = typedef.BaseType;
                    typeRefCopy = parent;
                    typedef = parent?.Resolve();
                }
                catch (AssemblyResolutionException)
                {
                    // this can happen for plugins.
                    break;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns if a typeRef is type.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsType(this TypeReference typeRef, Type type)
        {
            if (type.IsGenericType)
                return typeRef.GetElementType().FullName == type.FullName;
            else
                return typeRef.FullName == type.FullName;
        }



        /// <summary>
        /// Returns if typeRef is a multidimensional array.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <returns></returns>
        public static bool IsMultidimensionalArray(this TypeReference typeRef)
        {
            return typeRef is ArrayType arrayType && arrayType.Rank > 1;
        }


        /// <summary>
        /// Returns if typeRef can be resolved.
        /// </summary>
        /// <param name="typeRef"></param>
        /// <returns></returns>
        public static bool CanBeResolved(this TypeReference typeRef)
        {
            while (typeRef != null)
            {
                if (typeRef.Scope.Name == "Windows")
                {
                    return false;
                }

                if (typeRef.Scope.Name == "mscorlib")
                {
                    TypeDefinition resolved = typeRef.Resolve();
                    return resolved != null;
                }

                try
                {
                    typeRef = typeRef.Resolve().BaseType;
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }
    }

}