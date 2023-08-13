using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

public class CompiledAbstract {
    public required HlTypeWithAbsName Abstract { get; init; }

    public required CustomAttribute Attribute { get; init; }
}
