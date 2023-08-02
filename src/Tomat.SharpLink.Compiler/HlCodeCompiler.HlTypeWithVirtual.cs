using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

partial class HlCodeCompiler {
    private void ResolveHlTypeWithVirtual(HlTypeWithVirtual type, AssemblyDefinition asmDef) { }

    private void DefineHlTypeWithVirtual(HlTypeWithVirtual type, AssemblyDefinition asmDef) { }

    private void CompileHlTypeWithVirtual(HlTypeWithVirtual type, AssemblyDefinition asmDef) {
        var virtType = CreateAnonymousType(
            "",
            TypeAttributes.Class
          | TypeAttributes.NotPublic
          | TypeAttributes.AutoClass
          | TypeAttributes.AnsiClass
          | TypeAttributes.Sealed
          | TypeAttributes.BeforeFieldInit,
            asmDef.MainModule.TypeSystem.Object,
            asmDef
        );
        asmDef.MainModule.Types.Add(virtType);

        foreach (var field in type.Virtual.Fields) {
            var fieldType = TypeReferenceFromHlTypeRef(field.Type, asmDef);
            var name = field.Name;
            var fieldDef = new FieldDefinition(name, FieldAttributes.Public, fieldType);
            virtType.Fields.Add(fieldDef);
        }
    }
}
