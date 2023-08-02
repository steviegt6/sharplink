namespace Tomat.SharpLink;

public sealed class HlConstant {
    public int Global { get; set; }

    public int[] Fields { get; set; }

    public HlConstant(int global, int[] fields) {
        Global = global;
        Fields = fields;
    }
}
