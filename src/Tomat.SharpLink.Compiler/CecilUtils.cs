using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

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

    public static MethodReference MakeHostInstanceGeneric(this MethodReference method, params TypeReference[] args) {
        var reference = new MethodReference(
            method.Name,
            method.ReturnType,
            method.DeclaringType.MakeGenericInstanceType(args)
        ) {
            HasThis = method.HasThis,
            ExplicitThis = method.ExplicitThis,
            CallingConvention = method.CallingConvention,
        };
        
        foreach (var param in method.Parameters)
            reference.Parameters.Add(new ParameterDefinition(param.ParameterType));
        
        foreach (var genericParam in method.GenericParameters)
            reference.GenericParameters.Add(new GenericParameter(genericParam.Name, reference));
        
        return reference;
    }
}
