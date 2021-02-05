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
    using Microsoft.Extensions.Configuration;

    public class Tests
    {
        List<CodeFile> codeFiles = new List<CodeFile>();
        IOptions<SnippetsOptions> snippetsOptions;

        [SetUp]
        public void Setup()
        {
            var codeFile = new CodeFile() { Path = "__Main.razor" };
            codeFile.Content = "<MudDatePicker/>";
            codeFiles.Add(codeFile);
            var codeFile2 = new CodeFile() { Path = "Test.razor" };
            codeFile2.Content = "<h1>Test</h1>";
            codeFiles.Add(codeFile2);

            snippetsOptions = Options.Create(new SnippetsOptions(){SnippetsService = "/api/snippets/"});

        }

        [Test]
        public async Task TestGet()
        {
            var snippetService = new SnippetsService(snippetsOptions, new System.Net.Http.HttpClient());
            var codeFiles = await snippetService.GetSnippetContentAsync("2021020540572059");
            Assert.IsNotNull(codeFiles);
        }

        [Test]
        public async Task TestPut()
        {
            var snippetService = new SnippetsService(snippetsOptions, new System.Net.Http.HttpClient());
            var id = await snippetService.SaveSnippetAsync(codeFiles);
            Assert.IsNotNull(id);
            Console.WriteLine(id);
            var savedCodeFiles = await snippetService.GetSnippetContentAsync("2021020540572059");
            List<CodeFile> savedCodeFilesList = new List<CodeFile>(savedCodeFiles);
            for (int i = 0; i  < codeFiles.Count; i++ )
            {
                Assert.AreEqual(codeFiles[i].Path, savedCodeFilesList[i].Path);
                Assert.AreEqual(codeFiles[i].Content,  savedCodeFilesList[i].Content);
            }
        }
    }
}