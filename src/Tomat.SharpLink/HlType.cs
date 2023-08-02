namespace Tomat.SharpLink;

public class HlType {
    public HlTypeKind Kind { get; set; }
}

public class HlTypeFun {
    public struct HlTypeFunClosureType {
        public HlTypeKind Kind { get; set; }

        // TODO: whart
        public nint P { get; set; }
    }

    public struct HlTypeFunClosure {
        public HlTypeRef[]? Arguments { get; set; }

        public HlTypeRef? ReturnType { get; set; }

        public HlTypeRef? Parent { get; set; }
    }

    public HlTypeRef[]? Arguments { get; set; }

    public HlTypeRef? ReturnType { get; set; }

    public HlTypeFunClosureType? ClosureType { get; set; }

    public HlTypeFunClosure Closure { get; set; }
}

public class HlObjField {
    public string? Name { get; set; }

    public HlTypeRef? Type { get; set; }

    public int HashedName { get; set; }
}

public class HlObjProto {
    public string? Name { get; set; }

    public int FIndex { get; set; }

    public int PIndex { get; set; }

    public int HashedName { get; set; }
}

public class HlTypeObj {
    public string? Name { get; set; }

    public HlTypeRef? Super { get; set; }

    public HlObjField[]? Fields { get; set; }

    public HlObjProto[]? Protos { get; set; }

    public int[]? Bindings { get; set; }

    public nint GlobalValue { get; set; }

    // TODO: public HlModuleContext? Module { get; set; }

    // TODO: public HlRuntimeObject? Runtime { get; set; }
    public object? Runtime { get; set; }
}

public class HlModuleContext {
    // TODO
}

public class HlEnumConstruct {
    public string? Name { get; set; }

    public HlTypeRef[] Params { get; set; }

    public int Size { get; set; }

    public bool HasPtr { get; set; }

    public int[] Offsets { get; set; }
}

public class HlTypeEnum {
    public string? Name { get; set; }

    public HlEnumConstruct[]? Constructs { get; set; }

    public nint GlobalValue { get; set; }
}

public class HlTypeVirtual {
    public HlObjField[]? Fields { get; set; }

    // TODO: rt
    // int dataSize
    // int[] indexes
    // HlFieldLookup lookup
}

public sealed class HlTypeWithAbsName : HlType {
    public string? AbsName { get; set; }
}

public sealed class HlTypeWithFun : HlType {
    public HlTypeFun? Fun { get; set; }
}

public sealed class HlTypeWithObj : HlType {
    public HlTypeObj? Obj { get; set; }
}

public sealed class HlTypeWithEnum : HlType {
    public HlTypeEnum? Enum { get; set; }
}

public sealed class HlTypeWithVirtual : HlType {
    public HlTypeVirtual? Virtual { get; set; }
}

public sealed class HlTypeWithType : HlType {
    public HlTypeRef? Type { get; set; }
}
