namespace Tomat.SharpLink.Compiler.ILGeneration.Opcodes;

public class SetGlobalOpcodeEmitter : OpcodeEmitter {
    public LocalRegister Src { get; }

    public GlobalReference Dst { get; }

    public SetGlobalOpcodeEmitter(EmissionContext context) : base(context) {
        Src = CreateLocalRegister(Opcode.Parameters[0]);
        Dst = CreateGlobalReference(Opcode.Parameters[1]);
    }

    public override void Emit(FunctionEmitter emitter) {
        LoadLocalRegister(Src);
        EmitDynamicTypeConversion(Src, Dst);
        StoreGlobal(Dst);
    }
}
