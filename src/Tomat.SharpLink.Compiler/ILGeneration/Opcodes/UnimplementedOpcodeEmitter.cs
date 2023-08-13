using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler.ILGeneration.Opcodes;

public class UnimplementedOpcodeEmitter : OpcodeEmitter {
    private const bool throw_during_emission = false;

    public UnimplementedOpcodeEmitter(HlOpcode opcode, MethodDefinition method, List<VariableDefinition> locals, Dictionary<int, Instruction> markers, ILProcessor il, int index) : base(opcode, method, locals, markers, il, index) { }

    public override void Emit(FunctionEmitter emitter) {
        if (throw_during_emission)
            throw new System.NotImplementedException();
    }
}
