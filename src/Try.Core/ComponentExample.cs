namespace Try.Core
{
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
        /// The full filename of the ...Example.razor file looked up in the root directory
        /// </summary>
        public string ExampleFileName { get; set; }

        /// <summary>
        /// The code of the Example.razor file
        /// </summary>
        public string ExampleContent { get; set; }
    }
}
