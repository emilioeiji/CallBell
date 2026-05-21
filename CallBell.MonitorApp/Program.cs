using CallBell.Config;
using CallBell.Data.Db;
using CallBell.Data.Repositories;

namespace CallBell.MonitorApp;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var settings = AppSettingsProvider.Load();
        var factory = new SqliteConnectionFactory(settings);
        DbInitializer.EnsureCreatedAsync(factory).GetAwaiter().GetResult();

        var masterDataRepository = new MasterDataRepository(factory);
        var requestRepository = new RequestRepository(factory);

        Application.Run(new MonitorMainForm(settings, masterDataRepository, requestRepository));
    }
}
