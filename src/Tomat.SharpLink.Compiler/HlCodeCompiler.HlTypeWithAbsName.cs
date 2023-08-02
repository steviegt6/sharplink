using System.Collections.Generic;
using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
    private Dictionary<HlTypeWithAbsName, CustomAttribute> absNameDefs = new();

    private void ResolveHlTypeWithAbsName(HlTypeWithAbsName type, AssemblyDefinition asmDef) {
        var attr = new CustomAttribute(asmDef.MainModule.ImportReference(typeof(HashLinkAbstractAttribute).GetConstructor(new[] { typeof(string) })));
        absNameDefs.Add(type, attr);
    }

    private void DefineHlTypeWithAbsName(HlTypeWithAbsName type, AssemblyDefinition asmDef) {
        var attr = absNameDefs[type];
        attr.ConstructorArguments.Add(new CustomAttributeArgument(asmDef.MainModule.TypeSystem.String, type.AbsName));
    }

    private void CompileHlTypeWithAbsName(HlTypeWithAbsName type, AssemblyDefinition asmDef) {
        asmDef.CustomAttributes.Add(absNameDefs[type]);
    }
}
