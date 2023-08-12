using System.Collections.Generic;
using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

public class CompiledObj {
    public TypeDefinition Type { get; }

    public Dictionary<HlObjField, FieldDefinition> Fields { get; set; }

    public Dictionary<HlObjProto, FieldDefinition> Protos { get; set; }

    public List<FieldDefinition> AllFields { get; set; } = new();

    public List<FieldDefinition> AllProtos { get; set; } = new();

    public CompiledObj(TypeDefinition type, Dictionary<HlObjField, FieldDefinition> fields, Dictionary<HlObjProto, FieldDefinition> protos) {
        Type = type;
        Fields = fields;
        Protos = protos;
    }
}
