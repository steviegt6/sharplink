using System.Diagnostics.CodeAnalysis;

namespace Tomat.SharpLink;

// Glorified StrongBox<T>, lol.
public class HaxeRef<T> {
    [MaybeNull]
    public T Value;

    public HaxeRef(T value) {
        Value = value;
    }
}
