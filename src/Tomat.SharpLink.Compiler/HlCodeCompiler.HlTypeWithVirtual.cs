using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
    private void ResolveHlTypeWithVirtual(HlTypeWithVirtual type, AssemblyDefinition asmDef) {
        compilation.AddVirtual(new CompiledVirtual {
            Virtual = type,
            Type = CreateAnonymousType(
                "",
                TypeAttributes.Class
              | TypeAttributes.NotPublic
              | TypeAttributes.Sealed
              | TypeAttributes.BeforeFieldInit,
                asmDef.MainModule.TypeSystem.Object,
                asmDef
            ),
            Constructor = new MethodDefinition(
                ".ctor",
                MethodAttributes.Public
              | MethodAttributes.HideBySig
              | MethodAttributes.SpecialName
              | MethodAttributes.RTSpecialName,
                asmDef.MainModule.TypeSystem.Void
            ),
        });
    }

    private void DefineHlTypeWithVirtual(HlTypeWithVirtual type, AssemblyDefinition asmDef) {
        var compiled = compilation.GetVirtual(type);

        foreach (var field in type.Virtual.Fields) {
            var fieldType = TypeReferenceFromHlTypeRef(field.Type, asmDef);
            compiled.Fields.Add(new FieldDefinition(field.Name, FieldAttributes.Public, fieldType));
            compiled.ConstructorParameters.Add(new ParameterDefinition(field.Name, ParameterAttributes.None, fieldType));
        }
    }

    private void CompileHlTypeWithVirtual(HlTypeWithVirtual type, AssemblyDefinition asmDef) {
        var compiled = compilation.GetVirtual(type);
        asmDef.MainModule.Types.Add(compiled.Type);

        compiled.Type.Methods.Add(compiled.Constructor);

        var ctorDefIl = compiled.Constructor.Body.GetILProcessor();

        for (var i = 0; i < type.Virtual.Fields.Length; i++) {
            var fieldDef = compiled.Fields[i];
            compiled.Type.Fields.Add(fieldDef);
            compiled.AllFields.Add(fieldDef);

            var paramDef = compiled.ConstructorParameters[i];
            compiled.Constructor.Parameters.Add(paramDef);

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
        compiled.Type.Methods.Add(emptyCtorDef);

        var ctorIl = emptyCtorDef.Body.GetILProcessor();
        ctorIl.Emit(OpCodes.Ldarg_0);
        ctorIl.Emit(OpCodes.Call, asmDef.MainModule.ImportReference(compiled.Type.Methods.First(m => m.IsConstructor && m.Parameters.Count == 0)));
        ctorIl.Emit(OpCodes.Ret);
    }
}
