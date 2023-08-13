namespace Tomat.SharpLink.Compiler.ILGeneration.Opcodes;

public class NopOpcodeEmitter : OpcodeEmitter {
    private const bool emit_nop = false;

    public NopOpcodeEmitter(EmissionContext context) : base(context) { }

    public override void Emit(FunctionEmitter emitter) {
        if (emit_nop)
            IL.Emit(Nop);
    }
}
