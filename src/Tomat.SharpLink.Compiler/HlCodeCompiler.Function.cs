using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
    private Dictionary<HlFunction, MethodDefinition> funMethodDefs = new();
    private Dictionary<HlNative, MethodDefinition> nativeMethodDefs = new();

    private void DefineNative(HlNative native, AssemblyDefinition asmDef) {
        var funType = ((HlTypeWithFun)native.T.Value!).Fun;
        var method = CreateMethod(native, funType, asmDef);
        nativeMethodDefs.Add(native, method);
    }

    private void CompileNative(HlNative native, AssemblyDefinition asmDef) {
        var funType = ((HlTypeWithFun)native.T.Value!).Fun;
        var method = nativeMethodDefs[native];

        asmDef.MainModule.GetType("<Module>").Methods.Add(method);
    }

    private void DefineFunction(HlFunction fun, AssemblyDefinition asmDef) {
        var funType = ((HlTypeWithFun)fun.Type.Value!).Fun;
        var method = CreateMethod(fun, funType, asmDef);
        funMethodDefs.Add(fun, method);
    }

    private void CompileFunction(HlFunction fun, AssemblyDefinition asmDef) {
        var funType = ((HlTypeWithFun)fun.Type.Value!).Fun;
        var method = funMethodDefs[fun];
        var locals = CreateMethodLocals(fun, funType, asmDef);
        GenerateMethodBody(method, locals, fun, asmDef);

        asmDef.MainModule.GetType("<Module>").Methods.Add(method);
    }

    private MethodDefinition CreateMethod(HlFunction fun, HlTypeFun funType, AssemblyDefinition asmDef) {
        var retType = TypeReferenceFromHlTypeRef(funType.ReturnType, asmDef);
        var paramTypes = funType.Arguments.Select(param => TypeReferenceFromHlTypeRef(param, asmDef)).ToArray();
        var method = new MethodDefinition($"fun{fun.FIndex}", MethodAttributes.Public | MethodAttributes.Static, retType);

        var argCounter = 0;
        foreach (var paramType in paramTypes)
            method.Parameters.Add(new ParameterDefinition($"arg{argCounter++}", ParameterAttributes.None, paramType));
        return method;
    }

    private MethodDefinition CreateMethod(HlNative native, HlTypeFun funType, AssemblyDefinition asmDef) {
        var retType = TypeReferenceFromHlTypeRef(funType.ReturnType, asmDef);
        var paramTypes = funType.Arguments.Select(param => TypeReferenceFromHlTypeRef(param, asmDef)).ToArray();
        var method = new MethodDefinition($"fun{native.FIndex}", MethodAttributes.Public | MethodAttributes.Static, retType);

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

        var markers = new Dictionary<int, Instruction>();
        for (var i = 0; i < fun.Opcodes.Length; i++)
            markers[i] = il.Create(OpCodes.Nop);

        for (var i = 0; i < fun.Opcodes.Length; i++) {
            var instr = fun.Opcodes[i];
            il.Append(markers[i]);
            GenerateInstruction(instr, locals, il, asmDef, i, markers);
        }
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

    private void LoadGlobal(ILProcessor il, int index, AssemblyDefinition asmDef) {
        var module = asmDef.MainModule.GetType("<Module>");
        var field = module.Fields.FirstOrDefault(field => field.Name == $"global{index}");
        il.Emit(OpCodes.Ldsfld, field);
    }

    private void SetGlobal(ILProcessor il, int index, AssemblyDefinition asmDef) {
        var module = asmDef.MainModule.GetType("<Module>");
        var field = module.Fields.FirstOrDefault(field => field.Name == $"global{index}");
        il.Emit(OpCodes.Stsfld, field);
    }

    private void GenerateInstruction(HlOpcode instruction, List<VariableDefinition> locals, ILProcessor il, AssemblyDefinition asmDef, int originalIndex, Dictionary<int, Instruction> markers) {
        var originalIndexForJump = originalIndex + 1;

        if (markers.TryGetValue(originalIndex, out var marker))
            il.Append(marker);

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

            // *dst = strings[key]
            case HlOpcodeKind.String: {
                var destIndex = instruction.Parameters[0];
                var stringKey = instruction.Parameters[1];

                // TODO: Better check.
                var isDestBytes = locals[destIndex].VariableType.FullName == "Tomat.SharpLink.HaxeBytes";

                PushCached<string>(il, stringKey);

                if (isDestBytes) {
                    // Strings can be pushed directly to HBYTES, so we need to
                    // handle this ourselves.
                    PushConverter<string, HaxeBytes>(il, asmDef);
                }

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
                il.Emit(OpCodes.Add);
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
                il.Emit(OpCodes.Sub);
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
                il.Emit(OpCodes.Mul);
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
                il.Emit(OpCodes.Div);
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
                il.Emit(OpCodes.Div);
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
                il.Emit(OpCodes.Rem);
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
                il.Emit(OpCodes.Rem);
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
                il.Emit(OpCodes.Shl);
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
                il.Emit(OpCodes.Shr);
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
                il.Emit(OpCodes.Shr);
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
                il.Emit(OpCodes.And);
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
                il.Emit(OpCodes.Or);
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
                il.Emit(OpCodes.Xor);
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = -(*a)
            case HlOpcodeKind.Neg: {
                var dst = instruction.Parameters[0];
                var a = instruction.Parameters[1];

                LoadLocal(il, locals, a);
                il.Emit(OpCodes.Neg);
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = !(*a)
            case HlOpcodeKind.Not: {
                var dst = instruction.Parameters[0];
                var a = instruction.Parameters[1];

                LoadLocal(il, locals, a);
                il.Emit(OpCodes.Not);
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

            // *dst = (*a)()
            case HlOpcodeKind.Call0: {
                var dst = instruction.Parameters[0];
                var fun = instruction.Parameters[1];

                var def = ResolveDefinitionFromFIndex(fun);

                il.Emit(OpCodes.Call, def);
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = (*a)(*b)
            case HlOpcodeKind.Call1: {
                var dst = instruction.Parameters[0];
                var fun = instruction.Parameters[1];
                var arg = instruction.Parameters[2];

                var def = ResolveDefinitionFromFIndex(fun);

                LoadLocal(il, locals, arg);
                il.Emit(OpCodes.Call, def);
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = (*a)(*b, *c)
            case HlOpcodeKind.Call2: {
                var dst = instruction.Parameters[0];
                var fun = instruction.Parameters[1];
                var arg1 = instruction.Parameters[2];
                var arg2 = instruction.Parameters[3];

                var def = ResolveDefinitionFromFIndex(fun);

                LoadLocal(il, locals, arg1);
                LoadLocal(il, locals, arg2);
                il.Emit(OpCodes.Call, def);
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = (*a)(*b, *c, *d)
            case HlOpcodeKind.Call3: {
                var dst = instruction.Parameters[0];
                var fun = instruction.Parameters[1];
                var arg1 = instruction.Parameters[2];
                var arg2 = instruction.Parameters[3];
                var arg3 = instruction.Parameters[4];

                var def = ResolveDefinitionFromFIndex(fun);

                LoadLocal(il, locals, arg1);
                LoadLocal(il, locals, arg2);
                LoadLocal(il, locals, arg3);
                il.Emit(OpCodes.Call, def);
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = (*a)(*b, *c, *d, *e)
            case HlOpcodeKind.Call4: {
                var dst = instruction.Parameters[0];
                var fun = instruction.Parameters[1];
                var arg1 = instruction.Parameters[2];
                var arg2 = instruction.Parameters[3];
                var arg3 = instruction.Parameters[4];
                var arg4 = instruction.Parameters[5];

                var def = ResolveDefinitionFromFIndex(fun);

                LoadLocal(il, locals, arg1);
                LoadLocal(il, locals, arg2);
                LoadLocal(il, locals, arg3);
                LoadLocal(il, locals, arg4);
                il.Emit(OpCodes.Call, def);
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = (*a)(...)
            case HlOpcodeKind.CallN: {
                var dst = instruction.Parameters[0];
                var fun = instruction.Parameters[1];
                var args = instruction.Parameters[2..];

                var def = ResolveDefinitionFromFIndex(fun);

                foreach (var arg in args)
                    LoadLocal(il, locals, arg);
                il.Emit(OpCodes.Call, def);
                SetLocal(il, locals, dst);
                break;
            }

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

            // *dst = *a
            case HlOpcodeKind.GetGlobal: {
                var dst = instruction.Parameters[0];
                var a = instruction.Parameters[1];

                LoadGlobal(il, a, asmDef);
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = *a
            case HlOpcodeKind.SetGlobal: {
                var dst = instruction.Parameters[0];
                var a = instruction.Parameters[1];

                LoadLocal(il, locals, a);
                SetGlobal(il, dst, asmDef);
                break;
            }

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

            // Jump to offset if true.
            case HlOpcodeKind.JTrue: {
                var a = instruction.Parameters[0];
                var offset = instruction.Parameters[1];

                LoadLocal(il, locals, a);
                il.Emit(OpCodes.Brtrue, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if false.
            case HlOpcodeKind.JFalse: {
                var a = instruction.Parameters[0];
                var offset = instruction.Parameters[1];

                LoadLocal(il, locals, a);
                il.Emit(OpCodes.Brfalse, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if null.
            case HlOpcodeKind.JNull: {
                var a = instruction.Parameters[0];
                var offset = instruction.Parameters[1];

                LoadLocal(il, locals, a);
                il.Emit(OpCodes.Brfalse, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if not null.
            case HlOpcodeKind.JNotNull: {
                var a = instruction.Parameters[0];
                var offset = instruction.Parameters[1];

                LoadLocal(il, locals, a);
                il.Emit(OpCodes.Brtrue, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if *a < *b.
            case HlOpcodeKind.JSLt: {
                var a = instruction.Parameters[0];
                var b = instruction.Parameters[1];
                var offset = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(OpCodes.Clt);
                il.Emit(OpCodes.Brtrue, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if *a >= *b.
            case HlOpcodeKind.JSGte: {
                var a = instruction.Parameters[0];
                var b = instruction.Parameters[1];
                var offset = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(OpCodes.Clt);
                il.Emit(OpCodes.Brfalse, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if *a > *b.
            case HlOpcodeKind.JSGt: {
                var a = instruction.Parameters[0];
                var b = instruction.Parameters[1];
                var offset = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(OpCodes.Cgt);
                il.Emit(OpCodes.Brtrue, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if *a <= *b.
            case HlOpcodeKind.JSLte: {
                var a = instruction.Parameters[0];
                var b = instruction.Parameters[1];
                var offset = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(OpCodes.Cgt);
                il.Emit(OpCodes.Brfalse, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if *a < *b.
            case HlOpcodeKind.JULt: {
                var a = instruction.Parameters[0];
                var b = instruction.Parameters[1];
                var offset = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(OpCodes.Clt_Un);
                il.Emit(OpCodes.Brtrue, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if *a >= *b.
            case HlOpcodeKind.JUGte: {
                var a = instruction.Parameters[0];
                var b = instruction.Parameters[1];
                var offset = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(OpCodes.Clt_Un);
                il.Emit(OpCodes.Brfalse, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if !(*a < *b).
            case HlOpcodeKind.JNotLt: {
                var a = instruction.Parameters[0];
                var b = instruction.Parameters[1];
                var offset = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(OpCodes.Clt);
                il.Emit(OpCodes.Brfalse, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if !(*a >= *b).
            case HlOpcodeKind.JNotGte: {
                var a = instruction.Parameters[0];
                var b = instruction.Parameters[1];
                var offset = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(OpCodes.Clt);
                il.Emit(OpCodes.Brtrue, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if *a == *b.
            case HlOpcodeKind.JEq: {
                var a = instruction.Parameters[0];
                var b = instruction.Parameters[1];
                var offset = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brtrue, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if *a != *b.
            case HlOpcodeKind.JNotEq: {
                var a = instruction.Parameters[0];
                var b = instruction.Parameters[1];
                var offset = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brfalse, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset always.
            case HlOpcodeKind.JAlways: {
                var offset = instruction.Parameters[0];

                il.Emit(OpCodes.Br, markers[originalIndexForJump + offset]);
                break;
            }

            case HlOpcodeKind.ToDyn:
                break;

            case HlOpcodeKind.ToSFloat: {
                var dst = instruction.Parameters[0];
                var src = instruction.Parameters[1];

                LoadLocal(il, locals, src);
                // il.Emit(OpCodes.Conv_R4);
                il.Emit(OpCodes.Conv_R8);
                SetLocal(il, locals, dst);
                break;
            }

            case HlOpcodeKind.ToUFloat: {
                var dst = instruction.Parameters[0];
                var src = instruction.Parameters[1];

                LoadLocal(il, locals, src);
                il.Emit(OpCodes.Conv_R_Un);
                SetLocal(il, locals, dst);
                break;
            }

            case HlOpcodeKind.ToInt: {
                var dst = instruction.Parameters[0];
                var src = instruction.Parameters[1];

                LoadLocal(il, locals, src);
                il.Emit(OpCodes.Conv_I4);
                SetLocal(il, locals, dst);
                break;
            }

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

            case HlOpcodeKind.Nop: {
                // Ironically, this is not a nop.
                il.Emit(OpCodes.Nop);
                break;
            }

            case HlOpcodeKind.Last:
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private MethodDefinition ResolveDefinitionFromFIndex(int fIndex) {
        var corrected = hash.FunctionIndexes[fIndex];
        return corrected >= hash.Code.Functions.Count
            ? nativeMethodDefs[hash.Code.Natives[corrected - hash.Code.Functions.Count]]
            : funMethodDefs[hash.Code.Functions[corrected]];
    }
}
