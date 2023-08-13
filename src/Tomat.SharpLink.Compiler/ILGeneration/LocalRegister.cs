using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler.ILGeneration;

/// <summary>
///     A local register, which represents either a method argument or local
///     variable.
/// </summary>
public struct LocalRegister {
    public int RegisterIndex { get; }

    public bool IsParameter { get; }

    public int AdjustedIndex { get; }

    public LocalRegister(int registerIndex, IMethodSignature method, List<VariableDefinition> locals) {
        RegisterIndex = registerIndex;
        IsParameter = registerIndex < method.Parameters.Count;
        AdjustedIndex = IsParameter ? registerIndex : registerIndex - method.Parameters.Count;
    }
}
