using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler.ILGeneration;

public static class FunctionCompiler {
    public static MethodDefinition CompileNative(HlNative function, AssemblyDefinition asmDef, Compilation compilation) {
        var funType = ((HlTypeWithFun)function.Type.Value!).FunctionDescription;
        var method = compilation.GetNative(function).Method;

        var body = method.Body = new MethodBody(method);
        var il = body.GetILProcessor();

        var callNativeMethodRef = asmDef.MainModule.ImportReference(typeof(SharpLinkNativeCallerHelper).GetMethod("CallNative", new[] { typeof(string), typeof(string), typeof(object[]) }));

        // Make method invoke:
        // Tomat.SharpLink.SharpLinkNativeCallerHelper.CallNative(lib, name, new object[] { arg0, arg1, ...});

        il.Emit(Ldstr, function.Lib);
        il.Emit(Ldstr, function.Name);
        il.Emit(Ldc_I4, funType.Arguments.Length);
        il.Emit(Newarr, asmDef.MainModule.TypeSystem.Object);

        var argIndex = 0;

        for (var i = method.Parameters.Count - 1; i >= 0; i--) {
            il.Emit(Dup);
            il.Emit(Ldc_I4, argIndex++);
            il.Emit(Ldarg, i);

            if (method.Parameters[i].ParameterType.IsValueType)
                il.Emit(Box, method.Parameters[i].ParameterType);

            il.Emit(Stelem_Ref);
        }

        il.Emit(Call, callNativeMethodRef);

        if (method.ReturnType.FullName == "System.Void")
            il.Emit(Pop);
        else if (method.ReturnType.IsValueType)
            il.Emit(Unbox_Any, method.ReturnType);
        else
            il.Emit(Castclass, method.ReturnType);

        il.Emit(Ret);
        return method;
    }

    public static MethodDefinition CompileFunction(HlFunction function, AssemblyDefinition asmDef, HlCodeHash hash, Compilation compilation) {
        var compiled = compilation.GetFunction(function);
        var emitter = new FunctionEmitter(compilation.GetFunction(function), asmDef, hash, compilation);
        emitter.EmitToMethod(compiled.Method);
        return compiled.Method;
    }

