using System;
using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class UpdatesTests
    {
        [Theory]
        [InlineData("v2.0.1", 2, 0, 1)]
        [InlineData("2.1", 2, 1, 0)]
        [InlineData("V3.0.0-beta.1", 3, 0, 0)]
        public void ParseTag_Reads_Versions(string tag, int ma, int mi, int bu)
        {
            var v = Updates.ParseTag(tag);
            Assert.Equal(new Version(ma, mi, bu), v);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("latest")]
        public void ParseTag_Rejects_Garbage(string tag) => Assert.Null(Updates.ParseTag(tag));

        [Fact]
        public void IsNewer_Compares_Major_Minor_Build()
        {
            var cur = new Version(1, 9, 0, 0);
            Assert.True(Updates.IsNewer("v2.0.0", cur));
            Assert.True(Updates.IsNewer("1.9.1", cur));
            Assert.False(Updates.IsNewer("v1.9.0", cur));
            Assert.False(Updates.IsNewer("v1.8.9", cur));
            Assert.False(Updates.IsNewer("nonsense", cur));
        }

        [Fact]
        public void Parse_Reads_Release_Json()
        {
            var i = Updates.Parse("{\"tag_name\":\"v2.0.0\",\"html_url\":\"https://x/r\",\"body\":\"notes\"}", new Version(1, 9, 0));
            Assert.True(i.Ok); Assert.True(i.Newer); Assert.Equal("https://x/r", i.Url); Assert.Equal("notes", i.Notes);
            Assert.False(Updates.Parse("{}", new Version(1, 9, 0)).Ok);
            Assert.False(Updates.Parse("не json", new Version(1, 9, 0)).Ok);
        }
    }
}
