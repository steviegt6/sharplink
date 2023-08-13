namespace Tomat.SharpLink.Compiler.ILGeneration.Opcodes;

public class StringOpcodeEmitter : OpcodeEmitter {
    public LocalRegister Dst { get; }

    public int Key { get; }

    public StringOpcodeEmitter(EmissionContext context) : base(context) {
        Dst = CreateLocalRegister(Opcode.Parameters[0]);
        Key = Opcode.Parameters[1];
    }

    public override void Emit(FunctionEmitter emitter) {
        LoadCachedString(Key);
        EmitDynamicTypeConversion(new RuntimeTypeReferenceProvider(typeof(string)), Dst);
        StoreLocalRegister(Dst);
    }
}
