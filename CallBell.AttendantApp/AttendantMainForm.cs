using CallBell.Config;
using CallBell.Core.Entities;
using CallBell.Core.Models;
using CallBell.Data.Repositories;
using CallBell.Data.Services;

namespace CallBell.AttendantApp;

public sealed class AttendantMainForm : Form
{
    private const string ProfileFileName = "attendant-selection.json";

    private readonly CallBellSettings _settings;
    private readonly MasterDataRepository _masterDataRepository;
    private readonly RequestRepository _requestRepository;
    private readonly CallBellRequestService _requestService;
    private readonly System.Windows.Forms.Timer _refreshTimer = new();

    private readonly ComboBox _cboSector = new();
    private readonly Button _btnRefresh = new();
    private readonly Label _lblSummary = new();
    private readonly DataGridView _grid = new();

    private IReadOnlyList<Sector> _sectors = Array.Empty<Sector>();

    public AttendantMainForm(
        CallBellSettings settings,
        MasterDataRepository masterDataRepository,
        RequestRepository requestRepository,
        CallBellRequestService requestService)
    {
        _settings = settings;
        _masterDataRepository = masterDataRepository;
        _requestRepository = requestRepository;
        _requestService = requestService;

        Text = "CallBell - Fechamento";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1260, 800);
        BackColor = Color.FromArgb(243, 246, 250);
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

        BuildLayout();

