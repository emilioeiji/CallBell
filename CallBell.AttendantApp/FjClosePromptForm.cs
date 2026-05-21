using CallBell.Core.Entities;
using CallBell.Core.Validation;

namespace CallBell.AttendantApp;

public sealed class FjClosePromptForm : Form
{
    private readonly TextBox _txtFjCode = new();

    public FjClosePromptForm(AssistanceRequest request)
    {
        Text = "Confirmar fechamento";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(520, 270);
        MinimumSize = new Size(520, 270);
        BackColor = Color.White;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            Padding = new Padding(22, 20, 22, 18)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(new Label
        {
            Text = $"Fechar {request.TicketNumber} - {request.WorkAreaNamePt}",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(15, 23, 42),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        root.Controls.Add(new Label
        {
            Text = "Informe o FJ de quem realizou o atendimento",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(71, 85, 105),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 1);

        _txtFjCode.Dock = DockStyle.Fill;
        _txtFjCode.CharacterCasing = CharacterCasing.Upper;
        _txtFjCode.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point);
        _txtFjCode.Margin = new Padding(0, 0, 0, 4);
        root.Controls.Add(_txtFjCode, 0, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 10, 0, 0)
        };

        var btnOk = new Button
        {
            Text = "Confirmar",
            Width = 120,
            Height = 36,
            DialogResult = DialogResult.None,
            BackColor = Color.FromArgb(22, 163, 74),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += (_, _) => Confirm();

        var btnCancel = new Button
        {
            Text = "Cancelar",
            Width = 120,
            Height = 36,
            DialogResult = DialogResult.Cancel
        };
        btnCancel.Margin = new Padding(0, 0, 10, 0);

        buttons.Controls.Add(btnOk);
        buttons.Controls.Add(btnCancel);
        root.Controls.Add(buttons, 0, 3);

        AcceptButton = btnOk;
        CancelButton = btnCancel;

        Shown += (_, _) => _txtFjCode.Focus();
    }

    public string EnteredFjCode => _txtFjCode.Text.Trim().ToUpperInvariant();

    private void Confirm()
    {
        if (!CallBell.Core.Validation.FjCode.IsValid(EnteredFjCode))
        {
            MessageBox.Show(this, "Informe um FJ valido no formato FJ12345.", "CallBell", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
