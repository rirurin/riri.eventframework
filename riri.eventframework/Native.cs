using System.Runtime.InteropServices;

namespace riri.eventframework
{
    public static class Native
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern nint GetModuleHandleA(string lpModuleName);
    }
}
