using System.ComponentModel;
using System.Diagnostics;

namespace QuickTheme.Services;

internal static class ExplorerService
{
    public static async Task RestartAsync()
    {
        foreach (Process process in Process.GetProcessesByName("explorer"))
        {
            try
            {
                process.Kill();
            }
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
            {
                // Explorer may already be gone.
            }
            finally
            {
                process.Dispose();
            }
        }

        // Windows normally restarts the shell by itself; start it manually if it did not.
        for (int attempt = 0; attempt < 24; attempt++)
        {
            await Task.Delay(250).ConfigureAwait(false);

            Process[] current = Process.GetProcessesByName("explorer");
            bool running = current.Length > 0;
            foreach (Process process in current)
            {
                process.Dispose();
            }

            if (running)
            {
                return;
            }
        }

        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            // Nothing else we can do here.
        }
    }
}
