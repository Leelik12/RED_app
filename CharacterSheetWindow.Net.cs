using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace CyberpunkRED_Generator
{
    // вкладка «5: НЕТРАННИНГ»: интерфейс, кибердека, программы, броски в сети
    public partial class CharacterSheetWindow
    {
        private const string NetRoleName = "Нетраннер";

        private static Brush NetCyan => Theme.Accent();
        private static Brush NetRed => Theme.Danger();
        private static Brush NetGray => Theme.Get("Muted");
        private static Brush NetWhite => Theme.Get("Text");
        private static Brush NetYellow => Theme.Get("Warn");
        private static Brush NetDark => Theme.Get("Panel");

        private SheetViewModel _netVm;
        private NetrunData _net;
        private TextBlock _netInterfaceText, _netActionsText, _netSlotsText, _netReqText, _netRollLog, _netAbilityDesc;
        private StackPanel _netProgramList, _netHardwareList;
        private ComboBox _netActionCombo;
        private TextBox _netMod, _netTarget, _netSlotsBox;
        private CheckBox _netTargetRolls;
        private readonly List<string> _netLog = new List<string>();
        private readonly List<KeyValuePair<string, NetProgram>> _netRollOptions = new List<KeyValuePair<string, NetProgram>>();
        private bool _netTabHooked;

        public int NetInterfaceRank => _netVm?.GetRoleRank(NetRoleName) ?? 0;

        private static Border NetPanel(string title, UIElement content)
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var head = new Border { Background = NetRed, HorizontalAlignment = HorizontalAlignment.Left, Padding = new Thickness(10, 3, 10, 3), Margin = new Thickness(0, 0, 0, 6) };
            head.Child = new TextBlock { Text = title, Foreground = Theme.Get("OnAccent"), FontWeight = FontWeights.Bold, FontSize = 13 };
            Grid.SetRow(content, 1);
            grid.Children.Add(head); grid.Children.Add(content);
            return new Border { BorderBrush = NetRed, BorderThickness = new Thickness(2), Background = NetDark, Padding = new Thickness(10), Child = grid };
        }

        private static TextBox NetBox(double width, TextAlignment align = TextAlignment.Left)
            => NetBoxCore(width, align);

        private static TextBox NetBoxCore(double width, TextAlignment align)
        {
            var tb = new TextBox { Background = Theme.Get("Back"), Foreground = NetCyan, BorderBrush = Theme.Get("Line"), CaretBrush = NetCyan, Padding = new Thickness(3), TextAlignment = align, VerticalContentAlignment = VerticalAlignment.Center };
            if (width > 0) tb.Width = width;
            return tb;
        }

        private static TextBox NetNumBox(int value, Action<int> set, double width = 34)
        {
            var tb = NetBox(width, TextAlignment.Center);
            tb.Text = value.ToString();
            tb.TextChanged += (s, e) => { if (int.TryParse(tb.Text, out int v)) set(v); };
            return tb;
        }

        // ---------- построение вкладки ----------

        private void BuildNetTab()
        {
            _netVm = DataContext as SheetViewModel;
            if (_netVm == null || NetTab == null) return;
            if (_netVm.Net == null) _netVm.Net = new NetrunData();
            _net = _netVm.Net;
            _netLog.Clear();

            if (!_netTabHooked)
            {
                _netTabHooked = true;
                SheetTabControl.SelectionChanged += (s, e) => { if (e.OriginalSource == SheetTabControl && NetTab.IsSelected) RefreshNet(); };
            }
            _net.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(NetrunData.DeckSlots) && _netSlotsBox != null && _netSlotsBox.Text != _net.DeckSlots.ToString())
                    _netSlotsBox.Text = _net.DeckSlots.ToString();
            };
            _net.Programs.CollectionChanged += (s, e) => { RebuildNetPrograms(); RefreshNet(); };
            _net.Hardware.CollectionChanged += (s, e) => { RebuildNetHardware(); RefreshNet(); };

            var root = new Grid { Margin = new Thickness(0, 10, 0, 0) };
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(500) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(470) });

            var left = new Grid();
            left.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            left.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) });
            left.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var deckPanel = NetPanel("ИНТЕРФЕЙС И КИБЕРДЕКА", BuildNetDeckPanel());
            var rollPanel = NetPanel("БРОСКИ В СЕТИ", BuildNetRollPanel());
            Grid.SetRow(deckPanel, 0); Grid.SetRow(rollPanel, 2);
            left.Children.Add(deckPanel); left.Children.Add(rollPanel);

            var center = NetPanel("ПРОГРАММЫ И ЧЁРНЫЙ ЛЁД НА ДЕКЕ", BuildNetProgramsPanel());
            Grid.SetColumn(center, 2);

            var right = new Grid();
            right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) });
            right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var hw = NetPanel("ЖЕЛЕЗО ДЕКИ", BuildNetHardwarePanel());
            var notes = NetPanel("ЗАМЕТКИ НЕТРАННЕРА", BuildNetNotesPanel());
            Grid.SetRow(hw, 0); Grid.SetRow(notes, 2);
            right.Children.Add(hw); right.Children.Add(notes);
            Grid.SetColumn(right, 4);

            root.Children.Add(left); root.Children.Add(center); root.Children.Add(right);
            NetTab.Content = root;

            RebuildNetPrograms();
            RebuildNetHardware();
            RefreshNet();
        }

        private UIElement BuildNetDeckPanel()
        {
            var p = new StackPanel();
            _netInterfaceText = MakeText("", NetCyan, 22, FontWeights.Bold);
            _netActionsText = MakeText("", NetYellow, 16, FontWeights.Bold);
            _netReqText = MakeText("", NetGray, 12);
            _netReqText.TextWrapping = TextWrapping.Wrap;
            p.Children.Add(_netInterfaceText);
            p.Children.Add(_netActionsText);
            p.Children.Add(_netReqText);

            var deckRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 4) };
            deckRow.Children.Add(MakeText("КИБЕРДЕКА: ", NetRed, 13, FontWeights.Bold));
            var deckName = NetBox(250);
            deckName.SetBinding(TextBox.TextProperty, new Binding("DeckName") { Source = _net, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            deckRow.Children.Add(deckName);
            var pick = MakeButton("ВЫБРАТЬ", NetCyan); pick.Margin = new Thickness(8, 0, 0, 0);
            pick.Click += (s, e) => ShowNetCatalog(0);
            deckRow.Children.Add(pick);
            p.Children.Add(deckRow);

            var slotRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            slotRow.Children.Add(MakeText("СЛОТЫ ДЕКИ: ", NetRed, 13, FontWeights.Bold));
            var slots = NetNumBox(_net.DeckSlots, v => { _net.DeckSlots = v; RefreshNet(); }, 40);
            slots.ToolTip = "5 — низкое качество, 7 — обычная, 9 — отличная";
            _netSlotsBox = slots;
            slotRow.Children.Add(slots);
            _netSlotsText = MakeText("", NetWhite, 14, FontWeights.Bold); _netSlotsText.Margin = new Thickness(12, 0, 0, 0);
            slotRow.Children.Add(_netSlotsText);
            p.Children.Add(slotRow);

            var arm = new CheckBox { Content = "Дека в киберруке (+1 слот)", Foreground = NetWhite, Margin = new Thickness(0, 2, 0, 0) };
            arm.SetBinding(CheckBox.IsCheckedProperty, new Binding("HasDeckArm") { Source = _net });
            arm.Checked += (s, e) => RefreshNet(); arm.Unchecked += (s, e) => RefreshNet();
            var suit = new CheckBox { Content = "Костюм Bodyweight (+1 слот только для железа)", Foreground = NetWhite, Margin = new Thickness(0, 2, 0, 0) };
            suit.SetBinding(CheckBox.IsCheckedProperty, new Binding("HasBodyweight") { Source = _net });
            suit.Checked += (s, e) => RefreshNet(); suit.Unchecked += (s, e) => RefreshNet();
            p.Children.Add(arm); p.Children.Add(suit);

            var reset = MakeButton("НОВЫЙ НЕТРАН: ВОССТАНОВИТЬ ЦЕЛ, ВЫКЛЮЧИТЬ ВСЁ", NetYellow);
            reset.Margin = new Thickness(0, 10, 0, 0); reset.HorizontalAlignment = HorizontalAlignment.Left;
            reset.ToolTip = "Перед новой попыткой: все программы возвращаются к полной ЦЕЛ и деактивируются";
            reset.Click += (s, e) =>
            {
                foreach (var pr in _net.Programs) { pr.RezCurrent = pr.RezMax; pr.Active = false; }
                RebuildNetPrograms(); CommitChange();
            };
            p.Children.Add(reset);
            return p;
        }

        private UIElement BuildNetRollPanel()
        {
            var p = new StackPanel();
            _netActionCombo = new ComboBox { Margin = new Thickness(0, 0, 0, 6) };
            _netActionCombo.SelectionChanged += (s, e) => UpdateNetAbilityDesc();
            p.Children.Add(MakeText("ДЕЙСТВИЕ:", NetRed, 13, FontWeights.Bold));
            p.Children.Add(_netActionCombo);

            _netAbilityDesc = MakeText("", NetGray, 12);
            _netAbilityDesc.TextWrapping = TextWrapping.Wrap; _netAbilityDesc.Margin = new Thickness(0, 0, 0, 8);
            p.Children.Add(_netAbilityDesc);

            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
            row.Children.Add(MakeText("МОДИФИКАТОР: ", NetGray, 12));
            _netMod = NetBox(40, TextAlignment.Center); _netMod.Text = "0"; row.Children.Add(_netMod);
            row.Children.Add(MakeText("   СЛ / ЗАЩИТА ЦЕЛИ: ", NetGray, 12));
            _netTarget = NetBox(50, TextAlignment.Center); row.Children.Add(_netTarget);
            p.Children.Add(row);

            _netTargetRolls = new CheckBox { Content = "цель тоже бросает 1d10 (Интерфейс/ЗАЩ + 1d10)", Foreground = NetWhite, Margin = new Thickness(0, 0, 0, 8) };
            p.Children.Add(_netTargetRolls);

            var btn = MakeButton("🎲 БРОСИТЬ: ИНТЕРФЕЙС + 1d10", NetCyan);
            btn.HorizontalAlignment = HorizontalAlignment.Left;
            btn.Click += (s, e) => NetRoll();
            p.Children.Add(btn);

            _netRollLog = new TextBlock { Foreground = NetWhite, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0) };
            p.Children.Add(new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 330, Content = _netRollLog });
            return p;
        }

        private UIElement BuildNetProgramsPanel()
        {
            var g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var head = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            string[] titles = { "ВКЛ", "НАЗВАНИЕ", "КЛАСС", "АТК", "ЗАЩ", "ЦЕЛ", "СЛОТ", "" };
            double[] widths = { 40, 0, 190, 40, 40, 90, 40, 56 };
            for (int i = 0; i < titles.Length; i++)
            {
                head.ColumnDefinitions.Add(new ColumnDefinition { Width = widths[i] == 0 ? new GridLength(1, GridUnitType.Star) : new GridLength(widths[i]) });
                var t = MakeText(titles[i], NetGray, 11, FontWeights.Bold);
                t.HorizontalAlignment = i == 1 ? HorizontalAlignment.Left : HorizontalAlignment.Center;
                Grid.SetColumn(t, i); head.Children.Add(t);
            }
            Grid.SetRow(head, 0);

            _netProgramList = new StackPanel();
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _netProgramList };
            Grid.SetRow(scroll, 1);

            var btns = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            var add = MakeButton("+ ПРОГРАММА ИЗ КАТАЛОГА", NetCyan); add.Margin = new Thickness(0, 0, 8, 0);
            add.Click += (s, e) => ShowNetCatalog(1);
            var own = MakeButton("+ СВОЯ ПРОГРАММА", NetYellow);
            own.Click += (s, e) => { _net.Programs.Add(new NetProgram { Name = "Новая программа", Class = NetCatalog.ClsBoost, RezMax = 7, RezCurrent = 7 }); CommitChange(); };
            btns.Children.Add(add); btns.Children.Add(own);
            Grid.SetRow(btns, 2);

            g.Children.Add(head); g.Children.Add(scroll); g.Children.Add(btns);
            return g;
        }

        private UIElement BuildNetHardwarePanel()
        {
            var g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _netHardwareList = new StackPanel();
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _netHardwareList };
            var btns = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            var add = MakeButton("+ ЖЕЛЕЗО ИЗ КАТАЛОГА", NetCyan); add.Margin = new Thickness(0, 0, 8, 0);
            add.Click += (s, e) => ShowNetCatalog(3);
            var own = MakeButton("+ СВОЁ", NetYellow);
            own.Click += (s, e) => { _net.Hardware.Add(new NetHardware { Name = "Новое железо", Slots = 1 }); CommitChange(); };
            btns.Children.Add(add); btns.Children.Add(own);
            Grid.SetRow(btns, 1);
            g.Children.Add(scroll); g.Children.Add(btns);
            return g;
        }

        private UIElement BuildNetNotesPanel()
        {
            var tb = new TextBox { Background = Theme.Get("Back"), Foreground = NetCyan, BorderBrush = Theme.Get("Line"), CaretBrush = NetCyan, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(4) };
            tb.SetBinding(TextBox.TextProperty, new Binding("Notes") { Source = _net, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            return tb;
        }

        // ---------- обновление ----------

        public void RefreshNet()
        {
            if (_net == null || _netInterfaceText == null) return;
            int rank = NetInterfaceRank;
            _netInterfaceText.Text = rank > 0 ? $"ИНТЕРФЕЙС: {rank}" : "ИНТЕРФЕЙС: нет (нужна роль «Нетраннер»)";
            _netInterfaceText.Foreground = rank > 0 ? NetCyan : NetRed;
            _netActionsText.Text = rank > 0 ? $"СЕТЕВЫХ ДЕЙСТВИЙ ЗА ХОД: {NetCatalog.NetActions(rank)}" : "";

            int used = _net.SlotsUsed, total = _net.SlotsTotal;
            _netSlotsText.Text = $"занято {used} / {total}";
            _netSlotsText.Foreground = used > total ? NetRed : NetWhite;

            bool Has(string n) => _netVm.CyberwareBlocks != null && _netVm.CyberwareBlocks.Any(b => b.InstalledItems.Any(i => i.Name == n));
            bool goggles = _netVm.GearItems != null && _netVm.GearItems.Any(g => (g.Name ?? "").IndexOf("Очки виртуальности", StringComparison.OrdinalIgnoreCase) >= 0);
            string Mark(bool b) => b ? "✔" : "✘";
            _netReqText.Text = $"Нейролинк {Mark(Has("Нейролинк"))}   Интерфейсные разъёмы {Mark(Has("Интерфейсные разъёмы"))}   Очки виртуальности {Mark(goggles)}  (без них нетраннинг невозможен или опасен)";

            UpdateNetRollOptions();
        }

        private void UpdateNetRollOptions()
        {
            string prev = _netActionCombo.SelectedItem as string;
            _netRollOptions.Clear();
            foreach (var a in NetCatalog.Abilities) _netRollOptions.Add(new KeyValuePair<string, NetProgram>(a.Name, null));
            foreach (var pr in _net.Programs.Where(x => x.IsAttacker)) _netRollOptions.Add(new KeyValuePair<string, NetProgram>("Атака: " + pr.Name, pr));
            _netActionCombo.Items.Clear();
            foreach (var o in _netRollOptions) _netActionCombo.Items.Add(o.Key);
            int idx = prev != null ? _netActionCombo.Items.IndexOf(prev) : -1;
            _netActionCombo.SelectedIndex = idx >= 0 ? idx : 0;
        }

        private void UpdateNetAbilityDesc()
        {
            int i = _netActionCombo.SelectedIndex;
            if (i < 0 || i >= _netRollOptions.Count) { _netAbilityDesc.Text = ""; return; }
            var o = _netRollOptions[i];
            if (o.Value == null)
            {
                var a = NetCatalog.Abilities.FirstOrDefault(x => x.Name == o.Key);
                _netAbilityDesc.Text = a == null ? "" : a.Description + (a.IsMeat ? " (мясное действие)" : " (сетевое действие)");
            }
            else _netAbilityDesc.Text = $"{o.Value.Class}. Интерфейс + АТК {o.Value.Atk} + 1d10 против Интерфейса цели + 1d10 (или ЗАЩ программы/ЧЛ + 1d10). {o.Value.Effect}";
        }

        // ---------- броски ----------

        private void NetRoll()
        {
            int i = _netActionCombo.SelectedIndex;
            if (i < 0 || i >= _netRollOptions.Count) return;
            var o = _netRollOptions[i];
            int iface = NetInterfaceRank;
            int atk = o.Value?.Atk ?? 0;
            int mod = UIHelpers.ParseIntOr(_netMod.Text, 0);

            int mine = DiceRoller.D10Check(out string d1);
            int total = iface + atk + mod + mine;
            string line = $"{o.Key}: Интерфейс {iface}" + (atk != 0 ? $" + АТК {atk}" : "") + (mod != 0 ? $" {(mod > 0 ? "+" : "−")} {Math.Abs(mod)}" : "") + $" + d10 [{d1}] = {total}";

            if (int.TryParse(_netTarget.Text, out int target))
            {
                int tv = target;
                if (_netTargetRolls.IsChecked == true)
                {
                    int tr = DiceRoller.D10Check(out string d2);
                    tv += tr;
                    line += $"  против {target} + d10 [{d2}] = {tv}";
                }
                else line += $"  против СЛ {tv}";
                line += total > tv ? "  → УСПЕХ" : "  → ПРОВАЛ (при ничьей побеждает цель)";
            }
            AppendNetLog(line);
        }

        private void AppendNetLog(string line)
        {
            _netLog.Insert(0, line);
            if (_netLog.Count > 12) _netLog.RemoveRange(12, _netLog.Count - 12);
            _netRollLog.Text = string.Join("\n\n", _netLog);
        }

        // ---------- списки ----------

        private void RebuildNetPrograms()
        {
            if (_netProgramList == null) return;
            _netProgramList.Children.Clear();
            foreach (var pr in _net.Programs.ToList())
            {
                var program = pr;
                var row = new Grid { Margin = new Thickness(0, 0, 0, 2) };
                double[] widths = { 40, 0, 190, 40, 40, 90, 40, 56 };
                foreach (var w in widths) row.ColumnDefinitions.Add(new ColumnDefinition { Width = w == 0 ? new GridLength(1, GridUnitType.Star) : new GridLength(w) });

                var active = new CheckBox { IsChecked = program.Active, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, ToolTip = "Программа запущена (активна)" };
                active.Checked += (s, e) => program.Active = true; active.Unchecked += (s, e) => program.Active = false;
                var name = NetBox(0); name.Text = program.Name; name.TextChanged += (s, e) => program.Name = name.Text; name.ToolTip = program.Effect;
                var cls = NetBox(0); cls.Text = program.Class; cls.TextChanged += (s, e) => program.Class = cls.Text; cls.FontSize = 11; cls.Margin = new Thickness(3, 0, 3, 0);
                var atk = NetNumBox(program.Atk, v => program.Atk = v);
                var def = NetNumBox(program.Def, v => program.Def = v);
                var rez = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
                var rc = NetNumBox(program.RezCurrent, v => program.RezCurrent = v, 34);
                var rm = NetNumBox(program.RezMax, v => program.RezMax = v, 34);
                rez.Children.Add(rc); rez.Children.Add(MakeText("/", NetGray, 13)); rez.Children.Add(rm);
                var slots = NetNumBox(program.Slots, v => { program.Slots = v; RefreshNet(); });
                var del = MakeButton("✕", NetRed); del.Padding = new Thickness(6, 1, 6, 1); del.ToolTip = "Удалить с деки";
                del.Click += (s, e) => { _net.Programs.Remove(program); CommitChange(); };
                var back = MakeButton("↺", NetYellow); back.Padding = new Thickness(6, 1, 6, 1); back.ToolTip = "Восстановить ЦЕЛ";
                back.Click += (s, e) => { program.RezCurrent = program.RezMax; rc.Text = program.RezCurrent.ToString(); };
                var ctl = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
                ctl.Children.Add(back); ctl.Children.Add(del);

                UIElement[] cells = { active, name, cls, atk, def, rez, slots, ctl };
                for (int c = 0; c < cells.Length; c++) { Grid.SetColumn(cells[c], c); row.Children.Add(cells[c]); }

                var box = new StackPanel();
                box.Children.Add(row);
                if (program.IsBlackIce || !string.IsNullOrEmpty(program.Effect))
                {
                    string extra = program.IsBlackIce ? $"ВСП {program.Per}  РЕА {program.Spd}.  " : "";
                    var eff = MakeText(extra + program.Effect, NetGray, 11);
                    eff.TextWrapping = TextWrapping.Wrap; eff.Margin = new Thickness(44, 0, 6, 0);
                    box.Children.Add(eff);
                }
                _netProgramList.Children.Add(new Border { BorderBrush = Theme.Get("Line"), BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(0, 3, 0, 5), Child = box });
            }
            if (_net.Programs.Count == 0) _netProgramList.Children.Add(MakeText("Дека пуста. Добавь программы из каталога или впиши свою.", NetGray, 13));
        }

        private void RebuildNetHardware()
        {
            if (_netHardwareList == null) return;
            _netHardwareList.Children.Clear();
            foreach (var h in _net.Hardware.ToList())
            {
                var hw = h;
                var row = new Grid { Margin = new Thickness(0, 0, 0, 2) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var name = NetBox(0); name.Text = hw.Name; name.TextChanged += (s, e) => hw.Name = name.Text; name.ToolTip = hw.Effect;
                var slots = NetNumBox(hw.Slots, v => { hw.Slots = v; RefreshNet(); }); slots.Margin = new Thickness(4, 0, 4, 0); slots.ToolTip = "Слотов";
                var del = MakeButton("✕", NetRed); del.Padding = new Thickness(6, 1, 6, 1);
                del.Click += (s, e) => { _net.Hardware.Remove(hw); CommitChange(); };
                Grid.SetColumn(slots, 1); Grid.SetColumn(del, 2);
                row.Children.Add(name); row.Children.Add(slots); row.Children.Add(del);
                var box = new StackPanel();
                box.Children.Add(row);
                if (!string.IsNullOrEmpty(hw.Effect)) { var eff = MakeText(hw.Effect, NetGray, 11); eff.TextWrapping = TextWrapping.Wrap; box.Children.Add(eff); }
                _netHardwareList.Children.Add(new Border { BorderBrush = Theme.Get("Line"), BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(0, 3, 0, 5), Child = box });
            }
            if (_net.Hardware.Count == 0) _netHardwareList.Children.Add(MakeText("Железа нет.", NetGray, 13));
        }

        // ---------- каталог нетраннинга ----------

        public static void InstallNetItem(NetrunData net, NetItemDef d)
        {
            switch (d.Kind)
            {
                case "Кибердека": net.DeckName = d.Name; net.DeckSlots = d.Slots; break;
                case "Железо": net.Hardware.Add(new NetHardware { Name = d.Name, Slots = d.Slots, Effect = d.Effect }); break;
                default: net.Programs.Add(NetCatalog.ToProgram(d)); break;
            }
        }

        private void ShowNetCatalog(int startTab)
        {
            var win = Ui.Dialog(this, "КАТАЛОГ НЕТРАННЕРА", 940, 660);

            int tab = startTab;
            NetItemDef selected = null;
            var lists = new List<List<NetItemDef>> { NetCatalog.Decks, NetCatalog.Programs, NetCatalog.BlackIce, NetCatalog.Hardware };

            var root = new Grid { Margin = new Thickness(14) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var tabs = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            var tabButtons = new[] { MakeButton("КИБЕРДЕКИ", NetCyan), MakeButton("ПРОГРАММЫ", NetCyan), MakeButton("ЧЁРНЫЙ ЛЁД", NetCyan), MakeButton("ЖЕЛЕЗО", NetCyan) };
            foreach (var b in tabButtons) { b.Margin = new Thickness(0, 0, 8, 0); tabs.Children.Add(b); }

            var body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
            var listBox = new ListBox { Background = NetDark, Foreground = NetWhite, BorderBrush = NetGray, Margin = new Thickness(0, 0, 10, 0), FontSize = 14 };
            var detail = new Border { BorderBrush = NetGray, BorderThickness = new Thickness(1), Background = NetDark, Padding = new Thickness(12) };
            var detailScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            detail.Child = detailScroll;
            Grid.SetColumn(detail, 1);
            body.Children.Add(listBox); body.Children.Add(detail);
            Grid.SetRow(body, 1);

            var footer = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var status = new TextBlock { Foreground = NetGray, FontSize = 12, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center,
                Text = "Установка программы или железа занимает час игрового времени. Своё — через кнопки «+ СВОЯ» на вкладке." };
            var close = MakeButton("ЗАКРЫТЬ", NetRed);
            Grid.SetColumn(close, 1);
            footer.Children.Add(status); footer.Children.Add(close);
            Grid.SetRow(footer, 2);
            close.Click += (s, e) => win.Close();

            root.Children.Add(tabs); root.Children.Add(body); root.Children.Add(footer);
            win.Content = root;
            Grid.SetRow(tabs, 0);

            Action showDetail = () =>
            {
                var p = new StackPanel();
                detailScroll.Content = p;
                if (selected == null) { p.Children.Add(MakeText("Выбери предмет слева.", NetGray, 14)); return; }
                p.Children.Add(MakeText(selected.Name, NetCyan, 18, FontWeights.Bold));
                string stats = selected.Kind == "Кибердека" ? $"Слотов: {selected.Slots}"
                    : selected.Kind == "Железо" ? $"Слотов: {selected.Slots}"
                    : selected.Kind == "Чёрный лёд" ? $"{selected.Class}\nВСП {selected.Per}  РЕА {selected.Spd}  АТК {selected.Atk}  ЗАЩ {selected.Def}  ЦЕЛ {selected.Rez}  Слотов: {selected.Slots}"
                    : $"{selected.Class}\nАТК {selected.Atk}  ЗАЩ {selected.Def}  ЦЕЛ {selected.Rez}";
                var st = MakeText(stats + "\nЦена: " + GearCatalog.CostText(selected.Cost) + (selected.Source != null ? "\nИсточник: " + selected.Source : ""), NetWhite, 14);
                st.TextWrapping = TextWrapping.Wrap; st.Margin = new Thickness(0, 6, 0, 6);
                p.Children.Add(st);
                var eff = MakeText(selected.Effect, NetGray, 13); eff.TextWrapping = TextWrapping.Wrap; p.Children.Add(eff);
                var add = MakeButton(selected.Kind == "Кибердека" ? "ВЗЯТЬ ЭТУ ДЕКУ" : "УСТАНОВИТЬ НА ДЕКУ", NetCyan);
                add.HorizontalAlignment = HorizontalAlignment.Left; add.Margin = new Thickness(0, 12, 0, 0);
                var def = selected;
                add.Click += (s, e) =>
                {
                    InstallNetItem(_net, def);
                    CommitChange();
                    RefreshNet();
                    string tail = _net.SlotsUsed > _net.SlotsTotal ? $"  ВНИМАНИЕ: слотов не хватает ({_net.SlotsUsed}/{_net.SlotsTotal})" : "";
                    status.Text = "Добавлено: " + def.Name + tail;
                    status.Foreground = _net.SlotsUsed > _net.SlotsTotal ? NetRed : NetCyan;
                };
                p.Children.Add(add);
            };

            Action fill = () =>
            {
                for (int i = 0; i < tabButtons.Length; i++)
                    tabButtons[i].Background = i == tab ? Theme.Get("Tint") : Theme.Get("Raised");
                listBox.Items.Clear();
                foreach (var d in lists[tab])
                    listBox.Items.Add(new ListBoxItem { Content = d.Name + "  —  " + GearCatalog.CostText(d.Cost) + (d.Source != null ? "  [" + d.Source + "]" : ""), Tag = d, Foreground = NetWhite });
                selected = null; showDetail();
            };
            listBox.SelectionChanged += (s, e) => { selected = (listBox.SelectedItem as ListBoxItem)?.Tag as NetItemDef; showDetail(); };
            for (int i = 0; i < tabButtons.Length; i++) { int idx = i; tabButtons[i].Click += (s, e) => { tab = idx; fill(); }; }
            fill();
            Ui.Present(win);
        }
    }
}
