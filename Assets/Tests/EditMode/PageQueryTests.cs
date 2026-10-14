using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class PageQueryTests
    {
        const string Page = "https://jhiggins-tech.github.io/pooket-slug/index.html";

        [TestCase(Page + "?room=ABCD-EU", "room", "ABCD-EU")]
        [TestCase(Page + "?a=1&room=XY&b=2", "room", "XY")]
        [TestCase(Page + "?ROOM=abc", "room", "abc")]
        [TestCase(Page + "?room=ABCD#top", "room", "ABCD")]
        [TestCase(Page + "?name=Pooket%20Slug", "name", "Pooket Slug")]
        [TestCase(Page + "?name=Pooket+Slug", "name", "Pooket Slug")]
        [TestCase(Page + "?sum=1%2B1", "sum", "1+1")]
        [TestCase(Page + "?log=net:debug,game:warn", "log", "net:debug,game:warn")]
        [TestCase(Page + "?debug", "debug", "")]
        [TestCase(Page + "?debug=", "debug", "")]
        public void GetReadsAParameter(string url, string name, string expected)
        {
            Assert.AreEqual(expected, PageQuery.Get(url, name));
        }

        [TestCase(Page, "room")]
        [TestCase(Page + "?roomy=1", "room")]
        [TestCase(Page + "#room=1", "room")]
        [TestCase(Page + "#top?room=1", "room")]
        [TestCase("", "room")]
        [TestCase(null, "room")]
        [TestCase(Page + "?room=1", "")]
        public void GetReturnsNullWhenItIsAbsent(string url, string name)
        {
            Assert.IsNull(PageQuery.Get(url, name));
        }

        [TestCase(Page + "?room=ABCD-EU", Page)]
        [TestCase(Page + "#top", Page)]
        [TestCase(Page + "?log=all:debug#top", Page)]
        [TestCase(Page, Page)]
        [TestCase("", "")]
        [TestCase(null, "")]
        public void BaseDropsTheQueryAndFragment(string url, string expected)
        {
            Assert.AreEqual(expected, PageQuery.Base(url));
        }
    }
}
