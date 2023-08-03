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

public static class HaxeRef {
    public static HaxeRef<T> MakeRef<T>(T value) {
        return new HaxeRef<T>(value);
    }
}
