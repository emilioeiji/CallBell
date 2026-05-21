using CallBell.Config;
using CallBell.Data.Db;
using CallBell.Data.Repositories;
using CallBell.Data.Services;

namespace CallBell.OperatorApp;

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
        var triggerFileService = new TriggerFileService(settings);
        var requestService = new CallBellRequestService(requestRepository, triggerFileService);

        Application.Run(new OperatorMainForm(settings, masterDataRepository, requestService));
    }
}
