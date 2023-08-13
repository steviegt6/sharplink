using System;
using Mono.Cecil;

namespace Tomat.SharpLink.Compiler.ILGeneration;

public class RuntimeTypeReferenceProvider : ITypeReferenceProvider {
    private readonly Type type;

    public RuntimeTypeReferenceProvider(Type type) {
        this.type = type;
    }

    TypeReference ITypeReferenceProvider.GetReference(EmissionContext context) {
        return context.Assembly.MainModule.ImportReference(type);
    }
}
