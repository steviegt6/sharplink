namespace Tomat.SharpLink;

public static class SharpLinkExceptionHelper {
    public static Exception CreateNullCheckException() {
        return new NullReferenceException();
    }
}
