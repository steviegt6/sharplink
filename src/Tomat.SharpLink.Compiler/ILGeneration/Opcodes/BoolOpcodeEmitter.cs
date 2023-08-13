namespace Tomat.SharpLink.Compiler.ILGeneration.Opcodes;

public class BoolOpcodeEmitter : OpcodeEmitter {
    public LocalRegister Dst { get; }

    public int Value { get; }

    public BoolOpcodeEmitter(EmissionContext context) : base(context) {
        Dst = CreateLocalRegister(Opcode.Parameters[0]);
        Value = Opcode.Parameters[1];
    }

    public override void Emit(FunctionEmitter emitter) {
        LoadConstInt(Value != 0 ? 1 : 0);
        StoreLocalRegister(Dst);
    }
}
