namespace Tomat.SharpLink.Compiler.ILGeneration.Opcodes;

public class FloatOpcodeEmitter : OpcodeEmitter {
    public LocalRegister Dst { get; }

    public int Key { get; }

    public FloatOpcodeEmitter(EmissionContext context) : base(context) {
        Dst = CreateLocalRegister(Opcode.Parameters[0]);
        Key = Opcode.Parameters[1];
    }

    public override void Emit(FunctionEmitter emitter) {
        LoadCachedFloat(Key);
        StoreLocalRegister(Dst);
    }
}
