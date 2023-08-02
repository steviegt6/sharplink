using System;
using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

public partial class HlCodeCompiler {
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

    private void CompileType(HlType hlType, AssemblyDefinition asmDef) {
        switch (hlType.Kind) {
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
            case HlTypeKind.HTYPE:
            case HlTypeKind.HARRAY:
                // TODO: These are HL-only primitives, how will we handle them?
                return;
        }

        if (hlType is HlTypeWithAbsName absName)
            CompileHlTypeWithAbsName(absName, asmDef);
        else if (hlType is HlTypeWithFun fun)
            CompileHlTypeWithFun(fun, asmDef);
        else if (hlType is HlTypeWithObj obj)
            CompileHlTypeWithObj(obj, asmDef);
        else if (hlType is HlTypeWithEnum @enum)
            CompileHlTypeWithEnum(@enum, asmDef);
        else if (hlType is HlTypeWithVirtual @virtual)
            CompileHlTypeWithVirtual(@virtual, asmDef);
        else if (hlType is HlTypeWithType type)
            CompileHlTypeWithType(type, asmDef);
        else
            throw new ArgumentOutOfRangeException(nameof(hlType), $"Unexpected HlType: '{hlType.GetType().FullName}'.");
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
