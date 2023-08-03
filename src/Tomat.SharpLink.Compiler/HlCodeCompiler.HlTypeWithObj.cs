using System.Collections.Generic;
using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
    private Dictionary<HlTypeWithObj, TypeDefinition> objDefs = new();
    private Dictionary<HlTypeWithObj, Dictionary<HlObjField, FieldDefinition>> objFieldDefs = new();

    private void ResolveHlTypeWithObj(HlTypeWithObj type, AssemblyDefinition asmDef) {
        ExtractNameAndNamespace(type.Obj.Name, out var ns, out var name);
        var objDef = new TypeDefinition(
            ns ?? "",
            name,
            TypeAttributes.Class
          | TypeAttributes.Public
          | TypeAttributes.BeforeFieldInit,
            asmDef.MainModule.TypeSystem.Object // temporary
        );
        objDefs.Add(type, objDef);

        var fieldDefs = objFieldDefs[type] = new Dictionary<HlObjField, FieldDefinition>();

        foreach (var field in type.Obj.Fields) {
            var fieldDef = new FieldDefinition(
                field.Name,
                FieldAttributes.Public,
                asmDef.MainModule.TypeSystem.Object // temporary
            );
            fieldDefs.Add(field, fieldDef);
        }
    }

    private void DefineHlTypeWithObj(HlTypeWithObj type, AssemblyDefinition asmDef) {
        var objDef = objDefs[type];
        objDef.BaseType = type.Obj.Super is null ? asmDef.MainModule.TypeSystem.Object : TypeReferenceFromHlTypeRef(type.Obj.Super, asmDef);

        foreach (var field in type.Obj.Fields) {
            var fieldDef = objFieldDefs[type][field];
            fieldDef.FieldType = TypeReferenceFromHlTypeRef(field.Type, asmDef);
        }
    }

    private void CompileHlTypeWithObj(HlTypeWithObj type, AssemblyDefinition asmDef) {
        var objDef = objDefs[type];
        asmDef.MainModule.Types.Add(objDef);

        var fieldDefs = objFieldDefs[type];
        foreach (var fieldDef in fieldDefs.Values)
            objDef.Fields.Add(fieldDef);
    }
}
