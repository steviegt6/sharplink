using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler.ILGeneration.Opcodes;

public class SimpleBinaryMathOpcodeEmitter : OpcodeEmitter {
    public LocalRegister Dst { get; }

    public LocalRegister Left { get; }

    public LocalRegister Right { get; }

    public OpCode OpCode { get; }

    public SimpleBinaryMathOpcodeEmitter(EmissionContext context, OpCode opCode) : base(context) {
        Dst = CreateLocalRegister(Opcode.Parameters[0]);
        Left = CreateLocalRegister(Opcode.Parameters[1]);
        Right = CreateLocalRegister(Opcode.Parameters[2]);
        OpCode = opCode;
    }

    public override void Emit(FunctionEmitter emitter) {
        LoadLocalRegister(Left);
        LoadLocalRegister(Right);
        IL.Emit(OpCode);
        StoreLocalRegister(Dst);
    }
}
