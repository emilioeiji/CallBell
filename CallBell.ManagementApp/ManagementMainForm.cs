using System.ComponentModel;
using System.Text;
using CallBell.Core.Entities;
using CallBell.Core.Enums;
using CallBell.Core.Models;
using CallBell.Data.Repositories;
using Microsoft.Data.Sqlite;

namespace CallBell.ManagementApp;

public sealed class ManagementMainForm : Form
{
    private readonly MasterDataRepository _masterDataRepository;
    private readonly RequestRepository _requestRepository;
    private readonly ReportRepository _reportRepository;

    private readonly ComboBox _cboRequestsSector = new();
    private readonly ComboBox _cboRequestsStatus = new();
    private readonly DateTimePicker _dtpRequestsFrom = new();
    private readonly DateTimePicker _dtpRequestsTo = new();
    private readonly DataGridView _requestsGrid = new();

    private readonly DataGridView _sectorsGrid = new();
    private readonly DataGridView _areasGrid = new();
    private readonly DataGridView _equipmentsGrid = new();
    private readonly DataGridView _machinesGrid = new();
    private readonly DataGridView _reasonsGrid = new();
    private readonly DataGridView _equipmentReasonMappingsGrid = new();
    private readonly DataGridViewComboBoxColumn _areaSectorColumn = new();
    private readonly DataGridViewComboBoxColumn _equipmentSectorColumn = new();
    private readonly DataGridViewComboBoxColumn _machineSectorColumn = new();
    private readonly DataGridViewComboBoxColumn _machineAreaColumn = new();
    private readonly DataGridViewComboBoxColumn _mappingEquipmentColumn = new();
    private readonly DataGridViewComboBoxColumn _mappingReasonColumn = new();

    private readonly ComboBox _cboReportsSector = new();
    private readonly ComboBox _cboReportsShift = new();
    private readonly ComboBox _cboReportsReason = new();
    private readonly DateTimePicker _dtpReportsFrom = new();
    private readonly DateTimePicker _dtpReportsTo = new();
    private readonly Label _lblTotal = new();
    private readonly Label _lblOpen = new();
    private readonly Label _lblClosed = new();
    private readonly Label _lblAverage = new();
    private readonly FlowLayoutPanel _shiftChartPanel = new();
    private readonly FlowLayoutPanel _reasonChartPanel = new();
    private readonly DataGridView _reasonReportGrid = new();
    private readonly DataGridView _dailyReportGrid = new();
    private List<ReportRowView> _filteredReportRows = [];

    private BindingList<Sector> _sectorRows = new();
    private BindingList<WorkArea> _areaRows = new();
    private BindingList<Equipment> _equipmentRows = new();
    private BindingList<Machine> _machineRows = new();
    private BindingList<RequestReason> _reasonRows = new();
    private BindingList<EquipmentReasonMapping> _equipmentReasonMappingRows = new();
    private IReadOnlyList<Sector> _sectorOptions = Array.Empty<Sector>();
    private IReadOnlyList<LookupOption> _sectorLookupOptions = Array.Empty<LookupOption>();
    private IReadOnlyList<LookupOption> _areaLookupOptions = Array.Empty<LookupOption>();
    private IReadOnlyList<LookupOption> _equipmentLookupOptions = Array.Empty<LookupOption>();
    private IReadOnlyList<LookupOption> _reasonLookupOptions = Array.Empty<LookupOption>();

    public ManagementMainForm(
        MasterDataRepository masterDataRepository,
        RequestRepository requestRepository,
        ReportRepository reportRepository)
    {
        _masterDataRepository = masterDataRepository;
        _requestRepository = requestRepository;
        _reportRepository = reportRepository;

        Text = "CallBell - Gerenciamento";
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1320, 820);
        BackColor = Color.FromArgb(241, 245, 249);
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

