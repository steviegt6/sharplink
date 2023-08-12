using System.Collections.Generic;
using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

public class CompiledEnum {
    public TypeDefinition Type { get; }

    public MethodDefinition BaseConstructor { get; set; } = null!;

    public Dictionary<HlEnumConstruct, CompiledEnumConstruct> Constructs { get; set; } = new();

    public CompiledEnum(TypeDefinition type) {
        Type = type;
    }
}
