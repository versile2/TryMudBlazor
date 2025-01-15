namespace TryMudBlazor.Server.Services
{
    using System.Text.RegularExpressions;
    using Try.Core;

    public class ComponentService
    {
        private readonly string _basePath = "../MudBlazor.Docs/Pages/Components";
        public bool IsInitialized { get; private set; } = false;
        public List<ComponentExample> Examples { get; private set; } = [];

        public void Initialize()
        {
            LoadComponents();
        }

        private void LoadComponents()
        {            
            var componentDirs = Directory.GetDirectories(_basePath);

            foreach (var componentDir in componentDirs)
            {
                var componentShortName = Path.GetFileName(componentDir);
                var examplesDir = Path.Combine(componentDir, "Examples");

                if (Directory.Exists(examplesDir))
                {
                    // First create dictionary of example names to their titles from the main page
                    var exampleTitles = new Dictionary<string, string>();
                    var mainPageFile = Directory.GetFiles(componentDir, "*.razor")
                                              .FirstOrDefault(f => !f.Contains("Page"));

                    if (mainPageFile != null)
                    {
                        var pageContent = File.ReadAllText(mainPageFile);
                        var sectionMatches = Regex.Matches(pageContent, @"<DocsPageSection>.*?</DocsPageSection>", RegexOptions.Singleline);

                        foreach (Match sectionMatch in sectionMatches)
                        {
                            var section = sectionMatch.Value;

                            // Extract Title
                            var titleMatch = Regex.Match(section, @"<SectionHeader\s+Title=""([^""]+)""");
                            var title = titleMatch.Success ? titleMatch.Groups[1].Value : string.Empty;

                            // Extract Code example name
                            var codeMatch = Regex.Match(section, @"Code=""@nameof\(([^)]+)\)""");
                            if (codeMatch.Success)
                            {
                                var exampleName = codeMatch.Groups[1].Value;
                                if (!string.IsNullOrEmpty(title))
                                {
                                    exampleTitles[exampleName] = title;
                                }
                            }
                        }
                    }

                    // Now process example files and match with titles
                    var exampleFiles = Directory.GetFiles(examplesDir, "*Example.razor");
                    foreach (var exampleFile in exampleFiles)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(exampleFile);
                        // Extract short name by removing "Example" suffix
                        var shortName = fileName.EndsWith("Example")
                            ? fileName.Substring(0, fileName.Length - "Example".Length)
                            : fileName;
                        // Get content
                        var exampleContent = File.ReadAllText(exampleFile);
                        Examples.Add(new ComponentExample
                        {
                            ComponentName = componentShortName,
                            ExampleFullName = exampleTitles.TryGetValue(fileName, out var title) ? title : componentShortName,
                            ExampleShortName = shortName,
                            ExampleFileName = fileName,
                            ExampleContent = exampleContent,
                        });
                    }
                }
            }
        }
    }
}