        BuildLayout();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await LoadMasterDataAsync();
        await RefreshRequestsAsync();
        await RefreshReportsAsync();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            Padding = new Padding(20),
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 106));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(18, 8)
        };

        tabs.TabPages.Add(BuildRequestsTab());
        tabs.TabPages.Add(BuildParametersTab());
        tabs.TabPages.Add(BuildReportsTab());
        root.Controls.Add(BuildHero(), 0, 0);
        root.Controls.Add(tabs, 0, 1);
    }

    private Control BuildHero()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(15, 23, 42),
            Padding = new Padding(28, 18, 28, 16)
        };

        panel.Controls.Add(new Label
        {
            Text = "Cadastre parametros do CallBell, acompanhe o historico das solicitacoes e gere leituras gerenciais por setor.",
            Dock = DockStyle.Top,
            Height = 28,
            ForeColor = Color.FromArgb(191, 219, 254),
            Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point)
        });
        panel.Controls.Add(new Label
        {
            Text = "CallBell Management",
            Dock = DockStyle.Top,
            Height = 38,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 19F, FontStyle.Bold, GraphicsUnit.Point)
        });

        return panel;
    }

    private TabPage BuildRequestsTab()
    {
        var page = new TabPage("Chamados");
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(root);

        _dtpRequestsFrom.Format = DateTimePickerFormat.Short;
        _dtpRequestsTo.Format = DateTimePickerFormat.Short;
        _dtpRequestsFrom.Value = DateTime.Today.AddDays(-7);
        _dtpRequestsTo.Value = DateTime.Today;

        _cboRequestsStatus.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboRequestsStatus.DataSource = new List<StatusOption>
        {
            new() { Label = "Todos", Value = null },
            new() { Label = "Aberto", Value = RequestStatus.Open },
            new() { Label = "Fechado", Value = RequestStatus.Closed }
        };
        _cboRequestsStatus.DisplayMember = nameof(StatusOption.Label);
        _cboRequestsStatus.ValueMember = nameof(StatusOption.Value);

        var filterPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            BackColor = Color.White,
            Padding = new Padding(16)
        };
        for (var index = 0; index < 5; index++)
        {
            filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17));
        }
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));

        filterPanel.Controls.Add(BuildField("Setor", _cboRequestsSector), 0, 0);
        filterPanel.Controls.Add(BuildField("Status", _cboRequestsStatus), 1, 0);
        filterPanel.Controls.Add(BuildField("De", _dtpRequestsFrom), 2, 0);
        filterPanel.Controls.Add(BuildField("Ate", _dtpRequestsTo), 3, 0);
        filterPanel.Controls.Add(BuildField("Acao", BuildActionButton("Atualizar lista", async () => await RefreshRequestsAsync(), Color.FromArgb(37, 99, 235))), 4, 0);
        filterPanel.Controls.Add(BuildField("Leitura", BuildInfoLabel("Consulta historica com setor, equipamento, motivo e tempo de atendimento.")), 5, 0);

        ConfigureRequestsGrid();
        root.Controls.Add(filterPanel, 0, 0);
        root.Controls.Add(WrapGrid(_requestsGrid), 0, 1);
        return page;
    }

    private TabPage BuildParametersTab()
    {
        var page = new TabPage("Parametros");
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(16, 8)
        };

        ConfigureSectorGrid();
        ConfigureAreaGrid();
        ConfigureEquipmentGrid();
        ConfigureMachineGrid();
        ConfigureReasonGrid();
        ConfigureEquipmentReasonMappingGrid();

        tabs.TabPages.Add(BuildParameterPage("Setores", _sectorsGrid, async () => await SaveSectorsAsync(), "Cadastre os setores que existirao no sistema. Preencha codigo, nome e deixe ativo o que estiver em uso."));
        tabs.TabPages.Add(BuildParameterPage("Locais / Areas", _areasGrid, async () => await SaveAreasAsync(), "Cadastre os locais de atendimento escolhendo primeiro o setor correspondente."));
        tabs.TabPages.Add(BuildParameterPage("Equipamentos", _equipmentsGrid, async () => await SaveEquipmentsAsync(), "Cadastre os equipamentos por setor. Eles definem quais motivos o operador podera enxergar."));
        tabs.TabPages.Add(BuildParameterPage("Maquinas", _machinesGrid, async () => await SaveMachinesAsync(), "Cadastre as maquinas por setor e local para uso apenas nos motivos que exigem essa selecao."));
        tabs.TabPages.Add(BuildParameterPage("Motivos", _reasonsGrid, async () => await SaveReasonsAsync(), "Cadastre os motivos em portugues e japones e marque se a escolha de maquina sera obrigatoria."));
        tabs.TabPages.Add(BuildParameterPage("Vinculo Equipamento x Motivo", _equipmentReasonMappingsGrid, async () => await SaveEquipmentReasonMappingsAsync(), "Escolha o equipamento e depois o motivo permitido. Assim o operador vera somente as opcoes corretas."));

        page.Controls.Add(tabs);
        return page;
    }

    private TabPage BuildReportsTab()
    {
        var page = new TabPage("Relatorios");
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 126));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 280));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(root);

        _dtpReportsFrom.Format = DateTimePickerFormat.Short;
        _dtpReportsTo.Format = DateTimePickerFormat.Short;
        _dtpReportsFrom.Value = DateTime.Today.AddDays(-7);
        _dtpReportsTo.Value = DateTime.Today;
        _cboReportsShift.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboReportsReason.DropDownStyle = ComboBoxStyle.DropDownList;

        var filters = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            BackColor = Color.White,
            Padding = new Padding(16)
        };
        for (var index = 0; index < 5; index++)
        {
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16));
        }
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

        filters.Controls.Add(BuildField("Setor", _cboReportsSector), 0, 0);
        filters.Controls.Add(BuildField("Turno", _cboReportsShift), 1, 0);
        filters.Controls.Add(BuildField("Motivo", _cboReportsReason), 2, 0);
        filters.Controls.Add(BuildField("De", _dtpReportsFrom), 3, 0);
        filters.Controls.Add(BuildField("Ate", _dtpReportsTo), 4, 0);
        filters.Controls.Add(BuildField("Acoes", BuildReportActionsPanel()), 5, 0);

        var cards = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            BackColor = BackColor
        };
        for (var index = 0; index < 4; index++)
        {
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }

        cards.Controls.Add(BuildMetricCard("Total", _lblTotal, Color.FromArgb(37, 99, 235)), 0, 0);
        cards.Controls.Add(BuildMetricCard("Abertos", _lblOpen, Color.FromArgb(234, 88, 12)), 1, 0);
        cards.Controls.Add(BuildMetricCard("Fechados", _lblClosed, Color.FromArgb(22, 163, 74)), 2, 0);
        cards.Controls.Add(BuildMetricCard("Media (min)", _lblAverage, Color.FromArgb(14, 116, 144)), 3, 0);

        ConfigureBarChartPanel(_shiftChartPanel);
        ConfigureBarChartPanel(_reasonChartPanel);
        ConfigureSimpleGrid(_reasonReportGrid);
        ConfigureSimpleGrid(_dailyReportGrid);
        _reasonReportGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Motivo PT", DataPropertyName = nameof(ReasonSummary.ReasonNamePt), Width = 200 });
        _reasonReportGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Motivo JP", DataPropertyName = nameof(ReasonSummary.ReasonNameJp), Width = 180 });
        _reasonReportGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Qtde", DataPropertyName = nameof(ReasonSummary.TotalRequests), Width = 80 });
        _reasonReportGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Media min", DataPropertyName = nameof(ReasonSummary.AverageCloseMinutes), Width = 90 });

        _dailyReportGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Dia do turno", DataPropertyName = nameof(DailySummary.DayLabel), Width = 130 });
        _dailyReportGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Total", DataPropertyName = nameof(DailySummary.TotalRequests), Width = 80 });
        _dailyReportGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Fechados", DataPropertyName = nameof(DailySummary.ClosedRequests), Width = 90 });
        _dailyReportGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Media min", DataPropertyName = nameof(DailySummary.AverageCloseMinutes), Width = 90 });
        _dailyReportGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Turno noite", DataPropertyName = nameof(DailySummary.NightShiftRequests), Width = 95 });
        _dailyReportGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Turno dia", DataPropertyName = nameof(DailySummary.DayShiftRequests), Width = 90 });

        var charts = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            BackColor = BackColor
        };
        charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        charts.Controls.Add(BuildChartPanel("Chamados por turno", _shiftChartPanel), 0, 0);
        charts.Controls.Add(BuildChartPanel("Motivos mais chamados", _reasonChartPanel), 1, 0);

        var grids = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2
        };
        grids.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        grids.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        grids.Controls.Add(BuildGridPanel("Motivos mais chamados", _reasonReportGrid), 0, 0);
        grids.Controls.Add(BuildGridPanel("Resumo diario", _dailyReportGrid), 1, 0);

        root.Controls.Add(filters, 0, 0);
        root.Controls.Add(cards, 0, 1);
        root.Controls.Add(charts, 0, 2);
        root.Controls.Add(grids, 0, 3);
        return page;
    }

    private async Task LoadMasterDataAsync()
    {
        var catalog = await _masterDataRepository.GetCatalogAsync();
        _sectorOptions = catalog.Sectors.OrderBy(x => x.SortOrder).ThenBy(x => x.NamePt).ToList();
        _sectorLookupOptions = _sectorOptions
            .Select(x => new LookupOption(x.Id, $"{x.Code} - {x.NamePt}"))
            .ToList();
        _areaLookupOptions = catalog.WorkAreas
            .OrderBy(x => x.SectorId)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.NamePt)
            .Select(x => new LookupOption(
                x.Id,
                $"{ResolveSectorName(x.SectorId)} / {x.Code} - {x.NamePt}"))
            .ToList();
        _equipmentLookupOptions = catalog.Equipments
            .OrderBy(x => x.SectorId)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.NamePt)
            .Select(x => new LookupOption(
                x.Id,
                $"{ResolveSectorName(x.SectorId)} / {x.Code} - {x.NamePt}"))
            .ToList();
        _reasonLookupOptions = catalog.Reasons
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.NamePt)
            .Select(x => new LookupOption(x.Id, $"{x.Code} - {x.NamePt}"))
            .ToList();
        _sectorRows = new BindingList<Sector>(_sectorOptions.Select(Clone).ToList());
        _areaRows = new BindingList<WorkArea>(catalog.WorkAreas.OrderBy(x => x.SectorId).ThenBy(x => x.SortOrder).Select(Clone).ToList());
        _equipmentRows = new BindingList<Equipment>(catalog.Equipments.OrderBy(x => x.SectorId).ThenBy(x => x.SortOrder).Select(Clone).ToList());
        _machineRows = new BindingList<Machine>(catalog.Machines.OrderBy(x => x.SectorId).ThenBy(x => x.WorkAreaId).ThenBy(x => x.SortOrder).Select(Clone).ToList());
        _reasonRows = new BindingList<RequestReason>(catalog.Reasons.OrderBy(x => x.SortOrder).Select(Clone).ToList());
        _equipmentReasonMappingRows = new BindingList<EquipmentReasonMapping>(catalog.EquipmentReasonMappings.Select(Clone).ToList());

        _sectorsGrid.DataSource = _sectorRows;
        _areasGrid.DataSource = _areaRows;
        _equipmentsGrid.DataSource = _equipmentRows;
        _machinesGrid.DataSource = _machineRows;
        _reasonsGrid.DataSource = _reasonRows;
        _equipmentReasonMappingsGrid.DataSource = _equipmentReasonMappingRows;

        BindSectorCombos();
        RefreshLookupColumns();
    }

    private void BindSectorCombos()
    {
        var requestOptions = new List<Sector>
        {
            new() { Id = 0, Code = "ALL", NamePt = "Todos os setores", NameJp = "Todos os setores", IsActive = true }
        };
        requestOptions.AddRange(_sectorOptions.Where(x => x.IsActive).Select(Clone));

        _cboRequestsSector.DisplayMember = nameof(Sector.NamePt);
        _cboRequestsSector.ValueMember = nameof(Sector.Id);
        _cboRequestsSector.DataSource = requestOptions.ToList();

        _cboReportsSector.DisplayMember = nameof(Sector.NamePt);
        _cboReportsSector.ValueMember = nameof(Sector.Id);
        _cboReportsSector.DataSource = requestOptions.Select(Clone).ToList();

        _cboReportsShift.DisplayMember = nameof(ShiftOption.Label);
        _cboReportsShift.ValueMember = nameof(ShiftOption.Value);
        _cboReportsShift.DataSource = new List<ShiftOption>
        {
            new() { Label = "Todos os turnos", Value = string.Empty },
            new() { Label = "Turno do dia", Value = ShiftType.Day },
            new() { Label = "Turno da noite", Value = ShiftType.Night }
        };

        _cboReportsReason.DisplayMember = nameof(ReasonFilterOption.Label);
        _cboReportsReason.ValueMember = nameof(ReasonFilterOption.Value);
        ApplyReportReasonOptions();
    }

    private void ApplyReportReasonOptions()
    {
        var selectedValue = _cboReportsReason.SelectedValue as string ?? string.Empty;
        var options = new List<ReasonFilterOption>
        {
            new() { Label = "Todos os motivos", Value = string.Empty }
        };
        options.AddRange(_reasonRows
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.NamePt)
            .Select(x => new ReasonFilterOption
            {
                Label = $"{x.Code} - {x.NamePt}",
                Value = x.NamePt
            }));

        _cboReportsReason.DataSource = options;
        if (options.Any(x => x.Value == selectedValue))
        {
            _cboReportsReason.SelectedValue = selectedValue;
        }
    }

    private void RefreshLookupColumns()
    {
        ConfigureLookupColumn(_areaSectorColumn, MergeLookupOptions(_sectorLookupOptions, _areaRows.Select(x => x.SectorId)));
        ConfigureLookupColumn(_equipmentSectorColumn, MergeLookupOptions(_sectorLookupOptions, _equipmentRows.Select(x => x.SectorId)));
        ConfigureLookupColumn(_machineSectorColumn, MergeLookupOptions(_sectorLookupOptions, _machineRows.Select(x => x.SectorId)));
        ConfigureLookupColumn(_machineAreaColumn, MergeLookupOptions(_areaLookupOptions, _machineRows.Select(x => x.WorkAreaId)));
        ConfigureLookupColumn(_mappingEquipmentColumn, MergeLookupOptions(_equipmentLookupOptions, _equipmentReasonMappingRows.Select(x => x.EquipmentId)));
        ConfigureLookupColumn(_mappingReasonColumn, MergeLookupOptions(_reasonLookupOptions, _equipmentReasonMappingRows.Select(x => x.ReasonId)));
    }

    private async Task RefreshRequestsAsync()
    {
        var sectorId = _cboRequestsSector.SelectedValue is int requestSectorValue ? requestSectorValue : (int?)null;
        var status = (_cboRequestsStatus.SelectedItem as StatusOption)?.Value;
        var filter = new RequestSearchFilter
        {
            SectorId = sectorId is > 0 ? sectorId.Value : null,
            Status = status,
            FromUtc = _dtpRequestsFrom.Value.Date,
            ToUtc = _dtpRequestsTo.Value.Date.AddDays(1).AddSeconds(-1)
        };

        var rows = await _requestRepository.SearchAsync(filter);
        _requestsGrid.DataSource = rows.ToList();
    }

    private async Task RefreshReportsAsync()
    {
        var sectorId = _cboReportsSector.SelectedValue is int reportSectorValue ? reportSectorValue : (int?)null;
        var selectedShift = _cboReportsShift.SelectedValue is ShiftType shiftValue ? shiftValue : (ShiftType?)null;
        var selectedReason = _cboReportsReason.SelectedValue as string ?? string.Empty;
        var fromDate = _dtpReportsFrom.Value.Date;
        var toDate = _dtpReportsTo.Value.Date;

        var sourceFromUtc = ToLocalOffset(fromDate.AddDays(-1));
        var sourceToUtc = ToLocalOffset(toDate.AddDays(2).AddSeconds(-1));

        var rows = await _requestRepository.SearchAsync(new RequestSearchFilter
        {
            SectorId = sectorId is > 0 ? sectorId.Value : null,
            FromUtc = sourceFromUtc,
            ToUtc = sourceToUtc
        });

        _filteredReportRows = rows
            .Select(BuildReportRow)
            .Where(x => x.ShiftDate >= fromDate && x.ShiftDate <= toDate)
            .Where(x => selectedShift is null || x.Shift == selectedShift.Value)
            .Where(x => string.IsNullOrWhiteSpace(selectedReason) || x.ReasonNamePt == selectedReason)
            .OrderBy(x => x.ShiftDate)
            .ThenBy(x => x.ShiftSort)
            .ThenBy(x => x.RequestedAtLocal)
            .ToList();

        var summary = new ReportSummary
        {
            TotalRequests = _filteredReportRows.Count,
            OpenRequests = _filteredReportRows.Count(x => x.Status == RequestStatus.Open),
            ClosedRequests = _filteredReportRows.Count(x => x.Status == RequestStatus.Closed),
            AverageCloseMinutes = _filteredReportRows
                .Where(x => x.ElapsedMinutes.HasValue)
                .Select(x => x.ElapsedMinutes!.Value)
                .DefaultIfEmpty(0)
                .Average()
        };

        var reasons = _filteredReportRows
            .GroupBy(x => new { x.ReasonNamePt, x.ReasonNameJp })
            .Select(group => new ReasonSummary
            {
                ReasonNamePt = group.Key.ReasonNamePt,
                ReasonNameJp = group.Key.ReasonNameJp,
                TotalRequests = group.Count(),
                AverageCloseMinutes = group
                    .Where(x => x.ElapsedMinutes.HasValue)
                    .Select(x => x.ElapsedMinutes!.Value)
                    .DefaultIfEmpty(0)
                    .Average()
            })
            .OrderByDescending(x => x.TotalRequests)
            .ThenBy(x => x.ReasonNamePt)
            .ToList();

        var daily = _filteredReportRows
            .GroupBy(x => x.ShiftDate)
            .Select(group => new DailySummary
            {
                DayLabel = group.Key.ToString("yyyy-MM-dd"),
                TotalRequests = group.Count(),
                ClosedRequests = group.Count(x => x.Status == RequestStatus.Closed),
                AverageCloseMinutes = group
                    .Where(x => x.ElapsedMinutes.HasValue)
                    .Select(x => x.ElapsedMinutes!.Value)
                    .DefaultIfEmpty(0)
                    .Average(),
                DayShiftRequests = group.Count(x => x.Shift == ShiftType.Day),
                NightShiftRequests = group.Count(x => x.Shift == ShiftType.Night)
            })
            .OrderByDescending(x => x.DayLabel)
            .ToList();

        _lblTotal.Text = summary.TotalRequests.ToString();
        _lblOpen.Text = summary.OpenRequests.ToString();
        _lblClosed.Text = summary.ClosedRequests.ToString();
        _lblAverage.Text = summary.AverageCloseMinutes.ToString("0.0");

        _reasonReportGrid.DataSource = reasons;
        _dailyReportGrid.DataSource = daily;
        RenderShiftChart(_filteredReportRows);
        RenderReasonChart(reasons);
    }

    private static DateTimeOffset ToLocalOffset(DateTime dateTime)
    {
        return new DateTimeOffset(dateTime, TimeZoneInfo.Local.GetUtcOffset(dateTime));
    }

    private static ReportRowView BuildReportRow(AssistanceRequest request)
    {
        var requestedAtLocal = request.RequestedAtUtc.ToLocalTime();
        var shift = ResolveShift(requestedAtLocal);
        var shiftDate = NormalizeShiftDate(requestedAtLocal, shift);

        return new ReportRowView
        {
            TicketNumber = request.TicketNumber,
            SectorNamePt = request.SectorNamePt,
            WorkAreaNamePt = request.WorkAreaNamePt,
            EquipmentNamePt = request.EquipmentNamePt,
            MachineCode = request.MachineCode ?? string.Empty,
            ReasonNamePt = request.ReasonNamePt,
            ReasonNameJp = request.ReasonNameJp,
            RequestedByFjCode = request.RequestedByFjCode,
            RequestedAtLocal = requestedAtLocal,
            ClosedAtLocal = request.ClosedAtUtc?.ToLocalTime(),
            Status = request.Status,
            ElapsedMinutes = request.ElapsedMinutes,
            Shift = shift,
            ShiftDate = shiftDate
        };
    }

    private static ShiftType ResolveShift(DateTimeOffset localTime)
    {
        var time = localTime.TimeOfDay;
        return time >= new TimeSpan(8, 35, 0) && time < new TimeSpan(20, 35, 0)
            ? ShiftType.Day
            : ShiftType.Night;
    }

    private static DateTime NormalizeShiftDate(DateTimeOffset localTime, ShiftType shift)
    {
        return shift == ShiftType.Night && localTime.TimeOfDay < new TimeSpan(8, 35, 0)
            ? localTime.Date.AddDays(-1)
            : localTime.Date;
    }

    private string ResolveSectorName(int sectorId)
    {
        return _sectorOptions.FirstOrDefault(x => x.Id == sectorId)?.NamePt ?? $"Setor {sectorId}";
    }

    private static void ConfigureLookupColumn(DataGridViewComboBoxColumn column, IReadOnlyList<LookupOption> options)
    {
        column.DataSource = options.ToList();
        column.ValueMember = nameof(LookupOption.Id);
        column.DisplayMember = nameof(LookupOption.Label);
        column.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
        column.FlatStyle = FlatStyle.Flat;
    }

    private static IReadOnlyList<LookupOption> MergeLookupOptions(IReadOnlyList<LookupOption> options, IEnumerable<int> rowIds)
    {
        var merged = options.ToList();
        var knownIds = merged.Select(x => x.Id).ToHashSet();

        foreach (var missingId in rowIds.Where(x => x > 0 && !knownIds.Contains(x)).Distinct().OrderBy(x => x))
        {
            merged.Add(new LookupOption(missingId, $"Cadastro ausente (ID {missingId})"));
        }

        return merged;
    }

    private async Task SaveSectorsAsync()
    {
        await ExecuteSaveAsync(async () =>
        {
            EnsureUniqueSectors(_sectorRows.Where(IsValidSector));
            await _masterDataRepository.SaveSectorsAsync(_sectorRows.Where(IsValidSector));
            await LoadMasterDataAsync();
        });
    }

    private async Task SaveAreasAsync()
    {
        await ExecuteSaveAsync(async () =>
        {
            EnsureUniqueWorkAreas(_areaRows.Where(IsValidWorkArea));
            await _masterDataRepository.SaveWorkAreasAsync(_areaRows.Where(IsValidWorkArea));
            await LoadMasterDataAsync();
        });
    }

    private async Task SaveEquipmentsAsync()
    {
        await ExecuteSaveAsync(async () =>
        {
            EnsureUniqueEquipments(_equipmentRows.Where(IsValidEquipment));
            await _masterDataRepository.SaveEquipmentsAsync(_equipmentRows.Where(IsValidEquipment));
            await LoadMasterDataAsync();
        });
    }

    private async Task SaveMachinesAsync()
    {
        await ExecuteSaveAsync(async () =>
        {
            EnsureUniqueMachines(_machineRows.Where(IsValidMachine));
            await _masterDataRepository.SaveMachinesAsync(_machineRows.Where(IsValidMachine));
            await LoadMasterDataAsync();
        });
    }

    private async Task SaveReasonsAsync()
    {
        await ExecuteSaveAsync(async () =>
        {
            EnsureUniqueReasons(_reasonRows.Where(IsValidReason));
            await _masterDataRepository.SaveReasonsAsync(_reasonRows.Where(IsValidReason));
            await LoadMasterDataAsync();
        });
    }

    private async Task SaveEquipmentReasonMappingsAsync()
    {
        await ExecuteSaveAsync(async () =>
        {
            EnsureUniqueMappings(_equipmentReasonMappingRows.Where(IsValidEquipmentReasonMapping));
            await _masterDataRepository.ReplaceEquipmentReasonMappingsAsync(_equipmentReasonMappingRows.Where(IsValidEquipmentReasonMapping));
            await LoadMasterDataAsync();
        });
    }

    private async Task ExecuteSaveAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(this, ex.Message, "CallBell", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            MessageBox.Show(this,
                "Nao foi possivel salvar porque existe um cadastro duplicado ou um vinculo inconsistente no banco. Atualize a tela, revise os codigos e tente novamente.",
                "CallBell",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void EnsureUniqueSectors(IEnumerable<Sector> sectors)
    {
        var duplicate = sectors
            .GroupBy(x => NormalizeKey(x.Code))
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Key) && x.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Ja existe mais de um setor com o codigo '{duplicate.First().Code}'.");
        }
    }

    private void EnsureUniqueWorkAreas(IEnumerable<WorkArea> areas)
    {
        var duplicate = areas
            .GroupBy(x => new { x.SectorId, Code = NormalizeKey(x.Code) })
            .FirstOrDefault(x => x.Key.SectorId > 0 && !string.IsNullOrWhiteSpace(x.Key.Code) && x.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Ja existe mais de um local/area com o codigo '{duplicate.First().Code}' no setor '{ResolveSectorName(duplicate.Key.SectorId)}'.");
        }
    }

    private void EnsureUniqueEquipments(IEnumerable<Equipment> equipments)
    {
        var duplicate = equipments
            .GroupBy(x => new { x.SectorId, Code = NormalizeKey(x.Code) })
            .FirstOrDefault(x => x.Key.SectorId > 0 && !string.IsNullOrWhiteSpace(x.Key.Code) && x.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Ja existe mais de um equipamento com o codigo '{duplicate.First().Code}' no setor '{ResolveSectorName(duplicate.Key.SectorId)}'.");
        }
    }

    private void EnsureUniqueMachines(IEnumerable<Machine> machines)
    {
        var duplicate = machines
            .GroupBy(x => new { x.WorkAreaId, Code = NormalizeKey(x.Code) })
            .FirstOrDefault(x => x.Key.WorkAreaId > 0 && !string.IsNullOrWhiteSpace(x.Key.Code) && x.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Ja existe mais de uma maquina com o codigo '{duplicate.First().Code}' no mesmo local/area.");
        }
    }

    private void EnsureUniqueReasons(IEnumerable<RequestReason> reasons)
    {
        var duplicate = reasons
            .GroupBy(x => NormalizeKey(x.Code))
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Key) && x.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Ja existe mais de um motivo com o codigo '{duplicate.First().Code}'.");
        }
    }

    private void EnsureUniqueMappings(IEnumerable<EquipmentReasonMapping> mappings)
    {
        var duplicate = mappings
            .GroupBy(x => new { x.EquipmentId, x.ReasonId })
            .FirstOrDefault(x => x.Key.EquipmentId > 0 && x.Key.ReasonId > 0 && x.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException("Existe mais de um vinculo repetido entre o mesmo equipamento e motivo.");
        }
    }

    private static string NormalizeKey(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();
    }

    private void ConfigureRequestsGrid()
    {
        ConfigureSimpleGrid(_requestsGrid);
        _requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ticket", DataPropertyName = nameof(AssistanceRequest.TicketNumber), Width = 170 });
        _requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", DataPropertyName = nameof(AssistanceRequest.Status), Width = 90 });
        _requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Setor", DataPropertyName = nameof(AssistanceRequest.SectorNamePt), Width = 110 });
        _requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Area", DataPropertyName = nameof(AssistanceRequest.WorkAreaNamePt), Width = 110 });
        _requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Equipamento", DataPropertyName = nameof(AssistanceRequest.EquipmentNamePt), Width = 140 });
        _requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Motivo", DataPropertyName = nameof(AssistanceRequest.ReasonNamePt), Width = 180 });
        _requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Maquina", DataPropertyName = nameof(AssistanceRequest.MachineCode), Width = 110 });
        _requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Operador", DataPropertyName = nameof(AssistanceRequest.RequestedByFjCode), Width = 100 });
        _requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Abertura", DataPropertyName = nameof(AssistanceRequest.RequestedAtUtc), Width = 150 });
        _requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Fechado por", DataPropertyName = nameof(AssistanceRequest.ClosedByFjCode), Width = 110 });
        _requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Fechamento", DataPropertyName = nameof(AssistanceRequest.ClosedAtUtc), Width = 150 });
        _requestsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tempo min", DataPropertyName = nameof(AssistanceRequest.ElapsedMinutes), Width = 80 });
        _requestsGrid.CellFormatting += RequestsGridCellFormatting;
    }

    private void ConfigureSectorGrid()
    {
        ConfigureMasterGrid(_sectorsGrid);
        _sectorsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Codigo do setor", DataPropertyName = nameof(Sector.Code), Width = 170 });
        _sectorsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nome em portugues", DataPropertyName = nameof(Sector.NamePt), Width = 220 });
        _sectorsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nome em japones", DataPropertyName = nameof(Sector.NameJp), Width = 220 });
        _sectorsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ordem na lista", DataPropertyName = nameof(Sector.SortOrder), Width = 110 });
        _sectorsGrid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Ativo", DataPropertyName = nameof(Sector.IsActive), Width = 90 });
    }

    private void ConfigureAreaGrid()
    {
        ConfigureMasterGrid(_areasGrid);
        _areasGrid.DefaultValuesNeeded += (_, e) =>
        {
            e.Row.Cells[_areaSectorColumn.Index].Value = _sectorLookupOptions.FirstOrDefault()?.Id ?? 0;
            e.Row.Cells[5].Value = true;
        };
        _areaSectorColumn.HeaderText = "Setor";
        _areaSectorColumn.DataPropertyName = nameof(WorkArea.SectorId);
        _areaSectorColumn.Width = 220;
        _areasGrid.Columns.Add(_areaSectorColumn);
        _areasGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Codigo do local", DataPropertyName = nameof(WorkArea.Code), Width = 150 });
        _areasGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nome em portugues", DataPropertyName = nameof(WorkArea.NamePt), Width = 220 });
        _areasGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nome em japones", DataPropertyName = nameof(WorkArea.NameJp), Width = 220 });
        _areasGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ordem na lista", DataPropertyName = nameof(WorkArea.SortOrder), Width = 110 });
        _areasGrid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Ativo", DataPropertyName = nameof(WorkArea.IsActive), Width = 90 });
    }

    private void ConfigureEquipmentGrid()
    {
        ConfigureMasterGrid(_equipmentsGrid);
        _equipmentsGrid.DefaultValuesNeeded += (_, e) =>
        {
            e.Row.Cells[_equipmentSectorColumn.Index].Value = _sectorLookupOptions.FirstOrDefault()?.Id ?? 0;
            e.Row.Cells[5].Value = true;
        };
        _equipmentSectorColumn.HeaderText = "Setor";
        _equipmentSectorColumn.DataPropertyName = nameof(Equipment.SectorId);
        _equipmentSectorColumn.Width = 220;
        _equipmentsGrid.Columns.Add(_equipmentSectorColumn);
        _equipmentsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Codigo do equipamento", DataPropertyName = nameof(Equipment.Code), Width = 170 });
        _equipmentsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nome em portugues", DataPropertyName = nameof(Equipment.NamePt), Width = 220 });
        _equipmentsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nome em japones", DataPropertyName = nameof(Equipment.NameJp), Width = 220 });
        _equipmentsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ordem na lista", DataPropertyName = nameof(Equipment.SortOrder), Width = 110 });
        _equipmentsGrid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Ativo", DataPropertyName = nameof(Equipment.IsActive), Width = 90 });
    }

    private void ConfigureMachineGrid()
    {
        ConfigureMasterGrid(_machinesGrid);
        _machinesGrid.DefaultValuesNeeded += (_, e) =>
        {
            e.Row.Cells[_machineSectorColumn.Index].Value = _sectorLookupOptions.FirstOrDefault()?.Id ?? 0;
            e.Row.Cells[_machineAreaColumn.Index].Value = _areaLookupOptions.FirstOrDefault()?.Id ?? 0;
            e.Row.Cells[6].Value = true;
        };
        _machineSectorColumn.HeaderText = "Setor";
        _machineSectorColumn.DataPropertyName = nameof(Machine.SectorId);
        _machineSectorColumn.Width = 220;
        _machineAreaColumn.HeaderText = "Local / area";
        _machineAreaColumn.DataPropertyName = nameof(Machine.WorkAreaId);
        _machineAreaColumn.Width = 260;
        _machinesGrid.Columns.Add(_machineSectorColumn);
        _machinesGrid.Columns.Add(_machineAreaColumn);
        _machinesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Codigo da maquina", DataPropertyName = nameof(Machine.Code), Width = 170 });
        _machinesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nome em portugues", DataPropertyName = nameof(Machine.NamePt), Width = 220 });
        _machinesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nome em japones", DataPropertyName = nameof(Machine.NameJp), Width = 220 });
        _machinesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ordem na lista", DataPropertyName = nameof(Machine.SortOrder), Width = 110 });
        _machinesGrid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Ativo", DataPropertyName = nameof(Machine.IsActive), Width = 90 });
    }

    private void ConfigureReasonGrid()
    {
        ConfigureMasterGrid(_reasonsGrid);
        _reasonsGrid.DefaultValuesNeeded += (_, e) => e.Row.Cells[5].Value = true;
        _reasonsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Codigo do motivo", DataPropertyName = nameof(RequestReason.Code), Width = 170 });
        _reasonsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nome em portugues", DataPropertyName = nameof(RequestReason.NamePt), Width = 220 });
        _reasonsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nome em japones", DataPropertyName = nameof(RequestReason.NameJp), Width = 220 });
        _reasonsGrid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Exige maquina", DataPropertyName = nameof(RequestReason.RequiresMachine), Width = 130 });
        _reasonsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ordem na lista", DataPropertyName = nameof(RequestReason.SortOrder), Width = 110 });
        _reasonsGrid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Ativo", DataPropertyName = nameof(RequestReason.IsActive), Width = 90 });
    }

    private void ConfigureEquipmentReasonMappingGrid()
    {
        ConfigureMasterGrid(_equipmentReasonMappingsGrid);
        _equipmentReasonMappingsGrid.DefaultValuesNeeded += (_, e) =>
        {
            e.Row.Cells[_mappingEquipmentColumn.Index].Value = _equipmentLookupOptions.FirstOrDefault()?.Id ?? 0;
            e.Row.Cells[_mappingReasonColumn.Index].Value = _reasonLookupOptions.FirstOrDefault()?.Id ?? 0;
        };
        _mappingEquipmentColumn.HeaderText = "Equipamento";
        _mappingEquipmentColumn.DataPropertyName = nameof(EquipmentReasonMapping.EquipmentId);
        _mappingEquipmentColumn.Width = 340;
        _mappingReasonColumn.HeaderText = "Motivo permitido";
        _mappingReasonColumn.DataPropertyName = nameof(EquipmentReasonMapping.ReasonId);
        _mappingReasonColumn.Width = 320;
        _equipmentReasonMappingsGrid.Columns.Add(_mappingEquipmentColumn);
        _equipmentReasonMappingsGrid.Columns.Add(_mappingReasonColumn);
    }

    private void RequestsGridCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        var dataPropertyName = _requestsGrid.Columns[e.ColumnIndex].DataPropertyName;
        if (dataPropertyName == nameof(AssistanceRequest.RequestedAtUtc) && e.Value is DateTimeOffset requestedAt)
        {
            e.Value = requestedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm");
            e.FormattingApplied = true;
        }
        else if (dataPropertyName == nameof(AssistanceRequest.ClosedAtUtc) && e.Value is DateTimeOffset closedAt)
        {
            e.Value = closedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm");
            e.FormattingApplied = true;
        }
        else if (dataPropertyName == nameof(AssistanceRequest.Status) && e.Value is RequestStatus status)
        {
            e.Value = status == RequestStatus.Open ? "Aberto" : "Fechado";
            e.FormattingApplied = true;
        }
        else if (dataPropertyName == nameof(AssistanceRequest.ElapsedMinutes) && e.Value is double minutes)
        {
            e.Value = minutes.ToString("0.0");
            e.FormattingApplied = true;
        }
    }

    private TabPage BuildParameterPage(string title, Control grid, Func<Task> onSave, string description)
    {
        var page = new TabPage(title);
        page.Controls.Add(BuildGridPanel(title, grid, onSave, description));
        return page;
    }

    private Panel BuildGridPanel(string title, Control grid, Func<Task>? onSave = null, string? description = null)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(12)
        };

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 72,
            ColumnCount = 2
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));

        var titlePanel = new Panel { Dock = DockStyle.Fill };
        titlePanel.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(15, 23, 42),
            Height = 26
        });
        if (!string.IsNullOrWhiteSpace(description))
        {
            titlePanel.Controls.Add(new Label
            {
                Text = description,
                Dock = DockStyle.Top,
                ForeColor = Color.FromArgb(71, 85, 105),
                Height = 24
            });
        }

        header.Controls.Add(titlePanel, 0, 0);
        if (onSave is not null)
        {
            header.Controls.Add(BuildActionButton("Salvar alteracoes", onSave, Color.FromArgb(22, 163, 74)), 1, 0);
        }

        grid.Dock = DockStyle.Fill;
        panel.Controls.Add(grid);
        panel.Controls.Add(header);
        return panel;
    }

    private Control BuildChartPanel(string title, Control chart)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(8),
            Padding = new Padding(14)
        };

        panel.Controls.Add(chart);
        panel.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 28,
            Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(15, 23, 42)
        });

        return panel;
    }

    private Control BuildReportActionsPanel()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false,
            Height = 44,
            Margin = new Padding(0)
        };

        panel.Controls.Add(BuildActionButton("Gerar resumo", async () => await RefreshReportsAsync(), Color.FromArgb(14, 116, 144), 138));
        panel.Controls.Add(BuildActionButton("Exportar CSV", ExportReportCsvAsync, Color.FromArgb(51, 65, 85), 126));
        return panel;
    }

    private static Control BuildMetricCard(string title, Label valueLabel, Color accent)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(8),
            Padding = new Padding(18)
        };

        var titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 26,
            ForeColor = accent,
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point)
        };

        valueLabel.Dock = DockStyle.Fill;
        valueLabel.Text = "0";
        valueLabel.Font = new Font("Segoe UI Semibold", 28F, FontStyle.Bold, GraphicsUnit.Point);
        valueLabel.ForeColor = Color.FromArgb(15, 23, 42);
        valueLabel.TextAlign = ContentAlignment.MiddleLeft;

        panel.Controls.Add(valueLabel);
        panel.Controls.Add(titleLabel);
        return panel;
    }

    private static Button BuildActionButton(string text, Func<Task> action, Color color, int width = 0)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        if (width > 0)
        {
            button.Width = width;
            button.Dock = DockStyle.None;
        }
        button.FlatAppearance.BorderSize = 0;
        button.Click += async (_, _) => await action();
        return button;
    }

    private static Control BuildField(string title, Control control)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 0, 8, 0)
        };

        var label = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 24,
            Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(30, 41, 59)
        };

        control.Dock = DockStyle.Top;
        control.Height = 38;

        panel.Controls.Add(control);
        panel.Controls.Add(label);
        return panel;
    }

    private static Label BuildInfoLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(71, 85, 105)
        };
    }

    private static Panel WrapGrid(Control grid)
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(12),
            Controls = { grid }
        };
    }

    private static void ConfigureBarChartPanel(FlowLayoutPanel panel)
    {
        panel.Dock = DockStyle.Fill;
        panel.BackColor = Color.White;
        panel.FlowDirection = FlowDirection.TopDown;
        panel.WrapContents = false;
        panel.AutoScroll = true;
        panel.Padding = new Padding(10, 8, 10, 8);
    }

    private void RenderShiftChart(IEnumerable<ReportRowView> rows)
    {
        _shiftChartPanel.SuspendLayout();
        _shiftChartPanel.Controls.Clear();
        var items = new List<ChartBarItem>
        {
            new("Turno dia", rows.Count(x => x.Shift == ShiftType.Day), Color.FromArgb(234, 88, 12)),
            new("Turno noite", rows.Count(x => x.Shift == ShiftType.Night), Color.FromArgb(37, 99, 235))
        };
        AppendBars(_shiftChartPanel, items);
        _shiftChartPanel.ResumeLayout();
    }

    private void RenderReasonChart(IReadOnlyList<ReasonSummary> reasons)
    {
        _reasonChartPanel.SuspendLayout();
        _reasonChartPanel.Controls.Clear();
        var palette = new[]
        {
            Color.FromArgb(37, 99, 235),
            Color.FromArgb(14, 116, 144),
            Color.FromArgb(234, 88, 12),
            Color.FromArgb(22, 163, 74),
            Color.FromArgb(79, 70, 229),
            Color.FromArgb(219, 39, 119)
        };
        var items = reasons
            .Take(8)
            .Select((x, index) => new ChartBarItem(x.ReasonNamePt, x.TotalRequests, palette[index % palette.Length]))
            .ToList();
        AppendBars(_reasonChartPanel, items);
        _reasonChartPanel.ResumeLayout();
    }

    private static void AppendBars(FlowLayoutPanel panel, IReadOnlyList<ChartBarItem> items)
    {
        var max = Math.Max(1, items.DefaultIfEmpty(new ChartBarItem(string.Empty, 0, Color.White)).Max(x => x.Value));
        var total = Math.Max(1, items.Sum(x => x.Value));
        foreach (var item in items)
        {
            panel.Controls.Add(BuildBarCard(item, max, total));
        }
    }

    private static Control BuildBarCard(ChartBarItem item, int max, int total)
    {
        var percentage = item.Value <= 0 ? 0 : (item.Value / (double)total) * 100d;

        var shell = new Panel
        {
            Width = 540,
            Height = 78,
            Margin = new Padding(0, 0, 0, 12),
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(12, 10, 12, 10)
        };

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 26,
            ColumnCount = 3,
            BackColor = Color.Transparent
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 74));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62));

        var title = new Label
        {
            Text = item.Label,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(30, 41, 59),
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var value = new Label
        {
            Text = item.Value.ToString(),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = item.Color,
            BackColor = Color.White,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point),
            Margin = new Padding(6, 0, 6, 0)
        };

        var percentLabel = new Label
        {
            Text = $"{percentage:0}%",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Color.FromArgb(71, 85, 105),
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point)
        };

        header.Controls.Add(title, 0, 0);
        header.Controls.Add(value, 1, 0);
        header.Controls.Add(percentLabel, 2, 0);

        var subtitle = new Label
        {
            Text = item.Value == 1 ? "1 chamado" : $"{item.Value} chamados",
            Dock = DockStyle.Top,
            Height = 18,
            ForeColor = Color.FromArgb(100, 116, 139),
            Font = new Font("Segoe UI", 8.75F, FontStyle.Regular, GraphicsUnit.Point),
            Margin = new Padding(0, 2, 0, 0)
        };

        var trackHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 8, 0, 0),
            BackColor = Color.Transparent
        };

        var track = new Panel
        {
            Dock = DockStyle.Top,
            Height = 14,
            BackColor = Color.FromArgb(226, 232, 240)
        };

        var fill = new Panel
        {
            Width = Math.Max(10, (int)Math.Round((track.Width > 0 ? track.Width : 420) * (item.Value / (double)max))),
            Dock = DockStyle.Left,
            BackColor = item.Color
        };
        track.Resize += (_, _) =>
        {
            fill.Width = item.Value <= 0
                ? 0
                : Math.Max(12, (int)Math.Round(track.ClientSize.Width * (item.Value / (double)max)));
        };
        track.Controls.Add(fill);
        trackHost.Controls.Add(track);
        shell.Controls.Add(trackHost);
        shell.Controls.Add(subtitle);
        shell.Controls.Add(header);
        return shell;
    }

    private async Task ExportReportCsvAsync()
    {
        await Task.Yield();

        using var dialog = new SaveFileDialog
        {
            Title = "Exportar relatorio CSV",
            Filter = "CSV (*.csv)|*.csv",
            FileName = $"callbell-relatorio-{DateTime.Now:yyyyMMdd-HHmm}.csv"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("DiaTurno,Turno,Setor,Area,Equipamento,Maquina,MotivoPT,MotivoJP,Status,AberturaLocal,FechamentoLocal,TempoMin,Operador");

        foreach (var row in _filteredReportRows)
        {
            builder.AppendLine(string.Join(",",
                EscapeCsv(row.ShiftDate.ToString("yyyy-MM-dd")),
                EscapeCsv(row.Shift == ShiftType.Day ? "Turno do dia" : "Turno da noite"),
                EscapeCsv(row.SectorNamePt),
                EscapeCsv(row.WorkAreaNamePt),
                EscapeCsv(row.EquipmentNamePt),
                EscapeCsv(row.MachineCode),
                EscapeCsv(row.ReasonNamePt),
                EscapeCsv(row.ReasonNameJp),
                EscapeCsv(row.Status == RequestStatus.Open ? "Aberto" : "Fechado"),
                EscapeCsv(row.RequestedAtLocal.ToString("yyyy-MM-dd HH:mm")),
                EscapeCsv(row.ClosedAtLocal?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty),
                EscapeCsv(row.ElapsedMinutes?.ToString("0.0") ?? string.Empty),
                EscapeCsv(row.RequestedByFjCode)));
        }

        File.WriteAllText(dialog.FileName, builder.ToString(), new UTF8Encoding(true));
    }

    private static string EscapeCsv(string value)
    {
        var sanitized = value.Replace("\"", "\"\"");
        return $"\"{sanitized}\"";
    }

    private static void ConfigureSimpleGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AutoGenerateColumns = false;
        grid.ReadOnly = true;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        ApplyGridTheme(grid, readOnly: true);
    }

    private static void ConfigureMasterGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.AllowUserToAddRows = true;
        grid.AllowUserToDeleteRows = false;
        grid.AutoGenerateColumns = false;
        grid.RowHeadersVisible = false;
        grid.EditMode = DataGridViewEditMode.EditOnEnter;
        grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        grid.DataError += MasterGridDataError;
        ApplyGridTheme(grid, readOnly: false);
    }

    private static void MasterGridDataError(object? sender, DataGridViewDataErrorEventArgs e)
    {
        e.ThrowException = false;
        e.Cancel = false;
    }

    private static void ApplyGridTheme(DataGridView grid, bool readOnly)
    {
        grid.ReadOnly = readOnly;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
        grid.ColumnHeadersHeight = 40;
        grid.RowTemplate.Height = 36;
        grid.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
        grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
        grid.GridColor = Color.FromArgb(226, 232, 240);
    }

    private static bool IsValidSector(Sector? sector)
    {
        return sector is not null
            && !string.IsNullOrWhiteSpace(sector.Code)
            && !string.IsNullOrWhiteSpace(sector.NamePt)
            && !string.IsNullOrWhiteSpace(sector.NameJp);
    }

    private static bool IsValidWorkArea(WorkArea? area)
    {
        return area is not null
            && area.SectorId > 0
            && !string.IsNullOrWhiteSpace(area.Code)
            && !string.IsNullOrWhiteSpace(area.NamePt)
            && !string.IsNullOrWhiteSpace(area.NameJp);
    }

    private static bool IsValidEquipment(Equipment? equipment)
    {
        return equipment is not null
            && equipment.SectorId > 0
            && !string.IsNullOrWhiteSpace(equipment.Code)
            && !string.IsNullOrWhiteSpace(equipment.NamePt)
            && !string.IsNullOrWhiteSpace(equipment.NameJp);
    }

    private static bool IsValidMachine(Machine? machine)
    {
        return machine is not null
            && machine.SectorId > 0
            && machine.WorkAreaId > 0
            && !string.IsNullOrWhiteSpace(machine.Code)
            && !string.IsNullOrWhiteSpace(machine.NamePt)
            && !string.IsNullOrWhiteSpace(machine.NameJp);
    }

    private static bool IsValidReason(RequestReason? reason)
    {
        return reason is not null
            && !string.IsNullOrWhiteSpace(reason.Code)
            && !string.IsNullOrWhiteSpace(reason.NamePt)
            && !string.IsNullOrWhiteSpace(reason.NameJp);
    }

    private static bool IsValidEquipmentReasonMapping(EquipmentReasonMapping? mapping)
    {
        return mapping is not null
            && mapping.EquipmentId > 0
            && mapping.ReasonId > 0;
    }

    private static Sector Clone(Sector value) => new()
    {
        Id = value.Id,
        Code = value.Code,
        NamePt = value.NamePt,
        NameJp = value.NameJp,
        IsActive = value.IsActive,
        SortOrder = value.SortOrder
    };

    private static WorkArea Clone(WorkArea value) => new()
    {
        Id = value.Id,
        SectorId = value.SectorId,
        Code = value.Code,
        NamePt = value.NamePt,
        NameJp = value.NameJp,
        IsActive = value.IsActive,
        SortOrder = value.SortOrder
    };

    private static Equipment Clone(Equipment value) => new()
    {
        Id = value.Id,
        SectorId = value.SectorId,
        Code = value.Code,
        NamePt = value.NamePt,
        NameJp = value.NameJp,
        IsActive = value.IsActive,
        SortOrder = value.SortOrder
    };

    private static Machine Clone(Machine value) => new()
    {
        Id = value.Id,
        SectorId = value.SectorId,
        WorkAreaId = value.WorkAreaId,
        Code = value.Code,
        NamePt = value.NamePt,
        NameJp = value.NameJp,
        IsActive = value.IsActive,
        SortOrder = value.SortOrder
    };

    private static RequestReason Clone(RequestReason value) => new()
    {
        Id = value.Id,
        Code = value.Code,
        NamePt = value.NamePt,
        NameJp = value.NameJp,
        RequiresMachine = value.RequiresMachine,
        IsActive = value.IsActive,
        SortOrder = value.SortOrder
    };

    private static EquipmentReasonMapping Clone(EquipmentReasonMapping value) => new()
    {
        EquipmentId = value.EquipmentId,
        ReasonId = value.ReasonId
    };

    private sealed class StatusOption
    {
        public string Label { get; init; } = string.Empty;
        public RequestStatus? Value { get; init; }
    }

    private enum ShiftType
    {
        Day = 1,
        Night = 2
    }

    private sealed class ShiftOption
    {
        public string Label { get; init; } = string.Empty;
        public object? Value { get; init; }
    }

    private sealed class ReasonFilterOption
    {
        public string Label { get; init; } = string.Empty;
        public string Value { get; init; } = string.Empty;
    }

    private sealed class LookupOption
    {
        public LookupOption(int id, string label)
        {
            Id = id;
            Label = label;
        }

        public int Id { get; }
        public string Label { get; }
    }

    private sealed class ReportRowView
    {
        public string TicketNumber { get; init; } = string.Empty;
        public string SectorNamePt { get; init; } = string.Empty;
        public string WorkAreaNamePt { get; init; } = string.Empty;
        public string EquipmentNamePt { get; init; } = string.Empty;
        public string MachineCode { get; init; } = string.Empty;
        public string ReasonNamePt { get; init; } = string.Empty;
        public string ReasonNameJp { get; init; } = string.Empty;
        public string RequestedByFjCode { get; init; } = string.Empty;
        public DateTimeOffset RequestedAtLocal { get; init; }
        public DateTimeOffset? ClosedAtLocal { get; init; }
        public RequestStatus Status { get; init; }
        public double? ElapsedMinutes { get; init; }
        public ShiftType Shift { get; init; }
        public int ShiftSort => Shift == ShiftType.Day ? 0 : 1;
        public DateTime ShiftDate { get; init; }
    }

    private sealed class ChartBarItem
    {
        public ChartBarItem(string label, int value, Color color)
        {
            Label = label;
            Value = value;
            Color = color;
        }

        public string Label { get; }
        public int Value { get; }
        public Color Color { get; }
    }
}
