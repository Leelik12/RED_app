using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CyberpunkRED_Generator
{
    // Карта Найт-Сити: схема из 24 районов корника, поиск по банде / месту / менеджеру,
    // слои (недвижимость DLC, ночные рынки, свои метки), карточка района справа.
    public static class MapView
    {
        private const double CanvasW = 800, CanvasH = 950, Node = 50, PinSize = 26;
        private const string RockKey = "ROCK";

        private static string KindBrush(string kind)
        {
            switch (kind)
            {
                case "executive": return "TextBrush";
                case "rebuilding": return "AccentBrush";
                case "suburbs": return "WarnBrush";
                case "perimeter": return "MutedBrush";
                default: return "DangerBrush";   // combat, hot
            }
        }

        public static Window ShowWindow(Window owner, SheetViewModel vm, string select = null)
        {
            var win = Ui.Dialog(owner, "КАРТА НАЙТ-СИТИ", 1280, 860);
            var root = new Grid { Margin = new Thickness(12) };
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 360 });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(470) });

            // слева: поиск, слои и схема
            var left = new DockPanel { Margin = new Thickness(0, 0, 12, 0) };
            var top = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
            top.Children.Add(Ui.Text("НАЙТ-СИТИ", "DangerBrush", 16, true));
            var search = Ui.Input(220);
            search.Margin = new Thickness(14, 0, 0, 0);
            search.ToolTip = "Банда, место, менеджер или район — подсветит подходящие районы";
            top.Children.Add(search);
            var addPin = Ui.Button("＋ МЕТКА", "WarnBrush");
            addPin.Margin = new Thickness(14, 0, 0, 0);
            addPin.ToolTip = "Нажмите, затем щёлкните по карте — появится своя метка";
            top.Children.Add(addPin);
            var zoomIn = Ui.Button("＋"); var zoomOut = Ui.Button("−"); var zoomFit = Ui.Button("⌂");
            zoomIn.Margin = new Thickness(14, 0, 0, 0); zoomOut.Margin = new Thickness(4, 0, 0, 0); zoomFit.Margin = new Thickness(4, 0, 0, 0);
            zoomIn.ToolTip = "Приблизить (или колесо мыши)"; zoomOut.ToolTip = "Отдалить"; zoomFit.ToolTip = "Показать всю карту";
            top.Children.Add(zoomIn); top.Children.Add(zoomOut); top.Children.Add(zoomFit);
            DockPanel.SetDock(top, Dock.Top);
            left.Children.Add(top);

            var layerBar = new WrapPanel { Margin = new Thickness(0, 0, 0, 4) };
            DockPanel.SetDock(layerBar, Dock.Top);
            left.Children.Add(layerBar);

            var legend = new WrapPanel { Margin = new Thickness(0, 0, 0, 6) };
            foreach (var k in new[] { "executive", "rebuilding", "suburbs", "perimeter", "combat", "hot" })
            {
                var t = Ui.Text("● " + CityMap.KindName(k), KindBrush(k), 12);
                t.Margin = new Thickness(0, 0, 14, 0);
                legend.Children.Add(t);
            }
            legend.Children.Add(Ui.Text("колесо мыши — масштаб, перетаскивание — сдвиг; цифры у районов — недвижимость DLC, 🌙 — ночной рынок; положение районов приблизительно", "MutedBrush", 12));
            DockPanel.SetDock(legend, Dock.Top);
            left.Children.Add(legend);

            // карта с с. 310 корника берётся из встроенного ресурса; без неё остаётся схема районов
            var mapImage = LoadMapImage();
            double cw = mapImage != null ? mapImage.PixelWidth : CanvasW, ch = mapImage != null ? mapImage.PixelHeight : CanvasH;
            double u = mapImage != null ? cw / 1089.0 : 1;        // единицы значков: карта большая, значки растут вместе с ней
            double nodeSize = mapImage != null ? 44 * u : Node;
            double pinSize = PinSize * u;
            var canvas = new Canvas { Width = cw, Height = ch, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            if (mapImage != null)
            {
                canvas.Background = Brushes.Black;
                canvas.Children.Add(new Image { Source = mapImage, Width = cw, Height = ch, Stretch = Stretch.Fill, IsHitTestVisible = false });
            }
            else canvas.SetResourceReference(Panel.BackgroundProperty, "PanelBrush");
            var scale = new ScaleTransform(1, 1);
            var move = new TranslateTransform();
            canvas.RenderTransform = new TransformGroup { Children = { scale, move } };
            var host = new Grid { ClipToBounds = true, Background = Brushes.Transparent, MinHeight = 300 };
            host.Children.Add(canvas);
            left.Children.Add(Ui.Box(host, "AccentDarkBrush", "PanelBrush", 4));
            Grid.SetColumn(left, 0);
            root.Children.Add(left);

            var info = new StackPanel();
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = info };
            Grid.SetColumn(scroll, 1);
            root.Children.Add(scroll);

            var nodes = new Dictionary<string, Border>();
            var badges = new Dictionary<string, TextBlock>();
            var pinViews = new Dictionary<string, Border>();
            string current = null;                // выбранный район (или RockKey); у выбранной метки — "pin:<id>"
            bool placing = false;

            // подписи сторон города (на растровой карте не нужны)
            foreach (var g in CityMap.Districts.GroupBy(d => d.Side).Where(_ => mapImage == null))
            {
                var cap = Ui.Text(g.Key.ToUpperInvariant(), "MutedBrush", 13, true);
                cap.Opacity = 0.55;
                double cx = g.Average(d => d.X), cy = g.Min(d => d.Y) - 52;
                cap.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(cap, cx - cap.DesiredSize.Width / 2);
                Canvas.SetTop(cap, cy);
                canvas.Children.Add(cap);
            }

            double fit = 1, zoom = 1;
            bool userMoved = false;
            Action applyScale = null;
            Action<string> select_ = null;
            Action<string> render = null;
            Action refreshPins = null;

            Func<string, string> badgeText = letter =>
            {
                int props = CityMap.VisibleProperties(letter == RockKey ? CityMap.MorroRock : letter).Count;
                bool mk = letter != RockKey && CityMap.VisibleMarkets(letter).Count > 0;
                return (props > 0 ? "🏠" + props : "") + (mk ? "🌙" : "");
            };

            Action refreshBadges = () =>
            {
                foreach (var kv in badges) kv.Value.Text = badgeText(kv.Key);
            };

            Border MakeNode(string key, string label, double x, double y, string kind, string tip, bool approx)
            {
                var brush = KindBrush(kind);
                bool hot = kind == "hot";
                var letter = Ui.Text(label, hot ? "BackBrush" : brush, mapImage != null ? 18 * u : 20, true);
                letter.HorizontalAlignment = HorizontalAlignment.Center;
                var badge = Ui.Text("", hot ? "BackBrush" : "TextBrush", 10 * u);
                badge.HorizontalAlignment = HorizontalAlignment.Center;
                badges[key] = badge;
                var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                stack.Children.Add(letter);
                stack.Children.Add(badge);
                var node = new Border
                {
                    Width = nodeSize, Height = nodeSize, CornerRadius = new CornerRadius(nodeSize / 2), RenderTransformOrigin = new Point(0.5, 0.5), BorderThickness = new Thickness((approx ? 1.5 : 2.5) * u),
                    Child = stack, Cursor = Cursors.Hand, ToolTip = tip
                };
                node.SetResourceReference(Border.BorderBrushProperty, brush);
                node.SetResourceReference(Border.BackgroundProperty, hot ? brush : "BackBrush");
                if (approx) node.Opacity = 0.9;
                Canvas.SetLeft(node, x - nodeSize / 2);
                Canvas.SetTop(node, y - nodeSize / 2);
                node.MouseLeftButtonUp += (s, e) => { if (!placing) select_(key); e.Handled = !placing; };
                canvas.Children.Add(node);
                nodes[key] = node;
                return node;
            }

            foreach (var d in CityMap.Districts)
                MakeNode(d.Letter, d.Letter, mapImage != null && d.MX > 0 ? d.MX : d.X, mapImage != null && d.MX > 0 ? d.MY : d.Y, d.Kind, d.Letter + " — " + d.Name + (d.Approx ? " (положение приблизительно)" : ""), d.Approx);
            MakeNode(RockKey, "МР", mapImage != null ? 470 : 120, mapImage != null ? 1660 : 360, "perimeter", "Морро-Рок — остров-убежище кочевников и неформалов", false);

            Func<Window> parent = () => owner ?? win;

            Func<string, string, UIElement> Section = (title, text) =>
            {
                var p = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
                p.Children.Add(Ui.Text(title.ToUpperInvariant(), "DangerBrush", 12, true));
                p.Children.Add(Ui.Text(string.IsNullOrWhiteSpace(text) ? "—" : text, "TextBrush", 13));
                return p;
            };

            // свои метки: рисуются поверх карты, выбираются щелчком, правятся в карточке
            refreshPins = () =>
            {
                foreach (var v in pinViews.Values) canvas.Children.Remove(v);
                pinViews.Clear();
                if (!CityMap.LayerOn("custom")) return;
                foreach (var pin in AppSettings.Data.MapPins)
                {
                    var p = pin;
                    var tb = Ui.Text("📍", "WarnBrush", 16 * u);
                    tb.HorizontalAlignment = HorizontalAlignment.Center;
                    var b = new Border
                    {
                        Width = pinSize, Height = pinSize, CornerRadius = new CornerRadius(pinSize / 2), BorderThickness = new Thickness(2 * u), RenderTransformOrigin = new Point(0.5, 0.5),
                        Child = tb, Cursor = Cursors.Hand, ToolTip = p.Title
                    };
                    b.SetResourceReference(Border.BorderBrushProperty, "WarnBrush");
                    b.SetResourceReference(Border.BackgroundProperty, "BackBrush");
                    Canvas.SetLeft(b, p.X - pinSize / 2);
                    Canvas.SetTop(b, p.Y - pinSize / 2);
                    b.MouseLeftButtonUp += (s, e) => { if (!placing) select_("pin:" + p.Id); e.Handled = !placing; };
                    canvas.Children.Add(b);
                    pinViews[p.Id] = b;
                }
                applyScale?.Invoke();
            };

            render = key =>
            {
                info.Children.Clear();
                if (key != null && key.StartsWith("pin:"))
                {
                    var pin = AppSettings.Data.MapPins.FirstOrDefault(x => x.Id == key.Substring(4));
                    if (pin == null) return;
                    info.Children.Add(Ui.Text("📍 Своя метка", "WarnBrush", 18, true));
                    var title = Ui.Input();
                    title.Text = pin.Title;
                    title.Margin = new Thickness(0, 8, 0, 6);
                    title.TextChanged += (s, e) => { pin.Title = title.Text; if (pinViews.TryGetValue(pin.Id, out var v)) v.ToolTip = pin.Title; AppSettings.Save(); };
                    info.Children.Add(title);
                    var note = Ui.Input();
                    note.AcceptsReturn = true; note.TextWrapping = TextWrapping.Wrap; note.MinHeight = 160; note.VerticalContentAlignment = VerticalAlignment.Top;
                    note.Text = pin.Note;
                    note.TextChanged += (s, e) => { pin.Note = note.Text; AppSettings.Save(); };
                    info.Children.Add(note);
                    var del = Ui.Button("УДАЛИТЬ МЕТКУ", "DangerBrush");
                    del.HorizontalAlignment = HorizontalAlignment.Left;
                    del.Margin = new Thickness(0, 8, 0, 0);
                    del.Click += (s, e) => { CityMap.RemovePin(pin.Id); refreshPins(); current = null; info.Children.Clear(); };
                    info.Children.Add(del);
                    return;
                }
                if (key == RockKey)
                {
                    info.Children.Add(Ui.Text("Морро-Рок", "AccentBrush", 20, true));
                    info.Children.Add(Ui.Text("Скала в заливе за пределами районов Найт-Сити. На карте показана для привязки недвижимости DLC.", "MutedBrush", 12));
                    AddProperties(info, parent(), CityMap.VisibleProperties(CityMap.MorroRock));
                    return;
                }
                var d = CityMap.Get(key);
                if (d == null) return;
                info.Children.Add(Ui.Text(d.Letter + " · " + d.Name, "AccentBrush", 20, true));
                info.Children.Add(Ui.Text(d.Side + " · " + CityMap.KindName(d.Kind) + (d.Approx ? " · положение на схеме приблизительно" : ""), "MutedBrush", 12));
                info.Children.Add(Section("Сити-менеджер", d.Manager));
                info.Children.Add(Section("Безопасность", d.Security));
                info.Children.Add(Section("Банды", d.Gangs));

                var markets = CityMap.VisibleMarkets(d.Letter);
                if (markets.Count > 0)
                {
                    var mp = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
                    mp.Children.Add(Ui.Text("🌙 НОЧНЫЕ РЫНКИ", "DangerBrush", 12, true));
                    foreach (var m in markets)
                    {
                        var b = Ui.Button(m.Name, "WarnBrush");
                        b.HorizontalContentAlignment = HorizontalAlignment.Left;
                        b.Margin = new Thickness(0, 3, 0, 0);
                        b.Click += (s, e) => NightMarketView.ShowWindow(parent(), vm);
                        mp.Children.Add(b);
                    }
                    info.Children.Add(mp);
                }

                AddProperties(info, parent(), CityMap.VisibleProperties(d.Letter));

                if (d.Places.Count > 0)
                {
                    var pl = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
                    pl.Children.Add(Ui.Text("ЗНАЧИМЫЕ МЕСТА", "DangerBrush", 12, true));
                    string q = search.Text.Trim();
                    foreach (var p in d.Places)
                    {
                        bool hit = q.Length > 0 && ((p.Name ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 || (p.Text ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
                        var h = Ui.Text(p.Code + "  " + p.Name, hit ? "WarnBrush" : "AccentBrush", 14, true);
                        h.Margin = new Thickness(0, 8, 0, 0);
                        pl.Children.Add(h);
                        pl.Children.Add(Ui.Text(p.Text, "TextBrush", 13));
                    }
                    info.Children.Add(pl);
                }
                else
                {
                    var none = Ui.Text("В книге для района не описаны отдельные места.", "MutedBrush", 12);
                    none.Margin = new Thickness(0, 12, 0, 0);
                    info.Children.Add(none);
                }
            };

            select_ = key =>
            {
                current = key;
                foreach (var kv in nodes)
                {
                    bool sel = kv.Key == key;
                    kv.Value.BorderThickness = new Thickness((sel ? 4 : 2.5) * u);
                    var d = CityMap.Get(kv.Key);
                    kv.Value.SetResourceReference(Border.BackgroundProperty, sel ? "RaisedBrush" : d != null && d.Kind == "hot" ? "DangerBrush" : "BackBrush");
                }
                foreach (var kv in pinViews) kv.Value.BorderThickness = new Thickness((key == "pin:" + kv.Key ? 4 : 2) * u);
                render(key);
            };

            // слои
            foreach (var layer in CityMap.Layers)
            {
                var id = layer.Id;
                var cb = new CheckBox { Content = layer.Name, IsChecked = CityMap.LayerOn(id), Margin = new Thickness(0, 0, 14, 0), VerticalAlignment = VerticalAlignment.Center };
                cb.SetResourceReference(Control.ForegroundProperty, "TextBrush");
                Action apply = () =>
                {
                    CityMap.SetLayer(id, cb.IsChecked == true);
                    refreshBadges();
                    refreshPins();
                    if (current != null && !current.StartsWith("pin:")) render(current);
                };
                cb.Checked += (s, e) => apply();
                cb.Unchecked += (s, e) => apply();
                layerBar.Children.Add(cb);
            }

            // размещение метки: кнопка включает режим, следующий щелчок по холсту ставит метку
            addPin.Click += (s, e) =>
            {
                placing = !placing;
                canvas.Cursor = placing ? Cursors.Cross : null;
                addPin.Content = placing ? "щёлкните по карте…" : "＋ МЕТКА";
                if (placing && !CityMap.LayerOn("custom")) { CityMap.SetLayer("custom", true); }
            };
            canvas.MouseLeftButtonUp += (s, e) =>
            {
                if (!placing) return;
                var pos = e.GetPosition(canvas);
                var pin = CityMap.AddPin(pos.X, pos.Y);
                placing = false;
                canvas.Cursor = null;
                addPin.Content = "＋ МЕТКА";
                foreach (var child in layerBar.Children.OfType<CheckBox>()) if (child.Content as string == CityMap.Layers.First(l => l.Id == "custom").Name) child.IsChecked = true;
                refreshPins();
                select_("pin:" + pin.Id);
            };

            search.TextChanged += (s, e) =>
            {
                string q = search.Text;
                foreach (var d in CityMap.Districts)
                    nodes[d.Letter].Opacity = CityMap.Matches(d, q) ? 1 : 0.2;
                nodes[RockKey].Opacity = string.IsNullOrWhiteSpace(q) ? 1 : 0.2;
                if (current != null && !current.StartsWith("pin:")) render(current);
            };

            // масштаб и сдвиг: колесо — к курсору, перетаскивание — сдвиг, значки не растут вместе с картой
            applyScale = () =>
            {
                scale.ScaleX = scale.ScaleY = zoom;
                double k = Math.Pow(fit / zoom, 0.5);
                foreach (var n in nodes.Values) n.RenderTransform = new ScaleTransform(k, k);
                foreach (var n in pinViews.Values) n.RenderTransform = new ScaleTransform(k, k);
            };
            Action fitView = () =>
            {
                if (host.ActualWidth < 10 || host.ActualHeight < 10) return;
                fit = Math.Min(host.ActualWidth / cw, host.ActualHeight / ch);
                // растровая карта вытянута: показываем город крупнее, а не всю картинку целиком
                zoom = mapImage != null ? Math.Max(fit, Math.Min(host.ActualWidth / cw, host.ActualHeight / (ch * 0.55))) : fit;
                double fx = cw / 2, fy = ch / 2;
                if (mapImage != null && CityMap.Districts.Count > 0)
                {
                    fx = CityMap.Districts.Where(d => d.MX > 0).Average(d => d.MX);
                    fy = CityMap.Districts.Where(d => d.MX > 0).Average(d => d.MY);
                }
                move.X = host.ActualWidth / 2 - fx * zoom;
                move.Y = host.ActualHeight / 2 - fy * zoom;
                applyScale();
            };
            Action<double, Point> zoomAt = (factor, at) =>
            {
                double nz = Math.Max(fit * 0.8, Math.Min(fit * 10, zoom * factor));
                factor = nz / zoom;
                move.X = at.X - (at.X - move.X) * factor;
                move.Y = at.Y - (at.Y - move.Y) * factor;
                zoom = nz;
                userMoved = true;
                applyScale();
            };
            Func<Point> center = () => new Point(host.ActualWidth / 2, host.ActualHeight / 2);
            zoomIn.Click += (s, e) => zoomAt(1.4, center());
            zoomOut.Click += (s, e) => zoomAt(1 / 1.4, center());
            zoomFit.Click += (s, e) => { userMoved = false; fitView(); };
            host.SizeChanged += (s, e) => { if (!userMoved) fitView(); };
            host.MouseWheel += (s, e) => { zoomAt(e.Delta > 0 ? 1.2 : 1 / 1.2, e.GetPosition(host)); e.Handled = true; };

            Point dragFrom = default(Point), moveFrom = default(Point);
            bool pressed = false, dragged = false;
            host.PreviewMouseLeftButtonDown += (s, e) => { pressed = true; dragged = false; dragFrom = e.GetPosition(host); moveFrom = new Point(move.X, move.Y); };
            host.PreviewMouseMove += (s, e) =>
            {
                if (!pressed || e.LeftButton != MouseButtonState.Pressed) return;
                var p = e.GetPosition(host);
                if (!dragged && Math.Abs(p.X - dragFrom.X) + Math.Abs(p.Y - dragFrom.Y) < 5) return;
                if (!dragged) { dragged = true; host.CaptureMouse(); }
                move.X = moveFrom.X + p.X - dragFrom.X;
                move.Y = moveFrom.Y + p.Y - dragFrom.Y;
                userMoved = true;
            };
            host.PreviewMouseLeftButtonUp += (s, e) =>
            {
                pressed = false;
                if (!dragged) return;
                host.ReleaseMouseCapture();
                dragged = false;
                e.Handled = true;       // сдвиг карты не должен считаться щелчком по району или метке
            };

            win.Content = root;
            refreshBadges();
            refreshPins();
            select_(!string.IsNullOrEmpty(select) && nodes.ContainsKey(select) ? select : CityMap.Districts.Count > 0 ? CityMap.Districts[0].Letter : RockKey);
            Ui.Present(win);
            return win;
        }

        // растр карты (с. 312–313 русского издания, две половины склеены build_map_image.py) лежит в Content/night_city_map.jpg и встраивается при сборке
        private static System.Windows.Media.Imaging.BitmapImage LoadMapImage()
        {
            try
            {
                using (var s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Content.night_city_map.jpg"))
                {
                    if (s == null) return null;
                    var ms = new System.IO.MemoryStream();
                    s.CopyTo(ms);
                    ms.Position = 0;
                    var img = new System.Windows.Media.Imaging.BitmapImage();
                    img.BeginInit();
                    img.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    img.StreamSource = ms;
                    img.EndInit();
                    img.Freeze();
                    return img;
                }
            }
            catch (Exception ex) { ErrorLog.Write("map", ex); return null; }
        }

        private static void AddProperties(StackPanel info, Window owner, List<PackItem> props)
        {
            if (props.Count == 0) return;
            var pp = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
            pp.Children.Add(Ui.Text("🏠 НЕДВИЖИМОСТЬ (DLC)", "DangerBrush", 12, true));
            foreach (var it in props)
            {
                var row = new DockPanel { Margin = new Thickness(0, 4, 0, 0) };
                string rent = it.Get("rentNote") ?? (Housing.RentOf(it) + " eb в месяц");
                var t = Ui.Text(it.Name + "  — " + it.Get("code") + ", аренда " + rent + "  · " + Packs.PackName(it.Pack), "TextBrush", 13);
                t.ToolTip = string.IsNullOrWhiteSpace(it.Info) ? null : (it.Info.Length > 600 ? it.Info.Substring(0, 600) + "…" : it.Info);
                row.Children.Add(t);
                pp.Children.Add(row);
            }
            info.Children.Add(pp);
        }
    }
}
