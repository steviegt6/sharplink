using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
    private void ResolveHlTypeWithAbsName(HlTypeWithAbsName type, AssemblyDefinition asmDef) {
        var attr = new CustomAttribute(asmDef.MainModule.ImportReference(typeof(HashLinkAbstractAttribute).GetConstructor(new[] { typeof(string) })));
        compilation.AddAbstract(new CompiledAbstract {
            Abstract = type,
            Attribute = attr,
        });
    }

    private void DefineHlTypeWithAbsName(HlTypeWithAbsName type, AssemblyDefinition asmDef) {
        var compiled = compilation.GetAbstract(type);
        var attr = compiled.Attribute;
        attr.ConstructorArguments.Add(new CustomAttributeArgument(asmDef.MainModule.TypeSystem.String, type.AbstractName));
    }

    private void CompileHlTypeWithAbsName(HlTypeWithAbsName type, AssemblyDefinition asmDef) {
        asmDef.CustomAttributes.Add(compilation.GetAbstract(type).Attribute);
    }
}
