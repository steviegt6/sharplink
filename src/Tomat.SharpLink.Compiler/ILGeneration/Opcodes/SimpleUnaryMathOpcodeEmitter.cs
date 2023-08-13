using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler.ILGeneration.Opcodes;

public class SimpleUnaryMathOpcodeEmitter : OpcodeEmitter {
    public LocalRegister Dst { get; }

    public LocalRegister Left { get; }

    public OpCode OpCode { get; }

    public SimpleUnaryMathOpcodeEmitter(EmissionContext context, OpCode opCode) : base(context) {
        Dst = CreateLocalRegister(Opcode.Parameters[0]);
        Left = CreateLocalRegister(Opcode.Parameters[1]);
        OpCode = opCode;
    }

    public override void Emit(FunctionEmitter emitter) {
        LoadLocalRegister(Left);
        IL.Emit(OpCode);
        StoreLocalRegister(Dst);
    }
}
