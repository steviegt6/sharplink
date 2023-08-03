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

    private void PushCached<T>(ILProcessor il, int index) {
        if (typeof(T) == typeof(int) || typeof(T) == typeof(byte) || typeof(T) == typeof(ushort))
            il.Emit(OpCodes.Ldc_I4, hash.Code.Ints[index]);
        else if (typeof(T) == typeof(long))
            il.Emit(OpCodes.Ldc_I8, hash.Code.Ints[index]);
        else if (typeof(T) == typeof(float))
            il.Emit(OpCodes.Ldc_R4, (float)hash.Code.Floats[index]);
        else if (typeof(T) == typeof(double))
            il.Emit(OpCodes.Ldc_R8, hash.Code.Floats[index]);
        else if (typeof(T) == typeof(string))
            il.Emit(OpCodes.Ldstr, hash.Code.Strings[index]);
    }

    private void PushConverter<TSys, THaxe>(ILProcessor il, AssemblyDefinition asmDef) {
        il.Emit(OpCodes.Newobj, asmDef.MainModule.ImportReference(typeof(THaxe).GetConstructor(new[] { typeof(TSys) })));
    }

    private void LoadLocal(ILProcessor il, List<VariableDefinition> locals, int index) {
        il.Emit(OpCodes.Ldloc, locals[index]);
    }

    private void SetLocal(ILProcessor il, List<VariableDefinition> locals, int index) {
        il.Emit(OpCodes.Stloc, locals[index]);
    }

    private void GenerateInstruction(HlOpcode instruction, List<VariableDefinition> locals, ILProcessor il, AssemblyDefinition asmDef) {
        switch (instruction.Kind) {
            // *dst = *src
            case HlOpcodeKind.Mov: {
                var dst = instruction.Parameters[0];
                var src = instruction.Parameters[1];

                LoadLocal(il, locals, src);
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = ints[key]
            case HlOpcodeKind.Int: {
                var dst = instruction.Parameters[0];
                var key = instruction.Parameters[1];

                PushCached<int>(il, key);
                // PushConverter<int, HaxeI32>(il, asmDef);
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = floats[key]
            case HlOpcodeKind.Float: {
                var destIndex = instruction.Parameters[0];
                var floatKey = instruction.Parameters[1];

                PushCached<double>(il, floatKey);
                // PushConverter<double, HaxeF64>(il, asmDef);
                SetLocal(il, locals, destIndex);
                break;
            }

            // *dst = *value != 0
            case HlOpcodeKind.Bool: {
                var destIndex = instruction.Parameters[0];
                var value = instruction.Parameters[1];

                il.Emit(OpCodes.Ldc_I4, value);
                // PushConverter<int, HaxeBool>(il, asmDef);
                SetLocal(il, locals, destIndex);
                break;
            }

            // TODO
            case HlOpcodeKind.Bytes:
                // TODO: Uses Bytes and BytePositions I think. Version >= 5 ofc.
                throw new NotImplementedException();

            // TODO: Seems like sometimes the destination can be HaxeBytes, so
            // we may want to implement a check and handle conversion.
            //   haxeBytes = (HaxeBytes)(object)"Main";
            //   ldstr "Date"
            //   stloc 3 ([3] class [Tomat.SharpLink.CoreLib]Tomat.SharpLink.HaxeBytes)
            // *dst = strings[key]
            case HlOpcodeKind.String: {
                var destIndex = instruction.Parameters[0];
                var stringKey = instruction.Parameters[1];

                PushCached<string>(il, stringKey);
                // PushConverter<string, HaxeString>(il, asmDef);
                SetLocal(il, locals, destIndex);
                break;
            }

            // *dst = null
            case HlOpcodeKind.Null: {
                var destIndex = instruction.Parameters[0];

                il.Emit(OpCodes.Ldnull);
                SetLocal(il, locals, destIndex);
                break;
            }

            // *dst = *a + *b
            case HlOpcodeKind.Add: {
                var dst = instruction.Parameters[0];
                var a = instruction.Parameters[1];
                var b = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(OpCodes.Add); // TODO: use a real method later...
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = *a - *b
            case HlOpcodeKind.Sub: {
                var dst = instruction.Parameters[0];
                var a = instruction.Parameters[1];
                var b = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(OpCodes.Sub); // TODO: use a real method later...
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = *a * *b
            case HlOpcodeKind.Mul: {
                var dst = instruction.Parameters[0];
                var a = instruction.Parameters[1];
                var b = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(OpCodes.Mul); // TODO: use a real method later...
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = *a / *b
            case HlOpcodeKind.SDiv: {
                var dst = instruction.Parameters[0];
                var a = instruction.Parameters[1];
                var b = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(OpCodes.Div); // TODO: use a real method later...
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = *a / *b
            case HlOpcodeKind.UDiv: {
                var dst = instruction.Parameters[0];
                var a = instruction.Parameters[1];
                var b = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(OpCodes.Div); // TODO: use a real method later...
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = *a % *b
            case HlOpcodeKind.SMod: {
                var dst = instruction.Parameters[0];
                var a = instruction.Parameters[1];
                var b = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(OpCodes.Rem); // TODO: use a real method later...
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = *a % *b
            case HlOpcodeKind.UMod: {
                var dst = instruction.Parameters[0];
                var a = instruction.Parameters[1];
                var b = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(OpCodes.Rem); // TODO: use a real method later...
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = *a << *b
            case HlOpcodeKind.Shl: {
                var dst = instruction.Parameters[0];
                var a = instruction.Parameters[1];
                var b = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(OpCodes.Shl); // TODO: use a real method later...
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = *a >> *b
            case HlOpcodeKind.SShr: {
                var dst = instruction.Parameters[0];
                var a = instruction.Parameters[1];
                var b = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(OpCodes.Shr); // TODO: use a real method later...
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = *a >> *b
            case HlOpcodeKind.UShr: {
                var dst = instruction.Parameters[0];
                var a = instruction.Parameters[1];
                var b = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(OpCodes.Shr); // TODO: use a real method later...
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = *a & *b
            case HlOpcodeKind.And: {
                var dst = instruction.Parameters[0];
                var a = instruction.Parameters[1];
                var b = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(OpCodes.And); // TODO: use a real method later...
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = *a | *b
            case HlOpcodeKind.Or: {
                var dst = instruction.Parameters[0];
                var a = instruction.Parameters[1];
                var b = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(OpCodes.Or); // TODO: use a real method later...
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = *a ^ *b
            case HlOpcodeKind.Xor: {
                var dst = instruction.Parameters[0];
                var a = instruction.Parameters[1];
                var b = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(OpCodes.Xor); // TODO: use a real method later...
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = -(*a)
            case HlOpcodeKind.Neg: {
                var dst = instruction.Parameters[0];
                var a = instruction.Parameters[1];

                LoadLocal(il, locals, a);
                il.Emit(OpCodes.Neg); // TODO: use a real method later...
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = !(*a)
            case HlOpcodeKind.Not: {
                var dst = instruction.Parameters[0];
                var a = instruction.Parameters[1];

                LoadLocal(il, locals, a);
                il.Emit(OpCodes.Not); // TODO: use a real method later...
                SetLocal(il, locals, dst);
                break;
            }

            // (*dst)++
            case HlOpcodeKind.Incr: {
                var dst = instruction.Parameters[0];

                LoadLocal(il, locals, dst);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Add);
                SetLocal(il, locals, dst);
                break;
            }

            // (*dst)--
            case HlOpcodeKind.Decr: {
                var dst = instruction.Parameters[0];

                LoadLocal(il, locals, dst);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Sub);
                SetLocal(il, locals, dst);
                break;
            }

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
