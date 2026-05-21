using System.Text.Json;
using CallBell.Config;
using CallBell.Core.Entities;
using CallBell.Core.Models;
using CallBell.Data.Repositories;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace CallBell.MonitorApp;

public sealed class MonitorMainForm : Form
{
    private const string ProfileFileName = "monitor-selection.json";

    private readonly CallBellSettings _settings;
    private readonly MasterDataRepository _masterDataRepository;
    private readonly RequestRepository _requestRepository;
    private readonly WebView2 _webView = new();
    private readonly System.Windows.Forms.Timer _refreshTimer = new();

    private FileSystemWatcher? _triggerWatcher;
    private List<Sector> _sectorOptions = [];
    private int _selectedSectorId;
    private bool _boardReady;
    private bool _hasLoadedBoard;
    private int _lastLatestOpenRequestId;

    public MonitorMainForm(
        CallBellSettings settings,
        MasterDataRepository masterDataRepository,
        RequestRepository requestRepository)
    {
        _settings = settings;
        _masterDataRepository = masterDataRepository;
        _requestRepository = requestRepository;

        Text = "CallBell - Monitor";
        WindowState = FormWindowState.Maximized;
        BackColor = Color.Black;

        BuildLayout();

        _refreshTimer.Interval = settings.MonitorRefreshSeconds * 1000;
        _refreshTimer.Tick += async (_, _) => await RefreshBoardAsync();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await InitializeWebViewAsync();
        await LoadSectorsAsync();
        ConfigureTriggerWatcher();
        _refreshTimer.Start();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _refreshTimer.Stop();
        _triggerWatcher?.Dispose();
        base.OnFormClosed(e);
    }

    private void BuildLayout()
    {
        _webView.Dock = DockStyle.Fill;
        Controls.Add(_webView);
    }

    private async Task InitializeWebViewAsync()
    {
        await _webView.EnsureCoreWebView2Async();
        var core = _webView.CoreWebView2;
        core.Settings.IsWebMessageEnabled = true;
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.AreDevToolsEnabled = false;
        core.WebMessageReceived += CoreOnWebMessageReceived;
        core.SetVirtualHostNameToFolderMapping(
            "app",
            Path.Combine(Application.StartupPath, "ui", "monitor-board"),
            CoreWebView2HostResourceAccessKind.Allow);
        core.NavigationCompleted += async (_, _) =>
        {
            _boardReady = true;
            await RefreshBoardAsync();
        };
        core.Navigate("https://app/index.html");
    }

    private async Task LoadSectorsAsync()
    {
        _sectorOptions =
        [
            new Sector { Id = 0, Code = "ALL", NamePt = "Todos os setores", NameJp = "Todos os setores", IsActive = true }
        ];
        _sectorOptions.AddRange((await _masterDataRepository.GetSectorsAsync()).Where(x => x.IsActive));

        var profile = AppSettingsProvider.LoadProfile<MonitorSelectionProfile>(_settings, ProfileFileName);
        if (profile is not null && _sectorOptions.Any(x => x.Id == profile.SectorId))
        {
            _selectedSectorId = profile.SectorId;
        }
    }

    private void SaveSelection()
    {
        AppSettingsProvider.SaveProfile(_settings, ProfileFileName, new MonitorSelectionProfile
        {
            SectorId = _selectedSectorId
        });
    }

    private async Task RefreshBoardAsync()
    {
        if (!_boardReady || _webView.CoreWebView2 is null)
        {
            return;
        }

        try
        {
            var snapshot = await _requestRepository.GetMonitorSnapshotAsync(
                _settings.MonitorBoardTitle,
                _selectedSectorId > 0 ? _selectedSectorId : null);

            var playAlert = _hasLoadedBoard
                && snapshot.LatestOpenRequestId > _lastLatestOpenRequestId
                && snapshot.TotalOpenRequests > 0;

            _lastLatestOpenRequestId = Math.Max(_lastLatestOpenRequestId, snapshot.LatestOpenRequestId);
            _hasLoadedBoard = true;

            var payload = JsonSerializer.Serialize(new
            {
                type = "board",
                playAlert,
                data = new
                {
                    title = snapshot.BoardTitle,
                    sectorLabel = snapshot.SectorLabel,
                    sectors = _sectorOptions.Select(x => new { id = x.Id, name = x.NamePt }),
                    selectedSectorId = _selectedSectorId,
                    generatedAt = snapshot.GeneratedAtUtc.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    totalOpenRequests = snapshot.TotalOpenRequests,
                    requests = snapshot.Requests.Select(card => new
                    {
                        requestId = card.RequestId,
                        ticketNumber = card.TicketNumber,
                        sectorNamePt = card.SectorNamePt,
                        sectorNameJp = card.SectorNameJp,
                        workAreaNamePt = card.WorkAreaNamePt,
                        workAreaNameJp = card.WorkAreaNameJp,
                        reasonNamePt = card.ReasonNamePt,
                        reasonNameJp = card.ReasonNameJp,
                        machineCode = card.MachineCode ?? string.Empty,
                        machineNamePt = card.MachineNamePt ?? string.Empty,
                        machineNameJp = card.MachineNameJp ?? string.Empty,
                        requestedByFjCode = card.RequestedByFjCode,
                        requestedAt = card.RequestedAtUtc.LocalDateTime.ToString("HH:mm")
                    })
                }
            });

            _webView.CoreWebView2.PostWebMessageAsJson(payload);
            await _webView.CoreWebView2.ExecuteScriptAsync($"window.renderMonitorBoard?.({payload});");
            Text = $"CallBell - Monitor | Atualizado {DateTime.Now:HH:mm:ss} | Abertos: {snapshot.TotalOpenRequests}";
        }
        catch (Exception ex)
        {
            Text = $"CallBell - Monitor | {ex.Message}";
        }
    }

    private async void CoreOnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var document = JsonDocument.Parse(e.WebMessageAsJson);
            var root = document.RootElement;
            if (!root.TryGetProperty("type", out var typeElement) || typeElement.GetString() != "sectorSelection")
            {
                return;
            }

            if (!root.TryGetProperty("sectorId", out var sectorIdElement))
            {
                return;
            }

            var sectorId = sectorIdElement.GetInt32();
            if (_selectedSectorId == sectorId)
            {
                return;
            }

            _selectedSectorId = sectorId;
            SaveSelection();
            _hasLoadedBoard = false;
            await RefreshBoardAsync();
        }
        catch
        {
        }
    }

    private void ConfigureTriggerWatcher()
    {
        Directory.CreateDirectory(_settings.TriggerDirectory);
        _triggerWatcher = new FileSystemWatcher(_settings.TriggerDirectory)
        {
            Filter = "*.trigger",
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };

        _triggerWatcher.Created += TriggerWatcherOnChanged;
        _triggerWatcher.Changed += TriggerWatcherOnChanged;
        _triggerWatcher.Renamed += TriggerWatcherOnChanged;
    }

    private void TriggerWatcherOnChanged(object sender, FileSystemEventArgs e)
    {
        if (!IsHandleCreated)
        {
            return;
        }

        BeginInvoke(new Action(async () =>
        {
            await Task.Delay(150);
            await RefreshBoardAsync();
        }));
    }
}
