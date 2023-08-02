namespace Tomat.SharpLink;

public class HlTypeFun {
    /*public struct HlTypeFunClosureType {
        public HlTypeKind Kind { get; set; }

        // TODO: whart
        public nint P { get; set; }
    }

    public struct HlTypeFunClosure {
        public HlTypeRef[]? Arguments { get; set; }

        public HlTypeRef? ReturnType { get; set; }

        public HlTypeRef? Parent { get; set; }
    }*/

    public HlTypeRef[] Arguments { get; set; }

    public HlTypeRef ReturnType { get; set; }

    /*public HlTypeFunClosureType? ClosureType { get; set; }

    public HlTypeFunClosure Closure { get; set; }*/

    public HlTypeFun(HlTypeRef[] arguments, HlTypeRef returnType) {
        Arguments = arguments;
        ReturnType = returnType;
    }
}

public class HlObjField {
    public string Name { get; set; }

    public HlTypeRef Type { get; set; }

    public HlObjField(string name, HlTypeRef type) {
        Name = name;
        Type = type;
    }
}

public class HlObjProto {
    public string Name { get; set; }

    public int FIndex { get; set; }

    public int PIndex { get; set; }

    public HlObjProto(string name, int fIndex, int pIndex) {
        Name = name;
        FIndex = fIndex;
        PIndex = pIndex;
    }
}

public class HlTypeObj {
    public string Name { get; set; }

    public HlTypeRef? Super { get; set; }

    public HlObjField[] Fields { get; set; }

    public HlObjProto[] Protos { get; set; }

    public int[] Bindings { get; set; }

    public nint GlobalValue { get; set; }

    public HlTypeObj(string name, HlTypeRef? super, HlObjField[] fields, HlObjProto[] protos, int[] bindings, nint globalValue) {
        Name = name;
        Super = super;
        Fields = fields;
        Protos = protos;
        Bindings = bindings;
        GlobalValue = globalValue;
    }
}

public class HlModuleContext {
    // TODO
}

public class HlEnumConstruct {
    public string Name { get; set; }

    public HlTypeRef[] Params { get; set; }

    public int[] Offsets { get; set; }

    public HlEnumConstruct(string name, HlTypeRef[] @params, int[] offsets) {
        Name = name;
        Params = @params;
        Offsets = offsets;
    }
}

public class HlTypeEnum {
    public string Name { get; set; }

    public HlEnumConstruct[] Constructs { get; set; }

    public nint GlobalValue { get; set; }

    public HlTypeEnum(string name, HlEnumConstruct[] constructs, nint globalValue) {
        Name = name;
        Constructs = constructs;
        GlobalValue = globalValue;
    }
}

public class HlTypeVirtual {
    public HlObjField[] Fields { get; set; }

    public HlTypeVirtual(HlObjField[] fields) {
        Fields = fields;
    }
}

public class HlType {
    public HlTypeKind Kind { get; set; }

    public HlType(HlTypeKind kind) {
        Kind = kind;
    }
}

public sealed class HlTypeWithAbsName : HlType {
    public string AbsName { get; set; }

    public HlTypeWithAbsName(HlTypeKind kind, string absName) : base(kind) {
        AbsName = absName;
    }
}

public sealed class HlTypeWithFun : HlType {
    public HlTypeFun Fun { get; set; }

    public HlTypeWithFun(HlTypeKind kind, HlTypeFun fun) : base(kind) {
        Fun = fun;
    }
}

public sealed class HlTypeWithObj : HlType {
    public HlTypeObj Obj { get; set; }

    public HlTypeWithObj(HlTypeKind kind, HlTypeObj obj) : base(kind) {
        Obj = obj;
    }
}

public sealed class HlTypeWithEnum : HlType {
    public HlTypeEnum Enum { get; set; }

    public HlTypeWithEnum(HlTypeKind kind, HlTypeEnum @enum) : base(kind) {
        Enum = @enum;
    }
}

public sealed class HlTypeWithVirtual : HlType {
    public HlTypeVirtual Virtual { get; set; }

    public HlTypeWithVirtual(HlTypeKind kind, HlTypeVirtual @virtual) : base(kind) {
        Virtual = @virtual;
    }
}

public sealed class HlTypeWithType : HlType {
    public HlTypeRef Type { get; set; }

    public HlTypeWithType(HlTypeKind kind, HlTypeRef type) : base(kind) {
        Type = type;
    }
}
