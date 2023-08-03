namespace Tomat.SharpLink;

[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
public sealed class HashLinkAbstractAttribute : Attribute {
    public string AbstractName { get; }

    public int[] Globals { get; }

    public HashLinkAbstractAttribute(string abstractName, params int[] globals) {
        AbstractName = abstractName;
        Globals = globals;
    }
}
