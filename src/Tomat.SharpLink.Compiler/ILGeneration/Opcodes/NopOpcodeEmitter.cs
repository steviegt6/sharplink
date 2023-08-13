using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler.ILGeneration.Opcodes;

public class NopOpcodeEmitter : OpcodeEmitter {
    private const bool emit_nop = false;

    public NopOpcodeEmitter(HlOpcode opcode, MethodDefinition method, List<VariableDefinition> locals, Dictionary<int, JumpMarker> markers, ILProcessor il, int index) : base(opcode, method, locals, markers, il, index) { }

    public override void Emit(FunctionEmitter emitter) {
        if (emit_nop)
            IL.Emit(Nop);
    }
}
