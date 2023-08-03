using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
    private int methodCounter;

    private void CompileFunction(HlFunction fun, AssemblyDefinition asmDef) {
        var funType = ((HlTypeWithFun)fun.Type.Value!).Fun;
        var method = CreateMethod(fun, funType, asmDef);
        var locals = CreateMethodLocals(fun, funType, asmDef);
        GenerateMethodBody(method, locals, fun, asmDef);

        asmDef.MainModule.GetType("<Module>").Methods.Add(method);
    }

    private MethodDefinition CreateMethod(HlFunction fun, HlTypeFun funType, AssemblyDefinition asmDef) {
        var retType = TypeReferenceFromHlTypeRef(funType.ReturnType, asmDef);
        var paramTypes = funType.Arguments.Select(param => TypeReferenceFromHlTypeRef(param, asmDef)).ToArray();
        var method = new MethodDefinition($"fun{methodCounter++}", MethodAttributes.Public | MethodAttributes.Static, retType);

        var argCounter = 0;
        foreach (var paramType in paramTypes)
            method.Parameters.Add(new ParameterDefinition($"arg{argCounter++}", ParameterAttributes.None, paramType));
        return method;
    }

    private List<VariableDefinition> CreateMethodLocals(HlFunction fun, HlTypeFun funType, AssemblyDefinition asmDef) {
        var locals = new List<VariableDefinition>();

        /*// Registers are also taken up by function arguments, which we don't
        // need to care about.
        foreach (var local in fun.Regs[funType.Arguments.Length..]) {
            var localType = TypeReferenceFromHlTypeRef(local, asmDef);
            locals.Add(new VariableDefinition(localType));
        }*/

        // TODO: When I have time to clean up and optimize code, make it so we
        // don't lazily assign parameters to locals and treat them all like
        // regular hl registers.
        foreach (var local in fun.Regs) {
            var localType = TypeReferenceFromHlTypeRef(local, asmDef);
            locals.Add(new VariableDefinition(localType));
        }

        return locals;
    }

    private void GenerateMethodBody(MethodDefinition method, List<VariableDefinition> locals, HlFunction fun, AssemblyDefinition asmDef) {
        var body = method.Body = new MethodBody(method);
        foreach (var local in locals)
            method.Body.Variables.Add(local);

        var il = body.GetILProcessor();

        // Assign every parameter to a local variable corresponding to a
        // register.
        for (var i = 0; i < method.Parameters.Count; i++) {
            var param = method.Parameters[i];
            il.Emit(OpCodes.Ldarg, param);
            il.Emit(OpCodes.Stloc, locals[i]);
        }

        // Placeholder.
        il.Emit(OpCodes.Ldloc, locals[^1]);
        il.Emit(OpCodes.Ret);
    }
}
