namespace BlazorRepl.Client.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Text;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using BlazorRepl.Client.Models;
    using BlazorRepl.Core;
    using Microsoft.Extensions.Options;

    public class SnippetsService
    {
        private const int SnippetIdLength = 16;

        private static readonly IDictionary<char, char> LetterToDigitIdMappings = new Dictionary<char, char>
        {
            ['a'] = '0',
            ['k'] = '0',
            ['u'] = '0',
            ['E'] = '0',
            ['O'] = '0',
            ['Y'] = '0',
            ['b'] = '1',
            ['l'] = '1',
            ['v'] = '1',
            ['F'] = '1',
            ['P'] = '1',
            ['c'] = '2',
            ['m'] = '2',
            ['w'] = '2',
            ['G'] = '2',
            ['Q'] = '2',
            ['d'] = '3',
            ['n'] = '3',
            ['x'] = '3',
            ['H'] = '3',
            ['R'] = '3',
            ['e'] = '4',
            ['o'] = '4',
            ['y'] = '4',
            ['I'] = '4',
            ['S'] = '4',
            ['f'] = '5',
            ['p'] = '5',
            ['z'] = '5',
            ['J'] = '5',
            ['T'] = '5',
            ['g'] = '6',
            ['q'] = '6',
            ['A'] = '6',
            ['K'] = '6',
            ['U'] = '6',
            ['h'] = '7',
            ['r'] = '7',
            ['B'] = '7',
            ['L'] = '7',
            ['V'] = '7',
            ['i'] = '8',
            ['s'] = '8',
            ['C'] = '8',
            ['M'] = '8',
            ['W'] = '8',
            ['j'] = '9',
            ['t'] = '9',
            ['D'] = '9',
            ['N'] = '9',
            ['X'] = '9',
            ['Z'] = '9',
        };

        private readonly SnippetsOptions snippetsOptions;

        public SnippetsService(IOptions<SnippetsOptions> snippetsOptions)
        {
            this.snippetsOptions = snippetsOptions.Value;
        }

        public async Task<string> SaveSnippetAsync(IEnumerable<CodeFile> codeFiles)
        {
            if (codeFiles == null)
            {
                throw new ArgumentNullException(nameof(codeFiles));
            }

            var codeFilesValidationError = CodeFilesHelper.ValidateCodeFilesForSnippetCreation(codeFiles);
            if (!string.IsNullOrWhiteSpace(codeFilesValidationError))
            {
                throw new InvalidOperationException(codeFilesValidationError);
            }

            var yearFolder = DateTime.Now.Year;
            var monthFolder = DateTime.Now.Month;
            var dayFolder = DateTime.Now.Day;
            var time = Convert.ToInt32(DateTime.Now.TimeOfDay.TotalMilliseconds);
            var snippetFolder = $"{yearFolder:0000}/{monthFolder:00}/{dayFolder:00}";
            var snippetTime = $"{time:D8}";
            var snippetId = $"{yearFolder:0000}{monthFolder:00}{dayFolder:00}{snippetTime:D8}";
            var blobName = $"{snippetFolder}/{snippetTime}";



            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var codeFile in codeFiles)
                    {
                        var byteArray = Encoding.ASCII.GetBytes(codeFile.Content);
                        var codeEntry = archive.CreateEntry(codeFile.Path);
                        using (var entryStream = codeEntry.Open())
                        {
                            entryStream.Write(byteArray);
                        }
                    }
                }

                memoryStream.Position = 0;

                var blobServiceClient = new BlobServiceClient(this.snippetsOptions.StorageConnectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(this.snippetsOptions.SnippetsContainer);
                await containerClient.UploadBlobAsync(blobName, memoryStream);
            }

            return snippetId;
        }

        public async Task<IEnumerable<CodeFile>> GetSnippetContentAsync(string snippetId)
        {
            if (string.IsNullOrWhiteSpace(snippetId) || snippetId.Length != SnippetIdLength)
            {
                throw new ArgumentException("Invalid snippet ID.", nameof(snippetId));
            }

            var yearFolder = snippetId.Substring(0, 4);
            var monthFolder = snippetId.Substring(4, 2);
            var dayFolder = snippetId.Substring(6, 2);
            var time = snippetId.Substring(8);
            var snippetFolder = $"{yearFolder:0000}/{monthFolder:00}/{dayFolder:00}";
            var snippetTime = $"{time:00000000}";
            var blobName = $"{snippetFolder}/{snippetTime}";

            var blobServiceClient = new BlobServiceClient(this.snippetsOptions.StorageConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(this.snippetsOptions.SnippetsContainer);
            var blob = containerClient.GetBlobClient(blobName);
            var response = await blob.DownloadAsync();
            var zipStream = new MemoryStream();
            await response.Value.Content.CopyToAsync(zipStream);

            var snippetFiles = await ExtractSnippetFilesFromZip(zipStream);
            return snippetFiles;
        }

        private static async Task<IEnumerable<CodeFile>> ExtractSnippetFilesFromZip(MemoryStream zipStream)
        {
            var result = new List<CodeFile>();

            using var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read);
            foreach (var entry in zipArchive.Entries)
            {
                using var streamReader = new StreamReader(entry.Open());

                result.Add(new CodeFile { Path = entry.FullName, Content = await streamReader.ReadToEndAsync() });
            }

            return result;
        }

        private static string DecodeDateIdPart(string encodedPart)
        {
            var decodedPart = string.Empty;

            foreach (var letter in encodedPart)
            {
                if (!LetterToDigitIdMappings.TryGetValue(letter, out var digit))
                {
                    throw new InvalidDataException("Invalid snippet ID");
                }

                decodedPart += digit;
            }

            return decodedPart;
        }
    }
}
