using System.Linq;
using Mono.Cecil;

namespace Tomat.SharpLink.Compiler.ILGeneration;

public readonly struct GlobalReference : ITypeReferenceProvider {
    public int GlobalIndex { get; }

    public GlobalReference(int globalIndex) {
        GlobalIndex = globalIndex;
    }

    public FieldDefinition GetField(EmissionContext context) {
        var index = GlobalIndex;
        return context.Assembly.MainModule.GetType("<Module>").Fields.First(x => x.Name == $"global{index}");
    }

    TypeReference ITypeReferenceProvider.GetReference(EmissionContext context) {
        return GetField(context).FieldType;
    }
}
