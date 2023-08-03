namespace Tomat.SharpLink; 

public struct HaxeI64 {
    public long Value { get; }
    
    public HaxeI64(long value) {
        Value = value;
    }
    
    public static implicit operator HaxeI64(long value) => new(value);
    
    public static implicit operator long(HaxeI64 value) => value.Value;
}
