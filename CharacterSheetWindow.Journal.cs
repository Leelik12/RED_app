using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CyberpunkRED_Generator
{
    // клик по названию навыка = бросок 1d10 + СУМ (всплывашка и запись в журнал); ПКМ — правило в справочнике
    public partial class CharacterSheetWindow
    {
        private DispatcherTimer _toastTimer;

        private void InitJournal()
        {
            RegisterMenuItem("📓 Журнал сессии и портрет", () => JournalView.ShowWindow(this, DataContext as SheetViewModel), "ИГРА");
            RegisterMenuItem("🗺 Карта Найт-Сити", () => MapView.ShowWindow(this, DataContext as SheetViewModel), "ИГРА");
            SheetTabControl.PreviewMouseLeftButtonUp += SheetTabs_SkillClick;
            SheetTabControl.PreviewMouseRightButtonUp += SheetTabs_RulesHelp;
            if (DataContext is SheetViewModel vm0) HookJournal(vm0);
            DataContextChanged += (s, e) => { if (e.NewValue is SheetViewModel vm) HookJournal(vm); };
        }

        private void HookJournal(SheetViewModel vm)
        {
            if (vm.Journal == null) vm.Journal = new JournalData();
            var j = vm.Journal;
            Action show = () =>
            {
                MiniPortrait.Source = SidePortrait.Source = Portraits.ToImage(j.Portrait);
                MiniPortrait.Visibility = SidePortrait.Visibility = MiniPortrait.Source == null ? Visibility.Collapsed : Visibility.Visible;
                UpdateHudPortrait(MiniPortrait.Source);
            };
            j.Changed += show;
            show();
        }

        private void SheetTabs_SkillClick(object sender, MouseButtonEventArgs e)
        {
            if (!(e.OriginalSource is TextBlock tb) || !(tb.DataContext is SheetSkill skill)) return;
            // кликаем только по подписи навыка, не по цифрам и подсказкам
            if (tb.Text != skill.Name && !(skill.BaseName != null && tb.Text.StartsWith(skill.BaseName))) return;
            if (!(DataContext is SheetViewModel vm)) return;
            var r = SkillRoll.Roll(skill);
            vm.Journal.Add("roll", r.Text);
            ShowToast("🎲 " + r.Text);
        }

        // подписи характеристик и параметров, по которым ПКМ открывает нужный блок страницы правил
        private static readonly System.Collections.Generic.Dictionary<string, string> RuleTerms = new System.Collections.Generic.Dictionary<string, string>
        {
            { "ИНТ", "Интеллект" }, { "РЕА", "Реакция" }, { "ЛВК", "Ловкость" }, { "ТЕХ", "Техника" }, { "ХАР", "Харизма" },
            { "ВОЛЯ", "Сила воли" }, { "УДЧ", "Удача" }, { "СКО", "Скорость" }, { "ТЕЛ", "Телосложение" }, { "ЭМП", "Эмпатия" },
            { "ЧЕЛОВЕЧНОСТЬ", "Человечность" }, { "ИНИЦИАТИВА", "Инициатива" }, { "РЕГЕНЕРАЦИЯ", "Регенерация" },
            { "ШТРАФ СПАСБРОСКА", "Спасбросок от смерти" }, { "ТЯЖ. РАНЕНИЕ", "Тяжёлое ранение" }, { "ИСП. СМЕРТИ", "Спасбросок от смерти" },
            { "ПЗ", "Пункты здоровья" }, { "КРИТ. ТРАВМЫ", "Критические травмы" }
        };

        // ПКМ по названию навыка или характеристики — переход к нужному блоку страницы правил
        private void SheetTabs_RulesHelp(object sender, MouseButtonEventArgs e)
        {
            if (!(e.OriginalSource is TextBlock tb)) return;
            string query = null;
            if (tb.DataContext is SheetSkill skill && (tb.Text == skill.Name || (skill.BaseName != null && tb.Text.StartsWith(skill.BaseName))))
                query = skill.BaseName ?? skill.Name;
            else if (tb.Text != null && RuleTerms.TryGetValue(tb.Text.Trim().ToUpperInvariant(), out var term))
                query = term;
            if (query == null || Rules.Find(query) == null) return;
            e.Handled = true;
            RulesView.ShowWindow(this, query);
        }

        private void ShowToast(string text)
        {
            RollToastText.Text = text;
            RollToast.Visibility = Visibility.Visible;
            if (_toastTimer == null)
            {
                _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                _toastTimer.Tick += (s, e) => { _toastTimer.Stop(); RollToast.Visibility = Visibility.Collapsed; };
            }
            _toastTimer.Stop();
            _toastTimer.Start();
        }
    }
}
