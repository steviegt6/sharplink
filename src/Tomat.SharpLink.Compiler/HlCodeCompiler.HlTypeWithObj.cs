﻿using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
    private void ResolveHlTypeWithObj(HlTypeWithObj type, AssemblyDefinition asmDef) {
        ExtractNameAndNamespace(type.Obj.Name, out var ns, out var name);

        var objType = new TypeDefinition(
            ns ?? "",
            name,
            TypeAttributes.Class
          | TypeAttributes.Public
          | TypeAttributes.BeforeFieldInit,
            asmDef.MainModule.TypeSystem.Object // temporary
        );

        var fields = new Dictionary<HlObjField, FieldDefinition>();
        var protos = new Dictionary<HlObjProto, FieldDefinition>();

        foreach (var field in type.Obj.Fields) {
            var fieldDef = new FieldDefinition(
                field.Name,
                FieldAttributes.Public,
                asmDef.MainModule.TypeSystem.Object // temporary
            );
            fields.Add(field, fieldDef);
        }

        foreach (var proto in type.Obj.Protos) {
            // TODO: Handle property stuff...
            var protoDef = new FieldDefinition(
                proto.Name,
                FieldAttributes.Public
              | FieldAttributes.Static,
                asmDef.MainModule.TypeSystem.Object // temporary
            );
            protos.Add(proto, protoDef);
        }

        compilation.AddObj(new CompiledObj {
            Obj = type,
            Type = objType,
            Fields = fields,
            Protos = protos,
        });
    }

    private void DefineHlTypeWithObj(HlTypeWithObj type, AssemblyDefinition asmDef) {
        var compiled = compilation.GetObj(type);
        compiled.Type.BaseType = type.Obj.Super is null ? asmDef.MainModule.TypeSystem.Object : TypeReferenceFromHlTypeRef(type.Obj.Super, asmDef);

        foreach (var field in type.Obj.Fields)
            compiled.Fields[field].FieldType = TypeReferenceFromHlTypeRef(field.Type, asmDef);

        foreach (var proto in type.Obj.Protos)
            compiled.Protos[proto].FieldType = TypeDefinitionFromFunctionIndex(proto.FIndex);
    }

    private void CompileHlTypeWithObj(HlTypeWithObj type, AssemblyDefinition asmDef) {
        var compiled = compilation.GetObj(type);
        asmDef.MainModule.Types.Add(compiled.Type);

        foreach (var fieldDef in compiled.Fields.Values)
            compiled.Type.Fields.Add(fieldDef);

        foreach (var protoDef in compiled.Protos.Values)
            compiled.Type.Fields.Add(protoDef);

        void reverseAddAllProtos(HlType? theType, ICollection<FieldDefinition> protoFields) {
            if (theType is not HlTypeWithObj theTypeObj)
                return;

            reverseAddAllProtos(theTypeObj.Obj.Super?.Value ?? null, protoFields);

            var protoDefsForTheType = compilation.GetObj(theTypeObj).Protos;
            foreach (var proto in theTypeObj.Obj.Protos)
                protoFields.Add(protoDefsForTheType[proto]);
        }

        var allProtos = compiled.AllProtos;
        reverseAddAllProtos(type, allProtos);

        void reverseAddAllFields(HlType? theType, ICollection<FieldDefinition> fieldFields) {
            if (theType is not HlTypeWithObj theTypeObj)
                return;

            reverseAddAllFields(theTypeObj.Obj.Super?.Value ?? null, fieldFields);

            var fieldDefsForTheType = compilation.GetObj(theTypeObj).Fields;
            foreach (var field in theTypeObj.Obj.Fields)
                fieldFields.Add(fieldDefsForTheType[field]);
        }

        var allFields = compiled.AllFields;
        reverseAddAllFields(type, allFields);

        var ctorDef = new MethodDefinition(
            ".ctor",
            MethodAttributes.Public
          | MethodAttributes.HideBySig
          | MethodAttributes.SpecialName
          | MethodAttributes.RTSpecialName,
            asmDef.MainModule.TypeSystem.Void
        );
        compiled.Type.Methods.Add(ctorDef);

        var ctorIl = ctorDef.Body.GetILProcessor();
        ctorIl.Emit(OpCodes.Ldarg_0);
        ctorIl.Emit(OpCodes.Call, asmDef.MainModule.ImportReference(compiled.Type.Methods.First(m => m.IsConstructor && m.Parameters.Count == 0)));
        ctorIl.Emit(OpCodes.Ret);
    }
}
