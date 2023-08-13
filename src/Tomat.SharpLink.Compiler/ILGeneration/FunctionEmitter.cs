using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Tomat.SharpLink.Compiler.ILGeneration.Opcodes;

namespace Tomat.SharpLink.Compiler.ILGeneration;

public class FunctionEmitter {
    private readonly CompiledFunction function;
    private readonly AssemblyDefinition asmDef;
    private readonly HlCodeHash hash;
    private readonly Compilation compilation;

    private int currentOpcodeIndex;

    public HlOpcode[] Opcodes => function.Function!.Opcodes;

    public HlOpcode CurrentOpcode => Opcodes[currentOpcodeIndex];

    public List<OpcodeEmitter> TranslatedOpcodes { get; } = new();

    public OpcodeEmitter CurrentTranslatedOpcode => TranslatedOpcodes[currentOpcodeIndex];

    public FunctionEmitter(CompiledFunction function, AssemblyDefinition asmDef, HlCodeHash hash, Compilation compilation) {
        if (function.Function is null)
            throw new NullReferenceException(nameof(function.Function));

        this.function = function;
        this.asmDef = asmDef;
        this.hash = hash;
        this.compilation = compilation;
    }

    public void EmitToMethod(MethodDefinition method) {
        var locals = CreateMethodLocals();

        var body = method.Body = new MethodBody(method);
        foreach (var local in locals)
            method.Body.Variables.Add(local);

        var il = body.GetILProcessor();

        var markers = new Dictionary<int, Instruction>();
        for (var i = 0; i < Opcodes.Length; i++)
            markers[i] = il.Create(Nop);

        for (currentOpcodeIndex = 0; currentOpcodeIndex < Opcodes.Length; currentOpcodeIndex++)
            TranslatedOpcodes.Add(CreateOpcodeEmitter(CurrentOpcode, method, locals, markers, il));

        for (currentOpcodeIndex = 0; currentOpcodeIndex < Opcodes.Length; currentOpcodeIndex++) {
            il.Append(markers[currentOpcodeIndex]);
            CurrentTranslatedOpcode.Emit(this);
        }
    }

