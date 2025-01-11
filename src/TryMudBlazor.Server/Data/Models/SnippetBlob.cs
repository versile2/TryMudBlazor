namespace TryMudBlazor.Server.Data.Models
{
    using System.ComponentModel.DataAnnotations;

    public class SnippetBlob
    {
        [Key, Required]
        public string Id { get; set; } = default!;
        [Required]
        public byte[] Content { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
    }
}
