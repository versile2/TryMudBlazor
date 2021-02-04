namespace Tests
{
    using NUnit.Framework;
    using BlazorRepl.Client.Services;
    using BlazorRepl.Client.Models;
    using BlazorRepl.Core;
    using Microsoft.Extensions.Options;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System;

    public class Tests
    {
        List<CodeFile> codeFiles = new List<CodeFile>();

        [SetUp]
        public void Setup()
        {
            var codeFile = new CodeFile() { Path = "__Main.razor" };
            codeFile.Content = "<MudDatePicker/>";
            codeFiles.Add(codeFile);
            var codeFile2 = new CodeFile() { Path = "Test.razor" };
            codeFile2.Content = "<h1>Test</h1>";
            codeFiles.Add(codeFile2);
        }

        [Test]
        public async Task TestGet()
        {
            var snippetsOptions = Options.Create(new SnippetsOptions() {
                SnippetsContainer = "snippets"
            }); ;

            var snippetService = new SnippetsService(snippetsOptions);
            var codeFiles = await snippetService.GetSnippetContentAsync("2021020449939785");
            Assert.IsNotNull(codeFiles);

        }

        [Test]
        public async Task TestPut()
        {

            var snippetsOptions = Options.Create(new SnippetsOptions()
            {
                StorageConnectionString = Environment.GetEnvironmentVariable("StorageConnectionString"),
                SnippetsContainer = "snippets"
            });

            var snippetService = new SnippetsService(snippetsOptions);
            var id = await snippetService.SaveSnippetAsync(codeFiles);
            Assert.IsNotNull(id);
            Console.WriteLine(id);
        }
    }
}