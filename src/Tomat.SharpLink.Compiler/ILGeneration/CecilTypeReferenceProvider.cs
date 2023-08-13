using Mono.Cecil;

namespace Tomat.SharpLink.Compiler.ILGeneration;

public class CecilTypeReferenceProvider : ITypeReferenceProvider {
    private readonly TypeReference type;

    public CecilTypeReferenceProvider(TypeReference type) {
        this.type = type;
    }

    TypeReference ITypeReferenceProvider.GetReference(EmissionContext context) {
        return type;
    }
}
