using System;
using System.Collections.Generic;
using System.Linq;

namespace Tomat.SharpLink;

public class HlOpcode {
    public HlOpcodeKind Kind { get; set; }

    public int[] Parameters { get; set; }

    public HlOpcode(HlOpcodeKind kind) {
        Kind = kind;
        Parameters = Array.Empty<int>();
    }

    public HlOpcode(HlOpcodeKind kind, int p1) {
        Kind = kind;
        Parameters = new[] { p1 };
    }

    public HlOpcode(HlOpcodeKind kind, int p1, int p2) {
        Kind = kind;
        Parameters = new[] { p1, p2 };
    }

    public HlOpcode(HlOpcodeKind kind, int p1, int p2, int p3) {
        Kind = kind;
        Parameters = new[] { p1, p2, p3 };
    }

    public HlOpcode(HlOpcodeKind kind, int p1, int p2, int p3, int p4) {
        Kind = kind;
        Parameters = new[] { p1, p2, p3, p4 };
    }

    public HlOpcode(HlOpcodeKind kind, int p1, int p2, int p3, IEnumerable<int> extraParams) {
        Kind = kind;
        Parameters = new[] { p1, p2, p3 }.Concat(extraParams).ToArray();
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
