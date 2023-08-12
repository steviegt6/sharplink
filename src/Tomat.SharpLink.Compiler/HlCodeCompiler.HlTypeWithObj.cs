using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
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

    private Dictionary<HlTypeWithObj, CompiledObj> compiledObjs = new();
    private Dictionary<TypeDefinition, CompiledObj> compiledObjsByType = new();
    private Dictionary<TypeDefinition, CompiledVirtual> compiledVirtualsByType = new();

    private CompiledObj GetCompiledObj(HlTypeWithObj type) {
        return compiledObjs[type];
    }

    private List<FieldDefinition> GetAllFields(TypeDefinition type) {
        if (compiledObjsByType.TryGetValue(type, out var compiledObj))
            return compiledObj.AllFields;

        compiledObj = compiledObjs.Values.FirstOrDefault(x => x.Type.FullName == type.FullName);

        if (compiledObj is not null) {
            compiledObjsByType.Add(type, compiledObj);
            return compiledObj.AllFields;
        }

        if (compiledVirtualsByType.TryGetValue(type, out var compiledVirtual))
            return compiledVirtual.AllFields;

        compiledVirtual = compiledVirtuals.Values.FirstOrDefault(x => x.Type.FullName == type.FullName);

        if (compiledVirtual is not null) {
            compiledVirtualsByType.Add(type, compiledVirtual);
            return compiledVirtual.AllFields;
        }

        throw new KeyNotFoundException($"Could not find compiled object or virtual for type {type.FullName}");
    }

    private List<FieldDefinition> GetAllProtos(TypeDefinition type) {
        if (compiledObjsByType.TryGetValue(type, out var compiledObj))
            return compiledObj.AllProtos;

        compiledObj = compiledObjs.Values.FirstOrDefault(x => x.Type.FullName == type.FullName);

        if (compiledObj is not null) {
            compiledObjsByType.Add(type, compiledObj);
            return compiledObj.AllProtos;
        }

        throw new KeyNotFoundException($"Could not find compiled object for type {type.FullName}");
    }

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

        var fieldDefs = new Dictionary<HlObjField, FieldDefinition>();

        foreach (var field in type.Obj.Fields) {
            var fieldDef = new FieldDefinition(
                field.Name,
                FieldAttributes.Public,
                asmDef.MainModule.TypeSystem.Object // temporary
            );
            fieldDefs.Add(field, fieldDef);
        }

        var protoDefs = new Dictionary<HlObjProto, FieldDefinition>();

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

        compiledObjs.Add(type, new CompiledObj(objDef, fieldDefs, protoDefs));
    }

    private void DefineHlTypeWithObj(HlTypeWithObj type, AssemblyDefinition asmDef) {
        var compiled = compiledObjs[type];
        var objDef = compiled.Type;
        objDef.BaseType = type.Obj.Super is null ? asmDef.MainModule.TypeSystem.Object : TypeReferenceFromHlTypeRef(type.Obj.Super, asmDef);

        foreach (var field in type.Obj.Fields) {
            var fieldDef = compiled.Fields[field];
            fieldDef.FieldType = TypeReferenceFromHlTypeRef(field.Type, asmDef);
        }

        foreach (var proto in type.Obj.Protos) {
            var protoDef = compiled.Protos[proto];
            protoDef.FieldType = compiledFuns[(HlTypeWithFun)hash.Code.Functions[hash.FunctionIndexes[proto.FIndex]].Type.Value!].Type;
        }
    }

    private void CompileHlTypeWithObj(HlTypeWithObj type, AssemblyDefinition asmDef) {
        var compiled = compiledObjs[type];
        var objDef = compiled.Type;
        asmDef.MainModule.Types.Add(objDef);

        var fieldDefs = compiled.Fields;
        foreach (var fieldDef in fieldDefs.Values)
            objDef.Fields.Add(fieldDef);

        var protoDefs = compiled.Protos;
        foreach (var protoDef in protoDefs.Values)
            objDef.Fields.Add(protoDef);

        void reverseAddAllProtos(HlType? theType, List<FieldDefinition> protoFields) {
            if (theType is not HlTypeWithObj theTypeObj)
                return;

            reverseAddAllProtos(theTypeObj.Obj.Super?.Value ?? null, protoFields);

            var protoDefsForTheType = compiledObjs[theTypeObj].Protos;
            foreach (var proto in theTypeObj.Obj.Protos)
                protoFields.Add(protoDefsForTheType[proto]);
        }

        var allProtos = compiled.AllProtos;
        reverseAddAllProtos(type, allProtos);

        void reverseAddAllFields(HlType? theType, List<FieldDefinition> fieldFields) {
            if (theType is not HlTypeWithObj theTypeObj)
                return;

            reverseAddAllFields(theTypeObj.Obj.Super?.Value ?? null, fieldFields);

            var fieldDefsForTheType = compiledObjs[theTypeObj].Fields;
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
        objDef.Methods.Add(ctorDef);

        var ctorIl = ctorDef.Body.GetILProcessor();
        ctorIl.Emit(OpCodes.Ldarg_0);
        ctorIl.Emit(OpCodes.Call, asmDef.MainModule.ImportReference(objDef.Methods.First(m => m.IsConstructor && m.Parameters.Count == 0)));
        ctorIl.Emit(OpCodes.Ret);
    }
}
