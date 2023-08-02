using System;
using System.Linq;
using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

public static class CecilUtils {
    public static MethodReference DefaultCtorFor(TypeReference type) {
        var resolved = type.Resolve();
        if (resolved is null)
            throw new ArgumentException($"Type {type.FullName} could not be resolved.");

        var ctor = resolved.Methods.SingleOrDefault(x => x.IsConstructor && x.Parameters.Count == 0 && !x.IsStatic);
        if (ctor is null)
            return DefaultCtorFor(resolved.BaseType);

        return new MethodReference(".ctor", type.Module.TypeSystem.Void, type) {
            HasThis = true,
        };
    }
}
