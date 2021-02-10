using System.Collections.Generic;

namespace Server.ExampleDataServices
{
    public class ElementGroup
    {
        public string Wiki { get; set; }
        public IList<Element> Elements { get; set; }
    }
}