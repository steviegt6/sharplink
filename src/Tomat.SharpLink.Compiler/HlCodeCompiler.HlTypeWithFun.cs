using System.Linq;
using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
    private void ResolveHlTypeWithFun(HlTypeWithFun type, AssemblyDefinition asmDef) {
        var funDelegateDef = CreateAnonymousDelegate(asmDef);
        compilation.AddFun(new CompiledFun {
            Fun = type,
            Type = funDelegateDef,
        });
    }

    private void DefineHlTypeWithFun(HlTypeWithFun type, AssemblyDefinition asmDef) {
        var funDelegateDef = compilation.GetFun(type).Type;
        var retType = TypeReferenceFromHlTypeRef(type.FunctionDescription.ReturnType, asmDef);
        var paramTypes = type.FunctionDescription.Arguments.Select(param => TypeReferenceFromHlTypeRef(param, asmDef)).ToArray();
        DefineAnonymousDelegate(funDelegateDef, retType, paramTypes, asmDef);
    }

    private void CompileHlTypeWithFun(HlTypeWithFun type, AssemblyDefinition asmDef) {
        var funDelegateDef = compilation.GetFun(type).Type;
        asmDef.MainModule.Types.Add(funDelegateDef);
    }
}
