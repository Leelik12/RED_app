using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace CyberpunkRED_Generator
{
    // трекер погони (DLC «Hot Pursuit»): сетка, проверки позиционирования, манёвры, расстояния
    public static class ChaseView
    {
        private class Unit
        {
            public string Name;
            public int Combat = 20, Sdp = 50, MaxSdp = 50, Pos, Bonus = 8, SlowSteps;
            public bool Crashed;
        }

        public static Window ShowWindow(Window owner, SheetViewModel vm)
        {
            var win = Ui.Dialog(owner, "ПОГОНЯ (HOT PURSUIT)", 820, 780);
            var units = new List<Unit>();
            int round = 1;

            var root = new StackPanel { Margin = new Thickness(14) };
            root.Children.Add(Ui.Text("ПОГОНЯ", "DangerBrush", 16, true));
            var roundText = Ui.Text("", "AccentBrush", 14, true);
            root.Children.Add(roundText);

            // добавление участников
            var sources = new List<VehicleDef>();
            if (vm != null) foreach (var o in vm.Vehicles) sources.Add(new VehicleDef { Name = o.Name + " (мой)", Sdp = o.Sdp, Combat = o.Combat, Seats = o.Seats, Kind = o.Kind, Description = "" });
            sources.AddRange(Vehicles.All());
            var add = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 6) };
            var pick = new ComboBox { Width = 300 };
            foreach (var d in sources) pick.Items.Add(d.Name + " (СКО " + d.Combat + ", ПСП " + d.Sdp + ")");
            pick.SelectedIndex = 0;
            var addBtn = Ui.Button("+ УЧАСТНИК");
            var next = Ui.Button("КОНЕЦ РАУНДА ▶", "DangerBrush");
            var reset = Ui.Button("СБРОС");
            add.Children.Add(pick); add.Children.Add(new Border { Width = 6 }); add.Children.Add(addBtn);
            add.Children.Add(new Border { Width = 18 }); add.Children.Add(next); add.Children.Add(new Border { Width = 6 }); add.Children.Add(reset);
            root.Children.Add(add);

            var gridText = Ui.Text("", "TextBrush", 12);
            gridText.FontFamily = new System.Windows.Media.FontFamily(Ui.FontName);
            var gridBox = Ui.Box(gridText, pad: 10);
            root.Children.Add(gridBox);

            var rows = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
            root.Children.Add(rows);
            var log = Ui.Text("", "MutedBrush", 12);
            log.Margin = new Thickness(0, 6, 0, 0);

            var logLines = new List<string>();
            Action<string> say = m => { logLines.Insert(0, "Р" + round + ": " + m); if (logLines.Count > 30) logLines.RemoveAt(logLines.Count - 1); log.Text = string.Join("\n", logLines); };

            Action redraw = null;
            Action drawGrid = () =>
            {
                roundText.Text = "Раунд " + round + (Chase.IsOver(units.Where(u => !u.Crashed).Select(u => u.Pos)) ? " — ПОГОНЯ ОКОНЧЕНА (разрыв больше 8 клеток)" : "");
                if (units.Count == 0) { gridText.Text = "Добавьте участников. Дальше расстояние считается в клетках сетки погони."; return; }
                int min = units.Min(u => u.Pos), max = units.Max(u => u.Pos);
                var sb = new StringBuilder();
                foreach (var u in units.OrderByDescending(u => u.Pos))
                {
                    int width = Math.Min(40, max - min + 1);
                    int off = max - u.Pos; if (off > 39) off = 39;
                    sb.AppendLine((u.Name.Length > 22 ? u.Name.Substring(0, 22) : u.Name).PadRight(24) + new string('·', off) + (u.Crashed ? "✖" : "▶") + "  [" + u.Pos + "]");
                }
                var lead = units.OrderByDescending(u => u.Pos).ToList();
                sb.AppendLine();
                for (int i = 1; i < lead.Count; i++)
                {
                    int gap = lead[i - 1].Pos - lead[i].Pos;
                    sb.AppendLine(lead[i].Name + " → " + lead[i - 1].Name + ": " + gap + " кл. (" + Chase.RangeText(gap) + ")" + (Chase.Adjacent(gap) ? " — рядом" : ""));
                }
                gridText.Text = sb.ToString().TrimEnd();
            };

            redraw = () =>
            {
                rows.Children.Clear();
                foreach (var u0 in units.ToList())
                {
                    var u = u0;
                    var card = new StackPanel();
                    var head = new StackPanel { Orientation = Orientation.Horizontal };
                    var name = Ui.Input(220); name.Text = u.Name; name.TextChanged += (s, e) => { u.Name = name.Text; drawGrid(); };
                    head.Children.Add(name);
                    var dcText = Ui.Text("", "WarnBrush", 12, true); dcText.Margin = new Thickness(10, 0, 0, 0);
                    head.Children.Add(dcText);
                    card.Children.Add(head);

                    var f = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                    Action upd = null;
                    Func<string, string, Action<int>, TextBox> field = (label, init, set) =>
                    {
                        f.Children.Add(Ui.Text(label, "MutedBrush", 12));
                        var t = Ui.Input(44); t.Text = init; t.Margin = new Thickness(4, 0, 10, 0);
                        t.TextChanged += (s, e) => { if (int.TryParse(t.Text, out int n)) { set(n); upd(); } };
                        f.Children.Add(t);
                        return t;
                    };
                    upd = () =>
                    {
                        var dc = Chase.PositionDc(u.Combat, u.Sdp, u.MaxSdp, u.SlowSteps);
                        dcText.Text = u.Crashed ? "АВАРИЯ" : dc == null ? "ПОТЕРЯ УПРАВЛЕНИЯ" : "СЛ позиц. " + dc + (Chase.Damaged(u.Sdp, u.MaxSdp) ? " (повреждён)" : "") + (u.SlowSteps > 0 ? " · PIT" : "");
                    };
                    field("СКО", u.Combat.ToString(), n => u.Combat = n);
                    field("ПСП", u.Sdp.ToString(), n => u.Sdp = n);
                    field("макс", u.MaxSdp.ToString(), n => u.MaxSdp = n);
                    field("РЕА+навык", u.Bonus.ToString(), n => u.Bonus = n);
                    var pos = field("клетка", u.Pos.ToString(), n => { u.Pos = n; drawGrid(); });
                    card.Children.Add(f);
                    upd();

                    var b = new WrapPanel { Margin = new Thickness(0, 6, 0, 0) };
                    var roll = Ui.Button("🎲 ПОЗИЦИОНИРОВАНИЕ");
                    var hold = Ui.Button("ДЕРЖАТЬ");
                    var slow = Ui.Button("◀ ЗАМЕДЛИТЬСЯ");
                    var del = Ui.Button("✖", "DangerBrush");
                    roll.Click += (s, e) =>
                    {
                        if (u.Crashed) return;
                        var dc = Chase.PositionDc(u.Combat, u.Sdp, u.MaxSdp, u.SlowSteps);
                        if (dc == null) { u.Crashed = true; say(u.Name + " теряет управление и разбивается."); upd(); drawGrid(); return; }
                        int r = DiceRoller.D10Check(out string detail);
                        int total = r + u.Bonus;
                        bool ok = total >= dc.Value;
                        u.SlowSteps = 0;
                        if (ok) { u.Pos++; pos.Text = u.Pos.ToString(); }
                        say(u.Name + ": позиционирование " + detail + " + " + u.Bonus + " = " + total + " против СЛ " + dc + " — " + (ok ? "вперёд на 1 клетку" : "остаётся на месте"));
                        upd(); drawGrid();
                    };
                    hold.Click += (s, e) => { u.SlowSteps = 0; say(u.Name + " держит скорость."); upd(); };
                    slow.Click += (s, e) => { u.SlowSteps = 0; u.Pos--; pos.Text = u.Pos.ToString(); say(u.Name + " замедляется и отходит на 1 клетку."); upd(); drawGrid(); };
                    del.Click += (s, e) => { units.Remove(u); redraw(); drawGrid(); };
                    b.Children.Add(roll); b.Children.Add(new Border { Width = 4 }); b.Children.Add(hold); b.Children.Add(new Border { Width = 4 });
                    b.Children.Add(slow); b.Children.Add(new Border { Width = 12 });

                    // PIT накладывает штраф цели до конца раунда
                    var targets = new ComboBox { Width = 170, Margin = new Thickness(0, 0, 4, 0) };
                    foreach (var other in units.Where(o => o != u)) targets.Items.Add(other.Name);
                    var man = new ComboBox { Width = 190, Margin = new Thickness(0, 0, 4, 0) };
                    foreach (var m in Chase.Maneuvers) man.Items.Add(m.Name + " (СЛ " + m.Dc + ")");
                    man.SelectedIndex = 0;
                    var go = Ui.Button("МАНЁВР");
                    go.Click += (s, e) =>
                    {
                        var m = Chase.Maneuvers[Math.Max(0, man.SelectedIndex)];
                        int r = DiceRoller.D10Check(out string detail);
                        int total = r + u.Bonus;
                        bool ok = total >= m.Dc;
                        string line = u.Name + ": " + m.Name + " " + detail + " + " + u.Bonus + " = " + total + " против СЛ " + m.Dc + " — " + (ok ? "успех" : "провал (потеря контроля или препятствие)");
                        if (ok)
                        {
                            if (m.Name == "NOS") { u.Pos++; pos.Text = u.Pos.ToString(); }
                            var tgt = targets.SelectedIndex >= 0 ? units.Where(o => o != u).ElementAtOrDefault(targets.SelectedIndex) : null;
                            if (m.Name == "Полицейский манёвр PIT" && tgt != null) { tgt.SlowSteps = 1; line += "; у «" + tgt.Name + "» скорость на категорию ниже до конца раунда"; }
                            if (m.Name == "Выход вперёд" && tgt != null) { u.Pos = tgt.Pos + 1; pos.Text = u.Pos.ToString(); }
                        }
                        say(line); upd(); drawGrid(); redraw();
                    };
                    b.Children.Add(man); b.Children.Add(targets); b.Children.Add(go); b.Children.Add(new Border { Width = 12 }); b.Children.Add(del);
                    card.Children.Add(b);
                    var box = Ui.Box(card, pad: 8); box.Margin = new Thickness(0, 0, 0, 6);
                    rows.Children.Add(box);
                }
            };

            addBtn.Click += (s, e) =>
            {
                var d = sources[Math.Max(0, pick.SelectedIndex)];
                units.Add(new Unit { Name = d.Name, Combat = d.Combat, Sdp = d.Sdp, MaxSdp = d.Sdp, Pos = units.Count == 0 ? 0 : units.Min(u => u.Pos) });
                redraw(); drawGrid();
            };
            next.Click += (s, e) => { foreach (var u in units) u.SlowSteps = 0; round++; redraw(); drawGrid(); };
            reset.Click += (s, e) => { units.Clear(); logLines.Clear(); log.Text = ""; round = 1; redraw(); drawGrid(); };

            root.Children.Add(Ui.Text("ЖУРНАЛ", "DangerBrush", 13, true));
            root.Children.Add(log);
            root.Children.Add(Ui.Text("МАНЁВРЫ", "DangerBrush", 13, true));
            foreach (var m in Chase.Maneuvers)
            {
                var t = Ui.Text(m.Name + " (СЛ " + m.Dc + (m.NeedsAdjacent ? ", только рядом" : "") + "): " + m.Text, "MutedBrush", 12);
                t.Margin = new Thickness(0, 3, 0, 0);
                root.Children.Add(t);
            }
            root.Children.Add(Ui.Text("ПРАВИЛА", "DangerBrush", 13, true));
            var rules = Ui.Text(Chase.Rules, "MutedBrush", 12); rules.Margin = new Thickness(0, 3, 0, 0);
            root.Children.Add(rules);

            redraw(); drawGrid();
            win.Content = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Ui.Present(win);
            return win;
        }
    }
}
