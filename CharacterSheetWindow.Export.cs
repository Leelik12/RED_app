using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace CyberpunkRED_Generator
{
    // экспорт листа в PDF и печать: каждая вкладка рисуется в картинку 1920×1080
    public partial class CharacterSheetWindow
    {
        public const int ExportBaseWidth = 1920;
        public const int ExportBaseHeight = 1080;
        public const double ExportScale = 1.5;
        public const double EcoContrast = 1.6;   // «экономная печать»: тёмный фон становится белым, светлый текст — тёмным

        private static readonly string[] ExportPageNames =
        {
            "1: Характеристики и навыки", "2: Биография и связи", "3: Киберимпланты", "4: NPC", "5: Нетраннинг"
        };

        private void InitExportMenu()
        {
            RegisterMenuItem("📄 Экспорт в PDF…", () => ShowExportDialog(false), "ФАЙЛ");
            RegisterMenuItem("🖨 Печать…", () => ShowExportDialog(true), "ФАЙЛ");
        }

        private void ShowExportDialog(bool print)
        {
            if (!(DataContext is SheetViewModel vm)) return;
            CloseOverlays();

            var cyan = Theme.Accent();
            var red = Theme.Danger();
            var gray = Theme.Get("Muted");

            var win = Ui.Dialog(this, print ? "ПЕЧАТЬ ЛИСТА" : "ЭКСПОРТ В PDF", 460, 460);

            var root = new StackPanel { Margin = new Thickness(16) };
            root.Children.Add(MakeText("СТРАНИЦЫ", red, 14, FontWeights.Bold));

            bool hasNpc = vm.NPCs != null && vm.NPCs.Count > 0;
            bool hasNet = (vm.Net != null && vm.Net.Programs != null && vm.Net.Programs.Count > 0) || NetInterfaceRank > 0;
            bool[] defaults = { true, true, true, hasNpc, hasNet };

            var boxes = new List<CheckBox>();
            for (int i = 0; i < ExportPageNames.Length; i++)
            {
                var cb = new CheckBox
                {
                    Content = ExportPageNames[i],
                    IsChecked = defaults[i],
                    Foreground = cyan,
                    FontSize = 14,
                    Margin = new Thickness(0, 5, 0, 0)
                };
                boxes.Add(cb);
                root.Children.Add(cb);
            }

            var eco = new CheckBox
            {
                Content = "Экономная печать (светлый фон, чёрно-белая)",
                IsChecked = print,
                Foreground = cyan,
                FontSize = 14,
                Margin = new Thickness(0, 16, 0, 0)
            };
            root.Children.Add(eco);
            root.Children.Add(new TextBlock
            {
                Text = "Экономная печать не тратит тонер на чёрный фон.",
                Foreground = gray,
                FontSize = 12,
                Margin = new Thickness(20, 2, 0, 0)
            });

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 18, 0, 0) };
            var ok = MakeButton(print ? "ПЕЧАТЬ" : "СОХРАНИТЬ PDF", cyan);
            var cancel = MakeButton("ОТМЕНА", red);
            cancel.Margin = new Thickness(8, 0, 0, 0);
            buttons.Children.Add(ok); buttons.Children.Add(cancel);
            root.Children.Add(buttons);
            win.Content = root;

            foreach (var b in boxes)
            {
                b.Checked += (s, e) => { ok.IsEnabled = true; ok.Opacity = 1; };
                b.Unchecked += (s, e) =>
                {
                    bool any = boxes.Any(x => x.IsChecked == true);
                    ok.IsEnabled = any; ok.Opacity = any ? 1 : 0.4;
                };
            }
            cancel.Click += (s, e) => win.Close();
            ok.Click += (s, e) =>
            {
                var pages = boxes.Select((b, i) => new { b, i }).Where(x => x.b.IsChecked == true).Select(x => x.i).ToList();
                bool ecoOn = eco.IsChecked == true;
                win.Close();
                if (print) PrintSheet(pages, ecoOn);
                else SavePdfWithDialog(pages, ecoOn, vm);
            };
            Ui.Present(win);
        }

        private void SavePdfWithDialog(List<int> pages, bool eco, SheetViewModel vm)
        {
            string name = string.IsNullOrWhiteSpace(vm.Name) ? "character" : vm.Name;
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Экспорт листа в PDF",
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = name + ".pdf",
                DefaultExt = ".pdf"
            };
            if (dlg.ShowDialog(this) != true) return;
            try
            {
                ExportToPdf(dlg.FileName, pages, eco);
                MessageBox.Show(this, "Файл сохранён:\n" + dlg.FileName, "ЭКСПОРТ В PDF", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Не удалось сохранить PDF:\n" + ex.Message, "ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ExportToPdf(string path, IList<int> pages, bool eco)
        {
            var images = RenderSheetPages(pages, eco, ExportScale);
            var list = new List<PdfPageImage>();
            foreach (var img in images)
            {
                var enc = new JpegBitmapEncoder { QualityLevel = 90 };
                enc.Frames.Add(BitmapFrame.Create(img));
                byte[] jpeg;
                using (var ms = new MemoryStream()) { enc.Save(ms); jpeg = ms.ToArray(); }
                list.Add(new PdfPageImage { Jpeg = jpeg, Width = img.PixelWidth, Height = img.PixelHeight, Gray = JpegComponents(jpeg) == 1 });
            }
            string title = (DataContext as SheetViewModel)?.Name ?? "Cyberpunk RED";
            File.WriteAllBytes(path, PdfWriter.Build(list, title));
        }

        // число цветовых каналов JPEG (маркер SOFn): 1 — оттенки серого, 3 — цвет
        public static int JpegComponents(byte[] jpeg)
        {
            int i = 2;
            while (i + 9 < jpeg.Length)
            {
                if (jpeg[i] != 0xFF) { i++; continue; }
                byte m = jpeg[i + 1];
                if (m >= 0xC0 && m <= 0xCF && m != 0xC4 && m != 0xC8 && m != 0xCC) return jpeg[i + 9];
                if (m == 0xD8 || m == 0x01 || (m >= 0xD0 && m <= 0xD7) || m == 0xFF) { i += (m == 0xFF) ? 1 : 2; continue; }
                i += 2 + (jpeg[i + 2] << 8 | jpeg[i + 3]);
            }
            return 3;
        }

        // ScrollViewer'ы вкладки, в которых реально есть что прокручивать (внутренние полоски списков не считаем)
        private static List<ScrollViewer> FindScrollers(DependencyObject root)
        {
            var found = new List<ScrollViewer>();
            if (root == null) return found;
            var stack = new Stack<DependencyObject>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var o = stack.Pop();
                if (o is ScrollViewer sv && sv.ScrollableHeight > 8 && sv.ViewportHeight > 40) found.Add(sv);
                int n = (o is Visual || o is System.Windows.Media.Media3D.Visual3D) ? VisualTreeHelper.GetChildrenCount(o) : 0;
                for (int k = 0; k < n; k++) stack.Push(VisualTreeHelper.GetChild(o, k));
            }
            return found;
        }

        // рисует выбранные вкладки (индексы 0–4) в картинки; выбранная вкладка и панель кнопок возвращаются на место
        public List<BitmapSource> RenderSheetPages(IList<int> tabs, bool eco, double scale)
        {
            var result = new List<BitmapSource>();
            int saved = SheetTabControl.SelectedIndex;
            var barVisibility = TopBar.Visibility;
            TopBar.Visibility = Visibility.Hidden;
            var miniVisibility = MiniSheet.Visibility;
            MiniSheet.Visibility = Visibility.Hidden;
            bool side = _sideActive;
            if (side) SetLayoutGeometry(false);
            try
            {
                foreach (int tab in tabs)
                {
                    if (tab < 0 || tab >= SheetTabControl.Items.Count) continue;
                    SheetTabControl.SelectedIndex = tab;
                    SheetCanvas.UpdateLayout();
                    Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
                    SheetCanvas.UpdateLayout();

                    // вкладка с прокруткой (длинная биография и т. п.) уходит в несколько страниц: каждая — следующий экран прокрутки
                    var scrollers = FindScrollers(SheetTabControl.SelectedContent as DependencyObject);
                    int extra = scrollers.Count == 0 ? 0 : Math.Min(5, scrollers.Max(sv => (int)Math.Ceiling(sv.ScrollableHeight / Math.Max(1, sv.ViewportHeight))));
                    for (int part = 0; part <= extra; part++)
                    {
                        foreach (var sv in scrollers) sv.ScrollToVerticalOffset(Math.Min(sv.ScrollableHeight, part * sv.ViewportHeight));
                        SheetCanvas.UpdateLayout();
                        Dispatcher.Invoke(() => { }, DispatcherPriority.Render);

                        int w = (int)(ExportBaseWidth * scale), h = (int)(ExportBaseHeight * scale);
                        var dv = new DrawingVisual();
                        using (var dc = dv.RenderOpen())
                        {
                            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(5, 5, 5)), null, new Rect(0, 0, w, h));
                            dc.DrawRectangle(new VisualBrush(SheetCanvas) { Stretch = Stretch.Fill }, null, new Rect(0, 0, w, h));
                        }
                        var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                        rtb.Render(dv);
                        rtb.Freeze();
                        result.Add(eco ? MakeEco(rtb) : (BitmapSource)rtb);
                    }
                    foreach (var sv in scrollers) sv.ScrollToVerticalOffset(0);
                }
            }
            finally
            {
                if (side) SetLayoutGeometry(true);
                TopBar.Visibility = barVisibility;
                MiniSheet.Visibility = miniVisibility;
                SheetTabControl.SelectedIndex = saved;
            }
            return result;
        }

        // оттенки серого с инверсией: фон белеет, светлый текст темнеет
        public static BitmapSource MakeEco(BitmapSource src)
        {
            var g = new FormatConvertedBitmap(src, PixelFormats.Gray8, null, 0);
            int w = g.PixelWidth, h = g.PixelHeight;
            var px = new byte[w * h];
            g.CopyPixels(px, w, 0);
            for (int i = 0; i < px.Length; i++)
            {
                int v = (int)(px[i] * EcoContrast);
                px[i] = (byte)(255 - (v > 255 ? 255 : v));
            }
            var res = BitmapSource.Create(w, h, 96, 96, PixelFormats.Gray8, null, px, w);
            res.Freeze();
            return res;
        }

        private void PrintSheet(List<int> pages, bool eco)
        {
            var dlg = new PrintDialog();
            if (dlg.ShowDialog() != true) return;
            try
            {
                dlg.PrintTicket.PageOrientation = PageOrientation.Landscape;
                double pw = dlg.PrintableAreaWidth, ph = dlg.PrintableAreaHeight;
                if (pw < ph) { var t = pw; pw = ph; ph = t; }

                var doc = new FixedDocument();
                doc.DocumentPaginator.PageSize = new Size(pw, ph);
                foreach (var img in RenderSheetPages(pages, eco, ExportScale))
                {
                    var page = new FixedPage { Width = pw, Height = ph, Background = Brushes.White };
                    var image = new Image { Source = img, Width = pw, Height = ph, Stretch = Stretch.Uniform };
                    page.Children.Add(image);
                    var content = new PageContent();
                    ((System.Windows.Markup.IAddChild)content).AddChild(page);
                    doc.Pages.Add(content);
                }
                dlg.PrintDocument(doc.DocumentPaginator, "Cyberpunk RED — " + ((DataContext as SheetViewModel)?.Name ?? ""));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Не удалось напечатать:\n" + ex.Message, "ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
