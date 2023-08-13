namespace Tomat.SharpLink.Compiler.ILGeneration.Opcodes;

public class IntOpcodeEmitter : OpcodeEmitter {
    public LocalRegister Dst { get; }

    public int Key { get; }

    public IntOpcodeEmitter(EmissionContext context) : base(context) {
        Dst = CreateLocalRegister(Opcode.Parameters[0]);
        Key = Opcode.Parameters[1];
    }

    public override void Emit(FunctionEmitter emitter) {
        LoadCachedInt(Key);
        StoreLocalRegister(Dst);
    }
}
