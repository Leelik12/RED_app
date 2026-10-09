using System;
using System.Linq;
using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class TwistsTests
    {
        [Fact]
        public void Three_Tables_With_Ten_Rows()
        {
            Assert.Equal(3, Twists.All.Count);
            foreach (var t in Twists.All)
            {
                Assert.Equal(10, t.Rows.Length);
                Assert.All(t.Rows, r => Assert.False(string.IsNullOrWhiteSpace(r)));
            }
            Assert.Equal(3, Twists.All.Select(t => t.Id).Distinct().Count());
        }

        [Fact]
        public void Roll_Stays_In_Range_And_Hits_Every_Row()
        {
            var rng = new Random(7);
            var seen = Enumerable.Range(0, 2000).Select(_ => Twists.RollD10(rng)).Distinct().OrderBy(x => x).ToList();
            Assert.Equal(Enumerable.Range(1, 10), seen);
            Assert.Null(Twists.Line(Twists.All[0], 0));
            Assert.Null(Twists.Line(Twists.All[0], 11));
            Assert.Contains("10 патронов", Twists.Line(Twists.Get("sixes"), 1));
        }

        [Fact]
        public void Every_Table_Belongs_To_A_Known_Pack()
        {
            foreach (var t in Twists.All)
                Assert.Contains(Packs.All, p => p.Id == t.Pack);
        }
    }
}
