using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RptWatcher;

/// <summary>
/// Holder of the entry point of the program.
/// </summary>
public static class Program
{
    /// <summary>
    /// Entry point of the program.
    /// </summary>
    [STAThread]
    public static void Main()
    {
        NativeMethods.SetProcessDPIAware();
        Application.EnableVisualStyles();
        using Form map = new MainForm();
        Application.Run(map);
    }

    private static class NativeMethods
    {
        [DllImport("user32")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern bool SetProcessDPIAware();
    }
}