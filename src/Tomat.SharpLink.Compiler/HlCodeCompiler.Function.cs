using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
    private Dictionary<HlFunction, MethodDefinition> funMethodDefs = new();
    private Dictionary<HlNative, MethodDefinition> nativeMethodDefs = new();

    private void DefineNative(HlNative native, AssemblyDefinition asmDef) {
        var funType = ((HlTypeWithFun)native.T.Value!).Fun;
        var method = CreateMethod(native, funType, asmDef);
        var attr = new CustomAttribute(asmDef.MainModule.ImportReference(typeof(HashLinkNativeImport).GetConstructor(new[] { typeof(string), typeof(string) })));
        attr.ConstructorArguments.Add(new CustomAttributeArgument(asmDef.MainModule.TypeSystem.String, native.Lib));
        attr.ConstructorArguments.Add(new CustomAttributeArgument(asmDef.MainModule.TypeSystem.String, native.Name));
        method.CustomAttributes.Add(attr);
        nativeMethodDefs.Add(native, method);
    }

    private void CompileNative(HlNative native, AssemblyDefinition asmDef) {
        var funType = ((HlTypeWithFun)native.T.Value!).Fun;
        var method = nativeMethodDefs[native];

        var body = method.Body = new MethodBody(method);
        var il = body.GetILProcessor();

        var callNativeMethod = asmDef.MainModule.ImportReference(typeof(SharpLinkNativeCallerHelper).GetMethod("CallNative"));
        var callNativeMethodRef = asmDef.MainModule.ImportReference(typeof(SharpLinkNativeCallerHelper).GetMethod("CallNative", new[] { typeof(string), typeof(string), typeof(object[]) }));

        // Make method invoke:
        // Tomat.SharpLink.SharpLinkNativeCallerHelper.CallNative(lib, name, new object[] { arg0, arg1, ...});

        il.Emit(OpCodes.Ldstr, native.Lib);
        il.Emit(OpCodes.Ldstr, native.Name);
        il.Emit(OpCodes.Ldc_I4, funType.Arguments.Length);
        il.Emit(OpCodes.Newarr, asmDef.MainModule.TypeSystem.Object);

        var argIndex = 0;

        for (var i = method.Parameters.Count - 1; i >= 0; i--) {
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4, argIndex++);
            il.Emit(OpCodes.Ldarg, i);

            if (method.Parameters[i].ParameterType.IsValueType)
                il.Emit(OpCodes.Box, method.Parameters[i].ParameterType);

            il.Emit(OpCodes.Stelem_Ref);
        }

        il.Emit(OpCodes.Call, callNativeMethodRef);

        // check if return is void
        if (method.ReturnType.FullName == "System.Void")
            il.Emit(OpCodes.Pop);
        else if (method.ReturnType.IsValueType)
            il.Emit(OpCodes.Unbox_Any, method.ReturnType);
        else
            il.Emit(OpCodes.Castclass, method.ReturnType);

        il.Emit(OpCodes.Ret);

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
            GenerateInstruction(instr, locals, il, asmDef, i, markers, method);
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

    private TypeReference GetTypeForLocal(List<VariableDefinition> locals, int index) {
        return locals[index].VariableType;
    }

    private void GenerateInstruction(HlOpcode instruction, List<VariableDefinition> locals, ILProcessor il, AssemblyDefinition asmDef, int originalIndex, Dictionary<int, Instruction> markers, MethodDefinition method) {
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
                var args = instruction.Parameters[3..];

                var def = ResolveDefinitionFromFIndex(fun);

                foreach (var arg in args)
                    LoadLocal(il, locals, arg);
                il.Emit(OpCodes.Call, def);
                SetLocal(il, locals, dst);
                break;
            }

            case HlOpcodeKind.CallMethod: {
                var dst = instruction.Parameters[0];
                var field = instruction.Parameters[1];
                var args = instruction.Parameters[3..];

                var varDef = locals[args[0]];
                var varTypeDef = varDef.VariableType.Resolve();
                var fieldDef = objTypeDefProtos[varTypeDef][field];

                il.Emit(OpCodes.Ldfld, fieldDef);
                LoadLocal(il, locals, args[0]);
                for (var i = 1; i < args.Length; i++)
                    LoadLocal(il, locals, args[i]);
                il.Emit(OpCodes.Callvirt, fieldDef.FieldType.Resolve().Methods.First(m => m.Name == "Invoke"));
                SetLocal(il, locals, dst);
                break;
            }

            case HlOpcodeKind.CallThis: {
                var dst = instruction.Parameters[0];
                var field = instruction.Parameters[1];
                var args = instruction.Parameters[3..];

                var varDef = locals[0];
                var varTypeDef = varDef.VariableType.Resolve();
                var fieldDef = objTypeDefProtos[varTypeDef][field];

                il.Emit(OpCodes.Ldfld, fieldDef);
                il.Emit(OpCodes.Ldarg_0);
                foreach (var arg in args)
                    LoadLocal(il, locals, arg);
                il.Emit(OpCodes.Callvirt, fieldDef.FieldType.Resolve().Methods.First(m => m.Name == "Invoke"));
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
                    il.Emit(OpCodes.Ldc_I4, args.Length);
                    il.Emit(OpCodes.Newarr, asmDef.MainModule.ImportReference(typeof(object)));

                    for (var i = 0; i < args.Length; i++) {
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Ldc_I4, i);
                        LoadLocal(il, locals, args[i]);
                        if (locals[args[i]].VariableType.Resolve().IsValueType)
                            il.Emit(OpCodes.Box, locals[args[i]].VariableType.Resolve());
                        il.Emit(OpCodes.Stelem_Ref);
                    }
                }
                else {
                    for (var i = 0; i < args.Length; i++) {
                        var localArg = locals[args[i]];

                        LoadLocal(il, locals, args[i]);

                        if (invokeDef.Parameters[i].ParameterType.FullName == "Tomat.SharpLink.HaxeDyn" && localArg.VariableType.FullName != "Tomat.SharpLink.HaxeDyn") {
                            if (localArg.VariableType.Resolve().IsValueType)
                                il.Emit(OpCodes.Box, localArg.VariableType.Resolve());

                            var dynDef = asmDef.MainModule.ImportReference(typeof(HaxeDyn));
                            var dynCtor = dynDef.Resolve().Methods.First(m => m.IsConstructor && m.Parameters.Count == 1);
                            il.Emit(OpCodes.Newobj, asmDef.MainModule.ImportReference(dynCtor));
                        }
                    }
                }

                il.Emit(OpCodes.Callvirt, asmDef.MainModule.ImportReference(invokeDef));
                SetLocal(il, locals, dst);
                break;
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.StaticClosure:
                throw new NotImplementedException();

            // TODO: used
            case HlOpcodeKind.InstanceClosure:
                break;

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
                var fieldDef = objTypeDefFields[varTypeDef][field];

                LoadLocal(il, locals, obj);
                il.Emit(OpCodes.Ldfld, fieldDef);
                SetLocal(il, locals, dst);
                break;
            }

            case HlOpcodeKind.SetField: {
                var obj = instruction.Parameters[0];
                var field = instruction.Parameters[1];
                var src = instruction.Parameters[2];

                var varDef = locals[obj];
                var varTypeDef = varDef.VariableType.Resolve();
                var fieldDef = objTypeDefFields[varTypeDef][field];

                LoadLocal(il, locals, obj);
                LoadLocal(il, locals, src);
                il.Emit(OpCodes.Stfld, fieldDef);
                break;
            }

            case HlOpcodeKind.GetThis: {
                var dst = instruction.Parameters[0];
                var field = instruction.Parameters[1];

                var varDef = locals[0];
                var varTypeDef = varDef.VariableType.Resolve();
                var fieldDef = objTypeDefFields[varTypeDef][field];

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, fieldDef);
                SetLocal(il, locals, dst);
                break;
            }

            case HlOpcodeKind.SetThis: {
                var field = instruction.Parameters[0];
                var src = instruction.Parameters[1];

                var varDef = locals[0];
                var varTypeDef = varDef.VariableType.Resolve();
                var fieldDef = objTypeDefFields[varTypeDef][field];

                il.Emit(OpCodes.Ldarg_0);
                LoadLocal(il, locals, src);
                il.Emit(OpCodes.Stfld, fieldDef);
                break;
            }

            case HlOpcodeKind.DynGet: {
                var dst = instruction.Parameters[0];
                var obj = instruction.Parameters[1];
                var field = instruction.Parameters[2];

                var fieldName = hash.Code.Strings[field];

                // Call HaxeDyn::GetField(fieldName)
                LoadLocal(il, locals, obj);
                il.Emit(OpCodes.Ldstr, fieldName);
                il.Emit(OpCodes.Callvirt, asmDef.MainModule.ImportReference(typeof(HaxeDyn).GetMethod("GetField", new[] { typeof(string) })));
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
                il.Emit(OpCodes.Ldstr, fieldName);
                LoadLocal(il, locals, src);
                il.Emit(OpCodes.Callvirt, asmDef.MainModule.ImportReference(typeof(HaxeDyn).GetMethod("SetField", new[] { typeof(string), typeof(object) })));
                break;
            }

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

            // TODO: used
            case HlOpcodeKind.ToDyn: {
                var dst = instruction.Parameters[0];
                var src = instruction.Parameters[1];

                var haxeDynRef = asmDef.MainModule.ImportReference(typeof(HaxeDyn));
                var haxeDynCtor = asmDef.MainModule.ImportReference(haxeDynRef.Resolve().Methods.First(m => m.IsConstructor && m.Parameters.Count == 1));
                var varType = locals[src].VariableType.Resolve();

                LoadLocal(il, locals, src);
                if (varType.IsValueType)
                    il.Emit(OpCodes.Box, asmDef.MainModule.ImportReference(varType));
                il.Emit(OpCodes.Newobj, haxeDynCtor);
                SetLocal(il, locals, dst);
                break;
            }

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
                il.Emit(OpCodes.Call, genericToVirtual);
                SetLocal(il, locals, dst);
                break;
            }

            case HlOpcodeKind.Label: {
                // no-op
                il.Emit(OpCodes.Nop);
                break;
            }

            case HlOpcodeKind.Ret: {
                var localIndex = instruction.Parameters[0];

                var varType = locals[localIndex].VariableType.Resolve();
                var retType = method.ReturnType.Resolve();

                if (retType.FullName == "Tomat.SharpLink.HaxeDyn" && varType.FullName != "Tomat.SharpLink.HaxDyn") {
                    il.Emit(OpCodes.Ldloc, locals[localIndex]);

                    if (varType.IsValueType)
                        il.Emit(OpCodes.Box, asmDef.MainModule.ImportReference(varType));

                    il.Emit(OpCodes.Newobj, asmDef.MainModule.ImportReference(typeof(HaxeDyn).GetConstructor(new[] { typeof(object) })));
                }
                else {
                    il.Emit(OpCodes.Ldloc, locals[localIndex]);
                }

                il.Emit(OpCodes.Ret);
                break;
            }

            case HlOpcodeKind.Throw: {
                var reg = instruction.Parameters[0];

                // SharpLinkExceptionHelper::CreateNetExceptionFromHaxeException(SharpLinkException);
                var createNetExceptionFromHaxeException = asmDef.MainModule.ImportReference(typeof(SharpLinkExceptionHelper).GetMethod(nameof(SharpLinkExceptionHelper.CreateNetExceptionFromHaxeException)));

                LoadLocal(il, locals, reg);
                il.Emit(OpCodes.Call, createNetExceptionFromHaxeException);
                il.Emit(OpCodes.Throw);
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
                var label = il.Create(OpCodes.Nop);

                LoadLocal(il, locals, reg);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brfalse, label);
                il.Emit(OpCodes.Call, createNullCheckException);
                il.Emit(OpCodes.Throw);
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

                il.Emit(OpCodes.Newobj, ctor);
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
                il.Emit(OpCodes.Newobj, haxeRefCtor);
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
                il.Emit(OpCodes.Ldfld, haxeRefValueField);
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
                il.Emit(OpCodes.Nop);
                break;
            }

            case HlOpcodeKind.Last: {
                // not a real op-code
                // throw here maybe?
                break;
            }

            default: {
                throw new ArgumentOutOfRangeException(nameof(instruction));
            }
        }
    }

    private MethodDefinition ResolveDefinitionFromFIndex(int fIndex) {
        var corrected = hash.FunctionIndexes[fIndex];
        return corrected >= hash.Code.Functions.Count
            ? nativeMethodDefs[hash.Code.Natives[corrected - hash.Code.Functions.Count]]
            : funMethodDefs[hash.Code.Functions[corrected]];
    }
}
