using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CyberpunkRED_Generator
{
    // общие окна «Настройки», «О программе» и проверки обновлений: открываются из главного меню, листа и ГМ-экрана
    public static class SettingsView
    {
        private static readonly string[] Fonts = { "Bahnschrift", "Segoe UI", "Consolas", "Cascadia Mono", "Lucida Console", "Courier New" };

        public static void ShowSettings(Window owner)
        {
            var win = Ui.Dialog(owner, "НАСТРОЙКИ", 520, 700);
            var root = new StackPanel { Margin = new Thickness(18) };
            root.Children.Add(Ui.Text("ЦВЕТОВАЯ СХЕМА", "DangerBrush", 14, true));

            bool loading = true;
            var radios = new List<RadioButton>();
            var keep = new CheckBox { Margin = new Thickness(0, 14, 0, 0), FontSize = 14, Content = "Сохранять пропорции (поля по краям)", IsChecked = AppSettings.Data.KeepAspect };
            keep.SetResourceReference(Control.ForegroundProperty, "AccentBrush");
            Action apply = () =>
            {
                if (loading) return;
                var id = radios.Where(r => r.IsChecked == true).Select(r => (string)r.Tag).FirstOrDefault();
                AppSettings.Set(id, keep.IsChecked == true);
            };

            var themeGrid = new WrapPanel { Margin = new Thickness(0, 6, 0, 0) };
            foreach (var t in AppSettings.Themes)
            {
                // образец показывает цвета самой схемы, а не текущей
                var swatch = new Border { Width = 70, Height = 34, Background = new SolidColorBrush(t.Back), BorderBrush = new SolidColorBrush(t.Line), BorderThickness = new Thickness(1) };
                var sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
                sp.Children.Add(new Border { Width = 16, Height = 16, Background = new SolidColorBrush(t.Accent), Margin = new Thickness(0, 0, 4, 0) });
                sp.Children.Add(new Border { Width = 16, Height = 16, Background = new SolidColorBrush(t.Danger), Margin = new Thickness(0, 0, 4, 0) });
                sp.Children.Add(new Border { Width = 16, Height = 16, Background = new SolidColorBrush(t.Text) });
                swatch.Child = sp;
                var card = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
                card.Children.Add(swatch);
                var rb = new RadioButton { Tag = t.Id, GroupName = "theme", Margin = new Thickness(0, 4, 0, 8), IsChecked = t.Id == AppSettings.CurrentTheme.Id,
                    Content = new TextBlock { Text = t.Name, FontSize = 12, TextWrapping = TextWrapping.Wrap, Width = 120 } };
                rb.SetResourceReference(Control.ForegroundProperty, "AccentBrush");
                rb.Checked += (s, e) => apply();
                radios.Add(rb);
                card.Children.Add(rb);
                themeGrid.Children.Add(card);
            }
            root.Children.Add(themeGrid);

            root.Children.Add(Ui.Text("ШРИФТ", "DangerBrush", 14, true));
            var font = new ComboBox { Width = 220, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 6, 0, 0) };
            var installed = new HashSet<string>(Fonts.Where(f => Fonts != null && System.Windows.Media.Fonts.SystemFontFamilies.Any(x => x.Source == f)));
            foreach (var f in Fonts.Where(installed.Contains)) font.Items.Add(f);
            if (!font.Items.Contains("Segoe UI")) font.Items.Insert(0, "Segoe UI");
            font.SelectedItem = font.Items.Contains(AppSettings.Data.UiFont) ? AppSettings.Data.UiFont : font.Items[0];
            font.SelectionChanged += (s, e) => { if (!loading && font.SelectedItem != null) AppSettings.SetFont((string)font.SelectedItem); };
            root.Children.Add(font);

            var layoutTitle = Ui.Text("РАСКЛАДКА ЛИСТА", "DangerBrush", 14, true);
            layoutTitle.Margin = new Thickness(0, 14, 0, 0);
            root.Children.Add(layoutTitle);
            var layout = new ComboBox { Width = 320, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 6, 0, 0) };
            layout.Items.Add(new ComboBoxItem { Content = "Боковая панель (новая)", Tag = "side" });
            layout.Items.Add(new ComboBoxItem { Content = "Классическая (вкладки сверху)", Tag = "classic" });
            layout.SelectedIndex = AppSettings.SideLayout ? 0 : 1;
            layout.SelectionChanged += (s, e) => { if (!loading && layout.SelectedItem is ComboBoxItem it) AppSettings.SetLayout((string)it.Tag); };
            root.Children.Add(layout);
            root.Children.Add(Ui.Text("Применяется сразу; в классической раскладке меню остаётся выдвижным.", "MutedBrush", 12));

            var styleTitle = Ui.Text("СТИЛЬ ЛИСТА", "DangerBrush", 14, true);
            styleTitle.Margin = new Thickness(0, 14, 0, 0);
            root.Children.Add(styleTitle);
            var style = new ComboBox { Width = 320, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 6, 0, 0) };
            style.Items.Add(new ComboBoxItem { Content = "Минималистичный (строгий терминал)", Tag = "minimal" });
            style.Items.Add(new ComboBoxItem { Content = "Детальный (графический интерфейс)", Tag = "detailed" });
            style.Items.Add(new ComboBoxItem { Content = "Детальный 2 (неоновые контуры, таблетки)", Tag = "detailed2" });
            style.SelectedIndex = AppSettings.StyleId == "detailed" ? 1 : AppSettings.StyleId == "detailed2" ? 2 : 0;
            style.SelectionChanged += (s, e) => { if (!loading && style.SelectedItem is ComboBoxItem it) AppSettings.SetStyle((string)it.Tag); };
            root.Children.Add(style);
            root.Children.Add(Ui.Text("Детальные: шапка с портретом и шкалами ПЗ, брони и человечности, скруглённые карточки, градиенты, полоски уровней навыков и характеристик.", "MutedBrush", 12));

            var langTitle = Ui.Text("ЯЗЫК / LANGUAGE", "DangerBrush", 14, true);
            langTitle.Margin = new Thickness(0, 14, 0, 0);
            root.Children.Add(langTitle);
            var lang = new ComboBox { Width = 220, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 6, 0, 0) };
            lang.Items.Add(new ComboBoxItem { Content = "Русский", Tag = "ru" });
            lang.Items.Add(new ComboBoxItem { Content = "English", Tag = "en" });
            lang.SelectedIndex = Loc.English ? 1 : 0;
            var langHint = Ui.Text("Применится после перезапуска · Applied after restart. Описания правил, предметы и названия в сохранениях остаются русскими · Rules text, items and saved names stay Russian.", "MutedBrush", 12);
            lang.SelectionChanged += (s, e) =>
            {
                if (loading || !(lang.SelectedItem is ComboBoxItem it)) return;
                AppSettings.Data.Language = (string)it.Tag;
                AppSettings.Save();
            };
            root.Children.Add(lang);
            root.Children.Add(langHint);

            keep.Checked += (s, e) => apply();
            keep.Unchecked += (s, e) => apply();
            root.Children.Add(keep);
            var hint = Ui.Text("Изменения применяются сразу и запоминаются.", "MutedBrush", 12);
            hint.Margin = new Thickness(22, 4, 0, 0);
            root.Children.Add(hint);

            var upd = new CheckBox { Margin = new Thickness(0, 16, 0, 0), FontSize = 14, Content = "Проверять обновления при запуске", IsChecked = AppSettings.Data.CheckUpdates };
            upd.SetResourceReference(Control.ForegroundProperty, "AccentBrush");
            upd.Checked += (s, e) => { if (!loading) { AppSettings.Data.CheckUpdates = true; AppSettings.Save(); } };
            upd.Unchecked += (s, e) => { if (!loading) { AppSettings.Data.CheckUpdates = false; AppSettings.Save(); } };
            root.Children.Add(upd);
            var updHint = Ui.Text("Читается только публичная страница последнего релиза на GitHub; никакие данные не отправляются.", "MutedBrush", 12);
            updHint.Margin = new Thickness(22, 4, 0, 0);
            root.Children.Add(updHint);

            // подключённые DLC: их предметы появляются в каталогах и списках имплантов
            var packList = Packs.All.Where(p => p.Kind != "core" && (Packs.Items.Any(i => i.Pack == p.Id) || Npcs.Count(p.Id) > 0)).ToList();
            if (packList.Count > 0)
            {
                var ptitle = Ui.Text("ПОДКЛЮЧЁННЫЕ DLC", "DangerBrush", 14, true);
                ptitle.Margin = new Thickness(0, 18, 0, 4);
                root.Children.Add(ptitle);
                var plist = new StackPanel();
                foreach (var p in packList)
                {
                    string id = p.Id;
                    int n = Packs.Items.Count(i => i.Pack == id), nn = Npcs.Count(id);
                    var cb = new CheckBox { Content = $"{p.Name}  ({n}" + (nn > 0 ? $" + {nn} НИП" : "") + ")", IsChecked = Packs.IsEnabled(id), Margin = new Thickness(0, 3, 0, 0), FontSize = 13 };
                    cb.SetResourceReference(Control.ForegroundProperty, "AccentBrush");
                    cb.Checked += (s, ev) => { if (!loading) AppSettings.SetPackEnabled(id, true); };
                    cb.Unchecked += (s, ev) => { if (!loading) AppSettings.SetPackEnabled(id, false); };
                    plist.Children.Add(cb);
                }
                root.Children.Add(new ScrollViewer { Content = plist, MaxHeight = 170, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
                var ph = Ui.Text("Отключённые DLC скрываются в каталогах; уже добавленное в персонажа остаётся.", "MutedBrush", 12);
                ph.Margin = new Thickness(0, 4, 0, 0);
                root.Children.Add(ph);
            }

            var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 18, 0, 0) };
            var about = Ui.Button("О ПРОГРАММЕ");
            about.Margin = new Thickness(0, 0, 8, 0);
            about.Click += (s, e) => ShowAbout(owner);
            var check = Ui.Button("ПРОВЕРИТЬ ОБНОВЛЕНИЯ");
            check.Margin = new Thickness(0, 0, 8, 0);
            check.Click += (s, e) => CheckUpdates(owner, false);
            var close = Ui.Button("ЗАКРЫТЬ", "DangerBrush");
            close.Click += (s, e) => win.Close();
            row.Children.Add(about); row.Children.Add(check); row.Children.Add(close);
            root.Children.Add(row);

            win.Content = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            loading = false;
            Ui.Present(win);
        }

        public static void ShowAbout(Window owner)
        {
            MessageBox.Show(owner,
                AppInfo.Name + " v" + AppInfo.VersionText +
                "\nФормат сохранений: " + SaveSchema.Current +
                "\n\nЖурнал ошибок: " + ErrorLog.Folder +
                "\nПроект: github.com/" + AppInfo.Repository,
                "О ПРОГРАММЕ", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // silent: при запуске молчим, если обновлений нет или сети нет
        public static async void CheckUpdates(Window owner, bool silent)
        {
            var info = await Updates.CheckAsync();
            if (owner != null && !owner.IsLoaded && !silent) return;
            if (!info.Ok)
            {
                if (!silent) MessageBox.Show(owner, "Не удалось проверить обновления: " + info.Error, "ОБНОВЛЕНИЯ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!info.Newer)
            {
                if (!silent) MessageBox.Show(owner, "Установлена последняя версия (" + AppInfo.VersionText + ").", "ОБНОВЛЕНИЯ", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            string notes = string.IsNullOrWhiteSpace(info.Notes) ? "" : "\n\n" + (info.Notes.Length > 700 ? info.Notes.Substring(0, 700) + "…" : info.Notes);
            if (MessageBox.Show(owner, "Вышла версия " + info.Tag + " (у вас " + AppInfo.VersionText + ")." + notes + "\n\nОткрыть страницу релиза в браузере?",
                    "ДОСТУПНО ОБНОВЛЕНИЕ", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes && !string.IsNullOrEmpty(info.Url))
                System.Diagnostics.Process.Start(info.Url);
        }
    }
}
