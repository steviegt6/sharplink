namespace Tomat.SharpLink;

[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
public sealed class HashLinkAbstractAttribute : Attribute {
    public string AbstractName { get; }

    public HashLinkAbstractAttribute(string abstractName) {
        AbstractName = abstractName;
    }
}
