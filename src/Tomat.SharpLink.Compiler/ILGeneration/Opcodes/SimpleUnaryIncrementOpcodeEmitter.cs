using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler.ILGeneration.Opcodes;

public class SimpleUnaryIncrementOpcodeEmitter : OpcodeEmitter {
    public LocalRegister Dst { get; }

    public OpCode OpCode { get; }

    public SimpleUnaryIncrementOpcodeEmitter(EmissionContext context, OpCode opCode) : base(context) {
        Dst = CreateLocalRegister(Opcode.Parameters[0]);
        OpCode = opCode;
    }

    public override void Emit(FunctionEmitter emitter) {
        LoadLocalRegister(Dst);
        LoadConstInt(1);
        IL.Emit(OpCode);
        StoreLocalRegister(Dst);
    }
}
