namespace Tomat.SharpLink;

public struct HaxeBool {
    public bool Value { get; }

    public HaxeBool(bool value) {
        Value = value;
    }

    public static implicit operator HaxeBool(bool value) => new(value);

    public static implicit operator bool(HaxeBool value) => value.Value;
}
