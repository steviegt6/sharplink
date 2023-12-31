﻿using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tomat.SharpLink.Compiler.ILGeneration;

/// <summary>
///     A local register, which represents either a method argument or local
///     variable.
/// </summary>
public readonly struct LocalRegister : ITypeReferenceProvider {
    public int RegisterIndex { get; }

    public bool IsParameter { get; }

    public int AdjustedIndex { get; }

    public LocalRegister(int registerIndex, IMethodSignature method, List<VariableDefinition> locals) {
        RegisterIndex = registerIndex;
        IsParameter = registerIndex < method.Parameters.Count;
        AdjustedIndex = IsParameter ? registerIndex : registerIndex - method.Parameters.Count;
    }

    TypeReference ITypeReferenceProvider.GetReference(EmissionContext context) {
        return IsParameter ? context.Method.Parameters[AdjustedIndex].ParameterType : context.Locals[AdjustedIndex].VariableType;
    }
}
