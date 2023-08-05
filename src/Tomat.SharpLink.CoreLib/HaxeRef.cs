using System.Runtime.CompilerServices;

namespace Tomat.SharpLink;

public class HaxeRef<T> : IStrongBox {
    public T? Value;

    public HaxeRef(T value) {
        Value = value;
    }

    object? IStrongBox.Value {
        get => Value;
        set => Value = (T?)value;
    }
}
