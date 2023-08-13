using System.Linq;
using Mono.Cecil;
using Tomat.SharpLink.Compiler.ILGeneration;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
    private void DefineNative(HlNative native, AssemblyDefinition asmDef) {
        var method = CreateMethod(native, ((HlTypeWithFun)native.Type.Value!).FunctionDescription, asmDef);
        var attr = new CustomAttribute(asmDef.MainModule.ImportReference(typeof(HashLinkNativeImport).GetConstructor(new[] { typeof(string), typeof(string) })));
        attr.ConstructorArguments.Add(new CustomAttributeArgument(asmDef.MainModule.TypeSystem.String, native.Lib));
        attr.ConstructorArguments.Add(new CustomAttributeArgument(asmDef.MainModule.TypeSystem.String, native.Name));
        method.CustomAttributes.Add(attr);
        compilation.AddNative(new CompiledFunction {
            Native = native,
            Method = method,
        });
    }

    private void CompileNative(HlNative native, AssemblyDefinition asmDef) {
        var method = FunctionCompiler.CompileNative(native, asmDef, compilation);
        asmDef.MainModule.GetType("<Module>").Methods.Add(method);
    }

    private void DefineFunction(HlFunction fun, AssemblyDefinition asmDef) {
        var funType = ((HlTypeWithFun)fun.Type.Value!).FunctionDescription;
        var method = CreateMethod(fun, funType, asmDef);
        compilation.AddFunction(new CompiledFunction {
            Function = fun,
            Method = method,
        });
    }

    private void CompileFunction(HlFunction fun, AssemblyDefinition asmDef) {
        var method = FunctionCompiler.CompileFunction(fun, asmDef, hash, compilation);
        asmDef.MainModule.GetType("<Module>").Methods.Add(method);
    }

    private MethodDefinition CreateMethod(HlFunction fun, HlTypeFun funType, AssemblyDefinition asmDef) {
        var retType = compilation.TypeReferenceFromHlTypeRef(funType.ReturnType, asmDef);
        var paramTypes = funType.Arguments.Select(param => compilation.TypeReferenceFromHlTypeRef(param, asmDef)).ToArray();
        var method = new MethodDefinition($"fun{fun.FunctionIndex}", MethodAttributes.Public | MethodAttributes.Static, retType);

        var argCounter = 0;
        foreach (var paramType in paramTypes)
            method.Parameters.Add(new ParameterDefinition($"arg{argCounter++}", ParameterAttributes.None, paramType));
        return method;
    }

    private MethodDefinition CreateMethod(HlNative native, HlTypeFun funType, AssemblyDefinition asmDef) {
        var retType = compilation.TypeReferenceFromHlTypeRef(funType.ReturnType, asmDef);
        var paramTypes = funType.Arguments.Select(param => compilation.TypeReferenceFromHlTypeRef(param, asmDef)).ToArray();
        var method = new MethodDefinition($"fun{native.NativeIndex}", MethodAttributes.Public | MethodAttributes.Static, retType);

        var argCounter = 0;
        foreach (var paramType in paramTypes)
            method.Parameters.Add(new ParameterDefinition($"arg{argCounter++}", ParameterAttributes.None, paramType));
        return method;
    }
}
