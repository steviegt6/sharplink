﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

public partial class HlCodeCompiler {
    private readonly HlCodeHash hash;
    private readonly Compilation compilation = new();
    private readonly Dictionary<string, int> anonymousTypeCounter = new();

    public HlCodeCompiler(HlCodeHash hash) {
        this.hash = hash;
    }

    public AssemblyDefinition Compile(string name) {
        // HashLink preserves no version information.
        var asmNameDef = new AssemblyNameDefinition(name, new Version(1, 0, 0, 0));
        var asmDef = AssemblyDefinition.CreateAssembly(asmNameDef, name, ModuleKind.Dll);

        CompileTypes(asmDef);
        DeclareAndDecorateGlobals(asmDef);
        CompileFunctions(asmDef);

        return asmDef;
    }

    private void CompileTypes(AssemblyDefinition asmDef) {
        // Split into three steps.
        //   Resolution:  Makes us aware of types, does the bare minimum for type
        //                definition.
        //   Definition:  Type definition past the bare minimum.
        //   Compilation: Essentially makes things functional and adds
        //                everything to the assembly.

        // Resolution: Populates dictionaries with the bare minimum defined
        // objects.
        foreach (var type in hash.Code.Types)
            ResolveType(type, asmDef);

        // Definition: Populates defined objects with full type information
        // (such as what type is inherited, etc.).
        foreach (var type in hash.Code.Types)
            DefineType(type, asmDef);

        // Compilation: Adds defined objects to the assembly, further processing
        // for things such as functions, etc.
        foreach (var type in hash.Code.Types)
            CompileType(type, asmDef);
    }

    private void DeclareAndDecorateGlobals(AssemblyDefinition asmDef) {
        for (var i = 0; i < hash.Code.Globals.Count; i++) {
            var global = hash.Code.Globals[i];
            if (global.Value is not { } value)
                throw new InvalidOperationException($"Encountered global with missing value.");

            var type = value switch {
                HlTypeWithObj obj => compilation.GetObj(obj).Type,
                HlTypeWithEnum @enum => compilation.GetEnum(@enum).Type,
                HlTypeWithAbsName => asmDef.MainModule.TypeSystem.IntPtr,
                _ => throw new InvalidOperationException($"Encountered global with unknown type {value.GetType().Name}.")
            };

            var globalField = new FieldDefinition($"global{i}", FieldAttributes.Public | FieldAttributes.Static, type);
            asmDef.MainModule.GetType("<Module>").Fields.Add(globalField);
        }
    }

    private void CompileFunctions(AssemblyDefinition asmDef) {
        foreach (var func in hash.Code.Functions)
            DefineFunction(func, asmDef);

        foreach (var native in hash.Code.Natives)
            DefineNative(native, asmDef);

        foreach (var func in hash.Code.Functions)
            CompileFunction(func, asmDef);

        foreach (var native in hash.Code.Natives)
            CompileNative(native, asmDef);
    }

    private void ResolveType(HlType hlType, AssemblyDefinition asmDef) {
        switch (hlType.Kind) {
            case HlTypeKind.Void:
            case HlTypeKind.UI8:
            case HlTypeKind.UI16:
            case HlTypeKind.I32:
            case HlTypeKind.I64:
            case HlTypeKind.F32:
            case HlTypeKind.F64:
            case HlTypeKind.Bool:
                // These are primitive types with .NET equivalents.
                return;

            case HlTypeKind.Bytes:
            case HlTypeKind.Dyn:
            case HlTypeKind.Type:
            case HlTypeKind.Array:
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
            case HlTypeKind.Void:
            case HlTypeKind.UI8:
            case HlTypeKind.UI16:
            case HlTypeKind.I32:
            case HlTypeKind.I64:
            case HlTypeKind.F32:
            case HlTypeKind.F64:
            case HlTypeKind.Bool:
                // These are primitive types with .NET equivalents.
                return;

            case HlTypeKind.Bytes:
            case HlTypeKind.Dyn:
            case HlTypeKind.Type:
            case HlTypeKind.Array:
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
            case HlTypeKind.Void:
            case HlTypeKind.UI8:
            case HlTypeKind.UI16:
            case HlTypeKind.I32:
            case HlTypeKind.I64:
            case HlTypeKind.F32:
            case HlTypeKind.F64:
            case HlTypeKind.Bool:
            case HlTypeKind.Bytes:
            case HlTypeKind.Dyn:
            case HlTypeKind.Type:
            case HlTypeKind.Array:
                // These are HL-defined primitive types that we make our own
                // equivalents for in our stdlib implementation. We skip them
                // here since they contain not actual information.
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

    private TypeDefinition CreateAnonymousType(string @namespace, TypeAttributes attributes, TypeReference baseType, AssemblyDefinition asmDef) {
        if (!anonymousTypeCounter.ContainsKey(@namespace))
            anonymousTypeCounter.Add(@namespace, 0);

        var name = $"<>f__AnonymousType{anonymousTypeCounter[@namespace]++}";
        var typeDef = new TypeDefinition(@namespace, name, attributes, baseType);
        typeDef.CustomAttributes.Add(new CustomAttribute(asmDef.MainModule.ImportReference(typeof(CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes)!)));
        return typeDef;
    }

    private TypeDefinition CreateAnonymousDelegate(AssemblyDefinition asmDef) {
        /*var name = $"<>f__AnonymousDelegate{anonymousDelegateCounter++}";
        var typeDef = new TypeDefinition(null, name, TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Public, asmDef.MainModule.ImportReference(typeof(MulticastDelegate)));*/
        var typeDef = CreateAnonymousType("", TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Public, asmDef.MainModule.ImportReference(typeof(MulticastDelegate)), asmDef);

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

    private TypeDefinition TypeDefinitionFromFunctionIndex(int fIndex) {
        return compilation.GetFun((HlTypeWithFun)hash.Code.Functions[hash.FunctionIndexes[fIndex]].Type.Value!).Type;
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
