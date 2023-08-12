using System;
using System.Collections.Generic;
using System.Linq;

namespace Tomat.SharpLink;

/// <summary>
///     A HashLink function.
/// </summary>
public class HlFunction {
    /// <summary>
    ///     The index of the function in the HashLink binary.
    /// </summary>
    public int FunctionIndex { get; set; }

    /// <summary>
    ///     A reference to the type definition which describes this function.
    /// </summary>
    public HlTypeRef Type { get; set; }

    /// <summary>
    ///     The local variables (registers) of the function.
    /// </summary>
    public HlTypeRef[] LocalVariables { get; set; }

    /// <summary>
    ///     The opcodes of the function.
    /// </summary>
    public HlOpcode[] Opcodes { get; set; }

    /// <summary>
    ///     Debug information for the function. The file and line information
    ///     for each instruction.
    /// </summary>
    public int[]? Debug { get; set; }

    public HlFunction(int functionIndex, HlTypeRef type, HlTypeRef[] localVariables, HlOpcode[] opcodes) {
        FunctionIndex = functionIndex;
        Type = type;
        LocalVariables = localVariables;
        Opcodes = opcodes;
    }
}

/// <summary>
///     An opcode which makes up a HashLink function.
/// </summary>
public class HlOpcode {
    /// <summary>
    ///     The kind of opcode.
    /// </summary>
    public HlOpcodeKind Kind { get; set; }

    /// <summary>
    ///     The parameters of the opcode.
    /// </summary>
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
