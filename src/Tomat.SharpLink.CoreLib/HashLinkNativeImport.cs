namespace Tomat.SharpLink;

// TODO: We could allow multiple... is there a use case?
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class HashLinkNativeImport : Attribute {
    public string Lib { get; }

    public string Name { get; }

    public HashLinkNativeImport(string lib, string name) {
        Lib = lib;
        Name = name;
    }
}
