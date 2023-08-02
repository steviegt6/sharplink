using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tomat.SharpLink.IO;

namespace Tomat.SharpLink;

public sealed class HlCode {
    private const int min_version = 2;
    private const int max_version = 5;

    public int Version { get; set; }

    public int Entrypoint { get; set; }

    public bool HasDebug { get; set; }

    public List<int> Ints { get; set; } = new();

    public List<double> Floats { get; set; } = new();

    public List<string> Strings { get; set; } = new();

    public List<int> StringLengths { get; set; } = new();

    public List<byte> Bytes { get; set; } = new();

    public List<int> BytePositions { get; set; } = new();

    public List<string> DebugFiles { get; set; } = new();

    public List<int> DebugFileLengths { get; set; } = new();

    // UStrings would be here ig

    public List<HlType> Types { get; set; } = new();

    public List<HlTypeRef> Globals { get; set; } = new();

    public List<HlNative> Natives { get; set; } = new();

    public List<HlFunction> Functions { get; set; } = new();

    public List<HlConstant> Constants { get; set; } = new();

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
        if (index < 0 || index >= Math.Max(Types.Count, Types.Capacity)) {
            Console.WriteLine("Invalid type index.");
            index = 0;
        }

        return Types[index];
    }

    public HlTypeRef GetHlTypeRef(int index) {
        if (index < 0 || index >= Math.Max(Types.Count, Types.Capacity)) {
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
        if (reader.ReadByte() != hlb[0] || reader.ReadByte() != hlb[1] || reader.ReadByte() != hlb[2])
            throw new InvalidDataException("Not a valid HLB file");

        code.Version = reader.ReadByte();
        if (code.Version is < min_version or > max_version)
            throw new InvalidDataException($"Unsupported HLB version {code.Version}");

        var flags = reader.ReadUIndex();
        var intCount = reader.ReadUIndex();
        var floatCount = reader.ReadUIndex();
        var stringCount = reader.ReadUIndex();
        var byteCount = code.Version >= 5 ? reader.ReadUIndex() : 0;
        var typeCount = reader.ReadUIndex();
        var globalCount = reader.ReadUIndex();
        var nativeCount = reader.ReadUIndex();
        var functionCount = reader.ReadUIndex();
        var constantCount = reader.ReadUIndex();
        code.Entrypoint = reader.ReadUIndex();
        code.HasDebug = (flags & 1) != 0;

        code.Ints = new List<int>(intCount);
        for (var i = 0; i < intCount; i++)
            code.Ints.Add(reader.ReadInt32());

        code.Floats = new List<double>(floatCount);
        for (var i = 0; i < floatCount; i++)
            code.Floats.Add(reader.ReadDouble());

        var strings = reader.ReadStrings(stringCount, out var stringLengths);
        code.Strings = strings;
        code.StringLengths = stringLengths;

        if (code.Version >= 5) {
            var size = reader.ReadInt32();
            code.Bytes = reader.ReadBytes(size).ToArray().ToList();
            code.BytePositions = new List<int>(byteCount);
            for (var i = 0; i < byteCount; i++)
                code.BytePositions.Add(reader.ReadInt32());
        }
        else {
            code.Bytes = new List<byte>();
            code.BytePositions = new List<int>();
        }

        var debugFileCount = code.HasDebug ? reader.ReadUIndex() : 0;
        var debugFiles = reader.ReadStrings(debugFileCount, out var debugFileLengths);
        code.DebugFiles = debugFiles;
        code.DebugFileLengths = debugFileLengths;

        code.Types = new List<HlType>(typeCount);
        for (var i = 0; i < typeCount; i++)
            code.Types.Add(reader.ReadHlType());

        code.Globals = new List<HlTypeRef>(globalCount);
        for (var i = 0; i < globalCount; i++)
            code.Globals.Add(code.GetHlTypeRef(reader.ReadIndex()));

        code.Natives = new List<HlNative>(nativeCount);

        for (var i = 0; i < nativeCount; i++) {
            code.Natives.Add(
                new HlNative(
                    // In the hashlink source, these use hl_read_string instead, but
                    // we don't make a distinction between strings and ustrings, so
                    // this shouldn't be a concern for us.
                    lib: code.GetUString(reader.ReadIndex()),
                    name: code.GetUString(reader.ReadIndex()),
                    t: code.GetHlTypeRef(reader.ReadIndex()),
                    fIndex: reader.ReadUIndex()
                )
            );
        }

        code.Functions = new List<HlFunction>(functionCount);

        for (var i = 0; i < functionCount; i++) {
            HlFunction func;
            code.Functions.Add(func = reader.ReadHlFunction());

            if (!code.HasDebug)
                continue;

            func.Debug = reader.ReadDebugInfos(func.Opcodes.Length);

            if (code.Version < 3)
                continue;

            // Skip assigns.
            var nAssigns = reader.ReadUIndex();

            for (var j = 0; j < nAssigns; j++) {
                _ = reader.ReadUIndex();
                _ = reader.ReadIndex();
            }
        }

        code.Constants = new List<HlConstant>(constantCount);

        for (var i = 0; i < constantCount; i++) {
            HlConstant constant;
            code.Constants.Add(
                constant = new HlConstant(
                    global: reader.ReadUIndex(),
                    fields: new int[reader.ReadUIndex()]
                )
            );

            for (var j = 0; j < constant.Fields!.Length; j++)
                constant.Fields![j] = reader.ReadUIndex();
        }

        return code;
    }
}
