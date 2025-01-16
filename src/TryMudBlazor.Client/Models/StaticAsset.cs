namespace TryMudBlazor.Client.Models
{
    using Microsoft.CodeAnalysis;

    public class StaticAsset
    {
        public string Location { get; set; } = default!;
        
        public string Name { get; set; }

        public bool IsIncluded { get; set; }
    }
}
