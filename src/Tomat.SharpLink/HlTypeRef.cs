namespace Tomat.SharpLink;

public sealed class HlTypeRef {
    private readonly int typeId;
    private readonly HlCode code;

    public HlType? Value => code.GetHlType(typeId);

    public HlTypeRef(int typeId, HlCode code) {
        this.typeId = typeId;
        this.code = code;
    }
}
