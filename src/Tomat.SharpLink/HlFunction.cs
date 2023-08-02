namespace Tomat.SharpLink;

public class HlOpcode {
    public HlOpcodeKind Kind { get; set; }

    public int P1 { get; set; }

    public int P2 { get; set; }

    public int P3 { get; set; }

    public HlOpcode(HlOpcodeKind kind, int p1 = 0, int p2 = 0, int p3 = 0) {
        Kind = kind;
        P1 = p1;
        P2 = p2;
        P3 = p3;
    }
}

public class HlOpcodeWithP4 : HlOpcode {
    public int P4 { get; set; }

    public HlOpcodeWithP4(HlOpcodeKind kind, int p1, int p2, int p3, int p4) : base(kind, p1, p2, p3) {
        P4 = p4;
    }
}

public class HlOpcodeWithExtraParams : HlOpcode {
    public int[] ExtraParams { get; set; }

    public HlOpcodeWithExtraParams(HlOpcodeKind kind, int p1, int p2, int p3, int[] extraParams) : base(kind, p1, p2, p3) {
        ExtraParams = extraParams;
    }
}

public class HlFunction {
    public int FIndex { get; set; }

    public HlTypeRef Type { get; set; }

    public HlTypeRef[] Regs { get; set; }

    public HlOpcode[] Opcodes { get; set; }

    public int[]? Debug { get; set; }

    public HlFunction(int fIndex, HlTypeRef type, HlTypeRef[] regs, HlOpcode[] opcodes) {
        FIndex = fIndex;
        Type = type;
        Regs = regs;
        Opcodes = opcodes;
    }
}
