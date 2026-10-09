using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CyberpunkRED_Generator
{
    // деньги, образ жизни и жильё: расчёт ежемесячных трат и журнал операций
    public partial class CharacterSheetWindow
    {
        private const int FinanceLogCap = 200;

        public static int CurrentCash(SheetViewModel vm)
            => FinanceCatalog.TryParseMoney(vm.CashValue, out int v) ? v : 0;

        // меняет наличные и пишет операцию в журнал
        public static void ApplyMoney(SheetViewModel vm, int amount, string text)
        {
            if (vm.Finance == null) vm.Finance = new FinanceData();
            int balance = CurrentCash(vm) + amount;
            vm.CashValue = balance.ToString();
            vm.Finance.Log.Insert(0, new FinanceEntry { Stamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm"), Text = text, Amount = amount, Balance = balance });
            if (vm.Finance.Log.Count > FinanceLogCap) vm.Finance.Log.RemoveRange(FinanceLogCap, vm.Finance.Log.Count - FinanceLogCap);
        }

        public static LifestyleDef FindLifestyle(string name) => FinanceCatalog.Lifestyles.FirstOrDefault(l => l.Name == name);
        public static HousingDef FindHousing(string name) => FinanceCatalog.Housing.FirstOrDefault(h => h.Name == name);

        // переносит выбор в поля «образ жизни / жильё / аренда» на листе
        public static void SyncFinanceFields(SheetViewModel vm)
        {
            var f = vm.Finance;
            var ls = FindLifestyle(f.LifestyleName); var hs = FindHousing(f.HousingName);
            if (ls != null) vm.Lifestyle = ls.Name;
            if (hs != null)
            {
                vm.Housing = hs.Name;
                vm.Rent = f.HousingOwned ? "куплено" : hs.Provided ? "от корпорации" : hs.Rent > 0 ? hs.Rent + "eb" : "—";
            }
        }

        public static bool PayMonth(SheetViewModel vm, out string error)
        {
            error = null;
            var f = vm.Finance ?? (vm.Finance = new FinanceData());
            var ls = FindLifestyle(f.LifestyleName); var hs = FindHousing(f.HousingName);
            if (ls == null || hs == null) { error = "Выбери образ жизни и жильё."; return false; }
            int bill = FinanceCatalog.MonthlyBill(ls, hs, f.HousingOwned);
            f.MonthsPaid++;
            string rent = f.HousingOwned ? "жильё куплено" : hs.Provided ? "жильё от корпорации" : hs.Name + " " + hs.Rent + "eb";
            ApplyMoney(vm, -bill, $"Месяц {f.MonthsPaid}: {ls.Name} {ls.Cost}eb, {rent}");
            SyncFinanceFields(vm);
            return true;
        }

        public static bool BuyHousing(SheetViewModel vm, out string error)
        {
            error = null;
            var f = vm.Finance ?? (vm.Finance = new FinanceData());
            var hs = FindHousing(f.HousingName);
            if (hs == null || hs.Buy <= 0) { error = "Это жильё нельзя купить."; return false; }
            if (f.HousingOwned) { error = "Жильё уже куплено."; return false; }
            ApplyMoney(vm, -hs.Buy, $"Покупка жилья: {hs.Name}");
            f.HousingOwned = true;
            SyncFinanceFields(vm);
            return true;
        }

        private void ShowFinanceDialog()
        {
            if (!(DataContext is SheetViewModel vm)) return;
            CloseOverlays();
            if (vm.Finance == null) vm.Finance = new FinanceData();
            var f = vm.Finance;

            var cyan = Theme.Accent();
            var red = Theme.Danger();
            var gray = new SolidColorBrush(Color.FromRgb(153, 153, 153));
            var white = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            var yellow = new SolidColorBrush(Color.FromRgb(255, 204, 0));

            var win = new Window
            {
                Title = "ФИНАНСЫ",
                Owner = this,
                Width = 900,
                Height = 760,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = new SolidColorBrush(Color.FromRgb(5, 5, 5)),
                FontFamily = new FontFamily("Consolas")
            };
            win.Resources = this.Resources;

            var root = new StackPanel { Margin = new Thickness(14) };
            var cash = MakeText("", cyan, 28, FontWeights.Bold);
            root.Children.Add(cash);

            // операция
            root.Children.Add(MakeText("ОПЕРАЦИЯ", red, 14, FontWeights.Bold));
            var opRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 6) };
            var amount = new TextBox { Width = 90, Background = Brushes.Black, Foreground = cyan, BorderBrush = gray, CaretBrush = cyan, Padding = new Thickness(3), ToolTip = "Сумма в eb" };
            var note = new TextBox { Width = 330, Background = Brushes.Black, Foreground = cyan, BorderBrush = gray, CaretBrush = cyan, Padding = new Thickness(3), Margin = new Thickness(6, 0, 6, 0), ToolTip = "За что" };
            var btnIn = MakeButton("+ ДОХОД", cyan); btnIn.Margin = new Thickness(0, 0, 6, 0);
            var btnOut = MakeButton("− РАСХОД", red);
            opRow.Children.Add(MakeText("Сумма: ", gray, 12)); opRow.Children.Add(amount);
            opRow.Children.Add(note); opRow.Children.Add(btnIn); opRow.Children.Add(btnOut);
            root.Children.Add(opRow);

            var svcRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
            var svc = new ComboBox { Width = 520 };
            foreach (var s in FinanceCatalog.Services) svc.Items.Add($"{s.Name} — {GearCatalog.CostText(s.Cost)}");
            svc.SelectedIndex = 0;
            var btnSvc = MakeButton("ОПЛАТИТЬ УСЛУГУ", yellow); btnSvc.Margin = new Thickness(8, 0, 0, 0);
            svcRow.Children.Add(MakeText("Услуга: ", gray, 12)); svcRow.Children.Add(svc); svcRow.Children.Add(btnSvc);
            root.Children.Add(svcRow);

            // образ жизни и жильё
            root.Children.Add(MakeText("ОБРАЗ ЖИЗНИ И ЖИЛЬЁ (счёт в начале каждого месяца)", red, 14, FontWeights.Bold));
            var lsBox = new ComboBox { Width = 520, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 4, 0, 4) };
            foreach (var l in FinanceCatalog.Lifestyles) lsBox.Items.Add($"{l.Name} — {l.Cost}eb в месяц");
            var hsBox = new ComboBox { Width = 520, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 4) };
            foreach (var h in FinanceCatalog.Housing)
                hsBox.Items.Add(h.Name + " — " + (h.Provided ? "от корпорации" : h.Rent > 0 ? h.Rent + "eb аренда" : "бесплатно") + (h.Buy > 0 ? $", покупка {FinanceCatalog.FormatMoney(h.Buy)}eb" : ""));
            lsBox.SelectedIndex = Math.Max(0, FinanceCatalog.Lifestyles.FindIndex(l => l.Name == f.LifestyleName));
            hsBox.SelectedIndex = Math.Max(0, FinanceCatalog.Housing.FindIndex(h => h.Name == f.HousingName));
            root.Children.Add(lsBox); root.Children.Add(hsBox);

            var owned = new CheckBox { Content = "Жильё куплено (аренды нет)", Foreground = white, IsChecked = f.HousingOwned, Margin = new Thickness(0, 2, 0, 4) };
            root.Children.Add(owned);
            var info = MakeText("", gray, 12); info.TextWrapping = TextWrapping.Wrap; root.Children.Add(info);
            var bill = MakeText("", yellow, 18, FontWeights.Bold); bill.Margin = new Thickness(0, 6, 0, 6); root.Children.Add(bill);

            var monthRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            var btnPay = MakeButton("ОПЛАТИТЬ МЕСЯЦ", cyan); btnPay.Margin = new Thickness(0, 0, 8, 0);
            var btnBuy = MakeButton("КУПИТЬ ЖИЛЬЁ", yellow);
            monthRow.Children.Add(btnPay); monthRow.Children.Add(btnBuy);
            root.Children.Add(monthRow);

            var status = MakeText("", gray, 12); status.TextWrapping = TextWrapping.Wrap; root.Children.Add(status);

            root.Children.Add(MakeText("ЖУРНАЛ", red, 14, FontWeights.Bold));
            var logBox = new TextBlock { Foreground = white, FontSize = 12, TextWrapping = TextWrapping.Wrap };
            root.Children.Add(new ScrollViewer { Height = 170, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = logBox });

            var btnClose = MakeButton("ЗАКРЫТЬ", red); btnClose.HorizontalAlignment = HorizontalAlignment.Right; btnClose.Margin = new Thickness(0, 8, 0, 0);
            btnClose.Click += (s, e) => win.Close();
            root.Children.Add(btnClose);
            win.Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = root };

            Action refresh = () =>
            {
                int c = CurrentCash(vm);
                bool numeric = FinanceCatalog.TryParseMoney(vm.CashValue, out _);
                cash.Text = "НАЛИЧНЫЕ: " + FinanceCatalog.FormatMoney(c) + "eb" + (numeric ? "" : "  (поле «деньги» не число — считаю с 0)");
                cash.Foreground = c < 0 ? red : cyan;
                var ls = FinanceCatalog.Lifestyles[Math.Max(0, lsBox.SelectedIndex)];
                var hs = FinanceCatalog.Housing[Math.Max(0, hsBox.SelectedIndex)];
                int total = FinanceCatalog.MonthlyBill(ls, hs, owned.IsChecked == true);
                info.Text = ls.Info + "\n" + hs.Info;
                bill.Text = $"СЧЁТ ЗА МЕСЯЦ: {FinanceCatalog.FormatMoney(total)}eb  (образ жизни {ls.Cost}eb" + (owned.IsChecked == true ? ", жильё куплено)" : hs.Provided ? ", жильё от корпорации)" : $", аренда {hs.Rent}eb)");
                bill.Foreground = total > c ? red : yellow;
                btnBuy.IsEnabled = hs.Buy > 0 && owned.IsChecked != true; btnBuy.Opacity = btnBuy.IsEnabled ? 1 : 0.4;
                logBox.Text = f.Log.Count == 0 ? "Операций нет." : string.Join("\n", f.Log.Take(40).Select(x => $"{x.Stamp}  {(x.Amount >= 0 ? "+" : "−")}{FinanceCatalog.FormatMoney(Math.Abs(x.Amount))}eb  →  {FinanceCatalog.FormatMoney(x.Balance)}eb   {x.Text}"));
            };

            Action selChanged = () =>
            {
                f.LifestyleName = FinanceCatalog.Lifestyles[Math.Max(0, lsBox.SelectedIndex)].Name;
                f.HousingName = FinanceCatalog.Housing[Math.Max(0, hsBox.SelectedIndex)].Name;
                f.HousingOwned = owned.IsChecked == true;
                SyncFinanceFields(vm);
                refresh();
            };
            lsBox.SelectionChanged += (s, e) => { selChanged(); CommitChange(); };
            hsBox.SelectionChanged += (s, e) =>
            {
                owned.IsChecked = false; // другое жильё — снова арендное, пока не куплено
                selChanged(); CommitChange();
            };
            owned.Click += (s, e) => { selChanged(); CommitChange(); };

            Action<int> op = sign =>
            {
                if (!FinanceCatalog.TryParseMoney(amount.Text, out int a) || a <= 0) { status.Text = "Введи сумму (положительное число)."; status.Foreground = red; return; }
                ApplyMoney(vm, sign * a, string.IsNullOrWhiteSpace(note.Text) ? (sign > 0 ? "Доход" : "Расход") : note.Text.Trim());
                amount.Text = ""; note.Text = ""; status.Text = ""; CommitChange(); refresh();
            };
            btnIn.Click += (s, e) => op(1);
            btnOut.Click += (s, e) => op(-1);
            btnSvc.Click += (s, e) =>
            {
                var sd = FinanceCatalog.Services[svc.SelectedIndex];
                ApplyMoney(vm, -sd.Cost, sd.Name);
                status.Text = ""; CommitChange(); refresh();
            };
            btnPay.Click += (s, e) =>
            {
                selChanged();
                if (!PayMonth(vm, out string err)) { status.Text = err; status.Foreground = red; return; }
                status.Text = CurrentCash(vm) < 0 ? "Денег не хватило: баланс ушёл в минус. Если не оплатить образ жизни, через неделю — спасбросок от смерти каждый день." : "";
                status.Foreground = red; CommitChange(); refresh();
            };
            btnBuy.Click += (s, e) =>
            {
                selChanged();
                if (!BuyHousing(vm, out string err)) { status.Text = err; status.Foreground = red; return; }
                owned.IsChecked = true; status.Text = CurrentCash(vm) < 0 ? "Денег не хватило: баланс ушёл в минус." : ""; status.Foreground = red;
                CommitChange(); refresh();
            };

            refresh();
            win.ShowDialog();
        }
    }
}
