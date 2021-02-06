using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Blobs;
using BlazorRepl.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SnippetsController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly BlobContainerClient containerClient;
        public SnippetsController(IConfiguration config)
        {
            _config = config;
            var containerUri = new Uri(_config["SnippetsContainer"]);
            containerClient = new BlobContainerClient(containerUri, new DefaultAzureCredential());  
        }

        [HttpGet("{snippetId}")]
        public async Task<IActionResult> Get(string snippetId)
        {
            var blob = containerClient.GetBlobClient(BlobPath(snippetId));
            var response = await blob.DownloadAsync();
            var zipStream = new MemoryStream();
            await response.Value.Content.CopyToAsync(zipStream);
            zipStream.Position = 0;
            return File(zipStream, "application/octet-stream", "snippet.zip");
        }

        [HttpPut]
        public async Task<IActionResult> Put()
        {
            var newSnippetId = NewSnippetId();
            await containerClient.UploadBlobAsync(BlobPath(newSnippetId), Request.Body);
            return Ok(newSnippetId);
        }

        private static string NewSnippetId()
        {
            var yearFolder = DateTime.Now.Year;
            var monthFolder = DateTime.Now.Month;
            var dayFolder = DateTime.Now.Day;
            var time = Convert.ToInt32(DateTime.Now.TimeOfDay.TotalMilliseconds);
            var snippetTime = $"{time:D8}";
            return $"{yearFolder:0000}{monthFolder:00}{dayFolder:00}{snippetTime:D8}";
        }

        private static string BlobPath(string snippetId)
        {
            var yearFolder = snippetId.Substring(0, 4);
            var monthFolder = snippetId.Substring(4, 2);
            var dayFolder = snippetId.Substring(6, 2);
            var time = snippetId.Substring(8);
            var snippetFolder = $"{yearFolder:0000}/{monthFolder:00}/{dayFolder:00}";
            var snippetTime = $"{time:00000000}";
            return $"{snippetFolder}/{snippetTime}";
        }
    }
}
