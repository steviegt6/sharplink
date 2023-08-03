namespace Tomat.SharpLink;

public struct HaxeUI8 {
    public byte Value { get; }

    public HaxeUI8(byte value) {
        Value = value;
    }

    public static implicit operator HaxeUI8(byte value) => new(value);

    public static implicit operator byte(HaxeUI8 value) => value.Value;
}
