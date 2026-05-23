using System;
using System.Threading;
using System.Windows.Forms;
using PixelWorldsInjector.Forms;
using PixelWorldsInjector.Services;

namespace PixelWorldsInjector;

internal static class Program
{
    private static Mutex? _singleInstanceMutex;

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Single-instance guard for the launcher itself (not the game).
        _singleInstanceMutex = new Mutex(initiallyOwned: true, "PixelWorldsInjector.SingleInstance", out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "Pixel Worlds Injector is already running.\nCheck your system tray or task bar.",
                "Already running",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        try
        {
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                Logger.Error("Unhandled exception", args.ExceptionObject as Exception);
            };
            Application.ThreadException += (_, args) =>
            {
                Logger.Error("UI thread exception", args.Exception);
                MessageBox.Show(args.Exception.Message, "Unexpected error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            Application.Run(new MainForm());
        }
        finally
        {
            _singleInstanceMutex.ReleaseMutex();
            _singleInstanceMutex.Dispose();
        }
    }
}
