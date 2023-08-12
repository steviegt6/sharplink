using System.Collections.Generic;
using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

public class CompiledVirtual {
    public TypeDefinition Type { get; }

    public MethodDefinition BaseConstructor { get; set; }

    public List<FieldDefinition> Fields { get; set; } = new();

    public List<ParameterDefinition> ConstructorParameters { get; set; } = new();

    public List<FieldDefinition> AllFields { get; set; } = new();

    public CompiledVirtual(TypeDefinition type, MethodDefinition baseConstructor) {
        Type = type;
        BaseConstructor = baseConstructor;
    }
}
