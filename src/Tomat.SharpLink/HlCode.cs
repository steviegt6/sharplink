using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Tomat.SharpLink.IO;

namespace Tomat.SharpLink;

public class HlCode {
    /*private readonly struct HeaderInfo1 {
        public readonly int Flags;
        public readonly int NInts;
        public readonly int NFloats;
        public readonly int NStrings;

        public HeaderInfo1(int flags, int nInts, int nFloats, int nStrings) {
            Flags = flags;
            NInts = nInts;
            NFloats = nFloats;
            NStrings = nStrings;
        }
    }

    private readonly struct HeaderInfo2 {
        public readonly int NTypes;
        public readonly int NGlobals;
        public readonly int NNatives;
        public readonly int NFunctions;
        public readonly int NConstants;
        public readonly int Entrypoint;

        public HeaderInfo2(int nTypes, int nGlobals, int nNatives, int nFunctions, int nConstants, int entrypoint) {
            NTypes = nTypes;
            NGlobals = nGlobals;
            NNatives = nNatives;
            NFunctions = nFunctions;
            NConstants = nConstants;
            Entrypoint = entrypoint;
        }
    }*/

    private const int min_version = 2;
    private const int max_version = 5;

    public int Version { get; set; }

    public int Entrypoint { get; set; }

    public bool HasDebug { get; set; }

    public List<int> Ints { get; set; } = null!;

    public List<double> Floats { get; set; } = null!;

    public List<string> Strings { get; set; } = null!;

    public List<int> StringLengths { get; set; } = null!;

    public List<byte> Bytes { get; set; } = null!;

    public List<int> BytePositions { get; set; } = null!;

    public List<string> DebugFiles { get; set; } = null!;

    public List<int> DebugFileLengths { get; set; } = null!;

    // UStrings would be here ig

    /*public List<HlType> Types { get; set; } = null!;

    public List<HlType> Globals { get; set; } = null!;*/

    public HlType[] Types { get; set; } = null!;

    public HlType[] Globals { get; set; } = null!;

    public List<HlNative> Natives { get; set; } = null!;

    public List<HlFunction> Functions { get; set; } = null!;

    public string GetUString(int index) {
        if (index < 0 || index >= Strings.Count) {
            Console.WriteLine("Invalid string index.");
            index = 0;
        }

        // Official HL VM stores UStrings elsewhere, but we always decode to
        // UTF8 instead of ANSI or whatever... so it's probably fine?
        return Strings[index];
    }

    public HlType GetHlType(int index) {
        if (index < 0 || index >= /*Types.Count*/ Types.Length) {
            Console.WriteLine("Invalid type index.");
            index = 0;
        }

        return Types[index];
    }

    public HlTypeRef GetHlTypeAsTypeRef(int index) {
        if (index < 0 || index >= /*Types.Count*/ Types.Length) {
            Console.WriteLine("Invalid type index.");
            index = 0;
        }

        return new HlTypeRef(index, this);
    }

    // TODO: Seek to beginning?
    public static HlCode FromStream(Stream stream) {
        var bytes = new byte[stream.Length];
        var read = stream.Read(bytes, 0, bytes.Length);
        if (read != bytes.Length)
            throw new IOException("Could not read entire stream");

        return FromBytes(bytes);
    }

