using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
    private Dictionary<HlTypeWithVirtual, TypeDefinition> virtDefs = new();
    private Dictionary<HlTypeWithVirtual, MethodDefinition> virtCtorDefs = new();
    private Dictionary<HlTypeWithVirtual, List<FieldDefinition>> virtFieldDefs = new();
    private Dictionary<HlTypeWithVirtual, List<ParameterDefinition>> virtParamDefs = new();

    private void ResolveHlTypeWithVirtual(HlTypeWithVirtual type, AssemblyDefinition asmDef) {
        var virtDef = CreateAnonymousType(
            "",
            TypeAttributes.Class
          | TypeAttributes.NotPublic
          | TypeAttributes.Sealed
          | TypeAttributes.BeforeFieldInit,
            asmDef.MainModule.TypeSystem.Object,
            asmDef
        );
        virtDefs.Add(type, virtDef);

        var ctorDef = new MethodDefinition(
            ".ctor",
            MethodAttributes.Public
          | MethodAttributes.HideBySig
          | MethodAttributes.SpecialName
          | MethodAttributes.RTSpecialName,
            asmDef.MainModule.TypeSystem.Void
        );
        virtCtorDefs.Add(type, ctorDef);

        virtFieldDefs.Add(type, new List<FieldDefinition>());
        virtParamDefs.Add(type, new List<ParameterDefinition>());
    }

    private void DefineHlTypeWithVirtual(HlTypeWithVirtual type, AssemblyDefinition asmDef) {
        var virtFields = virtFieldDefs[type];
        var virtParams = virtParamDefs[type];

        foreach (var field in type.Virtual.Fields) {
            var fieldType = TypeReferenceFromHlTypeRef(field.Type, asmDef);
            var name = field.Name;
            var fieldDef = new FieldDefinition(name, FieldAttributes.Public, fieldType);
            virtFields.Add(fieldDef);

            var paramDef = new ParameterDefinition(name, ParameterAttributes.None, fieldType);
            virtParams.Add(paramDef);
        }
    }

    private void CompileHlTypeWithVirtual(HlTypeWithVirtual type, AssemblyDefinition asmDef) {
        var virtDef = virtDefs[type];
        asmDef.MainModule.Types.Add(virtDef);

        var ctorDef = virtCtorDefs[type];
        virtDef.Methods.Add(ctorDef);

        var ctorDefIl = ctorDef.Body.GetILProcessor();

        var virtFields = virtFieldDefs[type];
        var virtParams = virtParamDefs[type];

        var allFields = objTypeDefFields[virtDef] = new List<FieldDefinition>();

        for (var i = 0; i < type.Virtual.Fields.Length; i++) {
            var fieldDef = virtFields[i];
            virtDef.Fields.Add(fieldDef);
            allFields.Add(fieldDef);

            var paramDef = virtParams[i];
            ctorDef.Parameters.Add(paramDef);

            ctorDefIl.Emit(OpCodes.Ldarg_0);
            ctorDefIl.Emit(OpCodes.Ldarg, paramDef);
            ctorDefIl.Emit(OpCodes.Stfld, fieldDef);
        }

        ctorDefIl.Emit(OpCodes.Ret);

        var emptyCtorDef = new MethodDefinition(
            ".ctor",
            MethodAttributes.Public
          | MethodAttributes.HideBySig
          | MethodAttributes.SpecialName
          | MethodAttributes.RTSpecialName,
            asmDef.MainModule.TypeSystem.Void
        );
        virtDef.Methods.Add(emptyCtorDef);

        var ctorIl = emptyCtorDef.Body.GetILProcessor();
        ctorIl.Emit(OpCodes.Ldarg_0);
        ctorIl.Emit(OpCodes.Call, asmDef.MainModule.ImportReference(virtDef.Methods.First(m => m.IsConstructor && m.Parameters.Count == 0)));
        ctorIl.Emit(OpCodes.Ret);
    }
}
