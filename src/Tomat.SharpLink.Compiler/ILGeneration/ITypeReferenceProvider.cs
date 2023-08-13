using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler.ILGeneration;

public interface ITypeReferenceProvider {
    TypeReference GetReference(EmissionContext context);
}
