using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
    private void CompileHlTypeWithAbsName(HlTypeWithAbsName type, AssemblyDefinition asmDef) {
        var hashlinkAbstractAttribute = new CustomAttribute(asmDef.MainModule.ImportReference(typeof(HashLinkAbstractAttribute).GetConstructor(new[] { typeof(string) })));
        hashlinkAbstractAttribute.ConstructorArguments.Add(new CustomAttributeArgument(asmDef.MainModule.TypeSystem.String, type.AbsName));
        asmDef.CustomAttributes.Add(hashlinkAbstractAttribute);
    }
}
