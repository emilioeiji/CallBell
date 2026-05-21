using CallBell.Config;
using CallBell.Core.Entities;
using CallBell.Core.Models;
using CallBell.Data.Repositories;
using CallBell.Data.Services;

namespace CallBell.OperatorApp;

public sealed class OperatorMainForm : Form
{
    private const string ProfileFileName = "operator-station.json";

    private readonly CallBellSettings _settings;
    private readonly MasterDataRepository _masterDataRepository;
    private readonly CallBellRequestService _requestService;

    private readonly TextBox _txtFjCode = new();
    private readonly ComboBox _cboSector = new();
    private readonly ComboBox _cboWorkArea = new();
    private readonly ComboBox _cboEquipment = new();
    private readonly ComboBox _cboMachine = new();
    private readonly Button _btnSaveProfile = new();
    private readonly Button _btnSubmit = new();
    private readonly Label _lblSelectedReason = new();
    private readonly Label _lblStatus = new();
    private readonly Label _lblMachineHint = new();
    private readonly FlowLayoutPanel _reasonsPanel = new();
    private readonly Panel _machinePanel = new();
    private readonly TableLayoutPanel _actionColumn = new();

    private CatalogSnapshot _catalog = new();
    private RequestReason? _selectedReason;
    private bool _loadingSelections;

    public OperatorMainForm(
        CallBellSettings settings,
        MasterDataRepository masterDataRepository,
        CallBellRequestService requestService)
    {
        _settings = settings;
        _masterDataRepository = masterDataRepository;
        _requestService = requestService;

        Text = "CallBell - Solicitacao";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1200, 800);
        BackColor = Color.FromArgb(244, 247, 251);
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