    /*private void GenerateInstruction(HlOpcode instruction, List<VariableDefinition> locals, ILProcessor il, AssemblyDefinition asmDef, int originalIndex, Dictionary<int, Instruction> markers, MethodDefinition method) {
        var originalIndexForJump = originalIndex + 1;

        if (markers.TryGetValue(originalIndex, out var marker))
            il.Append(marker);

        switch (instruction.Kind) {
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

                il.Emit(Ldnull);
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
                il.Emit(Add);
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
                il.Emit(Sub);
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
                il.Emit(Mul);
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
                il.Emit(Div);
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
                il.Emit(Div);
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
                il.Emit(Rem);
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
                il.Emit(Rem);
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
                il.Emit(Shl);
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
                il.Emit(Shr);
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
                il.Emit(Shr);
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
                il.Emit(And);
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
                il.Emit(Or);
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
                il.Emit(Xor);
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = -(*a)
            case HlOpcodeKind.Neg: {
                var dst = instruction.Parameters[0];
                var a = instruction.Parameters[1];

                LoadLocal(il, locals, a);
                il.Emit(Neg);
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = !(*a)
            case HlOpcodeKind.Not: {
                var dst = instruction.Parameters[0];
                var a = instruction.Parameters[1];

                LoadLocal(il, locals, a);
                il.Emit(Not);
                SetLocal(il, locals, dst);
                break;
            }

            // (*dst)++
            case HlOpcodeKind.Incr: {
                var dst = instruction.Parameters[0];

                LoadLocal(il, locals, dst);
                il.Emit(Ldc_I4_1);
                il.Emit(Add);
                SetLocal(il, locals, dst);
                break;
            }

            // (*dst)--
            case HlOpcodeKind.Decr: {
                var dst = instruction.Parameters[0];

                LoadLocal(il, locals, dst);
                il.Emit(Ldc_I4_1);
                il.Emit(Sub);
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = (*a)()
            case HlOpcodeKind.Call0: {
                var dst = instruction.Parameters[0];
                var fun = instruction.Parameters[1];

                var def = CompiledFunctionFromFunctionIndex(fun).Method;

                il.Emit(Call, def);
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = (*a)(*b)
            case HlOpcodeKind.Call1: {
                var dst = instruction.Parameters[0];
                var fun = instruction.Parameters[1];
                var arg = instruction.Parameters[2];

                var def = CompiledFunctionFromFunctionIndex(fun).Method;

                LoadLocalThatMayNeedToBeConvertedToHaxeDyn(il, locals, arg, def.Parameters[0].ParameterType, asmDef);
                il.Emit(Call, def);
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = (*a)(*b, *c)
            case HlOpcodeKind.Call2: {
                var dst = instruction.Parameters[0];
                var fun = instruction.Parameters[1];
                var arg1 = instruction.Parameters[2];
                var arg2 = instruction.Parameters[3];

                var def = CompiledFunctionFromFunctionIndex(fun).Method;

                LoadLocalThatMayNeedToBeConvertedToHaxeDyn(il, locals, arg1, def.Parameters[0].ParameterType, asmDef);
                LoadLocalThatMayNeedToBeConvertedToHaxeDyn(il, locals, arg2, def.Parameters[1].ParameterType, asmDef);
                il.Emit(Call, def);
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

                var def = CompiledFunctionFromFunctionIndex(fun).Method;

                LoadLocalThatMayNeedToBeConvertedToHaxeDyn(il, locals, arg1, def.Parameters[0].ParameterType, asmDef);
                LoadLocalThatMayNeedToBeConvertedToHaxeDyn(il, locals, arg2, def.Parameters[1].ParameterType, asmDef);
                LoadLocalThatMayNeedToBeConvertedToHaxeDyn(il, locals, arg3, def.Parameters[2].ParameterType, asmDef);
                il.Emit(Call, def);
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

                var def = CompiledFunctionFromFunctionIndex(fun).Method;

                LoadLocalThatMayNeedToBeConvertedToHaxeDyn(il, locals, arg1, def.Parameters[0].ParameterType, asmDef);
                LoadLocalThatMayNeedToBeConvertedToHaxeDyn(il, locals, arg2, def.Parameters[1].ParameterType, asmDef);
                LoadLocalThatMayNeedToBeConvertedToHaxeDyn(il, locals, arg3, def.Parameters[2].ParameterType, asmDef);
                LoadLocalThatMayNeedToBeConvertedToHaxeDyn(il, locals, arg4, def.Parameters[3].ParameterType, asmDef);
                il.Emit(Call, def);
                SetLocal(il, locals, dst);
                break;
            }

            // *dst = (*a)(...)
            case HlOpcodeKind.CallN: {
                var dst = instruction.Parameters[0];
                var fun = instruction.Parameters[1];
                var args = instruction.Parameters[3..];

                var def = CompiledFunctionFromFunctionIndex(fun).Method;

                for (var i = 0; i < args.Length; i++)
                    LoadLocalThatMayNeedToBeConvertedToHaxeDyn(il, locals, args[i], def.Parameters[i].ParameterType, asmDef);

                il.Emit(Call, def);
                SetLocal(il, locals, dst);
                break;
            }

            case HlOpcodeKind.CallMethod: {
                var dst = instruction.Parameters[0];
                var field = instruction.Parameters[1];
                var args = instruction.Parameters[3..];

                var varDef = locals[args[0]];
                var varTypeDef = varDef.VariableType.Resolve();
                var fieldDef = compilation.GetAllProtosFor(varTypeDef)[field];
                var def = fieldDef.FieldType.Resolve().Methods.First(m => m.Name == "Invoke");

                il.Emit(Ldfld, fieldDef);
                LoadLocalThatMayNeedToBeConvertedToHaxeDyn(il, locals, args[0], def.Parameters[0].ParameterType, asmDef);
                for (var i = 1; i < args.Length; i++)
                    LoadLocalThatMayNeedToBeConvertedToHaxeDyn(il, locals, args[i], def.Parameters[i].ParameterType, asmDef);
                il.Emit(Callvirt, def);
                SetLocal(il, locals, dst);
                break;
            }

            case HlOpcodeKind.CallThis: {
                var dst = instruction.Parameters[0];
                var field = instruction.Parameters[1];
                var args = instruction.Parameters[3..];

                var varDef = locals[0];
                var varTypeDef = varDef.VariableType.Resolve();
                var fieldDef = compilation.GetAllProtosFor(varTypeDef)[field];
                var def = fieldDef.FieldType.Resolve().Methods.First(m => m.Name == "Invoke");

                il.Emit(Ldfld, fieldDef);
                LoadLocalThatMayNeedToBeConvertedToHaxeDyn(il, locals, 0, def.Parameters[0].ParameterType, asmDef);
                for (var i = 0; i < args.Length; i++)
                    LoadLocalThatMayNeedToBeConvertedToHaxeDyn(il, locals, args[i], def.Parameters[i + 1].ParameterType, asmDef);
                il.Emit(Callvirt, def);
                SetLocal(il, locals, dst);
                break;
            }

            case HlOpcodeKind.CallClosure: {
                var dst = instruction.Parameters[0];
                var fun = instruction.Parameters[1];
                var args = instruction.Parameters[3..];

                var varDef = locals[fun];
                var varTypeDef = varDef.VariableType.Resolve();

                var dynamic = varTypeDef.FullName == "Tomat.SharpLink.HaxeDyn";
                var invokeDef = dynamic ? varTypeDef.Methods.First(m => m.Name == "InvokeDynamic") : varTypeDef.Methods.First(m => m.Name == "Invoke");

                LoadLocal(il, locals, fun);

                if (dynamic) {
                    // pass in as params object[]
                    il.Emit(Ldc_I4, args.Length);
                    il.Emit(Newarr, asmDef.MainModule.ImportReference(typeof(object)));

                    for (var i = 0; i < args.Length; i++) {
                        il.Emit(Dup);
                        il.Emit(Ldc_I4, i);
                        LoadLocal(il, locals, args[i]);
                        if (locals[args[i]].VariableType.Resolve().IsValueType)
                            il.Emit(Box, locals[args[i]].VariableType.Resolve());
                        il.Emit(Stelem_Ref);
                    }
                }
                else {
                    for (var i = 0; i < args.Length; i++) {
                        var localArg = locals[args[i]];

                        LoadLocal(il, locals, args[i]);

                        if (invokeDef.Parameters[i].ParameterType.FullName == "Tomat.SharpLink.HaxeDyn" && localArg.VariableType.FullName != "Tomat.SharpLink.HaxeDyn") {
                            if (localArg.VariableType.Resolve().IsValueType)
                                il.Emit(Box, localArg.VariableType.Resolve());

                            var dynDef = asmDef.MainModule.ImportReference(typeof(HaxeDyn));
                            var dynCtor = dynDef.Resolve().Methods.First(m => m.IsConstructor && m.Parameters.Count == 1);
                            il.Emit(Newobj, asmDef.MainModule.ImportReference(dynCtor));
                        }
                    }
                }

                il.Emit(Callvirt, asmDef.MainModule.ImportReference(invokeDef));
                SetLocal(il, locals, dst);
                break;
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.StaticClosure:
                throw new NotImplementedException();

            case HlOpcodeKind.InstanceClosure: {
                var dst = instruction.Parameters[0];
                var fun = instruction.Parameters[1];
                var obj = instruction.Parameters[2];

                break;
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.VirtualClosure:
                throw new NotImplementedException();

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

            case HlOpcodeKind.Field: {
                var dst = instruction.Parameters[0];
                var obj = instruction.Parameters[1];
                var field = instruction.Parameters[2];

                var varDef = locals[obj];
                var varTypeDef = varDef.VariableType.Resolve();
                var fieldDef = compilation.GetAllFieldsFor(varTypeDef)[field];

                LoadLocal(il, locals, obj);
                il.Emit(Ldfld, fieldDef);
                SetLocal(il, locals, dst);
                break;
            }

            case HlOpcodeKind.SetField: {
                var obj = instruction.Parameters[0];
                var field = instruction.Parameters[1];
                var src = instruction.Parameters[2];

                var varDef = locals[obj];
                var varTypeDef = varDef.VariableType.Resolve();
                var fieldDef = compilation.GetAllFieldsFor(varTypeDef)[field];

                LoadLocal(il, locals, obj);
                LoadLocal(il, locals, src);
                il.Emit(Stfld, fieldDef);
                break;
            }

            case HlOpcodeKind.GetThis: {
                var dst = instruction.Parameters[0];
                var field = instruction.Parameters[1];

                var varDef = locals[0];
                var varTypeDef = varDef.VariableType.Resolve();
                var fieldDef = compilation.GetAllFieldsFor(varTypeDef)[field];

                il.Emit(Ldarg_0);
                il.Emit(Ldfld, fieldDef);
                SetLocal(il, locals, dst);
                break;
            }

            case HlOpcodeKind.SetThis: {
                var field = instruction.Parameters[0];
                var src = instruction.Parameters[1];

                var varDef = locals[0];
                var varTypeDef = varDef.VariableType.Resolve();
                var fieldDef = compilation.GetAllFieldsFor(varTypeDef)[field];

                il.Emit(Ldarg_0);
                LoadLocal(il, locals, src);
                il.Emit(Stfld, fieldDef);
                break;
            }

            case HlOpcodeKind.DynGet: {
                var dst = instruction.Parameters[0];
                var obj = instruction.Parameters[1];
                var field = instruction.Parameters[2];

                var fieldName = hash.Code.Strings[field];

                // Call HaxeDyn::GetField(fieldName)
                LoadLocal(il, locals, obj);
                il.Emit(Ldstr, fieldName);
                il.Emit(Callvirt, asmDef.MainModule.ImportReference(typeof(HaxeDyn).GetMethod("GetField", new[] { typeof(string) })));
                SetLocal(il, locals, dst);
                break;
            }

            // TODO: I haven't encountered this being used yet, make sure it
            // works.
            case HlOpcodeKind.DynSet: {
                var obj = instruction.Parameters[0];
                var field = instruction.Parameters[1];
                var src = instruction.Parameters[2];

                var fieldName = hash.Code.Strings[field];

                // Call HaxeDyn::SetField(fieldName, src)
                LoadLocal(il, locals, obj);
                il.Emit(Ldstr, fieldName);
                LoadLocal(il, locals, src);
                il.Emit(Callvirt, asmDef.MainModule.ImportReference(typeof(HaxeDyn).GetMethod("SetField", new[] { typeof(string), typeof(object) })));
                break;
            }

            // Jump to offset if true.
            case HlOpcodeKind.JTrue: {
                var a = instruction.Parameters[0];
                var offset = instruction.Parameters[1];

                LoadLocal(il, locals, a);
                il.Emit(Brtrue, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if false.
            case HlOpcodeKind.JFalse: {
                var a = instruction.Parameters[0];
                var offset = instruction.Parameters[1];

                LoadLocal(il, locals, a);
                il.Emit(Brfalse, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if null.
            case HlOpcodeKind.JNull: {
                var a = instruction.Parameters[0];
                var offset = instruction.Parameters[1];

                LoadLocal(il, locals, a);
                il.Emit(Brfalse, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if not null.
            case HlOpcodeKind.JNotNull: {
                var a = instruction.Parameters[0];
                var offset = instruction.Parameters[1];

                LoadLocal(il, locals, a);
                il.Emit(Brtrue, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if *a < *b.
            case HlOpcodeKind.JSLt: {
                var a = instruction.Parameters[0];
                var b = instruction.Parameters[1];
                var offset = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(Clt);
                il.Emit(Brtrue, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if *a >= *b.
            case HlOpcodeKind.JSGte: {
                var a = instruction.Parameters[0];
                var b = instruction.Parameters[1];
                var offset = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(Clt);
                il.Emit(Brfalse, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if *a > *b.
            case HlOpcodeKind.JSGt: {
                var a = instruction.Parameters[0];
                var b = instruction.Parameters[1];
                var offset = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(Cgt);
                il.Emit(Brtrue, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if *a <= *b.
            case HlOpcodeKind.JSLte: {
                var a = instruction.Parameters[0];
                var b = instruction.Parameters[1];
                var offset = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(Cgt);
                il.Emit(Brfalse, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if *a < *b.
            case HlOpcodeKind.JULt: {
                var a = instruction.Parameters[0];
                var b = instruction.Parameters[1];
                var offset = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(Clt_Un);
                il.Emit(Brtrue, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if *a >= *b.
            case HlOpcodeKind.JUGte: {
                var a = instruction.Parameters[0];
                var b = instruction.Parameters[1];
                var offset = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(Clt_Un);
                il.Emit(Brfalse, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if !(*a < *b).
            case HlOpcodeKind.JNotLt: {
                var a = instruction.Parameters[0];
                var b = instruction.Parameters[1];
                var offset = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(Clt);
                il.Emit(Brfalse, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if !(*a >= *b).
            case HlOpcodeKind.JNotGte: {
                var a = instruction.Parameters[0];
                var b = instruction.Parameters[1];
                var offset = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(Clt);
                il.Emit(Brtrue, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if *a == *b.
            case HlOpcodeKind.JEq: {
                var a = instruction.Parameters[0];
                var b = instruction.Parameters[1];
                var offset = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(Ceq);
                il.Emit(Brtrue, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset if *a != *b.
            case HlOpcodeKind.JNotEq: {
                var a = instruction.Parameters[0];
                var b = instruction.Parameters[1];
                var offset = instruction.Parameters[2];

                LoadLocal(il, locals, a);
                LoadLocal(il, locals, b);
                il.Emit(Ceq);
                il.Emit(Brfalse, markers[originalIndexForJump + offset]);
                break;
            }

            // Jump to offset always.
            case HlOpcodeKind.JAlways: {
                var offset = instruction.Parameters[0];

                il.Emit(Br, markers[originalIndexForJump + offset]);
                break;
            }

            // TODO: used
            case HlOpcodeKind.ToDyn: {
                var dst = instruction.Parameters[0];
                var src = instruction.Parameters[1];

                var haxeDynRef = asmDef.MainModule.ImportReference(typeof(HaxeDyn));
                var haxeDynCtor = asmDef.MainModule.ImportReference(haxeDynRef.Resolve().Methods.First(m => m.IsConstructor && m.Parameters.Count == 1));
                var varType = locals[src].VariableType.Resolve();

                LoadLocal(il, locals, src);
                if (varType.IsValueType)
                    il.Emit(Box, asmDef.MainModule.ImportReference(varType));
                il.Emit(Newobj, haxeDynCtor);
                SetLocal(il, locals, dst);
                break;
            }

            case HlOpcodeKind.ToSFloat: {
                var dst = instruction.Parameters[0];
                var src = instruction.Parameters[1];

                LoadLocal(il, locals, src);
                // il.Emit(OpCodes.Conv_R4);
                il.Emit(Conv_R8);
                SetLocal(il, locals, dst);
                break;
            }

            case HlOpcodeKind.ToUFloat: {
                var dst = instruction.Parameters[0];
                var src = instruction.Parameters[1];

                LoadLocal(il, locals, src);
                il.Emit(Conv_R_Un);
                SetLocal(il, locals, dst);
                break;
            }

            case HlOpcodeKind.ToInt: {
                var dst = instruction.Parameters[0];
                var src = instruction.Parameters[1];

                LoadLocal(il, locals, src);
                il.Emit(Conv_I4);
                SetLocal(il, locals, dst);
                break;
            }

            // TODO: used
            case HlOpcodeKind.SafeCast:
                break;

            // TODO: used
            case HlOpcodeKind.UnsafeCast:
                break;

            case HlOpcodeKind.ToVirtual: {
                var dst = instruction.Parameters[0];
                var src = instruction.Parameters[1];

                // TTo SharpLinkCastHelper::CastVirtual<TFrom, TTo>(TFrom);
                var toVirtual = asmDef.MainModule.ImportReference(typeof(SharpLinkCastHelper).GetMethod(nameof(SharpLinkCastHelper.CastVirtual)));
                var fromType = locals[src].VariableType;
                var toType = locals[dst].VariableType;
                var genericToVirtual = new GenericInstanceMethod(toVirtual);
                genericToVirtual.GenericArguments.Add(fromType);
                genericToVirtual.GenericArguments.Add(toType);

                LoadLocal(il, locals, src);
                il.Emit(Call, genericToVirtual);
                SetLocal(il, locals, dst);
                break;
            }

            case HlOpcodeKind.Label: {
                // no-op
                il.Emit(Nop);
                break;
            }

            case HlOpcodeKind.Ret: {
                var localIndex = instruction.Parameters[0];

                var varType = locals[localIndex].VariableType.Resolve();
                var retType = method.ReturnType.Resolve();

                LoadLocalThatMayNeedToBeConvertedToHaxeDyn(il, locals, localIndex, retType, asmDef);
                il.Emit(Ret);
                break;
            }

            case HlOpcodeKind.Throw: {
                var reg = instruction.Parameters[0];

                // SharpLinkExceptionHelper::CreateNetExceptionFromHaxeException(SharpLinkException);
                var createNetExceptionFromHaxeException = asmDef.MainModule.ImportReference(typeof(SharpLinkExceptionHelper).GetMethod(nameof(SharpLinkExceptionHelper.CreateNetExceptionFromHaxeException)));

                LoadLocal(il, locals, reg);
                il.Emit(Call, createNetExceptionFromHaxeException);
                il.Emit(Throw);
                break;
            }

            // TODO: used
            case HlOpcodeKind.Rethrow:
                break;

            // TODO: used
            case HlOpcodeKind.Switch:
                break;

            case HlOpcodeKind.NullCheck: {
                var reg = instruction.Parameters[0];

                // if (reg == null) throw SharpLinkExceptionHelper.CreateNullCheckException();

                var createNullCheckException = asmDef.MainModule.ImportReference(typeof(SharpLinkExceptionHelper).GetMethod(nameof(SharpLinkExceptionHelper.CreateNullCheckException)));
                var label = il.Create(Nop);

                LoadLocal(il, locals, reg);
                il.Emit(Ldnull);
                il.Emit(Ceq);
                il.Emit(Brfalse, label);
                il.Emit(Call, createNullCheckException);
                il.Emit(Throw);
                il.Append(label);
                break;
            }

            // TODO: used
            case HlOpcodeKind.Trap:
                break;

            // TODO: used
            case HlOpcodeKind.EndTrap:
                break;

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.GetI8:
                throw new NotImplementedException();

            // TODO: used
            case HlOpcodeKind.GetI16:
                break;

            // TODO: used
            case HlOpcodeKind.GetMem:
                break;

            // TODO: used
            case HlOpcodeKind.GetArray:
                break;

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.SetI8:
                throw new NotImplementedException();

            // TODO: used
            case HlOpcodeKind.SetI16:
                break;

            // TODO: used
            case HlOpcodeKind.SetMem:
                break;

            // TODO: used
            case HlOpcodeKind.SetArray:
                break;

            case HlOpcodeKind.New: {
                var dst = instruction.Parameters[0];

                var ctor = asmDef.MainModule.ImportReference(locals[dst].VariableType.Resolve().Methods.First(x => x.IsConstructor && x.Parameters.Count == 0));

                il.Emit(Newobj, ctor);
                SetLocal(il, locals, dst);
                break;
            }

            // TODO: used
            case HlOpcodeKind.ArraySize:
                break;

            // TODO: used
            case HlOpcodeKind.Type:
                break;

            // TODO: used
            case HlOpcodeKind.GetType:
                break;

            // TODO: used
            case HlOpcodeKind.GetTID:
                break;

            case HlOpcodeKind.Ref: {
                var dst = instruction.Parameters[0];
                var src = instruction.Parameters[1];

                var genericArgument = GetTypeForLocal(locals, src);
                var haxeRefType = asmDef.MainModule.ImportReference(typeof(HaxeRef<>)).MakeGenericInstanceType(genericArgument);
                var haxeRefCtor = asmDef.MainModule.ImportReference(haxeRefType.Resolve().Methods.First(x => x.IsConstructor && x.Parameters.Count == 1)).MakeHostInstanceGeneric(genericArgument);

                LoadLocal(il, locals, src);
                il.Emit(Newobj, haxeRefCtor);
                SetLocal(il, locals, dst);
                break;
            }

            case HlOpcodeKind.Unref: {
                var dst = instruction.Parameters[0];
                var src = instruction.Parameters[1];

                var genericArgument = GetTypeForLocal(locals, dst);
                var haxeRefType = asmDef.MainModule.ImportReference(typeof(HaxeRef<>)).MakeGenericInstanceType(genericArgument);
                var haxeRefValueField = asmDef.MainModule.ImportReference(haxeRefType.Resolve().Fields.First(x => x.Name == "Value")).MakeHostInstanceGeneric(genericArgument);

                LoadLocal(il, locals, src);
                il.Emit(Ldfld, haxeRefValueField);
                SetLocal(il, locals, dst);
                break;
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.Setref:
                throw new NotImplementedException();

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.MakeEnum:
                throw new NotImplementedException();

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.EnumAlloc:
                throw new NotImplementedException();

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.EnumIndex:
                throw new NotImplementedException();

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.EnumField:
                throw new NotImplementedException();

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.SetEnumField:
                throw new NotImplementedException();

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.Assert:
                throw new NotImplementedException();

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.RefData:
                throw new NotImplementedException();

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.RefOffset:
                throw new NotImplementedException();

            case HlOpcodeKind.Nop: {
                // Ironically, this is not a nop.
                il.Emit(Nop);
                break;
            }

            case HlOpcodeKind.Last:
                throw new InvalidOperationException("Last opcode should not be emitted.");

            default:
                throw new ArgumentOutOfRangeException(nameof(instruction));
        }
    }*/
}
