using System.Collections.Generic;
using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

public class CompiledEnum {
    public required HlTypeWithEnum Enum { get; init; }

    public required TypeDefinition Type { get; init; }

    public required MethodDefinition BaseConstructor { get; init; }

    public Dictionary<HlEnumConstruct, CompiledEnumConstruct> Constructs { get; } = new();
}
