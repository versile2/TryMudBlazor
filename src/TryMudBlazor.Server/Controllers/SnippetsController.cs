
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using TryMudBlazor.Server.Data;
using TryMudBlazor.Server.Data.Models;
using TryMudBlazor.Server.Services;
using static TryMudBlazor.Server.Utilities.SnippetsEncoder;

namespace TryMudBlazor.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SnippetsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ComponentService _componentService;

    public SnippetsController(IDbContextFactory<ApplicationDbContext> contextFactory, ComponentService componentService)
    {
        _context = contextFactory.CreateDbContext();
        _componentService = componentService;
    }

    [HttpGet("{snippetId}")]
    public async Task<IActionResult> Get(string snippetId)
    {
        snippetId = DecodeSnippetId(snippetId);
        var snippet = await _context.SnippetBlobs.FindAsync(snippetId);

        if (snippet == null)
            return NotFound();

        var memoryStream = new MemoryStream(snippet.Content);
        return File(memoryStream, "application/octet-stream", "snippet.zip");
    }

    [HttpPost]
    public async Task<IActionResult> Post()
    {
        var newSnippetId = NewSnippetId();

        using var memoryStream = new MemoryStream();
        await Request.Body.CopyToAsync(memoryStream);

        var snippet = new SnippetBlob
        {
            Id = newSnippetId,
            Content = memoryStream.ToArray(),
            CreatedAt = DateTime.UtcNow
        };

        _context.SnippetBlobs.Add(snippet);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex) 
        { 
            return BadRequest(ex.Message);
        }        

        return Ok(EncodeSnippetId(newSnippetId));
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

    [HttpGet("componentList")]
    public IActionResult GetComponentList()
    {
        if (!_componentService.IsInitialized)
        {
            _componentService.Initialize();
        }

        // Configure JSON serialization options
        var options = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        return new JsonResult(_componentService.Examples, options);
    }

    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok("API is working");
    }
}