using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
    private void ResolveHlTypeWithAbsName(HlTypeWithAbsName type, AssemblyDefinition asmDef) {
        compilation.AddAbstract(new CompiledAbstract {
            Abstract = type,
            Attribute = new CustomAttribute(asmDef.MainModule.ImportReference(typeof(HashLinkAbstractAttribute).GetConstructor(new[] { typeof(string) }))),
        });
    }

    private void DefineHlTypeWithAbsName(HlTypeWithAbsName type, AssemblyDefinition asmDef) {
        var compiled = compilation.GetAbstract(type);
        compiled.Attribute.ConstructorArguments.Add(new CustomAttributeArgument(asmDef.MainModule.TypeSystem.String, type.AbstractName));
    }

    private void CompileHlTypeWithAbsName(HlTypeWithAbsName type, AssemblyDefinition asmDef) {
        var compiled = compilation.GetAbstract(type);
        asmDef.CustomAttributes.Add(compiled.Attribute);
    }
}
