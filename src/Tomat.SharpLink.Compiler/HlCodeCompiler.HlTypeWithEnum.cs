using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
    private CompiledEnum GetCompiledEnum(HlTypeWithEnum type) {
        return compiledEnums[type];
    }

    private void ResolveHlTypeWithEnum(HlTypeWithEnum type, AssemblyDefinition asmDef) {
        ExtractNameAndNamespace(type.Enum.Name, out var enumNs, out var enumName);
        var enumDef = new TypeDefinition(
            enumNs ?? "",
            enumName,
            TypeAttributes.BeforeFieldInit
          | TypeAttributes.Public
          | TypeAttributes.Abstract,
            asmDef.MainModule.TypeSystem.Object
        );
        compiledEnums.Add(type, new CompiledEnum(enumDef));
    }

    private void DefineHlTypeWithEnum(HlTypeWithEnum type, AssemblyDefinition asmDef) {
        var compiled = compiledEnums[type];

        var enumBaseCtor = new MethodDefinition(
            ".ctor",
            MethodAttributes.Family
          | MethodAttributes.HideBySig
          | MethodAttributes.RTSpecialName
          | MethodAttributes.SpecialName,
            asmDef.MainModule.TypeSystem.Void
        );
        compiled.BaseConstructor = enumBaseCtor;

        foreach (var construct in type.Enum.Constructs) {
            var compiledConstruct = new CompiledEnumConstruct(
                new TypeDefinition(
                    compiled.Type.Namespace ?? "",
                    construct.Name,
                    TypeAttributes.BeforeFieldInit
                  | TypeAttributes.NestedPublic,
                    compiled.Type
                )
            ) {
                Constructor = new MethodDefinition(
                    ".ctor",
                    MethodAttributes.Public
                  | MethodAttributes.HideBySig
                  | MethodAttributes.RTSpecialName
                  | MethodAttributes.SpecialName,
                    asmDef.MainModule.TypeSystem.Void
                ),
            };

            var paramNumber = 0;

            foreach (var param in construct.Params) {
                var name = "param" + paramNumber;
                var fieldDef = new FieldDefinition(
                    name,
                    FieldAttributes.Public,
                    TypeReferenceFromHlTypeRef(param, asmDef)
                );
                compiledConstruct.Fields.Add(fieldDef);
                compiledConstruct.ConstructorParameters.Add(new ParameterDefinition(name, ParameterAttributes.None, fieldDef.FieldType));

                paramNumber++;
            }

            compiled.Constructs.Add(construct, compiledConstruct);
        }
    }

    private void CompileHlTypeWithEnum(HlTypeWithEnum type, AssemblyDefinition asmDef) {
        var compiled = compiledEnums[type];
        asmDef.MainModule.Types.Add(compiled.Type);

        compiled.Type.Methods.Add(compiled.BaseConstructor);

        var enumCtorIl = compiled.BaseConstructor.Body.GetILProcessor();
        enumCtorIl.Emit(OpCodes.Ldarg_0);
        enumCtorIl.Emit(OpCodes.Call, asmDef.MainModule.ImportReference(CecilUtils.DefaultCtorFor(compiled.Type.BaseType)));
        enumCtorIl.Emit(OpCodes.Ret);

        foreach (var construct in type.Enum.Constructs) {
            var compiledConstruct = compiled.Constructs[construct];
            compiled.Type.NestedTypes.Add(compiledConstruct.Type);

            compiledConstruct.Type.Methods.Add(compiledConstruct.Constructor);
            var nestedCtorIl = compiledConstruct.Constructor.Body.GetILProcessor();
            nestedCtorIl.Emit(OpCodes.Ldarg_0);
            nestedCtorIl.Emit(OpCodes.Call, asmDef.MainModule.ImportReference(CecilUtils.DefaultCtorFor(compiledConstruct.Type.BaseType)));

            for (var i = 0; i < construct.Params.Length; i++) {
                var fieldDef = compiledConstruct.Fields[i];
                compiled.Type.Fields.Add(fieldDef);

                var paramDef = compiledConstruct.ConstructorParameters[i];
                compiledConstruct.Constructor.Parameters.Add(paramDef);

                nestedCtorIl.Emit(OpCodes.Ldarg_0);
                nestedCtorIl.Emit(OpCodes.Ldarg, paramDef);
                nestedCtorIl.Emit(OpCodes.Stfld, fieldDef);
            }

            nestedCtorIl.Emit(OpCodes.Ret);
        }
    }
}
