using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CyberpunkRED_Generator
{
    // окно настроек оформления: цветовая схема и масштаб
    public partial class CharacterSheetWindow
    {
        private void InitSettingsMenu()
        {
            RegisterMenuItem("⚙ Настройки", ShowSettingsDialog);
        }

        private void ShowSettingsDialog()
        {
            CloseOverlays();
            var red = Theme.Danger();
            var gray = new SolidColorBrush(Color.FromRgb(153, 153, 153));

            var win = new Window
            {
                Title = "НАСТРОЙКИ",
                Owner = this,
                Width = 440,
                SizeToContent = SizeToContent.Height,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = new SolidColorBrush(Color.FromRgb(5, 5, 5)),
                FontFamily = new FontFamily("Consolas")
            };
            win.Resources = this.Resources;

            var root = new StackPanel { Margin = new Thickness(16) };
            var title = MakeText("ЦВЕТОВАЯ СХЕМА", red, 14, FontWeights.Bold);
            title.SetResourceReference(TextBlock.ForegroundProperty, "DangerBrush");
            root.Children.Add(title);

            var radios = new List<RadioButton>();
            bool loading = true;
            var keep = new CheckBox { Margin = new Thickness(0, 16, 0, 0), FontSize = 14, Content = "Сохранять пропорции (чёрные поля)", IsChecked = AppSettings.Data.KeepAspect };
            keep.SetResourceReference(Control.ForegroundProperty, "AccentBrush");

            Action apply = () =>
            {
                if (loading) return;
                string id = null;
                foreach (var r in radios) if (r.IsChecked == true) id = (string)r.Tag;
                AppSettings.Set(id, keep.IsChecked == true);
            };

            foreach (var t in AppSettings.Themes)
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal };
                row.Children.Add(new Border { Width = 18, Height = 18, Background = new SolidColorBrush(t.Accent), BorderBrush = new SolidColorBrush(t.Danger), BorderThickness = new Thickness(3), Margin = new Thickness(0, 0, 8, 0) });
                row.Children.Add(new TextBlock { Text = t.Name, FontSize = 14, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(t.Accent) });
                var rb = new RadioButton { Tag = t.Id, Content = row, GroupName = "theme", Margin = new Thickness(0, 7, 0, 0), IsChecked = t.Id == AppSettings.CurrentTheme.Id };
                rb.Checked += (s, e) => apply();
                radios.Add(rb);
                root.Children.Add(rb);
            }

            keep.Checked += (s, e) => apply();
            keep.Unchecked += (s, e) => apply();
            root.Children.Add(keep);
            root.Children.Add(new TextBlock { Text = "Изменения применяются сразу и запоминаются.", Foreground = gray, FontSize = 12, Margin = new Thickness(20, 4, 0, 0) });

            var close = MakeButton("ЗАКРЫТЬ", red);
            close.HorizontalAlignment = HorizontalAlignment.Right;
            close.Margin = new Thickness(0, 18, 0, 0);
            close.SetResourceReference(Control.ForegroundProperty, "DangerBrush");
            close.SetResourceReference(Control.BorderBrushProperty, "DangerBrush");
            close.Click += (s, e) => win.Close();
            root.Children.Add(close);

            win.Content = root;
            loading = false;
            win.ShowDialog();
        }
    }
}
