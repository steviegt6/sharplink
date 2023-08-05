namespace Tomat.SharpLink;

public static class SharpLinkExceptionHelper {
    public static Exception CreateNetExceptionFromHaxeException(object haxeException) {
        // TODO: implement this when we move onto the runtime
        return new Exception();
    }
    
    public static Exception CreateNullCheckException() {
        return new NullReferenceException();
    }
}
