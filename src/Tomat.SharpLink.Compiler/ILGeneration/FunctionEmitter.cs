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

        var markers = new Dictionary<int, JumpMarker>();
        for (var i = 0; i < Opcodes.Length; i++)
            markers[i] = new JumpMarker(i, il.Create(Nop));

        for (currentOpcodeIndex = 0; currentOpcodeIndex < Opcodes.Length; currentOpcodeIndex++)
            TranslatedOpcodes.Add(CreateOpcodeEmitter(CurrentOpcode, method, locals, markers, il));

        for (currentOpcodeIndex = 0; currentOpcodeIndex < Opcodes.Length; currentOpcodeIndex++) {
            if (markers[currentOpcodeIndex].IsReferenced)
                il.Append(markers[currentOpcodeIndex].Instruction);
            CurrentTranslatedOpcode.Emit(this);
        }
    }

    private OpcodeEmitter CreateOpcodeEmitter(HlOpcode opcode, MethodDefinition method, List<VariableDefinition> locals, Dictionary<int, JumpMarker> markers, ILProcessor il) {
        var context = new EmissionContext(opcode, method, locals, markers, il, currentOpcodeIndex, hash, compilation, asmDef);

        switch (opcode.Kind) {
            case HlOpcodeKind.Mov: {
                return new MovOpcodeEmitter(context);
            }

            case HlOpcodeKind.Int: {
                return new IntOpcodeEmitter(context);
            }

            case HlOpcodeKind.Float: {
                return new FloatOpcodeEmitter(context);
            }

            case HlOpcodeKind.Bool: {
                return new BoolOpcodeEmitter(context);
            }

            // TODO: I haven't encountered this being used yet.
            // TODO: Uses Bytes and BytePositions I think. Version >= 5 ofc.
            case HlOpcodeKind.Bytes: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.String: {
                return new StringOpcodeEmitter(context);
            }

            case HlOpcodeKind.Null: {
                return new NullOpcodeEmitter(context);
            }

            case HlOpcodeKind.Add: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.Sub: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.Mul: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.SDiv: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.UDiv: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.SMod: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.UMod: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.Shl: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.SShr: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.UShr: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.And: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.Or: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.Xor: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.Neg: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.Not: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.Incr: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.Decr: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.Call0: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.Call1: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.Call2: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.Call3: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.Call4: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.CallN: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.CallMethod: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.CallThis: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.CallClosure: {
                return new UnimplementedOpcodeEmitter(context);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.StaticClosure: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.InstanceClosure: {
                return new UnimplementedOpcodeEmitter(context);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.VirtualClosure: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.GetGlobal: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.SetGlobal: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.Field: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.SetField: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.GetThis: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.SetThis: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.DynGet: {
                return new UnimplementedOpcodeEmitter(context);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.DynSet: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.JTrue: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.JFalse: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.JNull: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.JNotNull: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.JSLt: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.JSGte: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.JSGt: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.JSLte: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.JULt: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.JUGte: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.JNotLt: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.JNotGte: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.JEq: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.JNotEq: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.JAlways: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.ToDyn: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.ToSFloat: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.ToUFloat: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.ToInt: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.SafeCast: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.UnsafeCast: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.ToVirtual: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.Label: {
                return new LabelOpcodeEmitter(context);
            }

            case HlOpcodeKind.Ret: {
                return new RetOpcodeEmitter(context);
            }

            case HlOpcodeKind.Throw: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.Rethrow: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.Switch: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.NullCheck: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.Trap: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.EndTrap: {
                return new UnimplementedOpcodeEmitter(context);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.GetI8: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.GetI16: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.GetMem: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.GetArray: {
                return new UnimplementedOpcodeEmitter(context);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.SetI8: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.SetI16: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.SetMem: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.SetArray: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.New: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.ArraySize: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.Type: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.GetType: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.GetTID: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.Ref: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.Unref: {
                return new UnimplementedOpcodeEmitter(context);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.Setref: {
                return new UnimplementedOpcodeEmitter(context);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.MakeEnum: {
                return new UnimplementedOpcodeEmitter(context);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.EnumAlloc: {
                return new UnimplementedOpcodeEmitter(context);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.EnumIndex: {
                return new UnimplementedOpcodeEmitter(context);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.EnumField: {
                return new UnimplementedOpcodeEmitter(context);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.SetEnumField: {
                return new UnimplementedOpcodeEmitter(context);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.Assert: {
                return new UnimplementedOpcodeEmitter(context);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.RefData: {
                return new UnimplementedOpcodeEmitter(context);
            }

            // TODO: I haven't encountered this being used yet.
            case HlOpcodeKind.RefOffset: {
                return new UnimplementedOpcodeEmitter(context);
            }

            case HlOpcodeKind.Nop: {
                return new NopOpcodeEmitter(context);
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

        var parameters = ((HlTypeWithFun)function.Function!.Type.Value!).FunctionDescription.Arguments;

        for (var i = parameters.Length; i < function.Function!.LocalVariables.Length; i++) {
            var local = function.Function!.LocalVariables[i];
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
