using System.Collections.Generic;
using System.Linq;
using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class TarotTests
    {
        [Fact]
        public void DeckHas22UniqueCards()
        {
            Assert.Equal(22, Tarot.Deck.Count);
            Assert.Equal(22, Tarot.Deck.Select(c => c.Id).Distinct().Count());
            Assert.All(Tarot.Deck, c => Assert.False(string.IsNullOrWhiteSpace(c.Effect)));
        }

        [Fact]
        public void DrawNeverRepeatsAndEmptiesDeck()
        {
            var drawn = new List<int>();
            for (int i = 0; i < 22; i++) Assert.NotNull(Tarot.Draw(drawn));
            Assert.Equal(22, drawn.Distinct().Count());
            Assert.Null(Tarot.Draw(drawn));
        }

        [Fact]
        public void DrawUsesOnlyUndrawnCards()
        {
            var drawn = Enumerable.Range(0, 21).ToList();   // осталась только 21
            Assert.Equal(21, Tarot.Draw(drawn, n => n).Id);
        }
    }
}
