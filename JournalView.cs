using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CyberpunkRED_Generator
{
    // «Журнал сессии»: броски навыков (по клику на лист), заметки мастера/игрока и портрет персонажа
    public static class JournalView
    {
        public static Window ShowWindow(Window owner, SheetViewModel vm)
        {
            var win = Ui.Dialog(owner, "ЖУРНАЛ СЕССИИ", 700, 760);
            var j = vm.Journal ?? (vm.Journal = new JournalData());

            var root = new Grid { Margin = new Thickness(14) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // портрет
            var head = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            var pic = new Image { Width = 96, Height = 96, Stretch = Stretch.Uniform, Margin = new Thickness(0, 0, 14, 0) };
            var picBox = Ui.Box(pic, "AccentDarkBrush", "PanelBrush", 2);
            var picBtns = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var choose = Ui.Button("🖼 ВЫБРАТЬ ПОРТРЕТ…");
            var remove = Ui.Button("УБРАТЬ", "MutedBrush");
            remove.Margin = new Thickness(0, 6, 0, 0);
            picBtns.Children.Add(Ui.Text(vm.Name ?? "", "AccentBrush", 18, true));
            picBtns.Children.Add(choose);
            picBtns.Children.Add(remove);
            head.Children.Add(picBox);
            head.Children.Add(picBtns);
            Grid.SetRow(head, 0);
            root.Children.Add(head);
            Action showPic = () => { pic.Source = Portraits.ToImage(j.Portrait); remove.IsEnabled = !string.IsNullOrEmpty(j.Portrait); };
            choose.Click += (s, e) =>
            {
                var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Изображения|*.png;*.jpg;*.jpeg;*.bmp;*.gif|Все файлы|*.*" };
                if (dlg.ShowDialog() != true) return;
                var b64 = Portraits.FromFile(dlg.FileName);
                if (b64 == null) { MessageBox.Show(win, "Не удалось открыть изображение.", "ПОРТРЕТ"); return; }
                j.Portrait = b64; showPic(); j.Changed?.Invoke();
            };
            remove.Click += (s, e) => { j.Portrait = null; showPic(); j.Changed?.Invoke(); };
            showPic();

            // добавление заметки
            var addRow = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            addRow.ColumnDefinitions.Add(new ColumnDefinition());
            addRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var note = Ui.Input();
            note.HorizontalAlignment = HorizontalAlignment.Stretch;
            var add = Ui.Button("+ ЗАМЕТКА");
            add.Margin = new Thickness(8, 0, 0, 0);
            Grid.SetColumn(add, 1);
            addRow.Children.Add(note); addRow.Children.Add(add);
            Grid.SetRow(addRow, 1);
            root.Children.Add(addRow);

            var list = new StackPanel();
            var scroll = new ScrollViewer { Content = list, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Grid.SetRow(scroll, 2);
            root.Children.Add(scroll);

            Action refresh = null;
            refresh = () =>
            {
                list.Children.Clear();
                if (j.Entries.Count == 0) list.Children.Add(Ui.Text("Пока пусто. Кликни по названию навыка на листе — бросок попадёт сюда.", "MutedBrush", 12));
                foreach (var e in Enumerable.Reverse(j.Entries).Take(200))
                {
                    var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                    row.Children.Add(Ui.Text(e.Time.ToString("HH:mm"), "MutedBrush", 12));
                    string icon = e.Kind == "roll" ? "  🎲 " : e.Kind == "note" ? "  📝 " : "  ◆ ";
                    row.Children.Add(Ui.Text(icon, "DangerBrush", 12));
                    var t = Ui.Text(e.Text, e.Kind == "roll" ? "AccentBrush" : "TextBrush", 13);
                    t.TextWrapping = TextWrapping.Wrap; t.MaxWidth = 560;
                    row.Children.Add(t);
                    list.Children.Add(row);
                }
                var clear = Ui.Button("ОЧИСТИТЬ ЖУРНАЛ", "DangerBrush", j.Entries.Count > 0);
                clear.Margin = new Thickness(0, 12, 0, 0); clear.HorizontalAlignment = HorizontalAlignment.Left;
                clear.Click += (s, ev) =>
                {
                    if (MessageBox.Show(win, "Стереть все записи журнала?", "ЖУРНАЛ", MessageBoxButton.YesNo) == MessageBoxResult.Yes) j.Clear();
                };
                list.Children.Add(clear);
            };
            Action onChanged = () => { if (win.IsLoaded || win.CloseHosted != null) refresh(); };
            j.Changed += onChanged;
            win.Closing += (s, e) => { j.Changed -= onChanged; };
            add.Click += (s, e) => { if (!string.IsNullOrWhiteSpace(note.Text)) { j.Add("note", note.Text.Trim()); note.Text = ""; } };
            note.KeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Enter) add.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent)); };
            refresh();

            win.Content = root;
            Ui.Present(win);
            return win;
        }
    }
}
