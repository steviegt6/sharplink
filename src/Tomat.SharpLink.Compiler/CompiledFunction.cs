using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

public class CompiledFunction {
    public HlFunction? Function { get; init; }

    public HlNative? Native { get; init; }

    public required MethodDefinition Method { get; init; }
}
