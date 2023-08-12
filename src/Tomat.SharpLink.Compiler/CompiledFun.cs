using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

public class CompiledFun {
    public TypeDefinition Type { get; }

    public CompiledFun(TypeDefinition type) {
        Type = type;
    }
}
