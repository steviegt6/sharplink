using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

public class CompiledFunction {
    public MethodDefinition MethodDefinition { get; set; }

    public CompiledFunction(MethodDefinition methodDefinition) {
        MethodDefinition = methodDefinition;
    }
}
