using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CyberpunkRED_Generator
{
    // окно «Транспорт» листа: гараж персонажа (ПСП, скорость, места, тюнинг) и добавление из каталога
    public static class VehiclesView
    {
        public static Window ShowWindow(Window owner, SheetViewModel vm)
        {
            if (vm == null) return null;
            var win = Ui.Dialog(owner, "ТРАНСПОРТ", 720, 720);
            var root = new StackPanel { Margin = new Thickness(14) };
            root.Children.Add(Ui.Text("ТРАНСПОРТ", "DangerBrush", 16, true));
            root.Children.Add(Ui.Text("Транспорт ломается как здоровье: при 0 ПСП он уничтожен и не может двигаться до ремонта. При ПСП меньше половины в погоне скорость считается на категорию ниже.", "MutedBrush", 12));

            // добавление
            var catalog = Vehicles.All();
            var add = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 6) };
            var pick = new ComboBox { Width = 330 };
            foreach (var d in catalog) pick.Items.Add(d.Name + " — " + d.Kind.ToLower() + (d.Source == Packs.CoreName ? "" : " [" + d.Source + "]"));
            pick.SelectedIndex = 0;
            var info = Ui.Text("", "MutedBrush", 12);
            var addBtn = Ui.Button("+ ДОБАВИТЬ");
            var custom = Ui.Button("СВОЙ");
            add.Children.Add(pick);
            add.Children.Add(new Border { Width = 8 });
            add.Children.Add(addBtn);
            add.Children.Add(new Border { Width = 6 });
            add.Children.Add(custom);
            root.Children.Add(add);
            root.Children.Add(info);
            Action showInfo = () =>
            {
                var d = catalog[Math.Max(0, pick.SelectedIndex)];
                info.Text = d.Description + "\nПСП " + d.Sdp + " · места " + d.Seats + " · СКО " + d.Combat + " · " + d.Narrative + " · " + GearCatalog.CostText(d.Cost);
            };
            pick.SelectionChanged += (s, e) => showInfo();
            showInfo();

            var list = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
            root.Children.Add(list);

            Action rebuild = null;
            rebuild = () =>
            {
                list.Children.Clear();
                if (vm.Vehicles.Count == 0) list.Children.Add(Ui.Text("Транспорта нет. Добавьте из каталога или создайте свой.", "MutedBrush", 13));
                foreach (var v in vm.Vehicles.ToList())
                {
                    var veh = v;
                    var card = new StackPanel();
                    var top = new StackPanel { Orientation = Orientation.Horizontal };
                    var name = Ui.Input(260); name.Text = veh.Name; name.FontSize = 14;
                    name.TextChanged += (s, e) => veh.Name = name.Text;
                    var state = Ui.Text("", "WarnBrush", 12, true);
                    state.Margin = new Thickness(10, 0, 0, 0);
                    Action upd = () => state.Text = veh.State.ToUpper();
                    upd();
                    top.Children.Add(name); top.Children.Add(state);
                    card.Children.Add(top);

                    var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
                    Func<string, string, TextBox> field = (label, init) =>
                    {
                        row.Children.Add(Ui.Text(label, "MutedBrush", 12));
                        var t = Ui.Input(48); t.Text = init; t.Margin = new Thickness(4, 0, 12, 0);
                        row.Children.Add(t);
                        return t;
                    };
                    var sdp = field("ПСП", veh.Sdp.ToString());
                    var max = field("макс", veh.MaxSdp.ToString());
                    var combat = field("СКО", veh.Combat.ToString());
                    var seats = field("места", veh.Seats.ToString());
                    sdp.TextChanged += (s, e) => { if (int.TryParse(sdp.Text, out int n)) { veh.Sdp = n; upd(); } };
                    max.TextChanged += (s, e) => { if (int.TryParse(max.Text, out int n)) { veh.MaxSdp = n; upd(); } };
                    combat.TextChanged += (s, e) => { if (int.TryParse(combat.Text, out int n)) veh.Combat = n; };
                    seats.TextChanged += (s, e) => { if (int.TryParse(seats.Text, out int n)) veh.Seats = n; };
                    var dmg = Ui.Button("−5"); var heal = Ui.Button("+5"); var full = Ui.Button("ЧИНИТЬ");
                    dmg.Click += (s, e) => { veh.Sdp -= 5; sdp.Text = veh.Sdp.ToString(); };
                    heal.Click += (s, e) => { veh.Sdp = Math.Min(veh.MaxSdp, veh.Sdp + 5); sdp.Text = veh.Sdp.ToString(); };
                    full.Click += (s, e) => { veh.Sdp = veh.MaxSdp; sdp.Text = veh.Sdp.ToString(); };
                    row.Children.Add(dmg); row.Children.Add(new Border { Width = 4 }); row.Children.Add(heal); row.Children.Add(new Border { Width = 4 }); row.Children.Add(full);
                    card.Children.Add(row);

                    card.Children.Add(Ui.Text("Тюнинг и заметки", "MutedBrush", 12));
                    var notes = Ui.Input(); notes.AcceptsReturn = true; notes.TextWrapping = TextWrapping.Wrap; notes.MinHeight = 44;
                    notes.Text = veh.Notes; notes.TextChanged += (s, e) => veh.Notes = notes.Text;
                    card.Children.Add(notes);

                    var del = Ui.Button("УДАЛИТЬ", "DangerBrush");
                    del.HorizontalAlignment = HorizontalAlignment.Left; del.Margin = new Thickness(0, 6, 0, 0);
                    del.Click += (s, e) =>
                    {
                        if (MessageBox.Show(win, "Убрать «" + veh.Name + "» из гаража?", "ТРАНСПОРТ", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                        vm.Vehicles.Remove(veh); rebuild();
                    };
                    card.Children.Add(del);
                    var box = Ui.Box(card, pad: 10);
                    box.Margin = new Thickness(0, 0, 0, 8);
                    list.Children.Add(box);
                }
            };

            addBtn.Click += (s, e) => { vm.Vehicles.Add(Vehicles.Own(catalog[Math.Max(0, pick.SelectedIndex)])); rebuild(); };
            custom.Click += (s, e) => { vm.Vehicles.Add(new OwnedVehicle { Name = "Мой транспорт", Kind = Vehicles.KindGround, MaxSdp = 50, Sdp = 50, Seats = 2, Combat = 20 }); rebuild(); };
            rebuild();

            win.Content = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Ui.Present(win);
            return win;
        }
    }
}
