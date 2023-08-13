namespace Tomat.SharpLink.Compiler.ILGeneration.Opcodes;

public class UnimplementedOpcodeEmitter : OpcodeEmitter {
    private const bool throw_during_emission = false;

    public UnimplementedOpcodeEmitter(EmissionContext context) : base(context) { }

    public override void Emit(FunctionEmitter emitter) {
        if (throw_during_emission)
            throw new System.NotImplementedException();
    }
}
