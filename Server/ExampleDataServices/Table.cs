using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Server.ExampleDataServices
{
    public class Table
    {
        [JsonPropertyName("table")]
        public IList<ElementGroup> ElementGroups { get; set; }
    }
}