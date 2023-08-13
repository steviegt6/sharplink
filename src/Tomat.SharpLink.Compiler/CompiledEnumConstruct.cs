using System.Collections.Generic;
using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

public class CompiledEnumConstruct {
    public required HlEnumConstruct Construct { get; init; }

    public required TypeDefinition Type { get; init; }

    public required MethodDefinition Constructor { get; init; }

    public List<FieldDefinition> Fields { get; } = new();

    public List<ParameterDefinition> ConstructorParameters { get; } = new();
}
