using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tomat.SharpLink.IO;

namespace Tomat.SharpLink;

/// <summary>
///     A HashLink bytecode binary file.
/// </summary>
public sealed class HlCode {
    private const int min_version = 2;
    private const int max_version = 5;

    /// <summary>
    ///     The binary file format version.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    ///     The index of the entrypoint function.
    /// </summary>
    public int Entrypoint { get; set; }

    /// <summary>
    ///     Whether this binary file contains debug information.
    /// </summary>
    public bool HasDebug { get; set; }

    /// <summary>
    ///     The integer constants in this binary file.
    /// </summary>
    public List<int> Ints { get; set; } = new();

    /// <summary>
    ///     The floating-point constants in this binary file.
    /// </summary>
    public List<double> Floats { get; set; } = new();

    /// <summary>
    ///     The string constants in this binary file.
    /// </summary>
    public List<string> Strings { get; set; } = new();

    /// <summary>
    ///     The lengths of the string constants in this binary file.
    /// </summary>
    public List<int> StringLengths { get; set; } = new();

    /// <summary>
    ///     The byte constants in this binary file.
    /// </summary>
    public List<byte> Bytes { get; set; } = new();

    /// <summary>
    ///     The positions of the byte constants in this binary file.
    /// </summary>
    public List<int> BytePositions { get; set; } = new();

    /// <summary>
    ///     The names of debug files.
    /// </summary>
    public List<string> DebugFiles { get; set; } = new();

    /// <summary>
    ///     The lengths of the names of debug files.
    /// </summary>
    public List<int> DebugFileLengths { get; set; } = new();

    /// <summary>
    ///     The types in this binary file.
    /// </summary>
    public List<HlType> Types { get; set; } = new();

    /// <summary>
    ///     The global variables in this binary file.
    /// </summary>
    public List<HlTypeRef> Globals { get; set; } = new();

    /// <summary>
    ///     The native functions in this binary file.
    /// </summary>
    public List<HlNative> Natives { get; set; } = new();

    /// <summary>
    ///     The functions in this binary file.
    /// </summary>
    public List<HlFunction> Functions { get; set; } = new();

    /// <summary>
    ///     The constants in this binary file.
    /// </summary>
    public List<HlConstant> Constants { get; set; } = new();

    /// <summary>
    ///     Retrieves a wide string constant from this binary file.
    /// </summary>
    /// <param name="index">The index of the string constant.</param>
    /// <returns>The wide string constant.</returns>
    public string GetUString(int index) {
        if (index < 0 || index >= Strings.Count) {
            Console.WriteLine("Invalid string index.");
            index = 0;
        }

        // Official HL VM stores UStrings elsewhere, but we always decode to
        // UTF8 instead of ANSI or whatever... so it's probably fine?
        return Strings[index];
    }

    /// <summary>
    ///     Retrieves a type from this binary file.
    /// </summary>
    /// <param name="index">The index of the type.</param>
    /// <returns>The type.</returns>
    public HlType GetHlType(int index) {
        if (index < 0 || index >= Math.Max(Types.Count, Types.Capacity)) {
            Console.WriteLine("Invalid type index.");
            index = 0;
        }

        return Types[index];
    }

    /// <summary>
    ///     Retrieves a reference to a type from this binary file.
    /// </summary>
    /// <param name="index">The index of the type.</param>
    /// <returns>A reference to the type.</returns>
    public HlTypeRef GetHlTypeRef(int index) {
        if (index < 0 || index >= Math.Max(Types.Count, Types.Capacity)) {
            Console.WriteLine("Invalid type index.");
            index = 0;
        }

        return new HlTypeRef(index, this);
    }

    /// <summary>
    ///     Creates a <see cref="HlCodeHash"/> instance, which contains
    ///     additional information computed from this binary file.
    /// </summary>
    /// <returns>An instance of <see cref="HlCodeHash"/>.</returns>
    public HlCodeHash CreateCodeHash() {
        return new HlCodeHash(this);
    }

    /// <summary>
    ///     Creates an instance of <see cref="HlCode"/> from the
    ///     <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>An instance of <see cref="HlCode"/>.</returns>
    public static HlCode FromStream(Stream stream) {
        var bytes = new byte[stream.Length];
        var read = stream.Read(bytes, 0, bytes.Length);
        if (read != bytes.Length)
            throw new IOException("Could not read entire stream");

        return FromBytes(bytes);
    }

    /// <summary>
    ///     Creates an instance of <see cref="HlCode"/> from the
    ///     <paramref name="data"/>.
    /// </summary>
    /// <param name="data">The data to read from.</param>
    /// <returns>An instance of <see cref="HlCode"/>.</returns>
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
                    typeRef: code.GetHlTypeRef(reader.ReadIndex()),
                    nativeIndex: reader.ReadUIndex()
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
                    globalIndex: reader.ReadUIndex(),
                    fields: new int[reader.ReadUIndex()]
                )
            );

            for (var j = 0; j < constant.Fields.Length; j++)
                constant.Fields[j] = reader.ReadUIndex();
        }

        return code;
    }
}

/// <summary>
///     A wrapper around <see cref="HlCode"/> which contains information
///     computed from (but not contained in) the binary file.
/// </summary>
public class HlCodeHash {
    /// <summary>
    ///     The wrapped <see cref="HlCode"/>.
    /// </summary>
    public HlCode Code { get; }

    /// <summary>
    ///     The unified function indexes, mapping both functions and natives to
    ///     a single set.
    /// </summary>
    public int[] FunctionIndexes { get; }

    public HlCodeHash(HlCode code) {
        Code = code;

        FunctionIndexes = new int[Code.Functions.Count + Code.Natives.Count];

        for (var i = 0; i < Code.Functions.Count; i++) {
            var func = Code.Functions[i];
            FunctionIndexes[func.FunctionIndex] = i;
        }

        for (var i = 0; i < Code.Natives.Count; i++) {
            var native = Code.Natives[i];
            FunctionIndexes[native.NativeIndex] = i + Code.Functions.Count;
        }

        // TODO: type hashes, global signs, etc.? idk if we need them
    }
}
