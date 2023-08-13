using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler.ILGeneration;

public class JumpMarker {
    public int Index { get; }

    public Instruction Instruction { get; }

    public bool IsReferenced { get; set; }

    public JumpMarker(int index, Instruction instruction) {
        Index = index;
        Instruction = instruction;
    }
}
