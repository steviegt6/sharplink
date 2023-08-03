using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

public partial class HlCodeCompiler {
    private readonly HlCode code;

    private readonly Dictionary<string, int> anonymousTypeCounter = new();
    private int anonymousDelegateCounter = 0;

    public HlCodeCompiler(HlCode code) {
        this.code = code;
    }

    public AssemblyDefinition Compile(string name) {
        // HashLink preserves no version information.
        var asmNameDef = new AssemblyNameDefinition(name, new Version(1, 0, 0, 0));
        var asmDef = AssemblyDefinition.CreateAssembly(asmNameDef, name, ModuleKind.Dll);

        // Split into three steps.
        //   Resolution:  Makes us aware of types, does the bare minimum for type
        //                definition.
        //   Definition:  Type definition past the bare minimum.
        //   Compilation: Essentially makes things functional and adds
        //                everything to the assembly.

        // Resolution: Populates dictionaries with the bare minimum defined
        // objects.
        foreach (var type in code.Types)
            ResolveType(type, asmDef);

        // Definition: Populates defined objects with full type information
        // (such as what type is inherited, etc.).
        foreach (var type in code.Types)
            DefineType(type, asmDef);

        // Compilation: Adds defined objects to the assembly, further processing
        // for things such as functions, etc.
        foreach (var type in code.Types)
            CompileType(type, asmDef);

        return asmDef;
    }

    private void ResolveType(HlType hlType, AssemblyDefinition asmDef) {
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
            ResolveHlTypeWithAbsName(absName, asmDef);
        else if (hlType is HlTypeWithFun fun)
            ResolveHlTypeWithFun(fun, asmDef);
        else if (hlType is HlTypeWithObj obj)
            ResolveHlTypeWithObj(obj, asmDef);
        else if (hlType is HlTypeWithEnum @enum)
            ResolveHlTypeWithEnum(@enum, asmDef);
        else if (hlType is HlTypeWithVirtual @virtual)
            ResolveHlTypeWithVirtual(@virtual, asmDef);
        else if (hlType is HlTypeWithType type)
            ResolveHlTypeWithType(type, asmDef);
        else
            throw new ArgumentOutOfRangeException(nameof(hlType), $"Unexpected HlType: '{hlType.GetType().FullName}'.");
    }

    private void DefineType(HlType hlType, AssemblyDefinition asmDef) {
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
            DefineHlTypeWithAbsName(absName, asmDef);
        else if (hlType is HlTypeWithFun fun)
            DefineHlTypeWithFun(fun, asmDef);
        else if (hlType is HlTypeWithObj obj)
            DefineHlTypeWithObj(obj, asmDef);
        else if (hlType is HlTypeWithEnum @enum)
            DefineHlTypeWithEnum(@enum, asmDef);
        else if (hlType is HlTypeWithVirtual @virtual)
            DefineHlTypeWithVirtual(@virtual, asmDef);
        else if (hlType is HlTypeWithType type)
            DefineHlTypeWithType(type, asmDef);
        else
            throw new ArgumentOutOfRangeException(nameof(hlType), $"Unexpected HlType: '{hlType.GetType().FullName}'.");
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

#if DEBUG
    private TypeReference TypeReferenceFromHlTypeRef(HlTypeRef typeRef, AssemblyDefinition asmDef) {
        try {
            return _TypeReferenceFromHlTypeRef(typeRef, asmDef);
        }
        catch {
            return asmDef.MainModule.TypeSystem.Object;
        }
    }

    private TypeReference _TypeReferenceFromHlTypeRef(HlTypeRef typeRef, AssemblyDefinition asmDef) {
#else
    private TypeReference TypeReferenceFromHlTypeRef(HlTypeRef typeRef, AssemblyDefinition asmDef) {
#endif
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
                return objDefs[(HlTypeWithObj)type];

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

    private TypeDefinition CreateAnonymousType(string @namespace, TypeAttributes attributes, TypeReference baseType, AssemblyDefinition asmDef) {
        if (!anonymousTypeCounter.ContainsKey(@namespace))
            anonymousTypeCounter.Add(@namespace, 0);

        var name = $"<>f__AnonymousType{anonymousTypeCounter[@namespace]++}";
        var typeDef = new TypeDefinition(@namespace, name, attributes, baseType);
        typeDef.CustomAttributes.Add(new CustomAttribute(asmDef.MainModule.ImportReference(typeof(CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes)!)));
        return typeDef;
    }

    private TypeDefinition CreateAnonymousDelegate(AssemblyDefinition asmDef) {
        var name = $"<>f__AnonymousDelegate{anonymousDelegateCounter++}";
        var typeDef = new TypeDefinition(null, name, TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Public, asmDef.MainModule.ImportReference(typeof(MulticastDelegate)));
        typeDef.CustomAttributes.Add(new CustomAttribute(asmDef.MainModule.ImportReference(typeof(CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes)!)));

        var ctor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, asmDef.MainModule.TypeSystem.Void);
        ctor.Parameters.Add(new ParameterDefinition("object", ParameterAttributes.None, asmDef.MainModule.TypeSystem.Object));
        ctor.Parameters.Add(new ParameterDefinition("method", ParameterAttributes.None, asmDef.MainModule.TypeSystem.IntPtr));
        ctor.CustomAttributes.Add(new CustomAttribute(asmDef.MainModule.ImportReference(typeof(CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes)!)));
        typeDef.Methods.Add(ctor);

        return typeDef;
    }

    private void DefineAnonymousDelegate(TypeDefinition delegateType, TypeReference returnType, IEnumerable<TypeReference> parameterTypes, AssemblyDefinition asmDef) {
        parameterTypes = parameterTypes.ToArray();

        var invoke = new MethodDefinition("Invoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, returnType);
        foreach (var parameterType in parameterTypes)
            invoke.Parameters.Add(new ParameterDefinition(parameterType));
        delegateType.Methods.Add(invoke);

        var beginInvoke = new MethodDefinition("BeginInvoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, asmDef.MainModule.ImportReference(typeof(IAsyncResult)));
        foreach (var parameterType in parameterTypes)
            beginInvoke.Parameters.Add(new ParameterDefinition(parameterType));
        delegateType.Methods.Add(beginInvoke);

        var endInvoke = new MethodDefinition("EndInvoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, returnType);
        endInvoke.Parameters.Add(new ParameterDefinition(asmDef.MainModule.ImportReference(typeof(IAsyncResult))));
        delegateType.Methods.Add(endInvoke);
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
