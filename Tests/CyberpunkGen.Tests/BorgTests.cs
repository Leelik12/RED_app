using System.Linq;
using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class BorgTests
    {
        [Fact]
        public void AllThirteenChassisLoad()
        {
            Assert.Equal(13, Borg.All.Count);
            foreach (var c in Borg.All)
            {
                Assert.True(c.Price > 0, c.Name);
                Assert.True(c.HlAvg > 0, c.Name);
                Assert.NotEmpty(c.Kit);
                Assert.NotEmpty(c.Free);
                Assert.Contains(c.EndoBody, new[] { 12, 14, 16 });
            }
        }

        [Fact]
        public void KitAverageIsCloseToPrintedHl()
        {
            // книга считает «среднее» грубо, но формула не должна расходиться больше чем на ~10%
            foreach (var c in Borg.All)
            {
                double avg = Borg.RollKitHl(c, n => (n + 1) / 2) ;   // детерминированно: целая половина грани
                Assert.InRange(avg, c.HlAvg * 0.8, c.HlAvg * 1.25);
            }
        }

        [Fact]
        public void HydraulicFloors()
        {
            Assert.Equal(0, Borg.HydraulicFloor(new[] { "Рация" }));
            Assert.Equal(4, Borg.HydraulicFloor(new[] { "Модернизация внутренней гидравлики" }));
            Assert.Equal(6, Borg.HydraulicFloor(new[] { "Тюнингованная модернизация внутренней гидравлики", "Модернизация внутренней гидравлики" }));
            Assert.Equal(8, Borg.HydraulicFloor(new[] { "Совершенная модернизация внутренней гидравлики" }));
            Assert.Equal(6, Borg.Find("Затмение").HydraulicFloor);
        }

        [Fact]
        public void MaxHumanityLossCountsBorgwareDouble()
        {
            var sera = Borg.Find("Сера");
            Assert.Contains(sera.Kit, k => k.Borgware);
            Assert.Equal(sera.Kit.Sum(k => k.Count * (k.Borgware ? 4 : 2)), sera.MaxHumanityLoss);
        }

        [Fact]
        public void FreeImplantsInBorg()
        {
            Assert.True(Borg.IsFreeInBorg(new CyberwareDef { Name = "Нейролинк" }));
            Assert.True(Borg.IsFreeInBorg(new CyberwareDef { Name = "Киберрука", Category = "ПРАВАЯ КИБЕРРУКА (CYBERARM R)", IsFoundation = true }));
            Assert.False(Borg.IsFreeInBorg(new CyberwareDef { Name = "Рация", Category = "КИБЕРАУДИО (CYBERAUDIO)" }));
        }

        [Fact]
        public void EndoskeletonBodyMatchesBook()
        {
            Assert.Equal(16, Borg.Find("Самсон").EndoBody);
            Assert.Equal(14, Borg.Find("Паук 2.0").EndoBody);
            Assert.Equal(12, Borg.Find("Класс Альфа").EndoBody);
        }
    }
}
