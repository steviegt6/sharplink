using System.Reflection;

namespace Tomat.SharpLink;

// TODO: Actually handle... lol?
public class HaxeDyn {
    public object? Value { get; }

    public HaxeDyn(object? value) {
        Value = value;
    }

    public HaxeDyn GetField(string name) {
        return new HaxeDyn(Value?.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(Value));
    }

    public void SetField(string name, object? value) {
        Value?.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance)?.SetValue(Value, value);
    }
}
