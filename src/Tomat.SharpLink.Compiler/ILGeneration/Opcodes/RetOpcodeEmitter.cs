namespace Tomat.SharpLink.Compiler.ILGeneration.Opcodes;

public class RetOpcodeEmitter : OpcodeEmitter {
    public LocalRegister Src { get; }

    public RetOpcodeEmitter(EmissionContext context) : base(context) {
        Src = CreateLocalRegister(Opcode.Parameters[0]);
    }

    public override void Emit(FunctionEmitter emitter) {
        // TODO: HaxeVoid existing in the first place is unfortunate and a
        // result of laziness. It should be removed.
        if (Method.ReturnType.FullName == typeof(HaxeVoid).FullName) {
            LoadLocalRegisterAddress(Src);
            IL.Emit(Initobj, Method.ReturnType);
            LoadLocalRegister(Src);
            IL.Emit(Ret);
            return;
        }

        LoadLocalRegister(Src);
        EmitDynamicTypeConversion(Src, new CecilTypeReferenceProvider(Method.ReturnType));
        IL.Emit(Ret);
    }
}
