﻿﻿﻿namespace Try.Core
{
    public class ComponentFile
    {
        /// <summary>
        /// The short name of the file (e.g. 'Dialog' for DialogExample_Dialog)
        /// </summary>
        public string ShortName { get; set; }

        /// <summary>
        /// The full filename of the file
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// The content of the file
        /// </summary>
        public string Content { get; set; }
    }

    public class ComponentExample
    {
        /// <summary>
        /// The Name of the component
        /// </summary>
        public string ComponentName { get; set; }
        /// <summary>
        /// The Title of the Example
        /// </summary>
        public string ExampleFullName { get; set; }
        /// <summary>
        /// The short name of the ...Example.razor file
        /// </summary>
        public string ExampleShortName { get; set; }

        /// <summary>
        /// Additional files associated with this example (e.g. dialog components)
        /// </summary>
        public List<ComponentFile> AssociatedFiles { get; set; } = new();
    }
}
