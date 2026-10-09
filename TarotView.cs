using System;
using System.Windows;
using System.Windows.Controls;

namespace CyberpunkRED_Generator
{
    // окно «Карты Таро Найт-Сити»: переключатель использования, вытягивание карты без повторов, журнал
    public static class TarotView
    {
        public static Window ShowWindow(Window owner)
        {
            var win = Ui.Dialog(owner, "КАРТЫ ТАРО НАЙТ-СИТИ", 560, 640);
            var data = AppSettings.Data;

            var root = new StackPanel { Margin = new Thickness(14) };
            root.Children.Add(Ui.Text("КАРТЫ ТАРО НАЙТ-СИТИ", "DangerBrush", 16, true));

            var enabled = new CheckBox { Content = "Использовать Таро за этим столом", IsChecked = data.TarotEnabled, Margin = new Thickness(0, 8, 0, 8), FontSize = 14 };
            enabled.SetResourceReference(Control.ForegroundProperty, "AccentBrush");
            root.Children.Add(enabled);
            root.Children.Add(Ui.Text(Tarot.Rule, "MutedBrush", 12));

            var bar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 10) };
            var draw = Ui.Button("🃏 ВЫТЯНУТЬ КАРТУ");
            var reset = Ui.Button("СБРОСИТЬ КОЛОДУ", "DangerBrush");
            reset.Margin = new Thickness(10, 0, 0, 0);
            bar.Children.Add(draw);
            bar.Children.Add(reset);
            root.Children.Add(bar);

            var left = Ui.Text("", "MutedBrush", 12);
            root.Children.Add(left);

            var cardTitle = Ui.Text("", "AccentBrush", 18, true);
            cardTitle.Margin = new Thickness(0, 10, 0, 2);
            var cardFlavor = Ui.Text("", "MutedBrush", 12);
            var cardEffect = Ui.Text("", "TextBrush", 14);
            cardEffect.Margin = new Thickness(0, 6, 0, 0);
            var cardBox = new StackPanel();
            cardBox.Children.Add(cardTitle);
            cardBox.Children.Add(cardFlavor);
            cardBox.Children.Add(cardEffect);
            root.Children.Add(Ui.Box(cardBox, pad: 10));

            var logTitle = Ui.Text("ЖУРНАЛ", "DangerBrush", 13, true);
            logTitle.Margin = new Thickness(0, 12, 0, 4);
            root.Children.Add(logTitle);
            var log = Ui.Text("", "MutedBrush", 12);
            root.Children.Add(log);

            Action refresh = () =>
            {
                bool on = enabled.IsChecked == true;
                int remaining = Tarot.Deck.Count - data.TarotDrawn.Count;
                left.Text = on ? "В колоде осталось карт: " + remaining + " из " + Tarot.Deck.Count : "Таро выключено — карты не тянутся.";
                draw.IsEnabled = on && remaining > 0;
                draw.Opacity = draw.IsEnabled ? 1 : 0.4;
                log.Text = data.TarotLog.Count == 0 ? "Пока пусто." : string.Join("\n", data.TarotLog);
            };

            enabled.Checked += (s, e) => { data.TarotEnabled = true; AppSettings.Save(); refresh(); };
            enabled.Unchecked += (s, e) => { data.TarotEnabled = false; AppSettings.Save(); refresh(); };
            draw.Click += (s, e) =>
            {
                var c = Tarot.Draw(data.TarotDrawn);
                if (c == null) return;
                cardTitle.Text = c.Title;
                cardFlavor.Text = c.Flavor;
                cardEffect.Text = "Эффект: " + c.Effect;
                data.TarotLog.Insert(0, DateTime.Now.ToString("HH:mm") + "  " + c.Title);
                AppSettings.Save();
                refresh();
            };
            reset.Click += (s, e) =>
            {
                if (MessageBox.Show(win, "Вернуть все карты в колоду и очистить журнал?", "ТАРО", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                data.TarotDrawn.Clear();
                data.TarotLog.Clear();
                AppSettings.Save();
                cardTitle.Text = cardFlavor.Text = cardEffect.Text = "";
                refresh();
            };
            refresh();

            win.Content = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Ui.Present(win);
            return win;
        }
    }
}
