using System.Text.RegularExpressions;
using TryMudBlazor.Client.Models;

namespace TryMudBlazor.Client.Services
{
    public class ComponentService
    {
        private readonly string _basePath = "MudBlazor.Docs/Pages/Components";
        public List<ComponentExample> Examples { get; private set; }
        public string[] Components { get; private set; }

        public ComponentService()
        {
            Examples = new List<ComponentExample>();
            LoadComponents();
        }

        private void LoadComponents()
        {
            var componentDirs = Directory.GetDirectories(_basePath);
            Components = componentDirs.Select(Path.GetFileName).ToArray();

            foreach (var componentDir in componentDirs)
            {
                var componentName = Path.GetFileName(componentDir);
                var examplesDir = Path.Combine(componentDir, "Examples");

                if (Directory.Exists(examplesDir))
                {
                    var exampleFiles = Directory.GetFiles(examplesDir, "*Example.razor");
                    foreach (var exampleFile in exampleFiles)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(exampleFile);
                        // Extract short name by removing "Example" suffix
                        var shortName = fileName.EndsWith("Example") 
                            ? fileName.Substring(0, fileName.Length - "Example".Length) 
                            : fileName;

                        Examples.Add(new ComponentExample
                        {
                            FullName = componentName,
                            ExampleShortName = shortName,
                            ExampleFullName = fileName,
                        });
                    }
                }
            }
        }

        public string GetExampleContent(string componentName, string exampleName)
        {
            var filePath = Path.Combine(_basePath, componentName, "Examples", $"{exampleName}Example.razor");
            return File.Exists(filePath) ? File.ReadAllText(filePath) : string.Empty;
        }
    }
}
