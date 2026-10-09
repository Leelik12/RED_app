using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace CyberpunkRED_Generator
{
    // поиск и фильтры на листе персонажа
    public partial class CharacterSheetWindow
    {
        private void TxtSkillSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is SheetViewModel vm) vm.ApplySkillSearch(TxtSkillSearch.Text);
        }

        private void TxtCyberSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!(DataContext is SheetViewModel vm)) return;
            string q = TxtCyberSearch.Text.Trim();
            var view = CollectionViewSource.GetDefaultView(vm.AvailableCyberware);
            view.Filter = q.Length == 0 ? (Predicate<object>)null : o => o is CyberwareDef def
                && ((def.Name ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                    || (def.Description ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            view.Refresh();
        }

        private void TxtEnemySearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!(DataContext is SheetViewModel vm)) return;
            string q = TxtEnemySearch.Text.Trim();
            var view = CollectionViewSource.GetDefaultView(vm.AvailableEnemies);
            view.Filter = q.Length == 0 ? (Predicate<object>)null : o => o is EnemySaveData en
                && (en.Name ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
            view.Refresh();
        }
    }
}
