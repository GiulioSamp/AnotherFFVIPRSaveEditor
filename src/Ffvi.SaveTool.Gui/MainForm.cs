using Ffvi.SaveTool;
using Ffvi.SaveTool.Data;

namespace Ffvi.SaveTool.Gui;

public class MainForm : Form
{
    private SaveFile? _save;
    private Character? _selectedCharacter;
    private bool _suppressEvents;

    private readonly ListBox _characterList = new() { Dock = DockStyle.Fill, IntegralHeight = true };
    private readonly TabSet _topTabs = new(stripHeight: 60, buttonWidth: 110, buttonHeight: 50, buttonTop: 5, buttonSpacing: 4, fontSize: 10F);
    private readonly TabSet _charSubTabs = new(stripHeight: 40, buttonWidth: 100, buttonHeight: 30, buttonTop: 5, buttonSpacing: 3, fontSize: 9F);
    private readonly TabSet _skillsSubTabs = new(stripHeight: 40, buttonWidth: 100, buttonHeight: 30, buttonTop: 5, buttonSpacing: 3, fontSize: 9F);
    private readonly Dictionary<int, ComboBox> _commandCombos = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _statusLabel = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };

    private readonly NumericUpDown _gilBox = NumBox(0, 9_999_999);
    private readonly NumericUpDown _totalGilBox = NumBox(0, 99_999_999);
    private readonly NumericUpDown _stepsBox = NumBox(0, 99_999_999);

    private readonly Dictionary<string, NumericUpDown> _statBoxes = new();
    private readonly Dictionary<string, (Label baseLbl, NumericUpDown totalBox, Func<RawStats, int> baseFn)> _totalStats = new();
    private readonly CheckedListBox _spellList = new() { Dock = DockStyle.Fill, CheckOnClick = true };
    private readonly ToolTip _tooltip = new() { AutoPopDelay = 12000, InitialDelay = 400, ReshowDelay = 200, ShowAlways = true };
    private readonly DataGridView _itemsGrid = new()
    {
        Dock = DockStyle.Fill,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2,
    };
    private readonly Dictionary<int, ComboBox> _equipCombos = new();
    private SplitContainer? _split;
    private readonly CheckedListBox _esperOwnedList = new() { Dock = DockStyle.Fill, CheckOnClick = true };
    private readonly ComboBox _equippedEsperCombo = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
        Margin = new Padding(6),
    };
    private readonly Label _veldtCountLabel = new()
    {
        AutoSize = true,
        Margin = new Padding(10, 4, 10, 4),
        Font = new Font("Segoe UI", 10F),
    };
    private readonly CheckedListBox _veldtList = new() { Dock = DockStyle.Fill, CheckOnClick = true };
    private readonly TextBox _veldtFilter = new() { Dock = DockStyle.Fill, PlaceholderText = "Filter formations..." };

    public MainForm()
    {
        Text = "FFVI Pixel Remaster — Save Editor";
        Width = 1100;
        Height = 720;
        StartPosition = FormStartPosition.CenterScreen;

        BuildMenu();
        BuildStatus();
        BuildLayout();

        _characterList.SelectedIndexChanged += (_, _) => OnCharacterSelected();
        _gilBox.ValueChanged += OnGilChanged;
        _totalGilBox.ValueChanged += (_, _) => { if (!_suppressEvents && _save is not null) _save.UserData.TotalGil = (int)_totalGilBox.Value; };
        _stepsBox.ValueChanged += (_, _) => { if (!_suppressEvents && _save is not null) _save.UserData.Steps = (int)_stepsBox.Value; };
        _spellList.ItemCheck += OnSpellChecked;

        SetEnabled(false);
        _statusLabel.Text = "Open a save file to begin (File → Open).";

        // WinForms resets SplitContainer.SplitterDistance during initial layout
        // when the control is constructed via field initializer — force it on Load.
        Load += (_, _) => { if (_split is not null) _split.SplitterDistance = 340; };
    }

    private void BuildMenu()
    {
        var menu = new MenuStrip();
        var fileMenu = new ToolStripMenuItem("&File");
        var openItem = new ToolStripMenuItem("&Open...", null, (_, _) => OnOpen()) { ShortcutKeys = Keys.Control | Keys.O };
        var saveItem = new ToolStripMenuItem("&Save", null, (_, _) => OnSave()) { ShortcutKeys = Keys.Control | Keys.S };
        var saveAsItem = new ToolStripMenuItem("Save &As...", null, (_, _) => OnSaveAs());
        var exitItem = new ToolStripMenuItem("E&xit", null, (_, _) => Close());
        fileMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            openItem, saveItem, saveAsItem, new ToolStripSeparator(), exitItem,
        });
        menu.Items.Add(fileMenu);
        MainMenuStrip = menu;
        Controls.Add(menu);
    }

    private void BuildStatus()
    {
        _status.Items.Add(_statusLabel);
        _status.Dock = DockStyle.Bottom;
        Controls.Add(_status);
    }

    private void BuildLayout()
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            Panel1MinSize = 260,
            SplitterDistance = 340,
        };
        _split = split;

        var leftLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(6) };
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var partyGroup = new GroupBox { Text = "Party", Dock = DockStyle.Fill };
        var partyGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(8) };
        partyGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        partyGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        partyGrid.Controls.Add(new Label { Text = "Gil:", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 0);
        partyGrid.Controls.Add(_gilBox, 1, 0);
        partyGrid.Controls.Add(new Label { Text = "Total Gil:", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 1);
        partyGrid.Controls.Add(_totalGilBox, 1, 1);
        partyGrid.Controls.Add(new Label { Text = "Steps:", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 2);
        partyGrid.Controls.Add(_stepsBox, 1, 2);
        partyGroup.Controls.Add(partyGrid);

        var charGroup = new GroupBox { Text = "Characters", Dock = DockStyle.Fill };
        charGroup.Controls.Add(_characterList);

        leftLayout.Controls.Add(partyGroup, 0, 0);
        leftLayout.Controls.Add(charGroup, 0, 1);
        split.Panel1.Controls.Add(leftLayout);

        _charSubTabs.AddTab("Stats", BuildStatsTab());
        _charSubTabs.AddTab("Magic", BuildSpellsTab());
        _charSubTabs.AddTab("Equipment", BuildEquipmentTab());
        _charSubTabs.AddTab("Commands", BuildCommandsTab());
        _charSubTabs.Show("Stats");

        _skillsSubTabs.AddTab("Rages", BuildSkillTab(new SkillTabState
        {
            Name = "Rages", OwnerCharacterName = "Gau",
            FirstId = Rages.FirstId, LastId = Rages.LastId, Offset = Rages.ContentIdOffset,
            Items = Rages.All.Select(r => (r.Id, r.Name)).ToList(),
        }));
        _skillsSubTabs.AddTab("Bushido", BuildSkillTab(new SkillTabState
        {
            Name = "Bushido", OwnerCharacterName = "Cyan",
            FirstId = Bushido.FirstId, LastId = Bushido.LastId, Offset = Bushido.ContentIdOffset,
            Items = Bushido.All.Select(b => (b.Id, b.Name)).ToList(),
        }));
        _skillsSubTabs.AddTab("Lore", BuildSkillTab(new SkillTabState
        {
            Name = "Lore", OwnerCharacterName = "Strago",
            FirstId = Lores.FirstId, LastId = Lores.LastId, Offset = Lores.ContentIdOffset,
            Items = Lores.All.Select(l => (l.Id, l.Name)).ToList(),
        }));
        _skillsSubTabs.AddTab("Blitz", BuildSkillTab(new SkillTabState
        {
            Name = "Blitz", OwnerCharacterName = "Sabin",
            FirstId = Blitzes.FirstId, LastId = Blitzes.LastId, Offset = Blitzes.ContentIdOffset,
            Items = Blitzes.All.Select(b => (b.Id, b.Name)).ToList(),
        }));
        _skillsSubTabs.Show("Rages");

        _topTabs.AddTab("Characters", _charSubTabs.BuildPanel());
        _topTabs.AddTab("Inventory", BuildItemsTab());
        _topTabs.AddTab("Skills", _skillsSubTabs.BuildPanel());
        _topTabs.AddTab("Espers", BuildEspersTab());
        _topTabs.AddTab("Veldt", BuildVeldtTab());
        _topTabs.Mount(split.Panel2);
        _topTabs.Show("Characters");

        Controls.Add(split);
    }

    private Panel BuildStatsTab()
    {
        var page = new Panel { Dock = DockStyle.Fill };
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(10),
        };

        stack.Controls.Add(new Label
        {
            Text = "Note: the in-game status screen also adds equipment modifiers (weapons, relics, armor) on top of "
                 + "what you set here. \"Total\" below = class base + permanent bonus only. If a character wears a "
                 + "+5 Evasion relic, the game will show 5 more Evasion than this editor.",
            AutoSize = true,
            MaximumSize = new Size(680, 0),
            ForeColor = SystemColors.GrayText,
            Font = new Font(Font, FontStyle.Italic),
            Margin = new Padding(4, 0, 4, 10),
        });

        stack.Controls.Add(BuildStatGroup("Vitals (level-dependent — base computed by game)",
        [
            ("Current HP",  "CurrentHp",       0, 99999, "Current health. Party members are capped at 9999 in-game."),
            ("+ Max HP",    "AdditionalMaxHp", 0, 99999, "Bonus added to base Max HP. Total Max HP = base (from level) + this. Level-based base isn't shown here yet."),
            ("Current MP",  "CurrentMp",       0, 99999, "Current MP. Party members are capped at 999 in-game. Will be clamped to Max MP on load."),
            ("+ Max MP",    "AdditionalMaxMp", 0,  9999, "Bonus added to base Max MP. Total Max MP = base (from level) + this. Level-based base isn't shown here yet."),
            ("+ Level",     "AdditionalLevel", 0,    99, "Bonus level added on top of the base. Levels factor into damage and level-dependent spells (Lv.5 Death, Lv.4 Flare, etc.)."),
        ]));

        stack.Controls.Add(BuildTotalStatGroup("Core Stats (edit Total)",
        [
            ("Strength",      "AdditionalPower",            s => s.Strength,     0, 255, "Physical damage stat. Effective cap is 128 even though it can be raised higher. Doubled and added to Attack in the damage formula."),
            ("Stamina",       "AdditionalVitality",         s => s.Stamina,      0, 255, "Resists Death attacks. Increases Regen heal, Poison/Sap damage taken, Tintinnabulum step-healing."),
            ("Speed",         "AdditionalAgility",          s => s.Speed,        0, 255, "Fills the ATB gauge faster. +20 baseline plus Haste/Slow effects."),
            ("Magic",         "AdditionalMagic",            s => s.Magic,        0, 255, "Magic Power. Increases magical damage. No 128 cap, unlike Strength. Sabin's Blitzes use this too."),
        ]));

        stack.Controls.Add(BuildTotalStatGroup("Combat (edit Total)",
        [
            ("Attack",        "AdditionalAttack",           s => s.Attack,       0, 255, "Battle Power (weapon-based). Added to (Strength x 2) for physical damage. Normally only changed by weapons."),
            ("Defense",       "AdditionalDefence",          s => s.Defense,      0, 255, "Reduces physical damage. Formula: damage * (255 - Defense) / 256 + 1."),
            ("Magic Defense", "AdditionalMagicDefense",     s => s.MagicDefense, 0, 255, "Reduces magical damage. Same formula as Defense."),
            ("Evasion",       "AdditionalEvasionRate",      s => s.Evasion,      0, 255, "Physical block %. Block value = (255 - Evasion x 2) + 1."),
            ("Magic Evasion", "AdditionalMagicEvasionRate", s => s.MagicEvasion, 0, 255, "Magic block %. Same formula as Evasion."),
        ]));

        stack.Controls.Add(BuildStatGroup("Bonus-only (no documented base)",
        [
            ("+ Hit Rate",      "AdditionalAccuracyRate", 0, 255, "Accuracy. Reduces miss chance. Mainly an enemy-side stat in vanilla mechanics."),
            ("+ Critical Rate", "AdditionalCriticalRate", 0, 255, "Critical hit chance."),
            ("+ Luck",          "AdditionalLuck",         0, 255, "Not documented as a real stat in Pixel Remaster. Present in the save data but likely vestigial."),
            ("+ Intelligence",  "AdditionalIntelligence", 0, 255, "Not documented as a real stat in Pixel Remaster. Present in the save data but likely vestigial."),
            ("+ Spirit",        "AdditionalSpirit",       0, 255, "Not documented as a real stat in Pixel Remaster. Present in the save data but likely vestigial."),
        ]));

        scroll.Controls.Add(stack);
        page.Controls.Add(scroll);
        return page;
    }

    private GroupBox BuildStatGroup(string title, (string label, string propName, int min, int max, string description)[] stats)
    {
        var group = new GroupBox
        {
            Text = title,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 0, 10),
            MinimumSize = new Size(600, 0),
        };
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 4,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };
        for (var i = 0; i < 4; i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        foreach (var (label, propName, min, max, description) in stats)
            AddStatRow(grid, label, propName, min, max, description);

        group.Controls.Add(grid);
        return group;
    }

    private GroupBox BuildTotalStatGroup(string title, (string label, string propName, Func<RawStats, int> baseFn, int min, int max, string description)[] stats)
    {
        var group = new GroupBox
        {
            Text = title,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 0, 10),
            MinimumSize = new Size(600, 0),
        };
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 3,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));

        // Header row
        grid.Controls.Add(new Label { Text = "Stat", AutoSize = true, Font = new Font(Font, FontStyle.Bold), Margin = new Padding(5) });
        grid.Controls.Add(new Label { Text = "Base", AutoSize = true, Font = new Font(Font, FontStyle.Bold), ForeColor = SystemColors.GrayText, Margin = new Padding(5) });
        grid.Controls.Add(new Label { Text = "Total", AutoSize = true, Font = new Font(Font, FontStyle.Bold), Margin = new Padding(5) });

        foreach (var (label, propName, baseFn, min, max, description) in stats)
            AddTotalStatRow(grid, label, propName, baseFn, min, max, description);

        group.Controls.Add(grid);
        return group;
    }

    private void AddTotalStatRow(TableLayoutPanel grid, string label, string propName,
        Func<RawStats, int> baseFn, int min, int max, string description)
    {
        var labelCtl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(5) };
        var baseLabel = new Label { Text = "—", AutoSize = true, ForeColor = SystemColors.GrayText, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(5) };
        var totalBox = NumBox(min, max);
        totalBox.Margin = new Padding(5);
        totalBox.Width = 90;

        totalBox.ValueChanged += (_, _) =>
        {
            if (_suppressEvents || _selectedCharacter is null) return;
            var bs = CharacterBaseStats.For(_selectedCharacter.Name);
            var baseVal = bs is null ? 0 : baseFn(bs);
            var total = (int)totalBox.Value;
            var prop = typeof(CharacterStats).GetProperty(propName);
            // Never write a negative bonus: the game only ever stores additive bonuses in
            // the addtional* fields and rejects saves containing negative values.
            prop?.SetValue(_selectedCharacter.Stats, Math.Max(0, total - baseVal));
        };

        _tooltip.SetToolTip(labelCtl, description);
        _tooltip.SetToolTip(totalBox, description);
        _totalStats[propName] = (baseLabel, totalBox, baseFn);
        grid.Controls.Add(labelCtl);
        grid.Controls.Add(baseLabel);
        grid.Controls.Add(totalBox);
    }

    private void AddStatRow(TableLayoutPanel grid, string label, string propName, int min, int max, string? description = null)
    {
        var labelCtl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Right, TextAlign = ContentAlignment.MiddleRight, Width = 120, Margin = new Padding(5) };
        var box = NumBox(min, max);
        box.Margin = new Padding(5);
        box.Width = 90;
        box.ValueChanged += (_, _) =>
        {
            if (_suppressEvents || _selectedCharacter is null) return;
            var prop = typeof(CharacterStats).GetProperty(propName);
            prop?.SetValue(_selectedCharacter.Stats, (int)box.Value);
        };
        if (!string.IsNullOrEmpty(description))
        {
            _tooltip.SetToolTip(labelCtl, description);
            _tooltip.SetToolTip(box, description);
        }
        _statBoxes[propName] = box;
        grid.Controls.Add(labelCtl);
        grid.Controls.Add(box);
    }

    private Panel BuildSpellsTab()
    {
        var page = new Panel { Dock = DockStyle.Fill };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(6) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var allBtn = new Button { Text = "Learn All", AutoSize = true };
        var noneBtn = new Button { Text = "Forget All", AutoSize = true };
        allBtn.Click += (_, _) => SetAllSpells(true);
        noneBtn.Click += (_, _) => SetAllSpells(false);
        btnPanel.Controls.AddRange(new Control[] { allBtn, noneBtn });

        foreach (var s in Spells.All)
            _spellList.Items.Add($"{s.Id,3}  {s.Name}");

        layout.Controls.Add(btnPanel, 0, 0);
        layout.Controls.Add(_spellList, 0, 1);
        page.Controls.Add(layout);
        return page;
    }

    private void SetAllSpells(bool learned)
    {
        if (_selectedCharacter is null) return;
        _suppressEvents = true;
        for (var i = 0; i < _spellList.Items.Count; i++)
        {
            _spellList.SetItemChecked(i, learned);
            var spell = Spells.All[i];
            if (learned) _selectedCharacter.Abilities.LearnSpell(spell.Id);
            else _selectedCharacter.Abilities.ForgetSpell(spell.Id);
        }
        _suppressEvents = false;
    }

    private Panel BuildItemsTab()
    {
        var page = new Panel { Dock = DockStyle.Fill };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(6) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var newEntryBtn = new Button { Text = "New Entry", AutoSize = true };
        var removeBtn = new Button { Text = "Remove Selected", AutoSize = true };
        var maxBtn = new Button { Text = "Max all to 99", AutoSize = true };
        newEntryBtn.Click += (_, _) => AddNewInventoryEntry();
        removeBtn.Click += (_, _) => RemoveInventoryStack();
        maxBtn.Click += (_, _) => MaxAllInventory();
        btnPanel.Controls.AddRange(new Control[] { newEntryBtn, removeBtn, maxBtn });

        var itemCol = new DataGridViewComboBoxColumn
        {
            HeaderText = "Item",
            DataSource = ItemDropdownEntries(),
            DisplayMember = "Display",
            ValueMember = "Id",
            FlatStyle = FlatStyle.Flat,
        };
        var countCol = new DataGridViewTextBoxColumn { HeaderText = "Count", ValueType = typeof(int) };
        _itemsGrid.Columns.Add(itemCol);
        _itemsGrid.Columns.Add(countCol);
        _itemsGrid.CellValueChanged += OnInventoryCellChanged;
        _itemsGrid.DataError += (s, e) => { e.ThrowException = false; e.Cancel = true; };

        layout.Controls.Add(btnPanel, 0, 0);
        layout.Controls.Add(_itemsGrid, 0, 1);
        page.Controls.Add(layout);
        return page;
    }

    private Panel BuildEspersTab()
    {
        var page = new Panel { Dock = DockStyle.Fill };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10),
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var allBtn = new Button { Text = "Own All", AutoSize = true };
        var noneBtn = new Button { Text = "Own None", AutoSize = true };
        allBtn.Click += (_, _) => SetAllEspers(true);
        noneBtn.Click += (_, _) => SetAllEspers(false);
        btnPanel.Controls.AddRange(new Control[] { allBtn, noneBtn });

        foreach (var e in Ffvi.SaveTool.Data.Espers.All)
            _esperOwnedList.Items.Add($"{e.Id,3}  {e.Name}");
        _esperOwnedList.ItemCheck += OnEsperOwnedChecked;

        var equippedRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
        };
        equippedRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        equippedRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var equippedLabel = new Label
        {
            Text = "Equipped on selected character:",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(6, 12, 6, 6),
        };
        var equippedEntries = new List<ItemRow> { new(0, "(none)") };
        equippedEntries.AddRange(Ffvi.SaveTool.Data.Espers.All.Select(e => new ItemRow(e.Id, e.Name)));
        _equippedEsperCombo.DataSource = equippedEntries;
        _equippedEsperCombo.DisplayMember = "Display";
        _equippedEsperCombo.ValueMember = "Id";
        _equippedEsperCombo.SelectedValueChanged += (_, _) =>
        {
            if (_suppressEvents || _selectedCharacter is null || _equippedEsperCombo.SelectedValue is not int id) return;
            _selectedCharacter.EquippedEsperId = id;
        };
        equippedRow.Controls.Add(equippedLabel);
        equippedRow.Controls.Add(_equippedEsperCombo);

        layout.Controls.Add(btnPanel, 0, 0);
        layout.Controls.Add(_esperOwnedList, 0, 1);
        layout.Controls.Add(equippedRow, 0, 2);
        page.Controls.Add(layout);
        return page;
    }

    private void SetAllEspers(bool owned)
    {
        if (_save is null) return;
        _suppressEvents = true;
        for (var i = 0; i < _esperOwnedList.Items.Count; i++)
        {
            _esperOwnedList.SetItemChecked(i, owned);
            var id = Ffvi.SaveTool.Data.Espers.All[i].Id;
            if (owned) _save.UserData.OwnedEsperIds.Add(id);
            else _save.UserData.OwnedEsperIds.Remove(id);
        }
        _suppressEvents = false;
    }

    private void OnEsperOwnedChecked(object? sender, ItemCheckEventArgs e)
    {
        if (_suppressEvents || _save is null) return;
        var id = Ffvi.SaveTool.Data.Espers.All[e.Index].Id;
        if (e.NewValue == CheckState.Checked) _save.UserData.OwnedEsperIds.Add(id);
        else _save.UserData.OwnedEsperIds.Remove(id);
    }

    // Commands that are safe to set on any character regardless of class.
    private static readonly HashSet<int> UniversalCommandIds = new() { 4, 1, 2, 3, 5 };

    private Panel BuildCommandsTab()
    {
        var page = new Panel { Dock = DockStyle.Fill };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 11,
            Padding = new Padding(20),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var explain = new Label
        {
            Text = "The dropdown is restricted to commands this character already had at load, plus universal-safe commands "
                 + "(Attack, Defend, Items, Row, [none]). Assigning a cross-class command (e.g. Sabin's Blitz to Terra) "
                 + "would otherwise corrupt the save.",
            AutoSize = true,
            MaximumSize = new Size(600, 0),
            Font = new Font(Font, FontStyle.Italic),
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(4, 0, 4, 12),
        };
        layout.Controls.Add(explain, 0, 0);
        layout.SetColumnSpan(explain, 2);

        var resetBtn = new Button
        {
            Text = "Reset to original",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(4, 0, 4, 8),
        };
        resetBtn.Click += (_, _) =>
        {
            if (_selectedCharacter is null) return;
            _selectedCharacter.Commands.ResetToOriginal();
            RefreshCommands();
        };
        layout.Controls.Add(resetBtn, 0, 1);
        layout.SetColumnSpan(resetBtn, 2);

        for (var slot = 0; slot < 8; slot++)
        {
            var slotIndex = slot;
            var lbl = new Label
            {
                Text = $"Slot {slot + 1}",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(4, 8, 4, 4),
            };
            var combo = new ComboBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = "Display",
                ValueMember = "Id",
                Margin = new Padding(4),
            };
            combo.SelectedValueChanged += (_, _) =>
            {
                if (_suppressEvents || _selectedCharacter is null || combo.SelectedValue is not int id) return;
                if (slotIndex < _selectedCharacter.Commands.Slots.Count)
                    _selectedCharacter.Commands.Slots[slotIndex] = id;
            };
            _commandCombos[slotIndex] = combo;
            layout.Controls.Add(lbl, 0, slot + 2);
            layout.Controls.Add(combo, 1, slot + 2);
        }

        page.Controls.Add(layout);
        return page;
    }

    private record CommandRow(int Id, string Display);

    private static List<CommandRow> AllowedCommandsFor(Character c)
    {
        var allowed = new HashSet<int>(UniversalCommandIds);
        foreach (var id in c.Commands.OriginalSlots) allowed.Add(id);
        return Commands.All
            .Where(cmd => allowed.Contains(cmd.Id))
            .OrderBy(cmd => cmd.Id == Commands.NoneId ? 0 : 1)
            .ThenBy(cmd => cmd.Name)
            .Select(cmd => new CommandRow(cmd.Id, $"{cmd.Name} ({cmd.Id})"))
            .ToList();
    }

    private void RefreshCommands()
    {
        if (_selectedCharacter is null) return;
        _suppressEvents = true;
        // Rebuild the allowed-commands list for the current character so each slot's dropdown
        // only offers safe choices.
        var entries = AllowedCommandsFor(_selectedCharacter);
        for (var i = 0; i < 8; i++)
        {
            if (!_commandCombos.TryGetValue(i, out var combo)) continue;
            combo.DataSource = new List<CommandRow>(entries);
            combo.DisplayMember = "Display";
            combo.ValueMember = "Id";

            if (i >= _selectedCharacter.Commands.Slots.Count)
            {
                combo.SelectedValue = Commands.NoneId;
                continue;
            }
            try { combo.SelectedValue = _selectedCharacter.Commands.Slots[i]; }
            catch { /* current value not in allowed list — keep first option */ }
        }
        _suppressEvents = false;
    }

    // Generic per-character "skill" tab. Used for Rages (Gau), Bushido (Cyan),
    // Lores (Strago), Blitzes (Sabin), and potentially Dance (Mog) later.
    // Each tab is independent state — they all share the same builder.
    private class SkillTabState
    {
        public string Name { get; init; } = "";
        public string OwnerCharacterName { get; init; } = "";
        public int FirstId { get; init; }
        public int LastId { get; init; }
        public int Offset { get; init; }
        public IReadOnlyList<(int Id, string Name)> Items { get; init; } = Array.Empty<(int, string)>();
        public CheckedListBox List { get; } = new() { Dock = DockStyle.Fill, CheckOnClick = true };
        public Label Header { get; } = new()
        {
            AutoSize = true,
            Margin = new Padding(10, 10, 10, 6),
            Font = new Font("Segoe UI", 10F),
        };
    }

    private readonly Dictionary<string, SkillTabState> _skillTabs = new();

    private Panel BuildSkillTab(SkillTabState s)
    {
        var page = new Panel { Dock = DockStyle.Fill };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(6),
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var learnAll = new Button { Text = "Learn All", AutoSize = true };
        var forgetAll = new Button { Text = "Forget All", AutoSize = true };
        learnAll.Click += (_, _) => SetAllSkill(s, true);
        forgetAll.Click += (_, _) => SetAllSkill(s, false);
        btnPanel.Controls.AddRange(new Control[] { learnAll, forgetAll });

        foreach (var (id, name) in s.Items)
            s.List.Items.Add($"{id,4}  {name}");
        s.List.ItemCheck += (_, e) => OnSkillItemChecked(s, e);

        layout.Controls.Add(s.Header, 0, 0);
        layout.Controls.Add(btnPanel, 0, 1);
        layout.Controls.Add(s.List, 0, 2);
        page.Controls.Add(layout);

        _skillTabs[s.Name] = s;
        return page;
    }

    private Character? GetSkillOwner(string name) =>
        _save?.UserData.Characters.FirstOrDefault(c => c.Name == name);

    private void SetAllSkill(SkillTabState s, bool learned)
    {
        var owner = GetSkillOwner(s.OwnerCharacterName);
        if (owner is null) return;
        _suppressEvents = true;
        for (var i = 0; i < s.List.Items.Count; i++)
        {
            s.List.SetItemChecked(i, learned);
            var id = s.Items[i].Id;
            if (learned) owner.Abilities.LearnSkill(id, s.Offset);
            else owner.Abilities.ForgetSkill(id);
        }
        _suppressEvents = false;
        RefreshSkill(s);
    }

    private void OnSkillItemChecked(SkillTabState s, ItemCheckEventArgs e)
    {
        if (_suppressEvents) return;
        var owner = GetSkillOwner(s.OwnerCharacterName);
        if (owner is null) { e.NewValue = e.CurrentValue; return; }
        var id = s.Items[e.Index].Id;
        if (e.NewValue == CheckState.Checked) owner.Abilities.LearnSkill(id, s.Offset);
        else owner.Abilities.ForgetSkill(id);
    }

    private void RefreshSkill(SkillTabState s)
    {
        var owner = GetSkillOwner(s.OwnerCharacterName);
        _suppressEvents = true;
        if (owner is null)
        {
            s.Header.Text = $"{s.Name} is {s.OwnerCharacterName}'s skill. {s.OwnerCharacterName} isn't in this save yet.";
            s.List.Enabled = false;
            for (var i = 0; i < s.List.Items.Count; i++) s.List.SetItemChecked(i, false);
        }
        else
        {
            s.List.Enabled = true;
            var learned = new HashSet<int>(
                owner.Abilities.LearnedSkillsInRange(s.FirstId, s.LastId).Select(a => a.AbilityId));
            for (var i = 0; i < s.List.Items.Count; i++)
                s.List.SetItemChecked(i, learned.Contains(s.Items[i].Id));
            s.Header.Text = $"{owner.Name}'s {s.Name}: {learned.Count} / {s.Items.Count} learned.";
        }
        _suppressEvents = false;
    }

    private void RefreshAllSkills()
    {
        foreach (var s in _skillTabs.Values) RefreshSkill(s);
    }

    private Panel BuildVeldtTab()
    {
        var page = new Panel { Dock = DockStyle.Fill };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(10),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var filterLabel = new Label { Text = "Filter:", AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(4, 8, 4, 4) };
        _veldtFilter.TextChanged += (_, _) => ApplyVeldtFilter();

        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var markBtn = new Button { Text = "Mark visible as seen", AutoSize = true, Margin = new Padding(0, 4, 8, 0) };
        var clearBtn = new Button { Text = "Clear visible", AutoSize = true, Margin = new Padding(0, 4, 8, 0) };
        markBtn.Click += (_, _) => SetVisibleVeldt(true);
        clearBtn.Click += (_, _) => SetVisibleVeldt(false);
        btnPanel.Controls.Add(markBtn);
        btnPanel.Controls.Add(clearBtn);
        btnPanel.Controls.Add(_veldtCountLabel);

        _veldtList.ItemCheck += OnVeldtItemCheck;

        layout.Controls.Add(filterLabel, 0, 0);
        layout.Controls.Add(_veldtFilter, 1, 0);
        layout.Controls.Add(btnPanel, 0, 1);
        layout.SetColumnSpan(btnPanel, 2);
        layout.Controls.Add(_veldtList, 0, 2);
        layout.SetColumnSpan(_veldtList, 2);
        page.Controls.Add(layout);
        return page;
    }

    // Map list-row index (which may be a filtered subset) back to the underlying encounter index.
    private readonly List<int> _veldtVisibleIndices = new();

    private void RefreshVeldt()
    {
        if (_save?.Veldt is null)
        {
            _veldtCountLabel.Text = "No Veldt data in this save.";
            _veldtList.Items.Clear();
            _veldtVisibleIndices.Clear();
            return;
        }
        ApplyVeldtFilter();
    }

    private void ApplyVeldtFilter()
    {
        if (_save?.Veldt is null) return;
        _suppressEvents = true;
        _veldtList.Items.Clear();
        _veldtVisibleIndices.Clear();
        var filter = _veldtFilter.Text?.Trim() ?? "";
        for (var i = 0; i < _save.Veldt.Encounters.Count; i++)
        {
            var name = VeldtFormations.NameFor(i);
            if (filter.Length > 0 && !name.Contains(filter, StringComparison.OrdinalIgnoreCase)) continue;
            _veldtList.Items.Add($"{i,3}  {name}", _save.Veldt.Encounters[i]);
            _veldtVisibleIndices.Add(i);
        }
        _veldtCountLabel.Text = $"Seen: {_save.Veldt.SeenCount} / {_save.Veldt.TotalCount}";
        _suppressEvents = false;
    }

    private void OnVeldtItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (_suppressEvents || _save?.Veldt is null) return;
        if (e.Index < 0 || e.Index >= _veldtVisibleIndices.Count) return;
        var underlyingIndex = _veldtVisibleIndices[e.Index];
        _save.Veldt.Encounters[underlyingIndex] = (e.NewValue == CheckState.Checked);
        _veldtCountLabel.Text = $"Seen: {_save.Veldt.SeenCount} / {_save.Veldt.TotalCount}";
    }

    private void SetVisibleVeldt(bool seen)
    {
        if (_save?.Veldt is null) return;
        // Acts on whatever rows are currently visible. With an empty filter that's everything;
        // with a filter typed in, only the matching formations are affected.
        foreach (var idx in _veldtVisibleIndices)
            _save.Veldt.Encounters[idx] = seen;
        ApplyVeldtFilter();
    }

    private void RefreshEspers()
    {
        if (_save is null) return;
        _suppressEvents = true;
        for (var i = 0; i < _esperOwnedList.Items.Count; i++)
            _esperOwnedList.SetItemChecked(i, _save.UserData.OwnedEsperIds.Contains(Ffvi.SaveTool.Data.Espers.All[i].Id));
        if (_selectedCharacter is not null)
        {
            // Fall back to "(none)" for ids outside our table — leaving the combo on the previous
            // character's esper would be misleading (and writing it back would be wrong).
            var equippedId = _selectedCharacter.EquippedEsperId;
            _equippedEsperCombo.SelectedValue =
                (equippedId == 0 || Ffvi.SaveTool.Data.Espers.ById(equippedId) is not null) ? equippedId : 0;
        }
        _suppressEvents = false;
    }

    private Panel BuildEquipmentTab()
    {
        var page = new Panel { Dock = DockStyle.Fill };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(20),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 6; i++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        AddEquipSlot(layout, "Weapon",  Equipment.WeaponKey,  ItemCategory.Weapon, Equipment.EmptyWeaponShieldId);
        AddEquipSlot(layout, "Shield",  Equipment.ShieldKey,  ItemCategory.Shield, Equipment.EmptyWeaponShieldId);
        AddEquipSlot(layout, "Helmet",  Equipment.HelmetKey,  ItemCategory.Helmet, Equipment.EmptyHelmetId);
        AddEquipSlot(layout, "Armor",   Equipment.ArmorKey,   ItemCategory.Armor,  Equipment.EmptyArmorId);
        AddEquipSlot(layout, "Relic 1", Equipment.Relic1Key,  ItemCategory.Relic,  Equipment.EmptyRelicId);
        AddEquipSlot(layout, "Relic 2", Equipment.Relic2Key,  ItemCategory.Relic,  Equipment.EmptyRelicId);

        page.Controls.Add(layout);
        return page;
    }

    private void AddEquipSlot(TableLayoutPanel grid, string label, int slotKey, ItemCategory category, int emptyId)
    {
        var lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(6) };
        var combo = new ComboBox
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            DropDownStyle = ComboBoxStyle.DropDownList,
            DataSource = EquipDropdownEntries(category, emptyId),
            DisplayMember = "Display",
            ValueMember = "Id",
            Margin = new Padding(6),
        };
        combo.SelectedValueChanged += (_, _) =>
        {
            if (_suppressEvents || _selectedCharacter is null || _save is null || combo.SelectedValue is not int id) return;
            _selectedCharacter.Equipment.SetSlot(slotKey, id);
            // The game validates equipped items against the inventory: if a slot points to an item
            // that isn't owned, the game unequips it on load. So we make sure the new item is in inventory.
            if (!IsEmptyPlaceholder(id)) EnsureInInventory(_save.UserData.NormalInventory, id);
            RefreshInventoryGrid();
        };
        _equipCombos[slotKey] = combo;
        grid.Controls.Add(lbl);
        grid.Controls.Add(combo);
    }

    private static IList<ItemRow> ItemDropdownEntries()
    {
        // Include EmptyPlaceholder slots — the game uses these to represent unfilled
        // equipment-slot stacks (IDs 93, 197, 198, 199, 200). Skip only the truly-invalid id=0.
        return Items.Normal
            .Where(i => i.Category is not ItemCategory.Empty)
            .OrderBy(i => i.Category == ItemCategory.EmptyPlaceholder ? 1 : 0)
            .ThenBy(i => i.Category.ToString())
            .ThenBy(i => i.Name)
            .Select(i => new ItemRow(i.Id,
                i.Category == ItemCategory.EmptyPlaceholder
                    ? i.Name
                    : $"[{i.Category}] {i.Name}"))
            .ToList();
    }

    private static IList<ItemRow> EquipDropdownEntries(ItemCategory category, int emptyId)
    {
        var list = new List<ItemRow> { new(emptyId, "(empty)") };
        list.AddRange(Items.Normal
            .Where(i => i.Category == category)
            .OrderBy(i => i.Name)
            .Select(i => new ItemRow(i.Id, i.Name)));
        return list;
    }

    private void OnInventoryCellChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (_suppressEvents || _save is null || e.RowIndex < 0) return;
        var row = _itemsGrid.Rows[e.RowIndex];
        var idObj = row.Cells[0].Value;
        var countObj = row.Cells[1].Value;
        if (idObj is not int id) return;
        var count = countObj is int c ? c : (int.TryParse(countObj?.ToString(), out var p) ? p : 0);
        count = Math.Clamp(count, 0, Inventory.MaxStackCount);
        var inv = _save.UserData.NormalInventory;
        inv.Set(e.RowIndex, id, count);

        // If the user picked an item that already exists in another row, merge immediately —
        // duplicate contentId entries corrupt the save (the game's equipment-count validation
        // reads per-item totals).
        if (inv.Stacks.Count(s => s.ItemId == id) > 1)
        {
            inv.MergeDuplicates();
            RefreshInventoryGrid();
            SelectInventoryRow(id);
        }
    }

    // Default new entries to Potion (id=2) — a universally-usable item, so the user can
    // immediately verify the row works. They then change the item via the dropdown.
    private const int DefaultNewItemId = 2;

    private void AddNewInventoryEntry()
    {
        if (_save is null) return;
        // Add() merges into an existing stack instead of creating a duplicate row.
        _save.UserData.NormalInventory.Add(DefaultNewItemId, 1);
        RefreshInventoryGrid();
        SelectInventoryRow(DefaultNewItemId);
    }

    private void SelectInventoryRow(int itemId)
    {
        if (_save is null) return;
        var idx = _save.UserData.NormalInventory.Stacks.FindIndex(s => s.ItemId == itemId);
        if (idx < 0 || idx >= _itemsGrid.Rows.Count) return;
        _itemsGrid.ClearSelection();
        _itemsGrid.Rows[idx].Selected = true;
        _itemsGrid.CurrentCell = _itemsGrid.Rows[idx].Cells[0];
    }

    private void RemoveInventoryStack()
    {
        if (_save is null || _itemsGrid.CurrentRow is null) return;
        var idx = _itemsGrid.CurrentRow.Index;
        if (idx < 0 || idx >= _save.UserData.NormalInventory.Stacks.Count) return;
        _save.UserData.NormalInventory.RemoveAt(idx);
        RefreshInventoryGrid();
    }

    private void MaxAllInventory()
    {
        if (_save is null) return;
        var inv = _save.UserData.NormalInventory;
        for (var i = 0; i < inv.Stacks.Count; i++)
        {
            var s = inv.Stacks[i];
            if (s.ItemId > 0 && s.ItemId is not 93 and not 197 and not 198 and not 199 and not 200)
                inv.Set(i, s.ItemId, Inventory.MaxStackCount);
        }
        RefreshInventoryGrid();
    }

    private static bool IsEmptyPlaceholder(int id) => id is 93 or 197 or 198 or 199 or 200;

    private static void EnsureInInventory(Inventory inv, int itemId)
    {
        if (inv.Stacks.Any(s => s.ItemId == itemId)) return;
        inv.Stacks.Add(new ItemStack(itemId, 1));
    }

    private void RefreshInventoryGrid()
    {
        if (_save is null) return;
        _suppressEvents = true;
        _itemsGrid.Rows.Clear();
        foreach (var stack in _save.UserData.NormalInventory.Stacks)
            _itemsGrid.Rows.Add(stack.ItemId, stack.Count);
        _suppressEvents = false;
    }

    private void RefreshEquipment()
    {
        if (_selectedCharacter is null) return;
        _suppressEvents = true;
        foreach (var (slotKey, combo) in _equipCombos)
        {
            try { combo.SelectedValue = _selectedCharacter.Equipment.GetSlot(slotKey); }
            catch { /* item id not in dropdown for this slot type — ignore */ }
        }
        _suppressEvents = false;
    }

    private record ItemRow(int Id, string Display);

    private void OnSpellChecked(object? sender, ItemCheckEventArgs e)
    {
        if (_suppressEvents || _selectedCharacter is null) return;
        var spell = Spells.All[e.Index];
        if (e.NewValue == CheckState.Checked) _selectedCharacter.Abilities.LearnSpell(spell.Id);
        else _selectedCharacter.Abilities.ForgetSpell(spell.Id);
    }

    // Total Gil only goes up in-game (it's a cumulative lifetime total). Mirror that here:
    // when the user increases Gil, bump Total Gil by the same delta; when they decrease, leave it.
    // User can still manually edit Total Gil directly.
    private void OnGilChanged(object? sender, EventArgs e)
    {
        if (_suppressEvents || _save is null) return;
        var oldGil = _save.UserData.Gil;
        var newGil = (int)_gilBox.Value;
        _save.UserData.Gil = newGil;
        if (newGil > oldGil)
        {
            _save.UserData.TotalGil += newGil - oldGil;
            _suppressEvents = true;
            _totalGilBox.Value = Math.Clamp(_save.UserData.TotalGil, _totalGilBox.Minimum, _totalGilBox.Maximum);
            _suppressEvents = false;
        }
    }

    private void OnOpen()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Open FFVI PR save file",
            InitialDirectory = SaveFile.DefaultSaveDirectory(),
            Filter = "All files|*",
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            _save = SaveFile.Load(dlg.FileName);
            if (!_save.IsSlotFile())
            {
                MessageBox.Show(this, "This file isn't a character save slot (no character data). Try a larger file in the same folder.", "Not a slot save", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _save = null;
                _statusLabel.Text = "Open a save file to begin.";
                SetEnabled(false);
                return;
            }
            PopulateUi();
            SetEnabled(true);
            _statusLabel.Text = $"Loaded: {Path.GetFileName(dlg.FileName)}  |  slot id={_save.SlotId}  |  {_save.UserData.Characters.Count} characters";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to load save:\n{ex.Message}", "Load error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // NumericUpDown.ValueChanged does NOT fire while the user is typing — only on arrow
    // clicks or focus loss. A Ctrl+S menu shortcut doesn't move focus, so a typed-but-not-
    // committed value would silently miss the save ("my edits didn't stick"). Same for an
    // in-progress DataGridView cell edit. Flush both before writing.
    private void CommitPendingEdits()
    {
        Validate();             // forces the focused control to validate its typed text
        _itemsGrid.EndEdit();   // commits any in-progress inventory cell edit
    }

    private void OnSave()
    {
        if (_save is null) return;
        CommitPendingEdits();
        try
        {
            _save.Save();
            _statusLabel.Text = $"Saved: {Path.GetFileName(_save.Path)}  |  {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to save:\n{ex.Message}", "Save error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnSaveAs()
    {
        if (_save is null) return;
        using var dlg = new SaveFileDialog
        {
            Title = "Save as",
            InitialDirectory = Path.GetDirectoryName(_save.Path),
            FileName = Path.GetFileName(_save.Path),
            Filter = "All files|*",
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        CommitPendingEdits();
        try
        {
            _save.Save(dlg.FileName);
            _statusLabel.Text = $"Saved as: {Path.GetFileName(dlg.FileName)}  |  {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to save:\n{ex.Message}", "Save error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void PopulateUi()
    {
        if (_save is null) return;
        _suppressEvents = true;
        _gilBox.Value = Math.Clamp(_save.UserData.Gil, (int)_gilBox.Minimum, (int)_gilBox.Maximum);
        _totalGilBox.Value = Math.Clamp(_save.UserData.TotalGil, (int)_totalGilBox.Minimum, (int)_totalGilBox.Maximum);
        _stepsBox.Value = Math.Clamp(_save.UserData.Steps, (int)_stepsBox.Minimum, (int)_stepsBox.Maximum);

        _characterList.Items.Clear();
        foreach (var c in _save.UserData.Characters)
            _characterList.Items.Add($"{c.Id,2}  {c.Name}");
        if (_characterList.Items.Count > 0) _characterList.SelectedIndex = 0;
        _suppressEvents = false;
        RefreshInventoryGrid();
        RefreshEspers();
        RefreshVeldt();
        RefreshAllSkills();
    }

    private void OnCharacterSelected()
    {
        if (_save is null) return;
        var idx = _characterList.SelectedIndex;
        if (idx < 0 || idx >= _save.UserData.Characters.Count) { _selectedCharacter = null; return; }
        _selectedCharacter = _save.UserData.Characters[idx];

        _suppressEvents = true;
        foreach (var kv in _statBoxes)
        {
            var prop = typeof(CharacterStats).GetProperty(kv.Key);
            if (prop is null) continue;
            var value = (int)(prop.GetValue(_selectedCharacter.Stats) ?? 0);
            kv.Value.Value = Math.Clamp(value, (int)kv.Value.Minimum, (int)kv.Value.Maximum);
        }

        var bs = CharacterBaseStats.For(_selectedCharacter.Name);
        foreach (var (propName, (baseLbl, totalBox, baseFn)) in _totalStats)
        {
            var baseVal = bs is null ? 0 : baseFn(bs);
            var prop = typeof(CharacterStats).GetProperty(propName);
            var bonus = prop is null ? 0 : (int)(prop.GetValue(_selectedCharacter.Stats) ?? 0);
            baseLbl.Text = bs is null ? "—" : baseVal.ToString();
            // Floor the box at the base value so the user physically can't request a total
            // below base (which would imply a negative bonus the game rejects).
            totalBox.Minimum = baseVal;
            totalBox.Value = Math.Clamp(baseVal + bonus, (int)totalBox.Minimum, (int)totalBox.Maximum);
        }

        var learned = new HashSet<int>(
            _selectedCharacter.Abilities.LearnedMagic().Select(a => a.AbilityId));
        for (var i = 0; i < _spellList.Items.Count; i++)
            _spellList.SetItemChecked(i, learned.Contains(Spells.All[i].Id));
        _suppressEvents = false;
        RefreshEquipment();
        RefreshEspers();
        RefreshCommands();
    }

    private void SetEnabled(bool enabled)
    {
        _gilBox.Enabled = _totalGilBox.Enabled = _stepsBox.Enabled = enabled;
        _topTabs.SetEnabled(enabled);
        _characterList.Enabled = enabled;
    }

    private static NumericUpDown NumBox(int min, int max)
    {
        var box = new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Width = 110,
            Anchor = AnchorStyles.Left,
        };
        // WinForms default: mouse-wheel over a NumericUpDown changes its value by +/-1
        // per tick. That hijacks the wheel from scrolling the surrounding panel and
        // silently mutates stat values whenever someone scrolls past. Suppress it.
        box.MouseWheel += (s, e) =>
        {
            if (e is HandledMouseEventArgs h) h.Handled = true;
        };
        return box;
    }

    private static Label ComingSoonLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter,
        ForeColor = Color.Gray,
    };
}