    public static HlCode FromBytes(byte[] data) {
        var code = new HlCode();
        var reader = new HlBinaryReader(data, code);

        var hlb = "HLB"u8;
        // if (reader.ReadBytes(3) != hlb)
        //     throw new InvalidDataException("Not a valid HLB file");
        if (reader.ReadByte() != hlb[0] || reader.ReadByte() != hlb[1] || reader.ReadByte() != hlb[2])
            throw new InvalidDataException("Not a valid HLB file");

        var version = reader.ReadByte();
        if (version is < min_version or > max_version)
            throw new InvalidDataException($"Unsupported HLB version {version}");

        /*var header1 = reader.Read<HeaderInfo1>();
        var nBytes = version >= 5 ? reader.ReadInt32() : 0;
        var header2 = reader.Read<HeaderInfo2>();
        var hasDebug = header1.Flags & 1;

        var ints = new int[header1.NInts];
        for (var i = 0; i < header1.NInts; i++)
            ints[i] = reader.ReadInt32();

        var floats = new double[header1.NFloats];
        for (var i = 0; i < header1.NFloats; i++)
            floats[i] = reader.ReadDouble();

        var strings = ReadStrings(reader, header1.NStrings, out var stringLengths);*/

        var flags = reader.ReadUIndex();
        var nInts = reader.ReadUIndex();
        var nFloats = reader.ReadUIndex();
        var nStrings = reader.ReadUIndex();
        var nBytes = version >= 5 ? reader.ReadUIndex() : 0;
        var nTypes = reader.ReadUIndex();
        var nGlobals = reader.ReadUIndex();
        var nNatives = reader.ReadUIndex();
        var nFunctions = reader.ReadUIndex();
        var nConstants = reader.ReadUIndex();
        var entrypoint = reader.ReadUIndex();
        var hasDebug = (flags & 1) != 0;

        var ints = new int[nInts];
        for (var i = 0; i < nInts; i++)
            ints[i] = reader.ReadInt32();
        code.Ints = ints.ToList();

        var floats = new double[nFloats];
        for (var i = 0; i < nFloats; i++)
            floats[i] = reader.ReadDouble();
        code.Floats = floats.ToList();

        var strings = ReadStrings(reader, nStrings, out var stringLengths);
        code.Strings = strings.ToList();
        code.StringLengths = stringLengths.ToList();

        byte[] bytes;
        int[] bytePositions;

        if (version >= 5) {
            var size = reader.ReadInt32();
            bytes = reader.ReadBytes(size).ToArray();
            bytePositions = new int[nBytes];
            for (var i = 0; i < nBytes; i++)
                bytePositions[i] = reader.ReadUIndex();
        }
        else {
            bytes = Array.Empty<byte>();
            bytePositions = Array.Empty<int>();
        }

        code.Bytes = bytes.ToList();
        code.BytePositions = bytePositions.ToList();

        var nDebugFiles = hasDebug ? reader.ReadUIndex() : 0;
        var debugFiles = ReadStrings(reader, nDebugFiles, out var debugFileLengths);
        code.DebugFiles = debugFiles.ToList();
        code.DebugFileLengths = debugFileLengths.ToList();

        /*var types = new HlType[nTypes];
        for (var i = 0; i < nTypes; i++)
            types[i] = reader.ReadHlType();
        code.Types = types.ToList();

        var globals = new HlType[nGlobals];
        for (var i = 0; i < nGlobals; i++)
            globals[i] = code.GetHlType(reader.ReadIndex());
        code.Globals = globals.ToList();*/

        /*code.Types = new List<HlType>(nTypes);
        for (var i = 0; i < nTypes; i++)
            code.Types.Add(reader.ReadHlType());

        code.Globals = new List<HlType>(nGlobals);
        for (var i = 0; i < nGlobals; i++)
            code.Globals.Add(code.GetHlType(reader.ReadUIndex()));*/

        code.Types = new HlType[nTypes];
        for (var i = 0; i < nTypes; i++)
            code.Types[i] = reader.ReadHlType();

        code.Globals = new HlType[nGlobals];
        for (var i = 0; i < nGlobals; i++)
            code.Globals[i] = code.GetHlType(reader.ReadIndex());

        var natives = new HlNative[nNatives];

        for (var i = 0; i < nNatives; i++) {
            natives[i] = new HlNative {
                // In the hashlink source, these use hl_read_string instead, but
                // we don't make a distinction between strings and ustrings, so
                // this shouldn't be a concern for us.
                Lib = code.GetUString(reader.ReadIndex()),
                Name = code.GetUString(reader.ReadIndex()),
                T = code.GetHlTypeAsTypeRef(reader.ReadIndex()),
                FIndex = reader.ReadUIndex(),
            };
        }

        code.Natives = natives.ToList();

        var functions = new HlFunction[nFunctions];

        for (var i = 0; i < nFunctions; i++) {
            functions[i] = reader.ReadHlFunction();

            if (hasDebug) {
                functions[i].Debug = reader.ReadDebugInfos(functions[i].Opcodes!.Length);

                if (version >= 3) {
                    // Skip assigns.
                    var nAssigns = reader.ReadUIndex();

                    for (var j = 0; j < nAssigns; j++) {
                        _ = reader.ReadUIndex();
                        _ = reader.ReadUIndex();
                    }
                }
            }
        }

        code.Functions = functions.ToList();

        var constants = new HlConstant[nConstants];

        for (var i = 0; i < nConstants; i++) {
            constants[i] = new HlConstant {
                Global = reader.ReadUIndex(),
                Fields = new int[reader.ReadUIndex()],
            };

            for (var j = 0; j < constants[i].Fields!.Length; j++)
                constants[i].Fields![j] = reader.ReadUIndex();
        }

        return code;
    }

    private static string[] ReadStrings(HlBinaryReader reader, int count, out int[] lengths) {
        var size = reader.ReadInt32();
        var bytes = reader.ReadBytes(size).ToArray();
        var strings = new string[count];
        lengths = new int[count];

        var offset = 0;

        for (var i = 0; i < count; i++) {
            var stringSize = reader.ReadUIndex();
            strings[i] = Encoding.UTF8.GetString(bytes, offset, stringSize);
            lengths[i] = stringSize;
            offset += stringSize + 1;
        }

        return strings;
    }
}
