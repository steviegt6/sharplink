using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Tomat.SharpLink.IO;

public sealed class HlBinaryReader {
    private int position;
    private readonly byte[] bytes;
    private readonly HlCode code;
    private readonly Dictionary<string, int> hashCache = new();

    public HlBinaryReader(byte[] bytes, HlCode code) {
        this.bytes = bytes;
        this.code = code;
    }

    internal T Read<T>() where T : unmanaged {
        var size = Unsafe.SizeOf<T>();
        if (position + size > bytes.Length)
            throw new IndexOutOfRangeException();

        var value = Unsafe.As<byte, T>(ref bytes[position]);
        position += size;
        return value;
    }

    public byte ReadByte() {
        if (position >= bytes.Length)
            throw new IndexOutOfRangeException();

        return bytes[position++];
    }

    public Span<byte> ReadBytes(int count) {
        if (position + count > bytes.Length)
            throw new IndexOutOfRangeException();

        var value = bytes.AsSpan(position, count);
        position += count;
        return value;
    }

    /*public Memory<byte> ReadBytes(int count) {
        if (position + count > bytes.Length)
            throw new IndexOutOfRangeException();

        var value = bytes.AsMemory(position, count);
        position += count;
        return value;
    }*/

    public int ReadIndex() {
        var b = ReadByte();
        if ((b & 0x80) == 0)
            return b & 0x7f;

        if ((b & 0x40) == 0) {
            var v = ReadByte() | ((b & 31) << 8);
            return (b & 0x20) == 0 ? v : -v;
        }
        else {
            var c = ReadByte();
            var d = ReadByte();
            var e = ReadByte();
            var v = ((b & 31) << 24) | (c << 16) | (d << 8) | e;
            return (b & 0x20) == 0 ? v : -v;
        }
    }

    public int ReadUIndex() {
        var i = ReadIndex();

        if (i >= 0)
            return i;

        Console.WriteLine("Warning: negative index");
        return 0;
    }

    public int ReadInt32() {
        return Read<int>();
    }

    public double ReadDouble() {
        return Read<double>();
    }

    /*public string ReadString() {
        
    }*/

    public int GenerateHash(string name, bool cache) {
        var h = 0;
        foreach (var c in name)
            h = 233 * h + c;
        h %= 0x1FFFFF7B;

        if (!cache)
            return h;

        // Check for potential conflicts (haxe#5572).
        /*if (hashCache.TryGetValue(name, out var cached)) {
            // TODO: implement
        }*/

        return hashCache[name] = h;
    }

    public string ReadUString() {
        var i = ReadIndex();

        if (i < 0 || i >= code.Strings.Count) {
            Console.WriteLine("Warning: invalid string index");
            i = 0;
        }

        return code.GetUString(i);
    }

