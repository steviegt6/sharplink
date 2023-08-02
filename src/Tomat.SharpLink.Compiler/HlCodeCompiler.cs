using System;
using System.Reflection;
using System.Reflection.Emit;
using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

public class HlCodeCompiler {
    private readonly HlCode code;

    public HlCodeCompiler(HlCode code) {
        this.code = code;
    }

    public AssemblyDefinition Compile(string name) {
        // HashLink preserves no version information.
        var asmNameDef = new AssemblyNameDefinition(name, new Version(1, 0, 0, 0));
        var asmDef = AssemblyDefinition.CreateAssembly(asmNameDef, name, ModuleKind.Dll);

        return asmDef;
    }
}
