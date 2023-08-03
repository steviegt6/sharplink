namespace Tomat.SharpLink;

public struct HaxeF64 {
    public double Value { get; }

    public HaxeF64(double value) {
        Value = value;
    }

    public static implicit operator HaxeF64(double value) => new(value);

    public static implicit operator double(HaxeF64 value) => value.Value;
}