    public HlType ReadHlType() {
        var kind = (HlTypeKind)ReadByte();

        switch (kind) {
            case HlTypeKind.HFUN:
            case HlTypeKind.HMETHOD: {
                var nArgs = ReadByte();
                var fun = new HlTypeFun {
                    Arguments = new HlTypeRef[nArgs],
                };
                for (var i = 0; i < nArgs; i++)
                    fun.Arguments[i] = code.GetHlTypeAsTypeRef(ReadIndex());
                fun.ReturnType = code.GetHlTypeAsTypeRef(ReadIndex());

                return new HlTypeWithFun {
                    Kind = kind,
                    Fun = fun,
                };
            }

            case HlTypeKind.HOBJ:
            case HlTypeKind.HSTRUCT: {
                var name = ReadUString();
                var super = ReadIndex();
                var global = ReadUIndex();
                var nFields = ReadUIndex();
                var nProtos = ReadUIndex();
                var nBindings = ReadUIndex();
                var obj = new HlTypeObj {
                    Name = name,
                    Super = super < 0 ? null : code.GetHlTypeAsTypeRef(super),
                    GlobalValue = global,
                    Fields = new HlObjField[nFields],
                    Protos = new HlObjProto[nProtos],
                    Bindings = new int[nBindings * 2],
                    Runtime = null,
                };

                for (var i = 0; i < nFields; i++) {
                    var fieldName = ReadUString();
                    obj.Fields[i] = new HlObjField {
                        Name = fieldName,
                        HashedName = GenerateHash(fieldName, cache: true),
                        Type = code.GetHlTypeAsTypeRef(ReadIndex()),
                    };
                }

                for (var i = 0; i < nProtos; i++) {
                    var protoName = ReadUString();
                    obj.Protos[i] = new HlObjProto {
                        Name = protoName,
                        HashedName = GenerateHash(protoName, cache: true),
                        FIndex = ReadUIndex(),
                        PIndex = ReadIndex(),
                    };
                }

                for (var i = 0; i < nBindings; i++) {
                    obj.Bindings[i << 1] = ReadUIndex();
                    obj.Bindings[(i << 1) | 1] = ReadUIndex();
                }

                return new HlTypeWithObj {
                    Obj = obj,
                };
            }

            case HlTypeKind.HREF: {
                return new HlTypeWithType {
                    Kind = kind,
                    Type = code.GetHlTypeAsTypeRef(ReadIndex()),
                };
            }

            case HlTypeKind.HVIRTUAL: {
                var nFields = ReadUIndex();
                var virt = new HlTypeVirtual {
                    Fields = new HlObjField[nFields],
                };

                for (var i = 0; i < nFields; i++) {
                    var name = ReadUString();
                    virt.Fields[i] = new HlObjField {
                        Name = name,
                        HashedName = GenerateHash(name, cache: true),
                        Type = code.GetHlTypeAsTypeRef(ReadIndex()),
                    };
                }

                return new HlTypeWithVirtual {
                    Virtual = virt,
                };
            }

            case HlTypeKind.HABSTRACT: {
                return new HlTypeWithAbsName {
                    AbsName = ReadUString(),
                };
            }

            case HlTypeKind.HENUM: {
                var @enum = new HlTypeEnum {
                    Name = ReadUString(),
                    GlobalValue = ReadUIndex(),
                    Constructs = new HlEnumConstruct[ReadUIndex()],
                };

                for (var i = 0; i < @enum.Constructs.Length; i++) {
                    var name = ReadUString();
                    var nParams = ReadUIndex();
                    var construct = @enum.Constructs[i] = new HlEnumConstruct {
                        Name = name,
                        Params = new HlTypeRef[nParams],
                        Offsets = new int[nParams],
                    };

                    for (var j = 0; j < nParams; j++)
                        construct.Params[j] = code.GetHlTypeAsTypeRef(ReadIndex());
                }

                return new HlTypeWithEnum {
                    Enum = @enum,
                };
            }

            case HlTypeKind.HNULL:
            case HlTypeKind.HPACKED: {
                return new HlTypeWithType {
                    Kind = kind,
                    Type = code.GetHlTypeAsTypeRef(ReadIndex()),
                };
            }

            default: {
                if (kind >= HlTypeKind.HLAST)
                    throw new InvalidDataException($"Invalid type kind: {kind}");

                return new HlType {
                    Kind = kind,
                };
            }
        }
    }

    public HlFunction ReadHlFunction() {
        var type = code.GetHlTypeAsTypeRef(ReadIndex());
        var fIndex = ReadUIndex();
        var nRegs = ReadUIndex();
        var nOps = ReadUIndex();
        var regs = new HlTypeRef[nRegs];
        for (var i = 0; i < nRegs; i++)
            regs[i] = code.GetHlTypeAsTypeRef(ReadIndex());
        var opcodes = new HlOpcode[nOps];
        for (var i = 0; i < nOps; i++)
            opcodes[i] = ReadHlOpcode();
        return new HlFunction {
            Type = type,
            FIndex = fIndex,
            Regs = regs,
            Opcodes = opcodes,
        };
    }

