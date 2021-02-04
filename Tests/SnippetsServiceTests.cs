namespace Tests
{
    using NUnit.Framework;
    using BlazorRepl.Client.Services;

    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestGet()
        {
            var snippetService = new SnippetsService();
        }
    }
}