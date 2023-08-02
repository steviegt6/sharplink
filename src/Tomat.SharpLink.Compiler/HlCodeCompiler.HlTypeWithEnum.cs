using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
    private Dictionary<HlTypeWithEnum, TypeDefinition> enumDefs = new();
    private Dictionary<HlTypeWithEnum, MethodDefinition> enumBaseCtorDefs = new();
    private Dictionary<HlTypeWithEnum, Dictionary<HlEnumConstruct, TypeDefinition>> enumConstructDefs = new();
    private Dictionary<HlTypeWithEnum, Dictionary<HlEnumConstruct, MethodDefinition>> enumConstructCtorDefs = new();
    private Dictionary<HlTypeWithEnum, Dictionary<HlEnumConstruct, List<FieldDefinition>>> enumConstructFieldDefs = new();
    private Dictionary<HlTypeWithEnum, Dictionary<HlEnumConstruct, List<ParameterDefinition>>> enumConstructCtorParamDefs = new();

    private void ResolveHlTypeWithEnum(HlTypeWithEnum type, AssemblyDefinition asmDef) {
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
        enumDefs.Add(type, enumDef);
    }

    private void DefineHlTypeWithEnum(HlTypeWithEnum type, AssemblyDefinition asmDef) {
        var enumDef = enumDefs[type];

        var enumBaseCtor = new MethodDefinition(
            ".ctor",
            MethodAttributes.Family
          | MethodAttributes.HideBySig
          | MethodAttributes.RTSpecialName
          | MethodAttributes.SpecialName,
            asmDef.MainModule.TypeSystem.Void
        );
        enumBaseCtorDefs.Add(type, enumBaseCtor);
        enumConstructDefs.Add(type, new Dictionary<HlEnumConstruct, TypeDefinition>());
        enumConstructCtorDefs.Add(type, new Dictionary<HlEnumConstruct, MethodDefinition>());
        enumConstructFieldDefs.Add(type, new Dictionary<HlEnumConstruct, List<FieldDefinition>>());
        enumConstructCtorParamDefs.Add(type, new Dictionary<HlEnumConstruct, List<ParameterDefinition>>());

        foreach (var construct in type.Enum.Constructs) {
            var nestedType = new TypeDefinition(
                enumDef.Namespace ?? "",
                construct.Name,
                TypeAttributes.AnsiClass
              | TypeAttributes.BeforeFieldInit
              | TypeAttributes.NestedPublic,
                enumDef
            );
            enumConstructDefs[type].Add(construct, nestedType);

            var nestedCtor = new MethodDefinition(
                ".ctor",
                MethodAttributes.Public
              | MethodAttributes.HideBySig
              | MethodAttributes.RTSpecialName
              | MethodAttributes.SpecialName,
                asmDef.MainModule.TypeSystem.Void
            );
            enumConstructCtorDefs[type].Add(construct, nestedCtor);

            var fieldDefs = enumConstructFieldDefs[type][construct] = new List<FieldDefinition>();
            var paramDefs = enumConstructCtorParamDefs[type][construct] = new List<ParameterDefinition>();

            var paramNumber = 0;

            foreach (var param in construct.Params) {
                var name = "param" + paramNumber;
                var fieldDef = new FieldDefinition(
                    name,
                    FieldAttributes.Public,
                    TypeReferenceFromHlTypeRef(param, asmDef)
                );
                fieldDefs.Add(fieldDef);

                var paramDef = new ParameterDefinition(name, ParameterAttributes.None, fieldDef.FieldType);
                paramDefs.Add(paramDef);

                paramNumber++;
            }
        }
    }

    private void CompileHlTypeWithEnum(HlTypeWithEnum type, AssemblyDefinition asmDef) {
        var enumDef = enumDefs[type];
        asmDef.MainModule.Types.Add(enumDef);

        var enumBaseCtor = enumBaseCtorDefs[type];
        enumDef.Methods.Add(enumBaseCtor);

        var enumCtorIl = enumBaseCtor.Body.GetILProcessor();
        enumCtorIl.Emit(OpCodes.Ldarg_0);
        enumCtorIl.Emit(OpCodes.Call, asmDef.MainModule.ImportReference(CecilUtils.DefaultCtorFor(enumDef.BaseType)));
        enumCtorIl.Emit(OpCodes.Ret);

        foreach (var construct in type.Enum.Constructs) {
            var nestedType = enumConstructDefs[type][construct];
            enumDef.NestedTypes.Add(nestedType);

            var nestedCtor = enumConstructCtorDefs[type][construct];
            nestedType.Methods.Add(nestedCtor);
            var nestedCtorIl = nestedCtor.Body.GetILProcessor();
            nestedCtorIl.Emit(OpCodes.Ldarg_0);
            nestedCtorIl.Emit(OpCodes.Call, asmDef.MainModule.ImportReference(CecilUtils.DefaultCtorFor(nestedType.BaseType)));

            for (var i = 0; i < construct.Params.Length; i++) {
                var fieldDef = enumConstructFieldDefs[type][construct][i];
                nestedType.Fields.Add(fieldDef);

                var paramDef = enumConstructCtorParamDefs[type][construct][i];
                nestedCtor.Parameters.Add(paramDef);

                nestedCtorIl.Emit(OpCodes.Ldarg_0);
                nestedCtorIl.Emit(OpCodes.Ldarg, paramDef);
                nestedCtorIl.Emit(OpCodes.Stfld, fieldDef);
            }

            nestedCtorIl.Emit(OpCodes.Ret);
        }
    }
}
