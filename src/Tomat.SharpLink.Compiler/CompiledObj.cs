using System.Collections.Generic;
using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

public class CompiledObj {
    public required HlTypeWithObj Obj { get; init; }

    public required TypeDefinition Type { get; init; }

    public required Dictionary<HlObjField, FieldDefinition> Fields { get; init; }

    public required Dictionary<HlObjProto, FieldDefinition> Protos { get; init; }

    public List<FieldDefinition> AllFields { get; } = new();

    public List<FieldDefinition> AllProtos { get; } = new();
}
