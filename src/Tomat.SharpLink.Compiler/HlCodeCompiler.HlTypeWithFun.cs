using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
    private CompiledFun GetCompiledFun(HlTypeWithFun type) {
        return compiledFuns[type];
    }

    private void ResolveHlTypeWithFun(HlTypeWithFun type, AssemblyDefinition asmDef) {
        var funDelegateDef = CreateAnonymousDelegate(asmDef);
        compiledFuns.Add(type, new CompiledFun(funDelegateDef));
    }

    private void DefineHlTypeWithFun(HlTypeWithFun type, AssemblyDefinition asmDef) {
        var funDelegateDef = compiledFuns[type].Type;
        var retType = TypeReferenceFromHlTypeRef(type.FunctionDescription.ReturnType, asmDef);
        var paramTypes = type.FunctionDescription.Arguments.Select(param => TypeReferenceFromHlTypeRef(param, asmDef)).ToArray();
        DefineAnonymousDelegate(funDelegateDef, retType, paramTypes, asmDef);
    }

    private void CompileHlTypeWithFun(HlTypeWithFun type, AssemblyDefinition asmDef) {
        var funDelegateDef = compiledFuns[type].Type;
        asmDef.MainModule.Types.Add(funDelegateDef);
    }
}
