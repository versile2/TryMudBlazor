namespace TryMudBlazor.Client.Models
{
    public class ComponentExample
    {
        /// <summary>
        /// The Name of the componenent
        /// </summary>
        public string FullName { get; set; }
        /// <summary>
        /// The name of the ...Example.razor file
        /// </summary>
        public string ExampleShortName { get; set; }

        /// <summary>
        /// The name of the ...Example.razor file looked up in the root component .razor file
        /// </summary>
        public string ExampleFullName { get; set; }
    }
}
