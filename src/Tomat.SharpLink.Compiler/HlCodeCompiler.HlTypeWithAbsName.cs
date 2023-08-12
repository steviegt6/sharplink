using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
    private Dictionary<HlTypeWithAbsName, List<int>> absNameGlobals = new();
    private Dictionary<HlTypeWithAbsName, CustomAttribute> absNameDefs = new();

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
        absNameDefs.Add(type, attr);
    }

    private void DefineHlTypeWithAbsName(HlTypeWithAbsName type, AssemblyDefinition asmDef) {
        var attr = absNameDefs[type];

        // (string name, int[] globals)
        attr.ConstructorArguments.Add(new CustomAttributeArgument(asmDef.MainModule.TypeSystem.String, type.AbstractName));

        if (!absNameGlobals.TryGetValue(type, out var globals))
            globals = new List<int>();
        attr.ConstructorArguments.Add(
            new CustomAttributeArgument(
                asmDef.MainModule.TypeSystem.Int32.MakeArrayType(),
                globals.Select(x => new CustomAttributeArgument(asmDef.MainModule.TypeSystem.Int32, x)).ToArray()
            )
        );
    }

    private void CompileHlTypeWithAbsName(HlTypeWithAbsName type, AssemblyDefinition asmDef) {
        asmDef.CustomAttributes.Add(absNameDefs[type]);
    }
}
