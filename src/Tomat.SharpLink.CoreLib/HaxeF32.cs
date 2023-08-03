namespace Tomat.SharpLink;

public struct HaxeF32 {
    public float Value { get; }

    public HaxeF32(float value) {
        Value = value;
    }

    public static implicit operator HaxeF32(float value) => new(value);

    public static implicit operator float(HaxeF32 value) => value.Value;
}
