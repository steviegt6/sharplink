﻿using System.Collections.Generic;
using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
    private Dictionary<HlTypeWithObj, TypeDefinition> objDefs = new();
    private Dictionary<HlTypeWithObj, Dictionary<HlObjField, FieldDefinition>> objFieldDefs = new();
    private Dictionary<HlTypeWithObj, Dictionary<HlObjProto, FieldDefinition>> objProtoDefs = new();

    private Dictionary<TypeDefinition, List<FieldDefinition>> objTypeDefProtos = new();

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

        var protoDefs = objProtoDefs[type] = new Dictionary<HlObjProto, FieldDefinition>();

        foreach (var proto in type.Obj.Protos) {
            // TODO: Handle property stuff...
            var protoDef = new FieldDefinition(
                proto.Name,
                FieldAttributes.Public
              | FieldAttributes.Static,
                asmDef.MainModule.TypeSystem.Object // temporary
            );
            protoDefs.Add(proto, protoDef);
        }
    }

    private void DefineHlTypeWithObj(HlTypeWithObj type, AssemblyDefinition asmDef) {
        var objDef = objDefs[type];
        objDef.BaseType = type.Obj.Super is null ? asmDef.MainModule.TypeSystem.Object : TypeReferenceFromHlTypeRef(type.Obj.Super, asmDef);

        foreach (var field in type.Obj.Fields) {
            var fieldDef = objFieldDefs[type][field];
            fieldDef.FieldType = TypeReferenceFromHlTypeRef(field.Type, asmDef);
        }

        foreach (var proto in type.Obj.Protos) {
            var protoDef = objProtoDefs[type][proto];
            protoDef.FieldType = funDefs[(HlTypeWithFun)hash.Code.Functions[hash.FunctionIndexes[proto.FIndex]].Type.Value!];
        }
    }

    private void CompileHlTypeWithObj(HlTypeWithObj type, AssemblyDefinition asmDef) {
        var objDef = objDefs[type];
        asmDef.MainModule.Types.Add(objDef);

        var fieldDefs = objFieldDefs[type];
        foreach (var fieldDef in fieldDefs.Values)
            objDef.Fields.Add(fieldDef);

        var protoDefs = objProtoDefs[type];
        foreach (var protoDef in protoDefs.Values)
            objDef.Fields.Add(protoDef);

        void reverseAddAllProtos(HlType? theType, List<FieldDefinition> protoFields) {
            if (theType is not HlTypeWithObj theTypeObj)
                return;

            reverseAddAllProtos(theTypeObj.Obj.Super?.Value ?? null, protoFields);

            var protoDefsForTheType = objProtoDefs[theTypeObj];
            foreach (var proto in theTypeObj.Obj.Protos)
                protoFields.Add(protoDefsForTheType[proto]);
        }

        var allProtos = objTypeDefProtos[objDef] = new List<FieldDefinition>();
        reverseAddAllProtos(type, allProtos);
    }
}
