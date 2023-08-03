using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
    private int methodCounter = 0;

    private void CompileFunction(HlFunction fun, AssemblyDefinition asmDef) {
        var method = CreateMethod(fun, asmDef);
        var locals = CreateMethodLocals(fun, asmDef);
        GenerateMethodBody(fun, locals, asmDef);

        asmDef.MainModule.GetType("<Module>").Methods.Add(method);
    }

    private MethodDefinition CreateMethod(HlFunction fun, AssemblyDefinition asmDef) {
        var funType = (HlTypeWithFun)fun.Type.Value!;
        var retType = TypeReferenceFromHlTypeRef(funType.Fun.ReturnType, asmDef);
        var paramTypes = funType.Fun.Arguments.Select(param => TypeReferenceFromHlTypeRef(param, asmDef)).ToArray();
        var method = new MethodDefinition($"fun{methodCounter++}", MethodAttributes.Public | MethodAttributes.Static, retType);

        foreach (var paramType in paramTypes)
            method.Parameters.Add(new ParameterDefinition(paramType));

        return method;
    }

    private List<VariableDefinition> CreateMethodLocals(HlFunction fun, AssemblyDefinition asmDef) {
        var locals = new List<VariableDefinition>();

        foreach (var local in fun.Regs) {
            var localType = TypeReferenceFromHlTypeRef(local, asmDef);
            locals.Add(new VariableDefinition(localType));
        }

        return locals;
    }

    private void GenerateMethodBody(HlFunction fun, List<VariableDefinition> locals, AssemblyDefinition asmDef) {
        /*var body = new MethodBody(method);
        var il = body.GetILProcessor();
        
        foreach (var local in locals)
            method.Body.Variables.Add(local);
        
        foreach (var instr in fun.Instrs)
            CompileInstruction(instr, il, locals, asmDef);
        
        method.Body = body;*/
    }
}
