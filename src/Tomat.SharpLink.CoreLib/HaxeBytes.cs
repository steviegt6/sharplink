namespace Tomat.SharpLink;

public class HaxeBytes {
    public byte[] Value { get; }

    public HaxeBytes(byte[] value) {
        Value = value;
    }

    public HaxeBytes(string value) {
        throw new NotImplementedException("impl when runtime is written");
    }

    public static implicit operator HaxeBytes(byte[] value) => new(value);

    public static implicit operator byte[](HaxeBytes value) => value.Value;
}
