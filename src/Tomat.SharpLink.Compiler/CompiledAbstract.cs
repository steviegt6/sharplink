using System.Collections.Generic;
using Mono.Cecil;

namespace Tomat.SharpLink.Compiler;

public class CompiledAbstract {
    public List<int> Globals { get; set; } = new();

    public CustomAttribute Attribute { get; }

    public CompiledAbstract(CustomAttribute attribute) {
        Attribute = attribute;
    }
}
