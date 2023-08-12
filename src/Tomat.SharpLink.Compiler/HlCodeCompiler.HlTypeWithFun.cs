using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
    private Dictionary<HlTypeWithFun, TypeDefinition> funDefs = new();

    private void ResolveHlTypeWithFun(HlTypeWithFun type, AssemblyDefinition asmDef) {
        var funDelegateDef = CreateAnonymousDelegate(asmDef);
        funDefs.Add(type, funDelegateDef);
    }

    private void DefineHlTypeWithFun(HlTypeWithFun type, AssemblyDefinition asmDef) {
        var funDelegateDef = funDefs[type];
        var retType = TypeReferenceFromHlTypeRef(type.FunctionDescription.ReturnType, asmDef);
        var paramTypes = type.FunctionDescription.Arguments.Select(param => TypeReferenceFromHlTypeRef(param, asmDef)).ToArray();
        DefineAnonymousDelegate(funDelegateDef, retType, paramTypes, asmDef);
    }

    private void CompileHlTypeWithFun(HlTypeWithFun type, AssemblyDefinition asmDef) {
        var funDelegateDef = funDefs[type];
        asmDef.MainModule.Types.Add(funDelegateDef);
    }
}
