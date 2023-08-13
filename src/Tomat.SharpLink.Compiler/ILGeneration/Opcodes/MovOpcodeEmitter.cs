using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler.ILGeneration.Opcodes;

public sealed class MovOpcodeEmitter : OpcodeEmitter {
    public LocalRegister Dst { get; }

    public LocalRegister Src { get; }

    public MovOpcodeEmitter(HlOpcode opcode, MethodDefinition method, List<VariableDefinition> locals, Dictionary<int, Instruction> markers, ILProcessor il, int index) : base(opcode, method, locals, markers, il, index) {
        Dst = CreateLocalRegister(opcode.Parameters[0]);
        Src = CreateLocalRegister(opcode.Parameters[1]);
    }

    public override void Emit(FunctionEmitter emitter) {
        LoadLocalRegister(Dst);
        ConvertLocalRegister(Dst, Src);
        StoreLocalRegister(Src);
    }
}
