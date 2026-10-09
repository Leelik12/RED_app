using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CyberpunkRED_Generator
{
    // инструменты (погода, транспорт, погоня…) открываются закрываемыми вкладками рядом с вкладками листа,
    // а меню — выдвижная панель слева
    public partial class CharacterSheetWindow : ITabHost
    {
        private const double DrawerWidth = 440;
        private readonly Dictionary<string, TabItem> _toolTabs = new Dictionary<string, TabItem>();
        private readonly Dictionary<TabItem, ToolWindow> _toolWindows = new Dictionary<TabItem, ToolWindow>();
        private readonly Dictionary<TabItem, TextBlock> _toolTitles = new Dictionary<TabItem, TextBlock>();
        private bool _drawerOpen;
        private bool _sideActive, _sideCollapsed;
        private readonly HashSet<string> _openGroups = new HashSet<string> { "ПЕРСОНАЖ", "КАТАЛОГИ", "ИГРА" };
        private const double SideWidth = 240, SideRail = 48;

        public bool OpenTab(ToolWindow tool)
        {
            if (SheetTabControl == null || !(tool.Content is FrameworkElement content)) return false;
            tool.Owner = null;
            // тот же инструмент уже открыт — просто переключаемся на него (новый экземпляр не нужен)
            if (_toolTabs.TryGetValue(tool.Title, out var existing) && SheetTabControl.Items.Contains(existing))
            {
                SheetTabControl.SelectedItem = existing;
                // лишний диалог закрываем штатно, чтобы сработали его Closing-обработчики (отписки, сохранение)
                tool.Close();
                return true;
            }

            tool.Content = null;
            var host = new Grid { Margin = new Thickness(0, 10, 0, 0) };
            content.HorizontalAlignment = HorizontalAlignment.Stretch;
            content.MaxWidth = Math.Max(tool.Width, 700) * 1.5;
            host.Children.Add(content);

            var title = new TextBlock { Text = tool.Title, FontSize = 14, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center };
            var close = new Button
            {
                Content = "✕", Margin = new Thickness(10, 0, 0, 0), Padding = new Thickness(4, 0, 4, 0), Cursor = System.Windows.Input.Cursors.Hand,
                Background = Brushes.Transparent, BorderThickness = new Thickness(0), FontSize = 13, ToolTip = "Закрыть вкладку"
            };
            close.SetResourceReference(Control.ForegroundProperty, "DangerBrush");
            var header = new StackPanel { Orientation = Orientation.Horizontal };
            header.Children.Add(title);
            header.Children.Add(close);

            var tab = new TabItem { Header = header, Content = host };
            // цвет подписи задаём явно (неявный стиль TextBlock перебивает наследование от шаблона вкладки)
            _toolTitles[tab] = title;
            PaintToolTabs();
            _toolTabs[tool.Title] = tab;
            _toolWindows[tab] = tool;
            tool.CloseHosted = () =>
            {
                int idx = SheetTabControl.Items.IndexOf(tab);
                SheetTabControl.Items.Remove(tab);
                _toolTabs.Remove(tool.Title);
                _toolWindows.Remove(tab);
                _toolTitles.Remove(tab);
                if (SheetTabControl.Items.Count > 0)
                    SheetTabControl.SelectedIndex = Math.Max(0, Math.Min(idx, SheetTabControl.Items.Count) - 1);
                RebuildSideNav();
            };
            close.Click += (s, e) => tool.Close();
            SheetTabControl.Items.Add(tab);
            SheetTabControl.SelectedItem = tab;
            CloseDrawer();
            RebuildSideNav();
            return true;
        }

        // цвет подписи задаём явно: неявный стиль TextBlock перебивает наследование от шаблона вкладки
        private void PaintToolTabs()
        {
            foreach (var kv in _toolTitles)
                kv.Value.SetResourceReference(TextBlock.ForegroundProperty, kv.Key.IsSelected ? "OnAccentBrush" : "AccentBrush");
        }

        private void SheetTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource != SheetTabControl) return;
            PaintToolTabs();
            // на первой вкладке все показатели и так на виду (в боковой раскладке они всегда слева)
            if (MiniSheet != null) MiniSheet.Visibility = MiniSheetShown() ? Visibility.Visible : Visibility.Collapsed;
            if (_sideActive) RebuildSideNav();
        }

        // перед закрытием листа даём инструментам сохранить введённое (Closing у них собственный)
        private void CloseToolTabs()
        {
            foreach (var t in _toolWindows.Values.ToList()) t.Close();
        }

        private void RebuildDrawer()
        {
            NavList.Children.Clear();
            var head = new TextBlock { Text = "МЕНЮ", FontSize = 22, FontWeight = FontWeights.Heavy, Margin = new Thickness(2, 0, 0, 6) };
            head.SetResourceReference(TextBlock.ForegroundProperty, "DangerBrush");
            NavList.Children.Add(head);

            foreach (var g in _menuItems.GroupBy(m => m.Group).OrderBy(g => Array.IndexOf(MenuGroups, g.Key) < 0 ? 99 : Array.IndexOf(MenuGroups, g.Key)))
            {
                var gt = new TextBlock { Text = g.Key, FontSize = 12, FontWeight = FontWeights.Bold, Margin = new Thickness(2, 16, 0, 4) };
                gt.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush");
                NavList.Children.Add(gt);
                foreach (var item in g)
                {
                    var action = item.Action;
                    var b = new Button { Content = item.Title, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 0, 0, 2), FontSize = 15, FontWeight = FontWeights.Bold, Cursor = System.Windows.Input.Cursors.Hand, BorderThickness = new Thickness(0, 0, 0, 0) };
                    b.SetResourceReference(Control.ForegroundProperty, "AccentBrush");
                    b.SetResourceReference(Control.BackgroundProperty, "RaisedBrush");
                    b.Click += (s, e) => { CloseDrawer(); action(); };
                    NavList.Children.Add(b);
                }
            }
        }

        private void OpenDrawer()
        {
            RebuildDrawer();
            _drawerOpen = true;
            NavLayer.Visibility = Visibility.Visible;
            var slide = new DoubleAnimation(0, TimeSpan.FromMilliseconds(180)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            NavSlide.BeginAnimation(TranslateTransform.XProperty, slide);
            NavLayer.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));
        }

        private void CloseDrawer()
        {
            if (!_drawerOpen) return;
            _drawerOpen = false;
            var slide = new DoubleAnimation(-DrawerWidth, TimeSpan.FromMilliseconds(150));
            slide.Completed += (s, e) => { if (!_drawerOpen) NavLayer.Visibility = Visibility.Collapsed; };
            NavSlide.BeginAnimation(TranslateTransform.XProperty, slide);
        }

        // ---- раскладка: боковая панель (новая) или вкладки сверху (классическая)
        private void InitLayout()
        {
            Action apply = () => ApplyLayout();
            Loaded += (s, e) => apply();
            AppSettings.LayoutChanged += apply;
            Closed += (s, e) => AppSettings.LayoutChanged -= apply;
        }

        private void ApplyLayout()
        {
            bool side = AppSettings.SideLayout;
            _sideActive = side;
            SetLayoutGeometry(side);
            if (side) CloseDrawer();
            if (MiniSheet != null) MiniSheet.Visibility = MiniSheetShown() ? Visibility.Visible : Visibility.Collapsed;
            RebuildSideNav();
        }

        // геометрия окна: боковая панель шириной SideWidth/SideRail или 0, полоса вкладок и верхняя панель кнопок
        private void SetLayoutGeometry(bool side)
        {
            double width = side ? (_sideCollapsed ? SideRail : SideWidth) : 0;
            SideCol.Width = new GridLength(width);
            Sidebar.Visibility = side ? Visibility.Visible : Visibility.Collapsed;
            SideBody.Visibility = _sideCollapsed ? Visibility.Collapsed : Visibility.Visible;
            SideToggle.Content = _sideCollapsed ? "▶" : "◀";
            SideToggle.HorizontalAlignment = _sideCollapsed ? HorizontalAlignment.Center : HorizontalAlignment.Right;
            TopBar.Visibility = side ? Visibility.Collapsed : Visibility.Visible;
            double top = 10 + (AppSettings.DetailedStyle ? HudHeight : 0);
            SheetContent.Margin = side ? new Thickness(10, top, 15, 5) : new Thickness(15, top, 15, 5);
            HudBar.Margin = side ? new Thickness(10, 8, 15, 0) : new Thickness(15, 8, 15, 0);
            SheetTabControl.ApplyTemplate();
            if (SheetTabControl.Template.FindName("HeaderPanel", SheetTabControl) is UIElement strip)
                strip.Visibility = side ? Visibility.Collapsed : Visibility.Visible;
            SheetCanvas.UpdateLayout();
        }

        private void SideToggle_Click(object sender, RoutedEventArgs e)
        {
            _sideCollapsed = !_sideCollapsed;
            SetLayoutGeometry(true);
        }

        private void SideSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            TxtSkillSearch.Text = SideSearch.Text;
            if (SideSearch.Text.Length > 0 && SheetTabControl.Items.Count > 0) SheetTabControl.SelectedIndex = 0;
        }

        private static string StripNumber(string header)
        {
            int i = header.IndexOf(": ", StringComparison.Ordinal);
            return i > 0 && i <= 2 ? header.Substring(i + 2) : header;
        }

        private Button SideButton(string text, bool selected, Action click, double fontSize = 14)
        {
            var b = new Button
            {
                Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap }, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(10, 7, 10, 7), Margin = new Thickness(0, 0, 0, 2),
                FontSize = fontSize, FontWeight = FontWeights.Bold, Cursor = System.Windows.Input.Cursors.Hand, BorderThickness = new Thickness(selected ? 4 : 0, 0, 0, 0)
            };
            b.SetResourceReference(Control.ForegroundProperty, selected ? "OnAccentBrush" : "AccentBrush");
            b.SetResourceReference(Control.BackgroundProperty, selected ? "DangerBrush" : "RaisedBrush");
            b.SetResourceReference(Control.BorderBrushProperty, "AccentBrush");
            b.Click += (s, e) => click();
            if (TryFindResource("NavBtnTemplate") is ControlTemplate nav) b.Template = nav;
            return b;
        }

        private TextBlock SideHeader(string text)
        {
            var h = new TextBlock { Text = text, FontSize = 12, FontWeight = FontWeights.Bold, Margin = new Thickness(2, 14, 0, 4) };
            h.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush");
            return h;
        }

        // разделы листа, открытые инструменты и сгруппированное меню
        private void RebuildSideNav()
        {
            if (!_sideActive || SideNav == null) return;
            SideNav.Children.Clear();
            SideNav.Children.Add(SideHeader("ЛИСТ"));
            foreach (var tab in SheetTabControl.Items.OfType<TabItem>().Where(t => !_toolWindows.ContainsKey(t)))
            {
                var t = tab;
                string title = StripNumber(t.Header as string ?? "");
                SideNav.Children.Add(SideButton(title, t.IsSelected, () => SheetTabControl.SelectedItem = t));
            }

            if (_toolWindows.Count > 0)
            {
                SideNav.Children.Add(SideHeader("ОТКРЫТО"));
                foreach (var kv in _toolWindows.ToList())
                {
                    var tab = kv.Key; var tool = kv.Value;
                    var row = new DockPanel { Margin = new Thickness(0, 0, 0, 2) };
                    var close = new Button { Content = "✕", Padding = new Thickness(8, 0, 8, 0), Margin = new Thickness(2, 0, 0, 0), BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand, ToolTip = "Закрыть вкладку" };
                    close.SetResourceReference(Control.ForegroundProperty, "DangerBrush");
                    close.SetResourceReference(Control.BackgroundProperty, "RaisedBrush");
                    close.Click += (s, e) => tool.Close();
                    DockPanel.SetDock(close, Dock.Right);
                    row.Children.Add(close);
                    var open = SideButton(tool.Title, tab.IsSelected, () => SheetTabControl.SelectedItem = tab, 13);
                    open.Margin = new Thickness(0);
                    row.Children.Add(open);
                    SideNav.Children.Add(row);
                }
            }

            foreach (var g in _menuItems.GroupBy(m => m.Group).OrderBy(g => Array.IndexOf(MenuGroups, g.Key) < 0 ? 99 : Array.IndexOf(MenuGroups, g.Key)))
            {
                string key = g.Key;
                bool open = _openGroups.Contains(key);
                var head = new Button
                {
                    Content = (open ? "▾ " : "▸ ") + key, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(2, 4, 2, 4), Margin = new Thickness(0, 10, 0, 2),
                    FontSize = 12, FontWeight = FontWeights.Bold, Cursor = System.Windows.Input.Cursors.Hand, BorderThickness = new Thickness(0), Background = Brushes.Transparent
                };
                head.SetResourceReference(Control.ForegroundProperty, "MutedBrush");
                head.Click += (s, e) => { if (!_openGroups.Remove(key)) _openGroups.Add(key); RebuildSideNav(); };
                SideNav.Children.Add(head);
                if (!open) continue;
                foreach (var item in g)
                {
                    var action = item.Action;
                    SideNav.Children.Add(SideButton(item.Title, false, () => action(), 13));
                }
            }
        }

        private void NavScrim_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) => CloseDrawer();

        private void BtnMenu_Click(object sender, RoutedEventArgs e)
        {
            if (_drawerOpen) CloseDrawer(); else OpenDrawer();
        }
    }
}
