namespace Tomat.SharpLink.Compiler.ILGeneration.Opcodes;

public class NullOpcodeEmitter : OpcodeEmitter {
    public LocalRegister Dst { get; }

    public NullOpcodeEmitter(EmissionContext context) : base(context) {
        Dst = CreateLocalRegister(Opcode.Parameters[0]);
    }

    public override void Emit(FunctionEmitter emitter) {
        IL.Emit(Ldnull);
        StoreLocalRegister(Dst);
    }
}
