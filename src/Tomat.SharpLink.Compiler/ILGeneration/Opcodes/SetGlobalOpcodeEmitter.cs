namespace Tomat.SharpLink.Compiler.ILGeneration.Opcodes;

public class SetGlobalOpcodeEmitter : OpcodeEmitter {
    public GlobalReference Dst { get; }

    public LocalRegister Src { get; }

    public SetGlobalOpcodeEmitter(EmissionContext context) : base(context) {
        Dst = CreateGlobalReference(Opcode.Parameters[0]);
        Src = CreateLocalRegister(Opcode.Parameters[1]);
    }

    public override void Emit(FunctionEmitter emitter) {
        LoadLocalRegister(Src);
        EmitDynamicTypeConversion(Src, Dst);
        StoreGlobal(Dst);
    }
}