    public HlOpcode ReadHlOpcode() {
        var kind = (HlOpcodeKind)ReadUIndex();

        if (kind >= HlOpcodeKind.Last) {
            Console.WriteLine($"Warning: invalid opcode kind: {kind}");
            return new HlOpcode {
                Kind = kind,
            };
        }

        switch (kind.GetArgumentCount()) {
            case 0: {
                return new HlOpcode {
                    Kind = kind,
                };
            }

            case 1: {
                return new HlOpcode {
                    Kind = kind,
                    P1 = ReadIndex(),
                };
            }

            case 2: {
                return new HlOpcode {
                    Kind = kind,
                    P1 = ReadIndex(),
                    P2 = ReadIndex(),
                };
            }

            case 3: {
                return new HlOpcode {
                    Kind = kind,
                    P1 = ReadIndex(),
                    P2 = ReadIndex(),
                    P3 = ReadIndex(),
                };
            }

            case 4: {
                return new HlOpcodeWithP4() {
                    Kind = kind,
                    P1 = ReadIndex(),
                    P2 = ReadIndex(),
                    P3 = ReadIndex(),
                    P4 = ReadIndex(),
                };
            }

            case -1: {
                switch (kind) {
                    case HlOpcodeKind.CallN:
                    case HlOpcodeKind.CallClosure:
                    case HlOpcodeKind.CallMethod:
                    case HlOpcodeKind.CallThis:
                    case HlOpcodeKind.OMakeEnum: {
                        var p1 = ReadIndex();
                        var p2 = ReadIndex();
                        var p3 = ReadByte();
                        var extraParams = new int[p3];
                        for (var i = 0; i < p3; i++)
                            extraParams[i] = ReadIndex();
                        return new HlOpcodeWithExtraParams {
                            Kind = kind,
                            P1 = p1,
                            P2 = p2,
                            P3 = p3,
                            ExtraParams = extraParams,
                        };
                    }

                    case HlOpcodeKind.Switch: {
                        var p1 = ReadUIndex();
                        var p2 = ReadUIndex();
                        var extraParams = new int[p2];
                        for (var i = 0; i < p2; i++)
                            extraParams[i] = ReadUIndex();
                        var p3 = ReadUIndex();
                        return new HlOpcodeWithExtraParams {
                            Kind = kind,
                            P1 = p1,
                            P2 = p2,
                            P3 = p3,
                            ExtraParams = extraParams,
                        };
                    }

                    default:
                        throw new InvalidDataException($"Invalid opcode kind: {kind}");
                }
            }

            default: {
                var size = kind.GetArgumentCount() - 3;
                var p1 = ReadIndex();
                var p2 = ReadIndex();
                var p3 = ReadIndex();
                var extraParams = new int[size];
                for (var i = 0; i < size; i++)
                    extraParams[i] = ReadIndex();
                return new HlOpcodeWithExtraParams {
                    Kind = kind,
                    P1 = p1,
                    P2 = p2,
                    P3 = p3,
                    ExtraParams = extraParams,
                };
            }
        }
    }

    public int[] ReadDebugInfos(int opcodeCount) {
        var currFile = -1;
        var currLine = 0;
        var debug = new int[opcodeCount * 2];
        var i = 0;

        while (i < opcodeCount) {
            var c = ReadByte();

            if ((c & 1) != 0) {
                c >>= 1;
                currFile = (c << 8) | ReadByte();
                if (currFile >= code.DebugFiles.Count)
                    throw new InvalidDataException($"Invalid debug file index: {currFile}");
            }
            else if ((c & 2) != 0) {
                var delta = c >> 6;
                var count = (c >> 2) & 15;
                if (i + count > opcodeCount)
                    throw new InvalidDataException($"Invalid debug line count: {count}");

                while (count-- > 0) {
                    debug[i << 1] = currFile;
                    debug[(i << 1) | 1] = currLine;
                    i++;
                }

                currLine += delta;
            }
            else if ((c & 4) != 0) {
                currLine += c >> 3;
                debug[i << 1] = currFile;
                debug[(i << 1) | 1] = currLine;
                i++;
            }
            else {
                var b2 = ReadByte();
                var b3 = ReadByte();
                currLine = (c >> 3) | (b2 << 5) | (b3 << 13);
                debug[i << 1] = currFile;
                debug[(i << 1) | 1] = currLine;
                i++;
            }
        }

        return debug;
    }
}
