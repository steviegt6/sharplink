namespace Tomat.SharpLink;

public class HlNative {
    public string Lib { get; set; }

    public string Name { get; set; }

    public HlTypeRef T { get; set; }

    public int FIndex { get; set; }

    public HlNative(string lib, string name, HlTypeRef t, int fIndex) {
        Lib = lib;
        Name = name;
        T = t;
        FIndex = fIndex;
    }
}
