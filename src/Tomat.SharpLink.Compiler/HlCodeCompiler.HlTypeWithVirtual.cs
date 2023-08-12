using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
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

        var ctorDef = new MethodDefinition(
            ".ctor",
            MethodAttributes.Public
          | MethodAttributes.HideBySig
          | MethodAttributes.SpecialName
          | MethodAttributes.RTSpecialName,
            asmDef.MainModule.TypeSystem.Void
        );

        compilation.AddVirtual(type, new CompiledVirtual(virtDef, ctorDef));
    }

    private void DefineHlTypeWithVirtual(HlTypeWithVirtual type, AssemblyDefinition asmDef) {
        var compiled = compilation.GetVirtual(type);
        var virtFields = compiled.Fields;
        var virtParams = compiled.ConstructorParameters;

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
        var compiled = compilation.GetVirtual(type);
        var virtDef = compiled.Type;
        asmDef.MainModule.Types.Add(virtDef);

        var ctorDef = compiled.BaseConstructor;
        virtDef.Methods.Add(ctorDef);

        var ctorDefIl = ctorDef.Body.GetILProcessor();

        var virtFields = compiled.Fields;
        var virtParams = compiled.ConstructorParameters;

        var allFields = compiled.AllFields;

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
