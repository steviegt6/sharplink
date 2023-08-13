using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

public class CompiledFun {
    public required HlTypeWithFun Fun { get; init; }

    public required TypeDefinition Type { get; init; }
}
