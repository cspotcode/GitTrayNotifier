using System.Runtime.InteropServices;

namespace GitTrayNotifier;

static class NativeMethods
{
    [DllImport("kernel32.dll")]
    public static extern bool AllocConsole();
}
