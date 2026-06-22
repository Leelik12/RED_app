using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CyberpunkRED_Generator
{
    public partial class CharacterGenerationWindow : Window
    {
        private Random _rnd = new Random();

        public CharacterGenerationWindow()
        {
            InitializeComponent();
            CalculateDerivedStats();
        }

        private void ModeChanged(object sender, RoutedEventArgs e)
        {
            if (PanelCalc == null || PanelRandom == null || StatsGrid == null) return;

            bool isManual = RbCalc.IsChecked == true;
            PanelCalc.Visibility = isManual ? Visibility.Visible : Visibility.Collapsed;
            PanelRandom.Visibility = isManual ? Visibility.Collapsed : Visibility.Visible;
        }

        private void CbRank_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePointsLeft();
        }

        // Единый обработчик для всех кнопок "Минус"
        private void BtnMinus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && FindName(btn.Tag.ToString()) is TextBlock tb)
            {
                AdjustStat(tb, -1);
            }
        }

        // Единый обработчик для всех кнопок "Плюс"
        private void BtnPlus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && FindName(btn.Tag.ToString()) is TextBlock tb)
            {
                AdjustStat(tb, 1);
            }
        }

        // Логика изменения стата
        private void AdjustStat(TextBlock tb, int delta)
        {
            if (int.TryParse(tb.Text, out int val))
            {
                int newVal = val + delta;

                // Ни один СТАТ не может быть выше 8 и ниже 2 
                if (newVal >= 2 && newVal <= 8)
                {
                    // Проверяем, хватает ли очков (только если мы прибавляем стат)
                    if (delta > 0)
                    {
                        int maxPoints = GetMaxPoints();
                        int currentPoints = GetTotalPointsSpent();
                        if (currentPoints >= maxPoints) return; // Очки закончились, игнорируем клик
                    }

                    tb.Text = newVal.ToString();
                    UpdatePointsLeft();
                    CalculateDerivedStats();
                }
            }
        }

        private int GetMaxPoints()
        {
            if (CbRank?.SelectedItem is ComboBoxItem item && int.TryParse(item.Tag?.ToString(), out int maxPoints))
                return maxPoints;
            return 62;
        }

        private int GetTotalPointsSpent()
        {
            return GetStat(TxtInt) + GetStat(TxtRef) + GetStat(TxtDex) + GetStat(TxtTech) +
                   GetStat(TxtCool) + GetStat(TxtWill) + GetStat(TxtLuck) + GetStat(TxtMove) +
                   GetStat(TxtBody) + GetStat(TxtEmp);
        }

        private void UpdatePointsLeft()
        {
            int currentPoints = GetTotalPointsSpent();
            int left = GetMaxPoints() - currentPoints;

            // Обновляем очки в ручном режиме
            if (TxtPointsLeft != null) TxtPointsLeft.Text = left.ToString();

            // Обновляем общую сумму статов
            if (TxtTotalStats != null) TxtTotalStats.Text = currentPoints.ToString();
        }

        private void CalculateDerivedStats()
        {
            if (TxtHp == null) return;

            int body = GetStat(TxtBody);
            int will = GetStat(TxtWill);
            int emp = GetStat(TxtEmp);
            // Твои Пункты Здоровья равны 10 + (5*½ [сумма значения ТЕЛ + ВОЛЯ с округлением вверх]) 
            int hp = 10 + (5 * (int)Math.Ceiling((body + will) / 2.0));

            // Порог Тяжелого ранения твоего Персонажа равен половине его общих ПЗ (округляется вверх) 
            int wounded = (int)Math.Ceiling(hp / 2.0);

            // За каждое очко Эмпатии персонаж получает 10 очков Человечности 
            int humanity = emp * 10;

            TxtHp.Text = hp.ToString();
            TxtWounded.Text = wounded.ToString();
            TxtDeathSave.Text = body.ToString(); // Спасбросок против Смерти равен характеристике ТЕЛ 
            TxtHumanity.Text = humanity.ToString();
        }

        private void BtnRollStats_Click(object sender, RoutedEventArgs e)
        {
            // Рандомный бросок 
            TxtInt.Text = RollStat().ToString();
            TxtRef.Text = RollStat().ToString();
            TxtDex.Text = RollStat().ToString();
            TxtTech.Text = RollStat().ToString();
            TxtCool.Text = RollStat().ToString();
            TxtWill.Text = RollStat().ToString();
            TxtLuck.Text = RollStat().ToString();
            TxtMove.Text = RollStat().ToString();
            TxtBody.Text = RollStat().ToString();
            TxtEmp.Text = RollStat().ToString();

            CalculateDerivedStats();
            UpdatePointsLeft();
        }

        private int RollStat()
        {
            List<int> rolls = new List<int> { _rnd.Next(1, 5), _rnd.Next(1, 5), _rnd.Next(1, 5) };
            return rolls.OrderByDescending(x => x).Take(2).Sum();
        }

        private int GetStat(TextBlock tb)
        {
            if (tb == null) return 2;
            return int.TryParse(tb.Text, out int result) ? result : 2;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            LifepathWindow pathWindow = new LifepathWindow();
            pathWindow.Show();
            this.Close();
        }
    }
}