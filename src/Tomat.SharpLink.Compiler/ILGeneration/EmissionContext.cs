using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler.ILGeneration;

public class EmissionContext {
    public HlOpcode Opcode { get; }

    public MethodDefinition Method { get; }

    public List<VariableDefinition> Locals { get; }

    public Dictionary<int, JumpMarker> Markers { get; }

    public ILProcessor IL { get; }

    public int Index { get; }

    public HlCodeHash Hash { get; }

    public Compilation Compilation { get; }

    public AssemblyDefinition Assembly { get; }

    public EmissionContext(HlOpcode opcode, MethodDefinition method, List<VariableDefinition> locals, Dictionary<int, JumpMarker> markers, ILProcessor il, int index, HlCodeHash hash, Compilation compilation, AssemblyDefinition assembly) {
        Opcode = opcode;
        Method = method;
        Locals = locals;
        Markers = markers;
        IL = il;
        Index = index;
        Hash = hash;
        Compilation = compilation;
        Assembly = assembly;
    }
}
