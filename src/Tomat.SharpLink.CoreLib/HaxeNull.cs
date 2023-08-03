namespace Tomat.SharpLink;

public class HaxeNull<T> {
    public T? Value { get; set; }

    public bool IsNull => Value == null;

    public HaxeNull(T value) {
        Value = value;
    }
}
