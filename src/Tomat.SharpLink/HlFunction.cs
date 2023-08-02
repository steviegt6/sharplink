namespace Tomat.SharpLink;

public class HlOpcode {
    public HlOpcodeKind Kind { get; set; }

    public int P1 { get; set; }

    public int P2 { get; set; }

    public int P3 { get; set; }
}

public class HlOpcodeWithP4 : HlOpcode {
    public int P4 { get; set; }
}

public class HlOpcodeWithExtraParams : HlOpcode {
    public int[]? ExtraParams { get; set; }
}

public class HlFunction {
    public int FIndex { get; set; }

    public int Ref { get; set; }

    public HlTypeRef? Type { get; set; }

    public HlTypeRef[]? Regs { get; set; }

    public HlOpcode[]? Opcodes { get; set; }

    public int[]? Debug { get; set; }

    public HlTypeObj? Obj { get; set; }
}

public class HlFunctionWithName : HlFunction {
    public string? Name { get; set; }
}

public class HlFunctionWithRef : HlFunction {
    public HlFunction? RefFunction { get; set; }
}
