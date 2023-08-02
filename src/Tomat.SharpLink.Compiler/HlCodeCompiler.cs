using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler;

public class HlCodeCompiler {
    private readonly HlCode code;

    public HlCodeCompiler(HlCode code) {
        this.code = code;
    }

    public AssemblyDefinition Compile(string name) {
        // HashLink preserves no version information.
        var asmNameDef = new AssemblyNameDefinition(name, new Version(1, 0, 0, 0));
        var asmDef = AssemblyDefinition.CreateAssembly(asmNameDef, name, ModuleKind.Dll);

        foreach (var type in code.Types)
            CompileType(type, asmDef);

        return asmDef;
    }

    private void CompileType(HlType type, AssemblyDefinition asmDef) {
        switch (type.Kind) {
            case HlTypeKind.HVOID:
            case HlTypeKind.HUI8:
            case HlTypeKind.HUI16:
            case HlTypeKind.HI32:
            case HlTypeKind.HI64:
            case HlTypeKind.HF32:
            case HlTypeKind.HF64:
            case HlTypeKind.HBOOL:
                // These are primitive types with .NET equivalents.
                return;

            case HlTypeKind.HBYTES:
            case HlTypeKind.HDYN:
                // TODO: These are HL-only primitives, how will we handle them?
                return;
        }

        if (type is HlTypeWithEnum @enum) {
            ExtractNameAndNamespace(@enum.Enum.Name, out var enumNs, out var enumName);
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

            foreach (var construct in @enum.Enum.Constructs) {
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

    private TypeReference TypeReferenceFromHlTypeRef(HlTypeRef typeRef, AssemblyDefinition asmDef) {
        if (typeRef.Value is not { } type)
            throw new ArgumentException("Type reference was null.", nameof(typeRef));

        switch (type.Kind) {
            case HlTypeKind.HVOID:
                return asmDef.MainModule.TypeSystem.Void;

            case HlTypeKind.HUI8:
                return asmDef.MainModule.TypeSystem.Byte;

            case HlTypeKind.HUI16:
                return asmDef.MainModule.TypeSystem.UInt16;

            case HlTypeKind.HI32:
                return asmDef.MainModule.TypeSystem.Int32;

            case HlTypeKind.HI64:
                return asmDef.MainModule.TypeSystem.Int64;

            case HlTypeKind.HF32:
                return asmDef.MainModule.TypeSystem.Single;

            case HlTypeKind.HF64:
                return asmDef.MainModule.TypeSystem.Double;

            case HlTypeKind.HBOOL:
                return asmDef.MainModule.TypeSystem.Boolean;

            case HlTypeKind.HBYTES:
                throw new NotImplementedException();

            // TODO: This will require special handling.
            case HlTypeKind.HDYN:
                return asmDef.MainModule.TypeSystem.Object;

            case HlTypeKind.HFUN:
                throw new NotImplementedException();

            case HlTypeKind.HOBJ:
                throw new NotImplementedException();

            case HlTypeKind.HARRAY:
                throw new NotImplementedException();

            case HlTypeKind.HTYPE:
                throw new NotImplementedException();

            case HlTypeKind.HREF:
                throw new NotImplementedException();

            case HlTypeKind.HVIRTUAL:
                throw new NotImplementedException();

            case HlTypeKind.HDYNOBJ:
                throw new NotImplementedException();

            case HlTypeKind.HABSTRACT:
                throw new NotImplementedException();

            case HlTypeKind.HENUM:
                throw new NotImplementedException();

            case HlTypeKind.HNULL:
                throw new NotImplementedException();

            case HlTypeKind.HMETHOD:
                throw new NotImplementedException();

            case HlTypeKind.HSTRUCT:
                throw new NotImplementedException();

            case HlTypeKind.HPACKED:
                throw new NotImplementedException();

            default:
                throw new ArgumentOutOfRangeException(nameof(typeRef), "Type kind out range.");
        }
    }

    private static void ExtractNameAndNamespace(string fullTypeName, out string? @namespace, out string name) {
        var lastDot = fullTypeName.LastIndexOf('.');

        if (lastDot == -1) {
            @namespace = null;
            name = fullTypeName;
        }
        else {
            @namespace = fullTypeName[..lastDot];
            name = fullTypeName[(lastDot + 1)..];
        }
    }
}
