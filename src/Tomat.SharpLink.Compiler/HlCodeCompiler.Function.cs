using System;
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

        foreach (var instr in fun.Opcodes)
            GenerateInstruction(instr, locals, il, asmDef);
    }

    private void GenerateInstruction(HlOpcode instruction, List<VariableDefinition> locals, ILProcessor il, AssemblyDefinition asmDef) {
        switch (instruction.Kind) {
            case HlOpcodeKind.Mov:
                break;

            case HlOpcodeKind.Int:
                break;

            case HlOpcodeKind.Float:
                break;

            case HlOpcodeKind.Bool:
                break;

            case HlOpcodeKind.Bytes:
                break;

            case HlOpcodeKind.String:
                break;

            case HlOpcodeKind.Null:
                break;

            case HlOpcodeKind.Add:
                break;

            case HlOpcodeKind.Sub:
                break;

            case HlOpcodeKind.Mul:
                break;

            case HlOpcodeKind.SDiv:
                break;

            case HlOpcodeKind.UDiv:
                break;

            case HlOpcodeKind.SMod:
                break;

            case HlOpcodeKind.UMod:
                break;

            case HlOpcodeKind.Shl:
                break;

            case HlOpcodeKind.SShr:
                break;

            case HlOpcodeKind.UShr:
                break;

            case HlOpcodeKind.And:
                break;

            case HlOpcodeKind.Or:
                break;

            case HlOpcodeKind.Xor:
                break;

            case HlOpcodeKind.Neg:
                break;

            case HlOpcodeKind.Not:
                break;

            case HlOpcodeKind.Incr:
                break;

            case HlOpcodeKind.Decr:
                break;

            case HlOpcodeKind.Call0:
                break;

            case HlOpcodeKind.Call1:
                break;

            case HlOpcodeKind.Call2:
                break;

            case HlOpcodeKind.Call3:
                break;

            case HlOpcodeKind.Call4:
                break;

            case HlOpcodeKind.CallN:
                break;

            case HlOpcodeKind.CallMethod:
                break;

            case HlOpcodeKind.CallThis:
                break;

            case HlOpcodeKind.CallClosure:
                break;

            case HlOpcodeKind.StaticClosure:
                break;

            case HlOpcodeKind.InstanceClosure:
                break;

            case HlOpcodeKind.VirtualClosure:
                break;

            case HlOpcodeKind.GetGlobal:
                break;

            case HlOpcodeKind.SetGlobal:
                break;

            case HlOpcodeKind.Field:
                break;

            case HlOpcodeKind.SetField:
                break;

            case HlOpcodeKind.GetThis:
                break;

            case HlOpcodeKind.SetThis:
                break;

            case HlOpcodeKind.DynGet:
                break;

            case HlOpcodeKind.DynSet:
                break;

            case HlOpcodeKind.JTrue:
                break;

            case HlOpcodeKind.JFalse:
                break;

            case HlOpcodeKind.JNull:
                break;

            case HlOpcodeKind.JNotNull:
                break;

            case HlOpcodeKind.JSLt:
                break;

            case HlOpcodeKind.JSGte:
                break;

            case HlOpcodeKind.JSGt:
                break;

            case HlOpcodeKind.JSLte:
                break;

            case HlOpcodeKind.JULt:
                break;

            case HlOpcodeKind.JUGte:
                break;

            case HlOpcodeKind.JNotLt:
                break;

            case HlOpcodeKind.JNotGte:
                break;

            case HlOpcodeKind.JEq:
                break;

            case HlOpcodeKind.JNotEq:
                break;

            case HlOpcodeKind.JAlways:
                break;

            case HlOpcodeKind.ToDyn:
                break;

            case HlOpcodeKind.ToSFloat:
                break;

            case HlOpcodeKind.ToUFloat:
                break;

            case HlOpcodeKind.ToInt:
                break;

            case HlOpcodeKind.SafeCast:
                break;

            case HlOpcodeKind.UnsafeCast:
                break;

            case HlOpcodeKind.ToVirtual:
                break;

            case HlOpcodeKind.Label:
                break;

            case HlOpcodeKind.Ret: {
                var localIndex = instruction.Parameters[0];

                il.Emit(OpCodes.Ldloc, locals[localIndex]);
                il.Emit(OpCodes.Ret);
                break;
            }

            case HlOpcodeKind.Throw:
                break;

            case HlOpcodeKind.Rethrow:
                break;

            case HlOpcodeKind.Switch:
                break;

            case HlOpcodeKind.NullCheck:
                break;

            case HlOpcodeKind.Trap:
                break;

            case HlOpcodeKind.EndTrap:
                break;

            case HlOpcodeKind.GetI8:
                break;

            case HlOpcodeKind.GetI16:
                break;

            case HlOpcodeKind.GetMem:
                break;

            case HlOpcodeKind.GetArray:
                break;

            case HlOpcodeKind.SetI8:
                break;

            case HlOpcodeKind.SetI16:
                break;

            case HlOpcodeKind.SetMem:
                break;

            case HlOpcodeKind.SetArray:
                break;

            case HlOpcodeKind.New:
                break;

            case HlOpcodeKind.ArraySize:
                break;

            case HlOpcodeKind.Type:
                break;

            case HlOpcodeKind.GetType:
                break;

            case HlOpcodeKind.GetTID:
                break;

            case HlOpcodeKind.Ref:
                break;

            case HlOpcodeKind.Unref:
                break;

            case HlOpcodeKind.Setref:
                break;

            case HlOpcodeKind.OMakeEnum:
                break;

            case HlOpcodeKind.OEnumAlloc:
                break;

            case HlOpcodeKind.OEnumIndex:
                break;

            case HlOpcodeKind.OEnumField:
                break;

            case HlOpcodeKind.OSetEnumField:
                break;

            case HlOpcodeKind.Assert:
                break;

            case HlOpcodeKind.RefData:
                break;

            case HlOpcodeKind.RefOffset:
                break;

            case HlOpcodeKind.Nop:
                break;

            case HlOpcodeKind.Last:
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
