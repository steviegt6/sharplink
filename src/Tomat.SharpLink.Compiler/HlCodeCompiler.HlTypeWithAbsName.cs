using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
    public class CompiledAbstract {
        public List<int> Globals { get; set; } = new();

        public CustomAttribute Attribute { get; }

        public CompiledAbstract(CustomAttribute attribute) {
            Attribute = attribute;
        }
    }

    private Dictionary<HlTypeWithAbsName, CompiledAbstract> compiledAbstracts = new();
    private Dictionary<HlTypeWithAbsName, List<int>> absNameGlobals = new();

    private CompiledAbstract GetCompiledAbstract(HlTypeWithAbsName type) {
        return compiledAbstracts[type];
    }

    private void InitializeAbsNameGlobals() {
        foreach (var absName in hash.Code.Globals.Select(x => x.Value).Where(x => x is HlTypeWithAbsName).Cast<HlTypeWithAbsName>())
            absNameGlobals.Add(absName, new List<int>());

        for (var i = 0; i < hash.Code.Globals.Count; i++) {
            var global = hash.Code.Globals[i];

            if (global.Value is HlTypeWithAbsName absName)
                absNameGlobals[absName].Add(i);
        }
    }

    private void ResolveHlTypeWithAbsName(HlTypeWithAbsName type, AssemblyDefinition asmDef) {
        var attr = new CustomAttribute(asmDef.MainModule.ImportReference(typeof(HashLinkAbstractAttribute).GetConstructor(new[] { typeof(string), typeof(int[]) })));
        compiledAbstracts.Add(type, new CompiledAbstract(attr));
    }

    private void DefineHlTypeWithAbsName(HlTypeWithAbsName type, AssemblyDefinition asmDef) {
        var compiled = compiledAbstracts[type];
        var attr = compiled.Attribute;

        // (string name, int[] globals)
        attr.ConstructorArguments.Add(new CustomAttributeArgument(asmDef.MainModule.TypeSystem.String, type.AbstractName));

        if (absNameGlobals.TryGetValue(type, out var globals))
            compiled.Globals = globals;

        attr.ConstructorArguments.Add(
            new CustomAttributeArgument(
                asmDef.MainModule.TypeSystem.Int32.MakeArrayType(),
                compiled.Globals.Select(x => new CustomAttributeArgument(asmDef.MainModule.TypeSystem.Int32, x)).ToArray()
            )
        );
    }

    private void CompileHlTypeWithAbsName(HlTypeWithAbsName type, AssemblyDefinition asmDef) {
        asmDef.CustomAttributes.Add(compiledAbstracts[type].Attribute);
    }
}
