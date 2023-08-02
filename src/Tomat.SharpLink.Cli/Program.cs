using System.IO;

namespace Tomat.SharpLink.Cli;

internal static class Program {
    internal static void Main(string[] args) {
        // TODO: parse args
        /*var debugPort = -1;
        var debugWait = false;
        var hotReload = false;
        var profileCount = 0;
        
        foreach (var arg in args) {
        
        }*/

        var code = HlCode.FromStream(File.OpenRead(args[0]));
    }
}
