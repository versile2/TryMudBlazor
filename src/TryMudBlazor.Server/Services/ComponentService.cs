namespace TryMudBlazor.Server.Services
{
    using System.Text.RegularExpressions;
    using Try.Core;

    public class ComponentService
    {
        private readonly string _basePath = Path.Combine("..", "submodules", "MudBlazor", "src", "MudBlazor.Docs", "Pages", "Components");
        private readonly string _basePathDocker = Path.Combine("submodules", "MudBlazor", "src", "MudBlazor.Docs", "Pages", "Components");
        public bool IsInitialized { get; private set; } = false;
        public List<ComponentExample> Examples { get; private set; } = [];

        public void Initialize()
        {
            LoadComponents();
        }

        private void LoadComponents()
        {
            string usePath = _basePath;
            if (!Directory.Exists(usePath))
            {
                usePath = _basePathDocker;
            }
            if (!Directory.Exists(usePath))
            {
                Console.WriteLine("No path to components found.");
                Console.WriteLine($"Current path: {Path.GetFullPath(".")}");
                return;
            }
            var componentDirs = Directory.GetDirectories(usePath);

            foreach (var componentDir in componentDirs)
            {
                var componentShortName = Path.GetFileName(componentDir);
                var examplesDir = Path.Combine(componentDir, "Examples");

                if (Directory.Exists(examplesDir))
                {
                    // First create dictionary of example names to their titles from the main page
                    var exampleTitles = new Dictionary<string, string>();
                    var mainPageFile = Directory.GetFiles(componentDir, "*.razor")
                                              .FirstOrDefault(f => f.Contains("Page"));

                    if (mainPageFile != null)
                    {
                        var pageContent = File.ReadAllText(mainPageFile);
                        var sectionMatches = Regex.Matches(pageContent, @"<DocsPageSection\b[^>]*>[\s\S]*?<\/DocsPageSection>", RegexOptions.Singleline);

                        foreach (Match sectionMatch in sectionMatches)
                        {
                            var section = sectionMatch.Value;

                            // Extract Title
                            var titleMatch = Regex.Match(section, @"<SectionHeader\b[^>]*Title\s*=\s*""([^""]+)""", RegexOptions.Singleline);
                            var title = titleMatch.Success ? titleMatch.Groups[1].Value : string.Empty;

                            // Extract Code example names - handle both single and multiple file cases
                            var singleCodeMatch = Regex.Match(section, @"Code\s*=\s*""@nameof\(([^)]+)\)""", RegexOptions.Singleline);
                            if (singleCodeMatch.Success)
                            {
                                var exampleName = singleCodeMatch.Groups[1].Value;
                                if (!string.IsNullOrEmpty(title))
                                {
                                    exampleTitles[exampleName] = title;
                                }
                            }

                            // Extract multiple files case
                            var multiCodeMatch = Regex.Match(section, @"Codes\s*=\s*""@\(new\[\]\s*{([^}]+)}\)""", RegexOptions.Singleline);
                            if (multiCodeMatch.Success)
                            {
                                var codeFilesContent = multiCodeMatch.Groups[1].Value;
                                var codeFileMatches = Regex.Matches(codeFilesContent, @"new\s+CodeFile\([^,]+,\s*nameof\(([^)]+)\)\)");

                                if (codeFileMatches.Count > 0)
                                {
                                    // Use the first file as the main example
                                    var mainExampleName = codeFileMatches[0].Groups[1].Value;
                                    if (!string.IsNullOrEmpty(title))
                                    {
                                        exampleTitles[mainExampleName] = title;
                                    }

                                    // Store associated files
                                    for (int i = 0; i < codeFileMatches.Count; i++)
                                    {
                                        var associatedFileName = codeFileMatches[i].Groups[1].Value;
                                        exampleTitles[$"{mainExampleName}_{associatedFileName}"] = title;
                                    }
                                }
                            }
                        }
                    }

                    // Process example files and match with titles
                    var allExampleFiles = Directory.GetFiles(examplesDir, "*Example*.razor");
                    var mainExampleFiles = allExampleFiles.Where(f => !Path.GetFileName(f).Contains("Example_")).ToList();
                    var associatedFiles = allExampleFiles.Where(f => Path.GetFileName(f).Contains("Example_")).ToList();

                    foreach (var mainExampleFile in mainExampleFiles)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(mainExampleFile);
                        var shortName = fileName.EndsWith("Example")
                            ? fileName.Substring(0, fileName.Length - "Example".Length)
                            : fileName;

                        var example = new ComponentExample
                        {
                            ComponentName = componentShortName,
                            ExampleFullName = exampleTitles.TryGetValue(fileName, out var title) ? title : componentShortName,
                            ExampleShortName = shortName,
                            AssociatedFiles = new List<ComponentFile> { new ComponentFile { FileName = fileName, ShortName = "Page", Content = CleanInvalidLines(File.ReadAllText(mainExampleFile)) } }
                        };

                        // Find and add associated files
                        var relatedFiles = associatedFiles.Where(f => Path.GetFileName(f).StartsWith(fileName + "_"));
                        foreach (var relatedFile in relatedFiles)
                        {
                            var relatedShortName = Path.GetFileNameWithoutExtension(relatedFile);
                            var relatedFileName = Path.GetFileName(relatedFile);

                            example.AssociatedFiles.Add(new ComponentFile
                            {
                                ShortName = relatedShortName,
                                FileName = relatedFileName,
                                Content = CleanInvalidLines(File.ReadAllText(relatedFile)),
                            });
                        }

                        Examples.Add(example);
                    }
                }
            }

            Examples = Examples.OrderBy(e => e.ComponentName).ThenBy(e => e.ExampleShortName).ToList();

        }

        private string CleanInvalidLines(string fileContents)
        {
            fileContents = Regex.Replace(fileContents, @"^@namespace\s+.*$", string.Empty, RegexOptions.Multiline);
            fileContents = Regex.Replace(fileContents, @"^@page\s+.*$", string.Empty, RegexOptions.Multiline);
            fileContents = Regex.Replace(fileContents, @"^@layout\s+.*$", string.Empty, RegexOptions.Multiline);
            return fileContents;
        }
    }
}