using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CyberpunkRED_Generator
{
    // общие элементы интерфейса для окон, собираемых в коде. Цвета берутся из ресурсов темы
    // (AccentBrush, DangerBrush, TextBrush, MutedBrush, WarnBrush, PanelBrush, RaisedBrush, BackBrush),
    // поэтому смена темы в настройках перекрашивает их сразу.
    public static class Ui
    {
        public const string FontName = "Consolas";

        public static TextBlock Text(string text, string brush = "TextBrush", double size = 14, bool bold = false)
        {
            var t = new TextBlock { Text = text, FontSize = size, FontWeight = bold ? FontWeights.Bold : FontWeights.Normal, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
            t.SetResourceReference(TextBlock.ForegroundProperty, brush);
            return t;
        }

        public static Button Button(string text, string brush = "AccentBrush", bool enabled = true)
        {
            var b = new Button
            {
                Content = text,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 4, 10, 4),
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand,
                IsEnabled = enabled,
                Opacity = enabled ? 1 : 0.4
            };
            b.SetResourceReference(Control.ForegroundProperty, brush);
            b.SetResourceReference(Control.BorderBrushProperty, brush);
            b.SetResourceReference(Control.BackgroundProperty, "RaisedBrush");
            return b;
        }

        public static TextBox Input(double width = 0)
        {
            var t = new TextBox { Padding = new Thickness(3), BorderThickness = new Thickness(1) };
            if (width > 0) t.Width = width;
            t.SetResourceReference(Control.BackgroundProperty, "BackBrush");
            t.SetResourceReference(Control.ForegroundProperty, "AccentBrush");
            t.SetResourceReference(Control.BorderBrushProperty, "MutedBrush");
            t.SetResourceReference(TextBox.CaretBrushProperty, "AccentBrush");
            return t;
        }

        public static Border Box(UIElement child, string border = "AccentDarkBrush", string background = "PanelBrush", double pad = 8, double thick = 1)
        {
            var b = new Border { Child = child, BorderThickness = new Thickness(thick), Padding = new Thickness(pad) };
            b.SetResourceReference(Border.BorderBrushProperty, border);
            b.SetResourceReference(Border.BackgroundProperty, background);
            return b;
        }

        // диалог с темой приложения; смена темы в настройках применяется к нему сразу
        public static ToolWindow Dialog(Window owner, string title, double width, double height)
        {
            var w = new ToolWindow
            {
                Title = title,
                Owner = owner,
                Width = width,
                Height = height,
                WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
                FontFamily = Theme.CurrentFont
            };
            Theme.Install(w);
            w.SetResourceReference(Control.BackgroundProperty, "BackBrush");
            return w;
        }

        // показать инструмент: во вкладке листа, если окно-владелец умеет (ITabHost), иначе отдельным окном
        public static void Present(ToolWindow win)
        {
            if (win.Owner is ITabHost host && host.OpenTab(win)) return;
            win.Show();
        }

        public static Brush Brush(FrameworkElement scope, string key)
            => scope.TryFindResource(key) as Brush ?? System.Windows.Media.Brushes.White;
    }

    // окно инструмента, которое можно встроить во вкладку: Close() закрывает вкладку и вызывает те же обработчики Closing
    public class ToolWindow : Window
    {
        private readonly List<CancelEventHandler> _closing = new List<CancelEventHandler>();
        public Action CloseHosted;   // задаётся, когда содержимое переехало во вкладку

        public new event CancelEventHandler Closing
        {
            add { base.Closing += value; _closing.Add(value); }
            remove { base.Closing -= value; _closing.Remove(value); }
        }

        public new void Close()
        {
            if (CloseHosted == null) { base.Close(); return; }
            var args = new CancelEventArgs();
            foreach (var h in _closing.ToArray()) h(this, args);
            if (!args.Cancel) CloseHosted();
        }
    }

    public interface ITabHost
    {
        // true — содержимое окна встроено во вкладку (само окно не показывается)
        bool OpenTab(ToolWindow tool);
    }
}