        _refreshTimer.Interval = settings.MonitorRefreshSeconds * 1000;
        _refreshTimer.Tick += async (_, _) => await RefreshRequestsAsync();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await LoadSectorsAsync();
        _refreshTimer.Start();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _refreshTimer.Stop();
        base.OnFormClosed(e);
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(20),
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 102));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(BuildHero(), 0, 0);
        root.Controls.Add(BuildFiltersCard(), 0, 1);
        root.Controls.Add(BuildGridCard(), 0, 2);
    }

    private Control BuildHero()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(15, 23, 42),
            Padding = new Padding(26, 18, 26, 16)
        };

        panel.Controls.Add(new Label
        {
            Text = "Acompanhe os chamados em aberto e finalize o atendimento com o FJ do responsavel.",
            Dock = DockStyle.Top,
            Height = 28,
            ForeColor = Color.FromArgb(191, 219, 254),
            Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point)
        });
        panel.Controls.Add(new Label
        {
            Text = "CallBell Atendimento",
            Dock = DockStyle.Top,
            Height = 38,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 19F, FontStyle.Bold, GraphicsUnit.Point)
        });

        return panel;
    }

    private Control BuildFiltersCard()
    {
        var shell = BuildCard();
        shell.Padding = new Padding(18);

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            BackColor = shell.BackColor
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54));
        shell.Controls.Add(panel);

        _cboSector.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboSector.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
        _cboSector.SelectedValueChanged += async (_, _) =>
        {
            SaveSelection();
            await RefreshRequestsAsync();
        };

        _btnRefresh.Text = "Atualizar agora";
        _btnRefresh.Dock = DockStyle.Top;
        _btnRefresh.Height = 42;
        _btnRefresh.FlatStyle = FlatStyle.Flat;
        _btnRefresh.FlatAppearance.BorderSize = 0;
        _btnRefresh.BackColor = Color.FromArgb(37, 99, 235);
        _btnRefresh.ForeColor = Color.White;
        _btnRefresh.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point);
        _btnRefresh.Click += async (_, _) => await RefreshRequestsAsync();

        _lblSummary.Dock = DockStyle.Fill;
        _lblSummary.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point);
        _lblSummary.ForeColor = Color.FromArgb(15, 23, 42);
        _lblSummary.TextAlign = ContentAlignment.MiddleLeft;
        _lblSummary.Text = "Carregando solicitacoes em aberto...";

        panel.Controls.Add(BuildField("Setor monitorado", "Filtra a fila de atendimento", _cboSector), 0, 0);
        panel.Controls.Add(BuildField("Acao", "Atualizacao manual", _btnRefresh), 1, 0);
        panel.Controls.Add(BuildSummaryPanel(), 2, 0);

        return shell;
    }

    private Control BuildSummaryPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 2, 8, 2)
        };

        panel.Controls.Add(_lblSummary);
        panel.Controls.Add(new Label
        {
            Text = "Resumo da fila",
            Dock = DockStyle.Top,
            Height = 24,
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(30, 41, 59)
        });

        return panel;
    }

    private Control BuildGridCard()
    {
        var shell = BuildCard();
        shell.Padding = new Padding(14);

        _grid.Dock = DockStyle.Fill;
        _grid.BackgroundColor = Color.White;
        _grid.BorderStyle = BorderStyle.None;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.ReadOnly = true;
        _grid.AutoGenerateColumns = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.RowHeadersVisible = false;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
        _grid.ColumnHeadersHeight = 42;
        _grid.RowTemplate.Height = 38;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
        _grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
        _grid.GridColor = Color.FromArgb(226, 232, 240);
        _grid.CellContentClick += GridCellContentClick;
        _grid.CellFormatting += GridCellFormatting;

        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ticket", DataPropertyName = nameof(AssistanceRequest.TicketNumber), Width = 160 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Setor", DataPropertyName = nameof(AssistanceRequest.SectorNamePt), Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Area", DataPropertyName = nameof(AssistanceRequest.WorkAreaNamePt), Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Equipamento", DataPropertyName = nameof(AssistanceRequest.EquipmentNamePt), Width = 140 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Motivo", DataPropertyName = nameof(AssistanceRequest.ReasonNamePt), Width = 190 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Maquina", DataPropertyName = nameof(AssistanceRequest.MachineCode), Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Operador", DataPropertyName = nameof(AssistanceRequest.RequestedByFjCode), Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Abertura", DataPropertyName = nameof(AssistanceRequest.RequestedAtUtc), Width = 150 });
        _grid.Columns.Add(new DataGridViewButtonColumn
        {
            HeaderText = "Fechar",
            Text = "Fechar",
            Width = 100,
            UseColumnTextForButtonValue = true
        });

        shell.Controls.Add(_grid);
        return shell;
    }

    private async Task LoadSectorsAsync()
    {
        _sectors = await _masterDataRepository.GetSectorsAsync();
        var options = new List<Sector>
        {
            new() { Id = 0, NamePt = "Todos os setores", NameJp = "Todos os setores", Code = "ALL", IsActive = true }
        };
        options.AddRange(_sectors.Where(x => x.IsActive));

        _cboSector.DisplayMember = nameof(Sector.NamePt);
        _cboSector.ValueMember = nameof(Sector.Id);
        _cboSector.DataSource = options;

        var selection = AppSettingsProvider.LoadProfile<MonitorSelectionProfile>(_settings, ProfileFileName);
        if (selection is not null)
        {
            _cboSector.SelectedValue = selection.SectorId;
        }

        await RefreshRequestsAsync();
    }

    private void SaveSelection()
    {
        if (_cboSector.SelectedValue is not int sectorId)
        {
            return;
        }

        AppSettingsProvider.SaveProfile(_settings, ProfileFileName, new MonitorSelectionProfile
        {
            SectorId = sectorId
        });
    }

    private async Task RefreshRequestsAsync()
    {
        try
        {
            UseWaitCursor = true;
            var sectorId = SelectedSectorId();
            var rows = await _requestRepository.GetOpenRequestsAsync(sectorId == 0 ? null : sectorId);
            var orderedRows = rows
                .OrderBy(x => x.RequestedAtUtc)
                .ThenBy(x => x.Id)
                .ToList();

            _grid.DataSource = orderedRows;
            _lblSummary.Text = $"{rows.Count} solicitacao(oes) em aberto {(sectorId == 0 ? "em todos os setores" : "no setor selecionado")}.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "CallBell", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private async void GridCellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex != _grid.Columns.Count - 1)
        {
            return;
        }

        if (_grid.Rows[e.RowIndex].DataBoundItem is not AssistanceRequest request)
        {
            return;
        }

        using var dialog = new FjClosePromptForm(request);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var closed = await _requestService.CloseAsync(new CloseAssistanceRequestCommand
            {
                RequestId = request.Id,
                ClosedByFjCode = dialog.EnteredFjCode
            });

            if (!closed)
            {
                MessageBox.Show(this, "A solicitacao nao estava mais em aberto.", "CallBell", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            await RefreshRequestsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "CallBell", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void GridCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (_grid.Columns[e.ColumnIndex].DataPropertyName == nameof(AssistanceRequest.RequestedAtUtc)
            && e.Value is DateTimeOffset requestedAt)
        {
            e.Value = requestedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm");
            e.FormattingApplied = true;
        }
    }

    private int SelectedSectorId()
    {
        return _cboSector.SelectedValue is int sectorId ? sectorId : 0;
    }

    private static Control BuildField(string title, string description, Control control)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 0, 8, 0)
        };

        panel.Controls.Add(control);
        panel.Controls.Add(new Label
        {
            Text = description,
            Dock = DockStyle.Top,
            Height = 22,
            ForeColor = Color.FromArgb(100, 116, 139)
        });
        panel.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 24,
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(30, 41, 59)
        });

        control.Dock = DockStyle.Top;
        control.Height = 42;
        return panel;
    }

    private static Panel BuildCard()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(0)
        };
    }
}
