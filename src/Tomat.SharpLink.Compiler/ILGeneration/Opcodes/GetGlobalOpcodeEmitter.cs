namespace Tomat.SharpLink.Compiler.ILGeneration.Opcodes;

public class GetGlobalOpcodeEmitter : OpcodeEmitter {
    public LocalRegister Dst { get; }

    public GlobalReference Src { get; }

    public GetGlobalOpcodeEmitter(EmissionContext context) : base(context) {
        Dst = CreateLocalRegister(Opcode.Parameters[0]);
        Src = CreateGlobalReference(Opcode.Parameters[1]);
    }

    public override void Emit(FunctionEmitter emitter) {
        LoadGlobal(Src);
        EmitDynamicTypeConversion(Src, Dst);
        StoreLocalRegister(Dst);
    }
}
