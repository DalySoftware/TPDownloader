using Microsoft.Extensions.DependencyInjection;
using TPDownloader.UI;

namespace TPDownloader;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<MainForm>();
            services.AddSingleton<TrayApplication>();

            var provider = services.BuildServiceProvider();
            var trayApp = provider.GetRequiredService<TrayApplication>();
            trayApp.Run();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Startup error: {ex}");
        }
    }
}