        BuildLayout();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await LoadCatalogAsync();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(24),
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 124));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 208));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(BuildHeaderPanel(), 0, 0);
        root.Controls.Add(BuildStationPanel(), 0, 1);
        root.Controls.Add(BuildReasonPanel(), 0, 2);
    }

    private Control BuildHeaderPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(23, 37, 84),
            Padding = new Padding(34, 22, 34, 20)
        };

        var title = new Label
        {
            Text = "CallBell Factory Request",
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold, GraphicsUnit.Point),
            Dock = DockStyle.Top,
            Height = 42
        };

        var subtitle = new Label
        {
            Text = "1. Identifique o posto. 2. Escolha o equipamento. 3. Toque no motivo. 4. Informe a maquina apenas quando o sistema pedir.",
            ForeColor = Color.FromArgb(191, 219, 254),
            Font = new Font("Segoe UI", 11.5F, FontStyle.Regular, GraphicsUnit.Point),
            Dock = DockStyle.Top,
            Height = 32
        };

        panel.Controls.Add(subtitle);
        panel.Controls.Add(title);
        return panel;
    }

    private Control BuildStationPanel()
    {
        var shell = BuildCard();
        shell.Padding = new Padding(24, 20, 24, 18);

        var container = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2,
            BackColor = shell.BackColor
        };
        container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
        container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
        container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
        container.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        container.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        shell.Controls.Add(container);

        ConfigureCombo(_cboSector, CboSectorChanged);
        ConfigureCombo(_cboWorkArea, CboWorkAreaChanged);

        _txtFjCode.CharacterCasing = CharacterCasing.Upper;
        _txtFjCode.Dock = DockStyle.Top;
        _txtFjCode.Height = 40;
        _txtFjCode.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold, GraphicsUnit.Point);
        _txtFjCode.Margin = new Padding(0);

        _btnSaveProfile.Text = "Salvar posto desta estacao";
        _btnSaveProfile.Height = 46;
        _btnSaveProfile.Width = 220;
        _btnSaveProfile.BackColor = Color.FromArgb(226, 232, 240);
        _btnSaveProfile.ForeColor = Color.FromArgb(30, 41, 59);
        _btnSaveProfile.FlatStyle = FlatStyle.Flat;
        _btnSaveProfile.FlatAppearance.BorderSize = 0;
        _btnSaveProfile.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point);
        _btnSaveProfile.Click += (_, _) => SaveProfile(showFeedback: true);

        container.Controls.Add(BuildSectionTitle("1. Identificacao do posto", "Informe FJ, setor e local para abrir o chamado."), 0, 0);
        container.SetColumnSpan(container.GetControlFromPosition(0, 0)!, 4);
        container.Controls.Add(BuildField("Codigo FJ", "Formato FJ12345", _txtFjCode), 0, 1);
        container.Controls.Add(BuildField("Setor", "Escolha o setor do posto", _cboSector), 1, 1);
        container.Controls.Add(BuildField("Local / Area", "Area fixa da estacao", _cboWorkArea), 2, 1);

        container.Controls.Add(BuildField("Posto desta estacao", "Grava setor e local para reaproveitar nesta maquina", _btnSaveProfile), 3, 1);

        return shell;
    }

    private Control BuildReasonPanel()
    {
        var shell = BuildCard();
        shell.Padding = new Padding(24, 14, 24, 20);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = shell.BackColor
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        shell.Controls.Add(layout);

        layout.Controls.Add(BuildSectionTitle("2. Equipamento e motivo", string.Empty), 0, 0);

        ConfigureCombo(_cboEquipment, CboEquipmentChanged);
        ConfigureCombo(_cboMachine, null);

        var equipmentPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(18, 10, 18, 10),
            Margin = new Padding(0, 0, 0, 8)
        };

        equipmentPanel.Controls.Add(BuildField("Equipamento", "Lista por setor selecionado", _cboEquipment), 0, 0);
        layout.Controls.Add(equipmentPanel, 0, 1);

        _reasonsPanel.Dock = DockStyle.Fill;
        _reasonsPanel.WrapContents = true;
        _reasonsPanel.AutoScroll = true;
        _reasonsPanel.Padding = new Padding(8);
        _reasonsPanel.BackColor = Color.FromArgb(248, 250, 252);
        _reasonsPanel.Margin = new Padding(0, 0, 0, 12);
        layout.Controls.Add(_reasonsPanel, 0, 2);

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            BackColor = shell.BackColor
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

        var statusPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            BackColor = Color.FromArgb(239, 246, 255),
            Padding = new Padding(16, 12, 16, 10),
            Margin = new Padding(0, 0, 12, 0)
        };
        statusPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 62));
        statusPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 38));

        _lblSelectedReason.Dock = DockStyle.Fill;
        _lblSelectedReason.Text = "Selecione um equipamento e depois toque no motivo";
        _lblSelectedReason.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold, GraphicsUnit.Point);
        _lblSelectedReason.ForeColor = Color.FromArgb(30, 41, 59);

        _lblStatus.Dock = DockStyle.Fill;
        _lblStatus.ForeColor = Color.FromArgb(22, 101, 52);
        _lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        _lblStatus.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);

        statusPanel.Controls.Add(_lblSelectedReason, 0, 0);
        statusPanel.Controls.Add(_lblStatus, 0, 1);

        _lblMachineHint.Dock = DockStyle.Fill;
        _lblMachineHint.Text = "A maquina so aparece quando o motivo exigir.";
        _lblMachineHint.ForeColor = Color.FromArgb(71, 85, 105);
        _lblMachineHint.TextAlign = ContentAlignment.MiddleLeft;
        _lblMachineHint.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);

        _machinePanel.Dock = DockStyle.Fill;
        _machinePanel.Visible = false;
        _machinePanel.Padding = new Padding(0, 0, 10, 0);
        _machinePanel.Controls.Add(BuildField("Maquina", "Escolha apenas quando solicitado", _cboMachine));

        var actionPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 10, 0, 0)
        };
        _btnSubmit.Text = "Confirmar solicitacao";
        _btnSubmit.Dock = DockStyle.None;
        _btnSubmit.Width = 290;
        _btnSubmit.Height = 52;
        _btnSubmit.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnSubmit.BackColor = Color.FromArgb(30, 64, 175);
        _btnSubmit.ForeColor = Color.White;
        _btnSubmit.FlatStyle = FlatStyle.Flat;
        _btnSubmit.FlatAppearance.BorderSize = 0;
        _btnSubmit.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold, GraphicsUnit.Point);
        _btnSubmit.Location = new Point(Math.Max(0, actionPanel.Width - _btnSubmit.Width), 10);
        actionPanel.Resize += (_, _) =>
        {
            _btnSubmit.Location = new Point(Math.Max(0, actionPanel.ClientSize.Width - _btnSubmit.Width), 10);
        };
        _btnSubmit.Click += async (_, _) => await SubmitRequestAsync();
        actionPanel.Controls.Add(_btnSubmit);

        footer.Controls.Add(statusPanel, 0, 0);
        footer.Controls.Add(BuildInfoCard("Maquina", _lblMachineHint), 1, 0);
        footer.Controls.Add(BuildActionColumn(_actionColumn, _machinePanel, actionPanel), 2, 0);

        layout.Controls.Add(footer, 0, 3);
        return shell;
    }

    private async Task LoadCatalogAsync()
    {
        UseWaitCursor = true;
        try
        {
            _catalog = await _masterDataRepository.GetCatalogAsync();
            BindSectors();
            BindEmptyWorkAreas();
            BindEmptyEquipments();
            BindEmptyMachines();
            RenderReasonButtons();
            UpdateReasonHeader();
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

    private void BindSectors()
    {
        _loadingSelections = true;
        try
        {
            var items = new List<Sector>
            {
                new() { Id = 0, Code = string.Empty, NamePt = "Selecione...", NameJp = "Selecione...", IsActive = true }
            };
            items.AddRange(_catalog.Sectors.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.NamePt));

            _cboSector.DisplayMember = nameof(Sector.NamePt);
            _cboSector.ValueMember = nameof(Sector.Id);
            _cboSector.DataSource = items;
            _cboSector.SelectedIndex = 0;
        }
        finally
        {
            _loadingSelections = false;
        }
    }

    private void SaveProfile(bool showFeedback)
    {
        if (_cboSector.SelectedValue is not int sectorId || sectorId <= 0)
        {
            MessageBox.Show(this, "Selecione um setor antes de salvar o posto.", "CallBell", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_cboWorkArea.SelectedValue is not int workAreaId || workAreaId <= 0)
        {
            MessageBox.Show(this, "Selecione uma area antes de salvar o posto.", "CallBell", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var profile = new OperatorStationProfile
        {
            RequestedByFjCode = _txtFjCode.Text.Trim().ToUpperInvariant(),
            SectorId = sectorId,
            WorkAreaId = workAreaId
        };

        AppSettingsProvider.SaveProfile(_settings, ProfileFileName, profile);
        if (showFeedback)
        {
            _lblStatus.Text = "Posto salvo nesta estacao.";
            _lblStatus.ForeColor = Color.FromArgb(22, 101, 52);
        }
    }

    private void UpdateWorkAreas()
    {
        if (_cboSector.SelectedValue is not int sectorId || sectorId <= 0)
        {
            BindEmptyWorkAreas();
            return;
        }

        var areas = new List<WorkArea>
        {
            new() { Id = 0, SectorId = sectorId, Code = string.Empty, NamePt = "Selecione...", NameJp = "Selecione...", IsActive = true }
        };
        areas.AddRange(_catalog.WorkAreas
            .Where(x => x.IsActive && x.SectorId == sectorId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.NamePt));

        _loadingSelections = true;
        try
        {
            _cboWorkArea.DisplayMember = nameof(WorkArea.NamePt);
            _cboWorkArea.ValueMember = nameof(WorkArea.Id);
            _cboWorkArea.DataSource = areas;
            _cboWorkArea.SelectedIndex = 0;
        }
        finally
        {
            _loadingSelections = false;
        }
    }

    private void BindEmptyWorkAreas()
    {
        _loadingSelections = true;
        try
        {
            _cboWorkArea.DisplayMember = nameof(WorkArea.NamePt);
            _cboWorkArea.ValueMember = nameof(WorkArea.Id);
            _cboWorkArea.DataSource = new List<WorkArea>
            {
                new() { Id = 0, NamePt = "Selecione o setor primeiro", NameJp = "Selecione o setor primeiro", IsActive = true }
            };
            _cboWorkArea.SelectedIndex = 0;
        }
        finally
        {
            _loadingSelections = false;
        }
    }

    private void UpdateEquipments()
    {
        if (_cboSector.SelectedValue is not int sectorId || sectorId <= 0)
        {
            BindEmptyEquipments();
            return;
        }

        var equipments = new List<Equipment>
        {
            new() { Id = 0, SectorId = sectorId, Code = string.Empty, NamePt = "Selecione...", NameJp = "Selecione...", IsActive = true }
        };
        equipments.AddRange(_catalog.Equipments
            .Where(x => x.IsActive && x.SectorId == sectorId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code));

        _loadingSelections = true;
        try
        {
            _cboEquipment.DisplayMember = nameof(Equipment.NamePt);
            _cboEquipment.ValueMember = nameof(Equipment.Id);
            _cboEquipment.DataSource = equipments;
            _cboEquipment.SelectedIndex = 0;
        }
        finally
        {
            _loadingSelections = false;
        }

        _selectedReason = null;
        UpdateReasonHeader();
        RenderReasonButtons();
    }

    private void BindEmptyEquipments()
    {
        _loadingSelections = true;
        try
        {
            _cboEquipment.DisplayMember = nameof(Equipment.NamePt);
            _cboEquipment.ValueMember = nameof(Equipment.Id);
            _cboEquipment.DataSource = new List<Equipment>
            {
                new() { Id = 0, NamePt = "Selecione o setor primeiro", NameJp = "Selecione o setor primeiro", IsActive = true }
            };
            _cboEquipment.SelectedIndex = 0;
        }
        finally
        {
            _loadingSelections = false;
        }
    }

    private void UpdateMachineOptions()
    {
        if (_cboSector.SelectedValue is not int sectorId || sectorId <= 0 ||
            _cboWorkArea.SelectedValue is not int workAreaId || workAreaId <= 0)
        {
            BindEmptyMachines();
            return;
        }

        var machines = new List<Machine>
        {
            new() { Id = 0, SectorId = sectorId, WorkAreaId = workAreaId, Code = string.Empty, NamePt = "Selecione...", NameJp = "Selecione...", IsActive = true }
        };
        machines.AddRange(_catalog.Machines
            .Where(x => x.IsActive && x.SectorId == sectorId && x.WorkAreaId == workAreaId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code));

        _loadingSelections = true;
        try
        {
            _cboMachine.DisplayMember = nameof(Machine.NamePt);
            _cboMachine.ValueMember = nameof(Machine.Id);
            _cboMachine.DataSource = machines;
            _cboMachine.SelectedIndex = 0;
        }
        finally
        {
            _loadingSelections = false;
        }
    }

    private void BindEmptyMachines()
    {
        _loadingSelections = true;
        try
        {
            _cboMachine.DisplayMember = nameof(Machine.NamePt);
            _cboMachine.ValueMember = nameof(Machine.Id);
            _cboMachine.DataSource = new List<Machine>
            {
                new() { Id = 0, NamePt = "Selecione a area primeiro", NameJp = "Selecione a area primeiro", IsActive = true }
            };
            _cboMachine.SelectedIndex = 0;
        }
        finally
        {
            _loadingSelections = false;
        }
    }

    private void RenderReasonButtons()
    {
        _reasonsPanel.Controls.Clear();

        var equipmentId = _cboEquipment.SelectedValue is int value ? value : 0;
        if (equipmentId <= 0)
        {
            AddPlaceholder("Selecione um equipamento para carregar os motivos.");
            return;
        }

        var allowedReasonIds = _catalog.EquipmentReasonMappings
            .Where(x => x.EquipmentId == equipmentId)
            .Select(x => x.ReasonId)
            .ToHashSet();

        var reasons = _catalog.Reasons
            .Where(x => x.IsActive && allowedReasonIds.Contains(x.Id))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.NamePt)
            .ToList();

        if (!reasons.Any())
        {
            AddPlaceholder("Nenhum motivo foi vinculado a este equipamento.");
            return;
        }

        foreach (var reason in reasons)
        {
            var button = new Button
            {
                Width = 320,
                Height = 126,
                Margin = new Padding(12),
                Tag = reason,
                Text = $"{reason.NamePt}{Environment.NewLine}{reason.NameJp}",
                TextAlign = ContentAlignment.MiddleCenter,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold, GraphicsUnit.Point)
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            button.FlatAppearance.BorderSize = 2;
            button.Click += (_, _) => SelectReason(reason);
            _reasonsPanel.Controls.Add(button);
        }
    }

    private void AddPlaceholder(string text)
    {
        var label = new Label
        {
            AutoSize = false,
            Width = 980,
            Height = 96,
            Margin = new Padding(12),
            Text = text,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(71, 85, 105),
            Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point)
        };
        _reasonsPanel.Controls.Add(label);
    }

    private void SelectReason(RequestReason reason)
    {
        _selectedReason = reason;
        UpdateReasonHeader();

        foreach (Control control in _reasonsPanel.Controls)
        {
            if (control is not Button button || button.Tag is not RequestReason item)
            {
                continue;
            }

            var isSelected = item.Id == reason.Id;
            button.BackColor = isSelected ? Color.FromArgb(219, 234, 254) : Color.White;
            button.FlatAppearance.BorderColor = isSelected ? Color.FromArgb(37, 99, 235) : Color.FromArgb(203, 213, 225);
        }
    }

    private void UpdateReasonHeader()
    {
        if (_selectedReason is null)
        {
            _lblSelectedReason.Text = "Selecione um motivo";
            _lblMachineHint.Text = "A maquina so aparece quando o motivo exigir.";
            _machinePanel.Visible = false;
            _actionColumn.RowStyles[0].Height = 0;
            _actionColumn.RowStyles[0].SizeType = SizeType.Absolute;
            _actionColumn.RowStyles[1].Height = 100;
            _actionColumn.RowStyles[1].SizeType = SizeType.Percent;
            return;
        }

        _lblSelectedReason.Text = $"Motivo: {_selectedReason.NamePt} / {_selectedReason.NameJp}";
        _lblMachineHint.Text = _selectedReason.RequiresMachine
            ? "Este motivo exige selecao de maquina."
            : "Este motivo nao precisa de maquina.";
        _machinePanel.Visible = _selectedReason.RequiresMachine;
        _actionColumn.RowStyles[0].Height = _selectedReason.RequiresMachine ? 65 : 0;
        _actionColumn.RowStyles[0].SizeType = _selectedReason.RequiresMachine ? SizeType.Percent : SizeType.Absolute;
        _actionColumn.RowStyles[1].Height = _selectedReason.RequiresMachine ? 42 : 100;
        _actionColumn.RowStyles[1].SizeType = SizeType.Percent;
    }

    private async Task SubmitRequestAsync()
    {
        try
        {
            if (_selectedReason is null)
            {
                throw new InvalidOperationException("Selecione um motivo para abrir a solicitacao.");
            }

            if (_cboSector.SelectedValue is not int sectorId || sectorId <= 0)
            {
                throw new InvalidOperationException("Selecione o setor.");
            }

            if (_cboWorkArea.SelectedValue is not int workAreaId || workAreaId <= 0)
            {
                throw new InvalidOperationException("Selecione a area.");
            }

            if (_cboEquipment.SelectedValue is not int equipmentId || equipmentId <= 0)
            {
                throw new InvalidOperationException("Selecione o equipamento.");
            }

            int? machineId = _selectedReason.RequiresMachine && _cboMachine.SelectedValue is int machineValue && machineValue > 0
                ? machineValue
                : null;

            var request = await _requestService.CreateAsync(new CreateAssistanceRequestCommand
            {
                RequestedByFjCode = _txtFjCode.Text,
                SectorId = sectorId,
                WorkAreaId = workAreaId,
                EquipmentId = equipmentId,
                ReasonId = _selectedReason.Id,
                MachineId = machineId
            });

            SaveProfile(showFeedback: false);
            _lblStatus.Text = $"Solicitacao {request.TicketNumber} enviada com sucesso.";
            _lblStatus.ForeColor = Color.FromArgb(22, 101, 52);

            _loadingSelections = true;
            try
            {
                _cboEquipment.SelectedIndex = 0;
            }
            finally
            {
                _loadingSelections = false;
            }

            _selectedReason = null;
            UpdateReasonHeader();
            RenderReasonButtons();
        }
        catch (Exception ex)
        {
            _lblStatus.Text = ex.Message;
            _lblStatus.ForeColor = Color.FromArgb(185, 28, 28);
        }
    }

    private void CboSectorChanged(object? sender, EventArgs e)
    {
        if (_loadingSelections)
        {
            return;
        }

        UpdateWorkAreas();
        UpdateEquipments();
        UpdateMachineOptions();
        _selectedReason = null;
        RenderReasonButtons();
        UpdateReasonHeader();
    }

    private void CboEquipmentChanged(object? sender, EventArgs e)
    {
        if (_loadingSelections)
        {
            return;
        }

        _selectedReason = null;
        UpdateReasonHeader();
        RenderReasonButtons();
    }

    private void CboWorkAreaChanged(object? sender, EventArgs e)
    {
        if (_loadingSelections)
        {
            return;
        }

        UpdateMachineOptions();
    }

    private static Panel BuildField(string title, string description, Control input)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10, 0, 10, 0),
            Margin = new Padding(0)
        };

        var titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 26,
            Font = new Font("Segoe UI Semibold", 11.5F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(15, 23, 42)
        };

        var descriptionLabel = new Label
        {
            Text = description,
            Dock = DockStyle.Top,
            Height = 24,
            ForeColor = Color.FromArgb(100, 116, 139)
        };

        input.Dock = DockStyle.Top;
        input.Height = 46;
        input.Margin = new Padding(0);

        panel.Controls.Add(input);
        panel.Controls.Add(descriptionLabel);
        panel.Controls.Add(titleLabel);
        return panel;
    }

    private static void ConfigureCombo(ComboBox comboBox, EventHandler? onChanged)
    {
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
        comboBox.Dock = DockStyle.Top;
        comboBox.Height = 46;
        if (onChanged is not null)
        {
            comboBox.SelectedValueChanged += onChanged;
        }
    }

    private static Panel BuildCard()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(0),
            Padding = new Padding(18)
        };
    }

    private static Panel BuildCenterLabelPanel(Label label)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 6, 8, 6)
        };
        panel.Controls.Add(label);
        return panel;
    }

    private static Control BuildSectionTitle(string title, string description)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0)
        };

        if (!string.IsNullOrWhiteSpace(description))
        {
            panel.Controls.Add(new Label
            {
                Text = description,
                Dock = DockStyle.Top,
                Height = 24,
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point)
            });
        }
        panel.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 28,
            ForeColor = Color.FromArgb(15, 23, 42),
            Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold, GraphicsUnit.Point)
        });

        return panel;
    }

    private static Panel BuildInfoCard(string title, Label contentLabel)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(16, 12, 16, 12),
            Margin = new Padding(0, 0, 0, 0)
        };

        panel.Controls.Add(contentLabel);
        panel.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 24,
            ForeColor = Color.FromArgb(30, 41, 59),
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point)
        });

        return panel;
    }

    private static TableLayoutPanel BuildActionColumn(TableLayoutPanel layout, Control machinePanel, Control actionPanel)
    {
        layout.Dock = DockStyle.Fill;
        layout.RowCount = 2;
        layout.Margin = new Padding(0);
        layout.Padding = new Padding(0);
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 56));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 44));
        layout.Controls.Add(machinePanel, 0, 0);
        layout.Controls.Add(actionPanel, 0, 1);
        return layout;
    }
}
