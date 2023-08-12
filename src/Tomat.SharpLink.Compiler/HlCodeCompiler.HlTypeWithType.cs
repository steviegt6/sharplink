using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
    // HlTypeWithType objects end up handling things like Ref types, which don't
    // require definitions, and can be handled ourselves during type resolution.
    // This includes Ref -> HaxeRef<T> and Null -> HaxeNull<T>.

    private void ResolveHlTypeWithType(HlTypeWithType type, AssemblyDefinition asmDef) { }

    private void DefineHlTypeWithType(HlTypeWithType type, AssemblyDefinition asmDef) { }

    private void CompileHlTypeWithType(HlTypeWithType type, AssemblyDefinition asmDef) { }
}
