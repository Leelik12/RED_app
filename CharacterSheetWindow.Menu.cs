using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CyberpunkRED_Generator
{
    // выпадающее меню «МЕНЮ» в верхней панели листа
    public partial class CharacterSheetWindow
    {
        private readonly List<KeyValuePair<string, Action>> _menuItems = new List<KeyValuePair<string, Action>>();

        private void InitMenu()
        {
            RegisterMenuItem("💰 Финансы и жильё", ShowFinanceDialog);
            RegisterMenuItem("📖 Каталог оружия", () => OpenCatalog(0));
            RegisterMenuItem("📖 Каталог брони", () => OpenCatalog(1));
            RegisterMenuItem("📖 Каталог снаряжения", () => OpenCatalog(2));
            InitExportMenu();
            InitSettingsMenu();
        }

        private void RegisterMenuItem(string title, Action action)
            => _menuItems.Add(new KeyValuePair<string, Action>(title, action));

        private void BtnMenu_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            var menu = new ContextMenu
            {
                Background = new SolidColorBrush(Color.FromRgb(10, 10, 10)),
                BorderBrush = Theme.Danger(),
                BorderThickness = new Thickness(2),
                FontFamily = new FontFamily("Consolas"),
                PlacementTarget = btn,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom
            };

            foreach (var item in _menuItems)
            {
                var action = item.Value;
                var mi = new MenuItem
                {
                    Header = item.Key,
                    Foreground = Theme.Accent(),
                    Background = new SolidColorBrush(Color.FromRgb(10, 10, 10)),
                    FontSize = 15,
                    FontWeight = FontWeights.Bold,
                    Padding = new Thickness(12, 8, 12, 8)
                };
                mi.Click += (s, ev) => action();
                menu.Items.Add(mi);
            }
            menu.IsOpen = true;
        }
    }
}
