namespace Tomat.SharpLink;

public class HaxeBytes {
    public byte[] Value { get; }

    public HaxeBytes(byte[] value) {
        Value = value;
    }

    public static implicit operator HaxeBytes(byte[] value) => new(value);

    public static implicit operator byte[](HaxeBytes value) => value.Value;
}
