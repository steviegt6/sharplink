using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler.ILGeneration.Opcodes;

public abstract class OpcodeEmitter {
    public HlOpcode Opcode { get; }

    public MethodDefinition Method { get; }

    public List<VariableDefinition> Locals { get; }

    public Dictionary<int, JumpMarker> Markers { get; }

    public ILProcessor IL { get; }

    public int Index { get; }

    protected OpcodeEmitter(HlOpcode opcode, MethodDefinition method, List<VariableDefinition> locals, Dictionary<int, JumpMarker> markers, ILProcessor il, int index) {
        Opcode = opcode;
        Method = method;
        Locals = locals;
        Markers = markers;
        IL = il;
        Index = index;
    }

    public abstract void Emit(FunctionEmitter emitter);

    protected LocalRegister CreateLocalRegister(int index) {
        return new LocalRegister(index, Method, Locals);
    }

    private void EmitArgumentLoad(int index) {
        switch (index) {
            case 0:
                IL.Emit(Ldarg_0);
                break;

            case 1:
                IL.Emit(Ldarg_1);
                break;

            case 2:
                IL.Emit(Ldarg_2);
                break;

            case 3:
                IL.Emit(Ldarg_3);
                break;

            case < 0xFF:
                IL.Emit(Ldarg_S, (byte)index);
                break;

            default:
                IL.Emit(Ldarg, index);
                break;
        }
    }

    private void EmitArgumentStore(int index) {
        switch (index) {
            case < 0xFF:
                IL.Emit(Starg_S, (byte)index);
                break;

            default:
                IL.Emit(Starg, index);
                break;
        }
    }

    private void EmitLocalLoad(int index) {
        switch (index) {
            case 0:
                IL.Emit(Ldloc_0);
                break;

            case 1:
                IL.Emit(Ldloc_1);
                break;

            case 2:
                IL.Emit(Ldloc_2);
                break;

            case 3:
                IL.Emit(Ldloc_3);
                break;

            case < 0xFF:
                IL.Emit(Ldloc_S, (byte)index);
                break;

            default:
                IL.Emit(Ldloc, index);
                break;
        }
    }

    private void EmitLocalStore(int index) {
        switch (index) {
            case 0:
                IL.Emit(Stloc_0);
                break;

            case 1:
                IL.Emit(Stloc_1);
                break;

            case 2:
                IL.Emit(Stloc_2);
                break;

            case 3:
                IL.Emit(Stloc_3);
                break;

            case < 0xFF:
                IL.Emit(Stloc_S, (byte)index);
                break;

            default:
                IL.Emit(Stloc, index);
                break;
        }
    }

    private void EmitDynamicTypeConversion(TypeReference from, TypeReference to) {
        if (to.FullName != "Tomat.SharpLink.HaxeDyn")
            return;

        if (from.FullName != to.FullName) {
            // TODO: handle dynamic conversion here
        }
    }

    protected void LoadLocalRegister(LocalRegister register) {
        if (register.IsParameter)
            EmitArgumentLoad(register.AdjustedIndex);
        else
            EmitLocalLoad(register.AdjustedIndex);
    }

    protected void StoreLocalRegister(LocalRegister register) {
        if (register.IsParameter)
            EmitArgumentStore(register.AdjustedIndex);
        else
            EmitLocalLoad(register.AdjustedIndex);
    }

    protected void ConvertLocalRegister(LocalRegister from, LocalRegister to) {
        var fromType = from.IsParameter ? Method.Parameters[from.AdjustedIndex].ParameterType : Locals[from.AdjustedIndex].VariableType;
        var toType = to.IsParameter ? Method.Parameters[to.AdjustedIndex].ParameterType : Locals[to.AdjustedIndex].VariableType;
        EmitDynamicTypeConversion(fromType, toType);
    }
}
