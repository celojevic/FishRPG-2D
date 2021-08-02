using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace FishNet.CodeGenerating.Helping.Extension
{

    public static class ILProcessorExtensions
    {
        //Inserts instructions at the beginning.
        public static void InsertFirst(this ILProcessor processor, List<Instruction> instructions)
        {
            for (int i = 0; i < instructions.Count; i++)
                processor.Body.Instructions.Insert(i, instructions[i]);
        }

        //public static Instruction Create<T>(this ILProcessor processor, OpCode code, Expression<Action<T>> expression)
        //{
        //    MethodReference typeref = processor.Body.Method.Module.ImportReference(expression);
        //    return processor.Create(code, typeref);
        //}

        //public static void Emit<T>(this ILProcessor processor, OpCode code, Expression<Action<T>> expression)
        //{
        //    MethodReference typeref = processor.Body.Method.Module.ImportReference(expression);
        //    processor.Emit(code, typeref);
        //} 
    }


}