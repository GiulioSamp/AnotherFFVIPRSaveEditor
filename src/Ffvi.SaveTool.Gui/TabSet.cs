namespace Ffvi.SaveTool.Gui;

// Reusable tab system built from a Panel strip of buttons and a content Panel that swaps
// the active tab's child panel. Replaces the broken WinForms TabControl on Win11 themes.
// Used at both top level and nested inside the Characters/Skills tabs.
public class TabSet
{
    public Panel Strip { get; }
    public Panel Content { get; }
    public IReadOnlyDictionary<string, Panel> Panels => _panels;
    public IReadOnlyDictionary<string, Button> Buttons => _buttons;

    private readonly Dictionary<string, Panel> _panels = new();
    private readonly Dictionary<string, Button> _buttons = new();
    private readonly int _buttonWidth;
    private readonly int _buttonHeight;
    private readonly int _buttonTop;
    private readonly int _buttonSpacing;
    private readonly float _fontSize;

    public TabSet(int stripHeight, int buttonWidth, int buttonHeight, int buttonTop, int buttonSpacing, float fontSize)
    {
        Strip = new Panel { Dock = DockStyle.Top, Height = stripHeight, BackColor = SystemColors.Control };
        Content = new Panel { Dock = DockStyle.Fill };
        _buttonWidth = buttonWidth;
        _buttonHeight = buttonHeight;
        _buttonTop = buttonTop;
        _buttonSpacing = buttonSpacing;
        _fontSize = fontSize;
    }

    public void AddTab(string name, Panel panel)
    {
        var btn = new Button
        {
            Text = name,
            Width = _buttonWidth,
            Height = _buttonHeight,
            FlatStyle = FlatStyle.Flat,
            AutoSize = false,
            Font = new Font("Segoe UI", _fontSize),
            Left = _buttons.Count * (_buttonWidth + _buttonSpacing) + 4,
            Top = _buttonTop,
            TextAlign = ContentAlignment.MiddleCenter,
            UseVisualStyleBackColor = true,
        };
        btn.FlatAppearance.BorderColor = SystemColors.ControlDark;
        btn.FlatAppearance.BorderSize = 1;
        btn.Click += (_, _) => Show(name);
        Strip.Controls.Add(btn);
        _buttons[name] = btn;

        panel.Dock = DockStyle.Fill;
        panel.Visible = false;
        _panels[name] = panel;
        Content.Controls.Add(panel);
    }

    public void Show(string name)
    {
        foreach (var (k, p) in _panels) p.Visible = (k == name);
        foreach (var (k, b) in _buttons) b.Font = new Font(b.Font, (k == name) ? FontStyle.Bold : FontStyle.Regular);
    }

    public void SetEnabled(bool enabled)
    {
        Content.Enabled = enabled;
        foreach (var b in _buttons.Values) b.Enabled = enabled;
    }

    // Attaches Strip + Content to a parent. Strip on top, Content fills. Order matters
    // because Dock=Fill must be added before Dock=Top in WinForms for the layout to be right.
    public void Mount(Control parent)
    {
        parent.Controls.Add(Content);
        parent.Controls.Add(Strip);
    }

    // Returns a fresh Panel containing Strip + Content arranged. For use as a child
    // of another TabSet (i.e. nested sub-tabs).
    public Panel BuildPanel()
    {
        var page = new Panel { Dock = DockStyle.Fill };
        Mount(page);
        return page;
    }
}
