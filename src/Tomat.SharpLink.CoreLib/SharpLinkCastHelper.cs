namespace Tomat.SharpLink;

public static class SharpLinkCastHelper {
    public static TTo CastVirtual<TFrom, TTo>(TFrom from) where TTo : new() {
        var to = new TTo();

        // TODO: implement this when we move onto the runtime

        return to;
    }
}
