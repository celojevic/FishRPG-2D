using Mono.Cecil;
using System;

namespace FishNet.CodeGenerating.Helping.Extension
{

    public static class MethodReferenceExtensions
    {

        /// <summary>
        /// Given a method of a generic class such as ArraySegment`T.get_Count,
        /// and a generic instance such as ArraySegment`int
        /// Creates a reference to the specialized method  ArraySegment`int`.get_Count
        /// <para> Note that calling ArraySegment`T.get_Count directly gives an invalid IL error </para>
        /// </summary>
        /// <param name="methodRef"></param>
        /// <param name="instanceType"></param>
        /// <returns></returns>
        public static MethodReference MakeHostInstanceGeneric(this MethodReference methodRef, GenericInstanceType instanceType)
        {
            var reference = new MethodReference(methodRef.Name, methodRef.ReturnType, instanceType)
            {
                CallingConvention = methodRef.CallingConvention,
                HasThis = methodRef.HasThis,
                ExplicitThis = methodRef.ExplicitThis
            };

            foreach (ParameterDefinition parameter in methodRef.Parameters)
                reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));

            foreach (GenericParameter generic_parameter in methodRef.GenericParameters)
                reference.GenericParameters.Add(new GenericParameter(generic_parameter.Name, reference));

            return methodRef.Module.ImportReference(reference);
        }
        public static bool Is<T>(this MethodReference method, string name)
        { 
        return method.DeclaringType.Is<T>() && method.Name == name;
        }
        public static bool Is<T>(this TypeReference td)
        {
            return Is(td, typeof(T));
        }

        public static bool Is(this TypeReference td, Type t)
        {
            if (t.IsGenericType)
            {
                return td.GetElementType().FullName == t.FullName;
            }
            return td.FullName == t.FullName;
        }



    }


}