using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
    private void ResolveHlTypeWithEnum(HlTypeWithEnum type, AssemblyDefinition asmDef) { }

    private void DefineHlTypeWithEnum(HlTypeWithEnum type, AssemblyDefinition asmDef) { }

    private void CompileHlTypeWithEnum(HlTypeWithEnum type, AssemblyDefinition asmDef) {
        ExtractNameAndNamespace(type.Enum.Name, out var enumNs, out var enumName);
        var enumDef = new TypeDefinition(
            enumNs ?? "",
            enumName,
            TypeAttributes.AnsiClass
          | TypeAttributes.BeforeFieldInit
          | TypeAttributes.Public
          | TypeAttributes.Abstract,
            asmDef.MainModule.TypeSystem.Object
        );
        asmDef.MainModule.Types.Add(enumDef);

        var enumCtor = new MethodDefinition(
            ".ctor",
            MethodAttributes.Family
          | MethodAttributes.HideBySig
          | MethodAttributes.RTSpecialName
          | MethodAttributes.SpecialName,
            asmDef.MainModule.TypeSystem.Void
        );
        enumDef.Methods.Add(enumCtor);
        var enumCtorIl = enumCtor.Body.GetILProcessor();
        enumCtorIl.Emit(OpCodes.Ldarg_0);
        enumCtorIl.Emit(OpCodes.Call, asmDef.MainModule.ImportReference(CecilUtils.DefaultCtorFor(enumDef.BaseType)));
        enumCtorIl.Emit(OpCodes.Ret);

        foreach (var construct in type.Enum.Constructs) {
            var nestedType = new TypeDefinition(
                enumNs ?? "",
                construct.Name,
                TypeAttributes.AnsiClass
              | TypeAttributes.BeforeFieldInit
              | TypeAttributes.NestedPublic,
                enumDef
            );
            // asmDef.MainModule.Types.Add(nestedType);
            enumDef.NestedTypes.Add(nestedType);

            var nestedCtor = new MethodDefinition(
                ".ctor",
                MethodAttributes.Public
              | MethodAttributes.HideBySig
              | MethodAttributes.RTSpecialName
              | MethodAttributes.SpecialName,
                asmDef.MainModule.TypeSystem.Void
            );
            nestedType.Methods.Add(nestedCtor);
            var nestedCtorIl = nestedCtor.Body.GetILProcessor();
            nestedCtorIl.Emit(OpCodes.Ldarg_0);
            nestedCtorIl.Emit(OpCodes.Call, asmDef.MainModule.ImportReference(CecilUtils.DefaultCtorFor(nestedType.BaseType)));

            var paramNumber = 0;

            foreach (var param in construct.Params) {
                var name = "param" + paramNumber;
                var fieldDef = new FieldDefinition(
                    name,
                    FieldAttributes.Public,
                    TypeReferenceFromHlTypeRef(param, asmDef)
                );
                nestedType.Fields.Add(fieldDef);

                var paramDef = new ParameterDefinition(name, ParameterAttributes.None, fieldDef.FieldType);
                nestedCtor.Parameters.Add(paramDef);

                nestedCtorIl.Emit(OpCodes.Ldarg_0);
                nestedCtorIl.Emit(OpCodes.Ldarg, paramDef);
                nestedCtorIl.Emit(OpCodes.Stfld, fieldDef);

                paramNumber++;
            }

            nestedCtorIl.Emit(OpCodes.Ret);
        }
    }
}
