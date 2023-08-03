namespace Tomat.SharpLink;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class HashLinkGlobalAttribute : Attribute {
    public int GlobalId { get; }

    public HashLinkGlobalAttribute(int globalId) {
        GlobalId = globalId;
    }
}