    private OpcodeEmitter CreateOpcodeEmitter(HlOpcode opcode, MethodDefinition method, List<VariableDefinition> locals, Dictionary<int, Instruction> markers, ILProcessor il) {
        switch (opcode.Kind) {
            case HlOpcodeKind.Mov: {
                return new MovOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Int: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Float: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Bool: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            // TODO: I haven't encountered this being used yet.
            // TODO: Uses Bytes and BytePositions I think. Version >= 5 ofc.
            case HlOpcodeKind.Bytes: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.String: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Null: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Add: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Sub: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Mul: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.SDiv: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.UDiv: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.SMod: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.UMod: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Shl: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.SShr: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.UShr: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.And: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Or: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Xor: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Neg: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Not: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Incr: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Decr: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Call0: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Call1: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Call2: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Call3: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Call4: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.CallN: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.CallMethod: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.CallThis: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.CallClosure: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.StaticClosure: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.InstanceClosure: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.VirtualClosure: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.GetGlobal: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.SetGlobal: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Field: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.SetField: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.GetThis: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.SetThis: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.DynGet: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.DynSet: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.JTrue: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.JFalse: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.JNull: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.JNotNull: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.JSLt: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.JSGte: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.JSGt: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.JSLte: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.JULt: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.JUGte: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.JNotLt: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.JNotGte: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.JEq: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.JNotEq: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.JAlways: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.ToDyn: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.ToSFloat: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.ToUFloat: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.ToInt: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.SafeCast: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.UnsafeCast: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.ToVirtual: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Label: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Ret: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Throw: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Rethrow: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Switch: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.NullCheck: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Trap: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.EndTrap: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.GetI8: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.GetI16: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.GetMem: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.GetArray: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.SetI8: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.SetI16: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.SetMem: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.SetArray: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.New: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.ArraySize: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Type: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.GetType: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.GetTID: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Ref: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Unref: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.Setref: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.MakeEnum: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.EnumAlloc: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.EnumIndex: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.EnumField: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.SetEnumField: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.Assert: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.RefData: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.RefOffset: {
                return new UnimplementedOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Nop: {
                return new NopOpcodeEmitter(opcode, method, locals, markers, il, currentOpcodeIndex);
            }

            case HlOpcodeKind.Last:
                throw new InvalidOperationException("'Last' opcode should not be emitted.");

            default:
                throw new ArgumentOutOfRangeException(nameof(opcode));
        }
    }

#region Emission utilities
    /*private void PushCached<T>(ILProcessor il, int index) {
        if (typeof(T) == typeof(int) || typeof(T) == typeof(byte) || typeof(T) == typeof(ushort))
            il.Emit(Ldc_I4, hash.Code.Ints[index]);
        else if (typeof(T) == typeof(long))
            il.Emit(Ldc_I8, hash.Code.Ints[index]);
        else if (typeof(T) == typeof(float))
            il.Emit(Ldc_R4, (float)hash.Code.Floats[index]);
        else if (typeof(T) == typeof(double))
            il.Emit(Ldc_R8, hash.Code.Floats[index]);
        else if (typeof(T) == typeof(string))
            il.Emit(Ldstr, hash.Code.Strings[index]);
    }

    private void PushConverter<TSys, THaxe>(ILProcessor il, AssemblyDefinition asmDef) {
        il.Emit(Newobj, asmDef.MainModule.ImportReference(typeof(THaxe).GetConstructor(new[] { typeof(TSys) })));
    }

    private void LoadLocal(ILProcessor il, List<VariableDefinition> locals, int index) {
        il.Emit(Ldloc, locals[index]);
    }

    private void LoadLocalThatMayNeedToBeConvertedToHaxeDyn(ILProcessor il, List<VariableDefinition> locals, int index, TypeReference targetType, AssemblyDefinition asmDef) {
        LoadLocal(il, locals, index);

        var localVar = locals[index];

        if (targetType.FullName != "Tomat.SharpLink.HaxeDyn" || localVar.VariableType.FullName == "Tomat.SharpLink.HaxeDyn")
            return;

        if (localVar.VariableType.IsValueType)
            il.Emit(Box, localVar.VariableType);

        var haxeDynCtor = asmDef.MainModule.ImportReference(typeof(HaxeDyn).GetConstructor(new[] { typeof(object) }));
        il.Emit(Newobj, haxeDynCtor);
    }

    private void SetLocal(ILProcessor il, List<VariableDefinition> locals, int index) {
        il.Emit(Stloc, locals[index]);
    }

    private void LoadGlobal(ILProcessor il, int index, AssemblyDefinition asmDef) {
        var module = asmDef.MainModule.GetType("<Module>");
        var field = module.Fields.FirstOrDefault(field => field.Name == $"global{index}");
        il.Emit(Ldsfld, field);
    }

    private void SetGlobal(ILProcessor il, int index, AssemblyDefinition asmDef) {
        var module = asmDef.MainModule.GetType("<Module>");
        var field = module.Fields.FirstOrDefault(field => field.Name == $"global{index}");
        il.Emit(Stsfld, field);
    }

    private TypeReference GetTypeForLocal(List<VariableDefinition> locals, int index) {
        return locals[index].VariableType;
    }*/
#endregion

    private List<VariableDefinition> CreateMethodLocals() {
        var locals = new List<VariableDefinition>();

        // TODO: When I have time to clean up and optimize code, make it so we
        // don't lazily assign parameters to locals and treat them all like
        // regular hl registers.
        foreach (var local in function.Function!.LocalVariables) {
            var localType = compilation.TypeReferenceFromHlTypeRef(local, asmDef);
            locals.Add(new VariableDefinition(localType));
        }

        return locals;
    }

    private CompiledFunction CompiledFunctionFromFunctionIndex(int fIndex) {
        var corrected = hash.FunctionIndexes[fIndex];
        return corrected >= hash.Code.Functions.Count
            ? compilation.GetNative(hash.Code.Natives[corrected - hash.Code.Functions.Count])
            : compilation.GetFunction(hash.Code.Functions[corrected]);
    }
}
