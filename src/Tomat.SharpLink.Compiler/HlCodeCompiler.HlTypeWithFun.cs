using System.Linq;
using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
    private void ResolveHlTypeWithFun(HlTypeWithFun type, AssemblyDefinition asmDef) {
        compilation.AddFun(new CompiledFun {
            Fun = type,
            Type = CreateAnonymousDelegate(asmDef),
        });
    }

    private void DefineHlTypeWithFun(HlTypeWithFun type, AssemblyDefinition asmDef) {
        var compiled = compilation.GetFun(type);
        var returnType = TypeReferenceFromHlTypeRef(type.FunctionDescription.ReturnType, asmDef);
        var parameterTypes = type.FunctionDescription.Arguments.Select(x => TypeReferenceFromHlTypeRef(x, asmDef)).ToArray();
        DefineAnonymousDelegate(compiled.Type, returnType, parameterTypes, asmDef);
    }

    private void CompileHlTypeWithFun(HlTypeWithFun type, AssemblyDefinition asmDef) {
        var compiled = compilation.GetFun(type);
        asmDef.MainModule.Types.Add(compiled.Type);
    }
}
