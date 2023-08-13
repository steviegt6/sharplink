namespace Tomat.SharpLink.Compiler.ILGeneration.Opcodes;

public sealed class MovOpcodeEmitter : OpcodeEmitter {
    public LocalRegister Dst { get; }

    public LocalRegister Src { get; }

    public MovOpcodeEmitter(EmissionContext context) : base(context) {
        Dst = CreateLocalRegister(Opcode.Parameters[0]);
        Src = CreateLocalRegister(Opcode.Parameters[1]);
    }

    public override void Emit(FunctionEmitter emitter) {
        LoadLocalRegister(Dst);
        EmitDynamicTypeConversion(Dst, Src);
        StoreLocalRegister(Src);
    }
}
