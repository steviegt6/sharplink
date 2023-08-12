using System.Collections.Generic;
using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

public class CompiledEnumConstruct {
    public TypeDefinition Type { get; }

    public MethodDefinition Constructor { get; set; } = null!;

    public List<FieldDefinition> Fields { get; set; } = new();

    public List<ParameterDefinition> ConstructorParameters { get; set; } = new();

    public CompiledEnumConstruct(TypeDefinition type) {
        Type = type;
    }
}
