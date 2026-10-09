using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CyberpunkRED_Generator
{
    // каталог оружия, брони и снаряжения: выбор из книги в одну строку листа.
    // Свои предметы по-прежнему вносятся вручную прямо в поля листа.
    public partial class CharacterSheetWindow
    {
        private void BtnCatalogWeapons_Click(object sender, RoutedEventArgs e) => OpenCatalog(0);
        private void BtnCatalogArmor_Click(object sender, RoutedEventArgs e) => OpenCatalog(1);
        private void BtnCatalogGear_Click(object sender, RoutedEventArgs e) => OpenCatalog(2);

        private const string CatalogTitle = "КАТАЛОГ СНАРЯЖЕНИЯ";
        private ToolWindow _catalogWindow;
        private Action<int> _catalogSetTab;

        // каталог открывается вкладкой в рабочей области листа; уже открытый переключается на нужный раздел
        private void OpenCatalog(int tab)
        {
            if (!(DataContext is SheetViewModel vm)) return;
            CloseOverlays();
            if (_catalogWindow != null && _catalogSetTab != null)
            {
                _catalogSetTab(tab);
                var stub = Ui.Dialog(this, CatalogTitle, 100, 100);
                stub.Content = new Grid();
                Ui.Present(stub);
                return;
            }
            ShowCatalogDialog(vm, tab);
        }

        // ---------- добавление на лист (используются и тестами) ----------

        public static string WeaponNotes(WeaponDef d, int quality, string magLabel)
        {
            var parts = new List<string> { d.Skill };
            if (d.Hands != "0") parts.Add(d.Hands == "зависит от типа" ? "рук: зависит от типа" : "рук: " + d.Hands);
            if (!string.IsNullOrEmpty(d.Alt)) parts.Add(d.Alt);
            if (!d.IsExotic) parts.Add(d.Concealable ? "можно скрыть" : "нельзя скрыть");
            if (quality < 0) parts.Add("низкое качество: при критическом провале (1) заклинивает");
            if (quality > 0) parts.Add("отличное качество: +1 к атаке");
            if (!string.IsNullOrEmpty(magLabel)) parts.Add(magLabel);
            if (!string.IsNullOrEmpty(d.Info)) parts.Add(d.Info);
            parts.Add(GearCatalog.CostText(WeaponCost(d, quality)));
            return string.Join("; ", parts);
        }

        public static int WeaponCost(WeaponDef d, int quality)
            => d.FixedQuality ? d.Cost : quality < 0 ? GearCatalog.PoorCost(d.Cost) : quality > 0 ? GearCatalog.ExcellentCost(d.Cost) : d.Cost;

        // quality: -1 низкое, 0 обычное, 1 отличное; mag: 0 стандартный, 1 расширенный, 2 барабан
        public static WeaponData BuildWeapon(WeaponDef d, int quality, int mag)
        {
            string model = d.IsMelee ? d.Standard : quality < 0 ? d.Poor : quality > 0 ? d.Excellent : d.Standard;
            string name = d.IsMelee ? d.Standard : d.Name;
            if (!d.IsExotic && !d.IsMelee && !string.IsNullOrEmpty(model)) name += " (" + model + ")";
            if (d.IsMelee && quality != 0) name += quality < 0 ? " (низк. кач.)" : " (отл. кач.)";
            if (d.IsExotic && string.IsNullOrEmpty(name)) name = d.Name;

            int ammo = d.Mag;
            string magLabel = null;
            if (d.Clips != null && mag > 0 && !d.IsExotic)
            {
                ammo = d.Clips[Math.Min(mag, 2)];
                magLabel = mag == 1 ? "расширенный магазин" : "барабанный магазин";
            }

            return new WeaponData
            {
                Name = name,
                Damage = d.Damage,
                AmmoMax = ammo > 0 ? ammo.ToString() : "",
                AmmoCurrent = ammo > 0 ? ammo.ToString() : "",
                AmmoTotal = "",
                Rof = d.Rof > 0 ? d.Rof.ToString() : "—",
                Notes = WeaponNotes(d, quality, magLabel)
            };
        }

        public static void PutWeapon(SheetViewModel vm, WeaponData w)
        {
            var slot = vm.Weapons.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Name) && string.IsNullOrWhiteSpace(x.Damage));
            if (slot == null) { vm.Weapons.Add(w); return; }
            slot.Name = w.Name; slot.Damage = w.Damage; slot.AmmoMax = w.AmmoMax; slot.AmmoCurrent = w.AmmoCurrent;
            slot.AmmoTotal = w.AmmoTotal; slot.Rof = w.Rof; slot.Notes = w.Notes;
        }

        // zone: 0 голова, 1 тело, 2 щит; костюм Bodyweight ставится на голову и тело сразу
        public static void PutArmor(SheetViewModel vm, ArmorDef a, int zone)
        {
            if (vm.Armor == null) vm.Armor = new ArmorData();
            var ar = vm.Armor;
            string pen = a.Penalty > 0 ? "-" + a.Penalty : "0";
            if (a.IsShield) { ar.ShieldName = a.Name; ar.ShieldSpCurrent = a.SP; ar.ShieldSpMax = a.SP; ar.ShieldPenalty = "0"; return; }
            if (a.IsSuit || zone == 0) { ar.HeadName = a.Name; ar.HeadSpCurrent = a.SP; ar.HeadSpMax = a.SP; ar.HeadPenalty = pen; }
            if (a.IsSuit || zone == 1) { ar.BodyName = a.Name; ar.BodySpCurrent = a.SP; ar.BodySpMax = a.SP; ar.BodyPenalty = pen; }
        }

        public static void PutGear(SheetViewModel vm, GearDef g)
        {
            string notes = GearCatalog.CostText(g.Cost) + (string.IsNullOrEmpty(g.Info) ? "" : "; " + g.Info);
            var slot = vm.GearItems.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Name) && string.IsNullOrWhiteSpace(x.Notes));
            if (slot == null) { vm.GearItems.Add(new GearRowItem { Name = g.Name, Notes = notes }); return; }
            slot.Name = g.Name; slot.Notes = notes;
        }

        // ---------- окно каталога ----------

        private void ShowCatalogDialog(SheetViewModel vm, int startTab)
        {
            var cyan = Theme.Accent();
            var red = Theme.Danger();
            var gray = Theme.Get("Muted");
            var white = Theme.Get("Text");
            var yellow = Theme.Get("Warn");
            var dark = Theme.Get("Panel");

            var win = Ui.Dialog(this, CatalogTitle, 980, 700);

            int tab = startTab;
            object selected = null;

            var root = new Grid { Margin = new Thickness(14) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var tabs = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            var tabButtons = new[] { MakeButton("ОРУЖИЕ", cyan), MakeButton("БРОНЯ", cyan), MakeButton("СНАРЯЖЕНИЕ, БОЕПРИПАСЫ, МОДУЛИ", cyan) };
            foreach (var b in tabButtons) { b.Margin = new Thickness(0, 0, 8, 0); tabs.Children.Add(b); }
            Grid.SetRow(tabs, 0);

            var searchPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            searchPanel.Children.Add(MakeText("ПОИСК: ", red, 13, FontWeights.Bold));
            var search = new TextBox { Width = 280, Background = Theme.Get("Back"), Foreground = cyan, BorderBrush = gray, CaretBrush = cyan, Padding = new Thickness(3) };
            searchPanel.Children.Add(search);
            searchPanel.Children.Add(MakeText("   ИСТОЧНИК: ", red, 13, FontWeights.Bold));
            var cbSource = new ComboBox { Width = 260, Margin = new Thickness(0, 0, 0, 0) };
            searchPanel.Children.Add(cbSource);
            Grid.SetRow(searchPanel, 1);

            var body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
            var listBox = new ListBox { Background = dark, Foreground = white, BorderBrush = gray, Margin = new Thickness(0, 0, 10, 0), FontSize = 14 };
            var detail = new Border { BorderBrush = gray, BorderThickness = new Thickness(1), Background = dark, Padding = new Thickness(12) };
            var detailScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            detail.Child = detailScroll;
            Grid.SetColumn(detail, 1);
            body.Children.Add(listBox); body.Children.Add(detail);
            Grid.SetRow(body, 2);

            var footer = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var status = new TextBlock { Foreground = gray, FontSize = 12, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center,
                Text = "Нужного предмета нет в списке? Впиши своё оружие, броню или снаряжение прямо в поля листа — они остаются редактируемыми." };
            var btnClose = MakeButton("ЗАКРЫТЬ", red);
            Grid.SetColumn(btnClose, 1);
            footer.Children.Add(status); footer.Children.Add(btnClose);
            Grid.SetRow(footer, 3);

            root.Children.Add(tabs); root.Children.Add(searchPanel); root.Children.Add(body); root.Children.Add(footer);
            win.Content = root;
            btnClose.Click += (s, e) => win.Close();

            Action showDetail = null;
            Action fillList = null;

            Func<string, bool> match = text => { string q = search.Text.Trim(); return q.Length == 0 || (text ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0; };
            const string AllSources = "Все источники";
            Func<string, bool> fromSource = src => cbSource.SelectedItem == null || (string)cbSource.SelectedItem == AllSources || (string)cbSource.SelectedItem == src;
            Func<string, string> tag = src => src == Packs.CoreName ? "" : "[" + src + "] ";
            bool fillingSources = false;
            Action fillSources = () =>
            {
                fillingSources = true;
                string keep = cbSource.SelectedItem as string;
                var names = (tab == 0 ? GearCatalog.Weapons.Select(w => w.Source) : tab == 1 ? GearCatalog.Armor.Select(a => a.Source) : GearCatalog.Gear.Select(g => g.Source)).Distinct().ToList();
                names.Sort((a, b) => a == Packs.CoreName ? -1 : b == Packs.CoreName ? 1 : string.Compare(a, b, StringComparison.CurrentCultureIgnoreCase));
                cbSource.Items.Clear();
                cbSource.Items.Add(AllSources);
                foreach (var n in names) cbSource.Items.Add(n);
                cbSource.SelectedItem = keep != null && cbSource.Items.Contains(keep) ? keep : AllSources;
                fillingSources = false;
            };

            fillList = () =>
            {
                listBox.Items.Clear();
                for (int i = 0; i < tabButtons.Length; i++)
                {
                    tabButtons[i].Background = i == tab ? Theme.Get("Tint") : Theme.Get("Raised");
                }
                if (tab == 0)
                    foreach (var w in GearCatalog.Weapons.Where(w => fromSource(w.Source) && (match(w.Name) || match(w.Standard) || match(w.Group))))
                        listBox.Items.Add(new ListBoxItem { Content = $"{tag(w.Source)}[{w.Group}] {w.Name}", Tag = w, Foreground = w.IsExotic ? yellow : white });
                else if (tab == 1)
                    foreach (var a in GearCatalog.Armor.Where(a => fromSource(a.Source) && match(a.Name)))
                        listBox.Items.Add(new ListBoxItem { Content = $"{tag(a.Source)}{a.Name}  —  {(a.IsShield ? a.SP + " ПЗ" : "ОС " + a.SP)}", Tag = a, Foreground = white });
                else
                    foreach (var g in GearCatalog.Gear.Where(g => fromSource(g.Source) && (match(g.Name) || match(g.Group))))
                        listBox.Items.Add(new ListBoxItem { Content = $"{tag(g.Source)}[{g.Group}] {g.Name}", Tag = g, Foreground = white });
                selected = null;
                showDetail();
            };

            listBox.SelectionChanged += (s, e) =>
            {
                selected = (listBox.SelectedItem as ListBoxItem)?.Tag;
                showDetail();
            };

            Action<string> say = msg => { status.Text = msg; status.Foreground = cyan; };

            showDetail = () =>
            {
                var p = new StackPanel();
                detailScroll.Content = p;
                if (selected == null) { p.Children.Add(MakeText("Выбери предмет слева.", gray, 14)); return; }

                if (selected is WeaponDef wd)
                {
                    p.Children.Add(MakeText(wd.Name, cyan, 18, FontWeights.Bold));
                    if (wd.Source != Packs.CoreName) p.Children.Add(MakeText("Источник: " + wd.Source, yellow, 12));
                    var stats = MakeText($"Урон {wd.Damage}   СКОР {(wd.Rof > 0 ? wd.Rof.ToString() : "—")}   Рук: {wd.Hands}" + (wd.Mag > 0 ? $"   Магазин {wd.Mag} ({wd.AmmoKind})" : ""), white, 14);
                    stats.TextWrapping = TextWrapping.Wrap; stats.Margin = new Thickness(0, 6, 0, 6);
                    p.Children.Add(stats);
                    p.Children.Add(MakeText("Навык: " + wd.Skill, gray, 13));
                    if (!string.IsNullOrEmpty(wd.Alt)) p.Children.Add(MakeText("Режимы/особенности: " + wd.Alt, gray, 13));
                    if (!string.IsNullOrEmpty(wd.Info)) { var t = MakeText(wd.Info, yellow, 13); t.TextWrapping = TextWrapping.Wrap; t.Margin = new Thickness(0, 6, 0, 0); p.Children.Add(t); }

                    ComboBox cbQ = null, cbM = null;
                    if (!wd.FixedQuality)
                    {
                        p.Children.Add(MakeText("Качество:", red, 13, FontWeights.Bold));
                        cbQ = new ComboBox { Margin = new Thickness(0, 2, 0, 8), Width = 330, HorizontalAlignment = HorizontalAlignment.Left };
                        cbQ.Items.Add($"Обычное — {GearCatalog.CostText(wd.Cost)}");
                        cbQ.Items.Add($"Низкое (заклинивает при крит. провале) — {GearCatalog.CostText(GearCatalog.PoorCost(wd.Cost))}");
                        cbQ.Items.Add($"Отличное (+1 к атаке) — {GearCatalog.CostText(GearCatalog.ExcellentCost(wd.Cost))}");
                        cbQ.SelectedIndex = 0;
                        p.Children.Add(cbQ);
                    }
                    else p.Children.Add(MakeText("Цена: " + GearCatalog.CostText(wd.Cost), white, 14));
                    if (wd.Clips != null && !wd.IsExotic)
                    {
                        p.Children.Add(MakeText("Магазин:", red, 13, FontWeights.Bold));
                        cbM = new ComboBox { Margin = new Thickness(0, 2, 0, 8), Width = 330, HorizontalAlignment = HorizontalAlignment.Left };
                        cbM.Items.Add($"Стандартный ({wd.Clips[0]})");
                        cbM.Items.Add($"Расширенный ({wd.Clips[1]}) +{GearCatalog.CostText(GearCatalog.ExtendedMagCost)}");
                        cbM.Items.Add($"Барабанный ({wd.Clips[2]}) +{GearCatalog.CostText(GearCatalog.DrumMagCost)}");
                        cbM.SelectedIndex = 0;
                        p.Children.Add(cbM);
                    }
                    if (!wd.IsMelee && !wd.IsExotic && !string.IsNullOrEmpty(wd.Standard))
                    {
                        var brands = MakeText($"Примеры: {wd.Poor} / {wd.Standard} / {wd.Excellent}", gray, 12);
                        brands.TextWrapping = TextWrapping.Wrap; brands.Margin = new Thickness(0, 0, 0, 8);
                        p.Children.Add(brands);
                    }
                    var add = MakeButton("ДОБАВИТЬ В ОРУЖИЕ", cyan);
                    add.HorizontalAlignment = HorizontalAlignment.Left; add.Margin = new Thickness(0, 8, 0, 0);
                    add.Click += (s, e) =>
                    {
                        int q = cbQ == null ? 0 : cbQ.SelectedIndex == 1 ? -1 : cbQ.SelectedIndex == 2 ? 1 : 0;
                        int m = cbM == null ? 0 : cbM.SelectedIndex;
                        var w = BuildWeapon(wd, q, m);
                        PutWeapon(vm, w);
                        CommitChange();
                        say("Добавлено в оружие: " + w.Name);
                    };
                    p.Children.Add(add);
                }
                else if (selected is ArmorDef ad)
                {
                    p.Children.Add(MakeText(ad.Name, cyan, 18, FontWeights.Bold));
                    if (ad.Source != Packs.CoreName) p.Children.Add(MakeText("Источник: " + ad.Source, yellow, 12));
                    var st = MakeText((ad.IsShield ? $"ПЗ {ad.SP}" : $"ОС {ad.SP}") + (ad.Penalty > 0 ? $"   Штраф: −{ad.Penalty} РЕА, ЛВК и СКО" : "   Штрафа нет") + "   Цена: " + GearCatalog.CostText(ad.Cost), white, 14);
                    st.TextWrapping = TextWrapping.Wrap; st.Margin = new Thickness(0, 6, 0, 6);
                    p.Children.Add(st);
                    var info = MakeText(ad.Info, gray, 13); info.TextWrapping = TextWrapping.Wrap; p.Children.Add(info);
                    var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
                    if (ad.IsShield)
                    {
                        var b = MakeButton("НА ЩИТ", cyan); row.Children.Add(b);
                        b.Click += (s, e) => { PutArmor(vm, ad, 2); CommitChange(); say("Установлено: " + ad.Name); };
                    }
                    else if (ad.IsSuit)
                    {
                        var b = MakeButton("ГОЛОВА И ТЕЛО", cyan); row.Children.Add(b);
                        b.Click += (s, e) => { PutArmor(vm, ad, 0); CommitChange(); say("Установлено на голову и тело: " + ad.Name); };
                    }
                    else
                    {
                        var bh = MakeButton("НА ГОЛОВУ", cyan); bh.Margin = new Thickness(0, 0, 8, 0);
                        var bb = MakeButton("НА ТЕЛО", cyan);
                        row.Children.Add(bh); row.Children.Add(bb);
                        bh.Click += (s, e) => { PutArmor(vm, ad, 0); CommitChange(); say("На голове: " + ad.Name); };
                        bb.Click += (s, e) => { PutArmor(vm, ad, 1); CommitChange(); say("На теле: " + ad.Name); };
                    }
                    p.Children.Add(row);
                    var warn = MakeText("Прежняя броня в этой зоне заменится (можно отменить Ctrl+Z / кнопкой ↶).", gray, 12);
                    warn.Margin = new Thickness(0, 8, 0, 0); warn.TextWrapping = TextWrapping.Wrap; p.Children.Add(warn);
                }
                else if (selected is GearDef gd)
                {
                    p.Children.Add(MakeText(gd.Name, cyan, 18, FontWeights.Bold));
                    if (gd.Source != Packs.CoreName) p.Children.Add(MakeText("Источник: " + gd.Source, yellow, 12));
                    var st = MakeText(gd.Group + "   Цена: " + GearCatalog.CostText(gd.Cost), white, 14);
                    st.Margin = new Thickness(0, 6, 0, 6); p.Children.Add(st);
                    if (!string.IsNullOrEmpty(gd.Info)) { var t = MakeText(gd.Info, gray, 13); t.TextWrapping = TextWrapping.Wrap; p.Children.Add(t); }
                    var add = MakeButton("ДОБАВИТЬ В СНАРЯЖЕНИЕ", cyan);
                    add.HorizontalAlignment = HorizontalAlignment.Left; add.Margin = new Thickness(0, 12, 0, 0);
                    add.Click += (s, e) => { PutGear(vm, gd); CommitChange(); say("Добавлено в снаряжение: " + gd.Name); };
                    p.Children.Add(add);
                }
            };

            for (int i = 0; i < tabButtons.Length; i++)
            {
                int idx = i;
                tabButtons[i].Click += (s, e) => { tab = idx; fillSources(); fillList(); };
            }
            search.TextChanged += (s, e) => fillList();
            cbSource.SelectionChanged += (s, e) => { if (!fillingSources) fillList(); };
            fillSources();

            fillList();

            _catalogWindow = win;
            _catalogSetTab = idx => { tab = idx; fillSources(); fillList(); };
            win.Closing += (s, e) => { _catalogWindow = null; _catalogSetTab = null; };
            Ui.Present(win);
        }
    }
}
