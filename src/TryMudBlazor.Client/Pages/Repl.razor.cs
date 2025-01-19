namespace TryMudBlazor.Client.Pages
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.CodeAnalysis;
    using Microsoft.JSInterop;
    using MudBlazor;
    using Try.Core;
    using TryMudBlazor.Client.Components;
    using TryMudBlazor.Client.Models;
    using TryMudBlazor.Client.Services;

    public partial class Repl : IDisposable
    {
        [Inject] private LayoutService LayoutService { get; set; }

        private const string MainComponentCodePrefix = "@page \"/__main\"\n";
        private const string MainUserPagePath = "/__main";

        private DotNetObjectReference<Repl> dotNetInstance;
        private string errorMessage;
        private CodeFile activeCodeFile;
        private bool _examplesOpen = false;
        private bool _dockExamples = false;
        private List<ComponentExample> _compList = [];
        private ComponentExample _selectedExample = null;
        private string _compSearch = string.Empty;
        private bool _overlayExamples = false;
        private List<StaticAsset> cssORjsFiles = 
            [
            new StaticAsset { Location="https://code.jquery.com/jquery-3.7.1.slim.min.js", IsIncluded=true, Name="jquery-3.7.1.slim.min.js", FileType = FileType.JS },
            new StaticAsset { Location="https://stackpath.bootstrapcdn.com/font-awesome/4.7.0/css/font-awesome.min.css", IsIncluded=true, Name="font-awesome.min.css", FileType = FileType.CSS }
            ];

        private string LayoutStyle()
        {
            var num = 0;
            var layStyle = "padding-left: ";
            if (_dockExamples) num++;
            if (num == 0)
            {
                layStyle = string.Empty;
            }
            else if (num == 1)
            {
                layStyle += "calc(var(--mud-drawer-width, var(--mud-drawer-width-left)) + 2px);";
            }
            return layStyle;
        }

        [Inject]
        public ISnackbar Snackbar { get; set; }

        [Inject]
        public SnippetsService SnippetsService { get; set; }

        [Inject]
        public CompilationService CompilationService { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        [Inject]
        public IJSInProcessRuntime JsRuntime { get; set; }

        [Parameter]
        public string SnippetId { get; set; }

        public CodeEditor CodeEditorComponent { get; set; }

        public IDictionary<string, CodeFile> CodeFiles { get; set; } = new Dictionary<string, CodeFile>();

        private IList<string> CodeFileNames { get; set; } = new List<string>();

        private string CodeEditorContent => this.activeCodeFile?.Content;

        private CodeFileType CodeFileType => this.activeCodeFile?.Type ?? CodeFileType.Razor;

        private bool SaveSnippetPopupVisible { get; set; }

        private bool StaticAssetsPopupVisible { get; set; }

        private bool ShowConfirmExamplePopupVisible { get; set; }

        private IReadOnlyCollection<CompilationDiagnostic> Diagnostics { get; set; } = Array.Empty<CompilationDiagnostic>();

        private int ErrorsCount => this.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);

        private int WarningsCount => this.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);

        private bool AreDiagnosticsShown { get; set; }

        private string LoaderText { get; set; } = "Loading";

        private bool Loading { get; set; } = true;

        private bool ShowDiagnostics { get; set; }

        private void ToggleExamples()
        {
            _examplesOpen = !_examplesOpen;
            _dockExamples = false;
            UpdateOverlay();
        }

        private void ToggleDock(string docType)
        {
            if (docType == "examples")
            {
                _dockExamples = !_dockExamples;
            }
            UpdateOverlay();
        }

        private void OverlayClicked()
        {
            _overlayExamples = false;
            if (!_dockExamples) _examplesOpen = false;
            StateHasChanged();
        }

        private void UpdateOverlay()
        {
            if (!_dockExamples && _examplesOpen) _overlayExamples = true;
        }

        private void ToggleDiagnostics()
        {
            ShowDiagnostics = !ShowDiagnostics;
            AreDiagnosticsShown = ShowDiagnostics;
        }

        private async Task ClearCache()
        {
            await JsRuntime.InvokeVoidAsync("Try.clearCache");
            NavigationManager.NavigateTo(NavigationManager.BaseUri);
        }

        private string Version
        {
            get
            {
                //var v = typeof(MudText).Assembly.GetName().Version;
                return $"{DateTimeVersion.LatestVersion}";
            }
        }

        [JSInvokable]
        public async Task TriggerCompileAsync()
        {
            await this.CompileAsync();

            this.StateHasChanged();
        }

        public void Dispose()
        {
            this.dotNetInstance?.Dispose();
            this.JsRuntime.InvokeVoid(Try.Dispose);
        }

        protected override async Task OnInitializedAsync()
        {
            Snackbar.Clear();

            if (!string.IsNullOrWhiteSpace(this.SnippetId))
            {
                try
                {
                    this.CodeFiles = (await this.SnippetsService.GetSnippetContentAsync(this.SnippetId)).ToDictionary(f => f.Path, f => f);
                    if (!this.CodeFiles.Any())
                    {
                        this.errorMessage = "No files in snippet.";
                    }
                    else
                    {
                        this.activeCodeFile = this.CodeFiles.First().Value;
                    }
                }
                catch (ArgumentException)
                {
                    this.errorMessage = "Invalid Snippet ID.";
                }
                catch (Exception)
                {
                    this.errorMessage = "Unable to get snippet content. Please try again later.";
                }
            }

            if (!this.CodeFiles.Any())
            {
                this.activeCodeFile = new CodeFile
                {
                    Path = CoreConstants.MainComponentFilePath,
                    Content = CoreConstants.MainComponentDefaultFileContent,
                };
                this.CodeFiles.Add(CoreConstants.MainComponentFilePath, this.activeCodeFile);
            }

            this.CodeFileNames = this.CodeFiles.Keys.ToList();

            await base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!string.IsNullOrWhiteSpace(this.errorMessage))
            {
                Snackbar.Add(this.errorMessage, Severity.Error);
                this.errorMessage = null;
            }

            if (firstRender)
            {
                this.dotNetInstance = DotNetObjectReference.Create(this);
                await this.JsRuntime.InvokeVoidAsync(Try.Initialize, this.dotNetInstance);
                _compList = await SnippetsService.GetComponentExamples();
                Loading = false;
                StateHasChanged();
                await Task.Delay(1000);
                Snackbar.Add("You can now use .js and .css files as well as Static Assets from cdn.", Severity.Info);
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        private void ConfirmExample(ComponentExample comp)
        {
            _selectedExample = comp;
            ShowConfirmExamplePopup();
        }

        private void ConfirmExampleChanged(bool confirmed)
        {            
            if (confirmed)
            {
                CopyExample(_selectedExample);
            }
            _selectedExample = null;
        }

        private void CopyExample(ComponentExample comp)
        {
            // Clear existing files
            CodeFiles.Clear();

            // Add main example file
            var mainFile = new CodeFile
            {
                Path = CoreConstants.MainComponentFilePath,
                Content = comp.AssociatedFiles[0].Content,
            };
            CodeFiles[mainFile.Path] = mainFile;

            // Process associated files, skipping the first one
            for (int i = 1; i < comp.AssociatedFiles.Count; i++)
            {
                var file = comp.AssociatedFiles[i];
                var associatedFile = new CodeFile
                {
                    Path = file.FileName,
                    Content = file.Content
                };
                CodeFiles[associatedFile.Path] = associatedFile;
            }


            // Update active file to main component
            activeCodeFile = mainFile;
            CodeFileNames = CodeFiles.Keys.ToList();

            if (!_dockExamples)
            {
                ToggleExamples();
            }
        }

        private async Task CompileAsync()
        {
            this.Loading = true;
            this.LoaderText = "Processing";

            await Task.Delay(10); // Ensure rendering has time to be called

            CompileToAssemblyResult compilationResult = null;
            CodeFile mainComponent = null;
            string originalMainComponentContent = null;
            try
            {
                this.UpdateActiveCodeFileContent();

                // Add the necessary main component code prefix and store the original content so we can revert right after compilation.
                if (this.CodeFiles.TryGetValue(CoreConstants.MainComponentFilePath, out mainComponent))
                {
                    originalMainComponentContent = mainComponent.Content;
                    mainComponent.Content = MainComponentCodePrefix + "\n" + originalMainComponentContent.Replace(MainComponentCodePrefix, "");
                }

                compilationResult = await this.CompilationService.CompileToAssemblyAsync(
                    this.CodeFiles.Values,
                    this.UpdateLoaderTextAsync);

                //// clear user scripts and styles
                //await JsRuntime.InvokeVoidAsync("removeUserScriptsAndStyles");

                //// add cdn style scripts and styles
                //await JsRuntime.InvokeVoidAsync("insertAssetsIntoIframe");

                //// set js scripts
                //foreach (CodeFile f  in this.CodeFiles.Values.Where(x => x.Type == CodeFileType.Js))
                //{
                //    await JsRuntime.InvokeVoidAsync("insertJsContentIntoIframe", f.Content);
                //}

                //// set css scripts
                //foreach (CodeFile f in this.CodeFiles.Values.Where(x => x.Type == CodeFileType.Css))
                //{
                //    await JsRuntime.InvokeVoidAsync("insertCssContentIntoIframe", f.Content);
                //}

                this.Diagnostics = compilationResult.Diagnostics.OrderByDescending(x => x.Severity).ThenBy(x => x.Code).ToList();
                this.AreDiagnosticsShown = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Snackbar.Add("Error while compiling the code.", Severity.Error);
            }
            finally
            {
                if (mainComponent != null)
                {
                    mainComponent.Content = originalMainComponentContent;
                }

                this.Loading = false;
            }

            if (compilationResult?.AssemblyBytes?.Length > 0)
            {
                // Make sure the DLL is updated before reloading the user page
                await this.JsRuntime.InvokeVoidAsync(Try.CodeExecution.UpdateUserComponentsDll, compilationResult.AssemblyBytes);

                // TODO: Add error page in iframe
                this.JsRuntime.InvokeVoid(Try.ReloadIframe, "user-page-window", MainUserPagePath);
            }
        }

        private void ShowSaveSnippetPopup() => this.SaveSnippetPopupVisible = !this.SaveSnippetPopupVisible;

        private void ShowStaticAssetsPopup() => this.StaticAssetsPopupVisible = !this.StaticAssetsPopupVisible;

        private void ShowConfirmExamplePopup() => this.ShowConfirmExamplePopupVisible = !this.ShowConfirmExamplePopupVisible;

        private void HandleTabActivate(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            this.UpdateActiveCodeFileContent();

            if (this.CodeFiles.TryGetValue(name, out var codeFile))
            {
                this.activeCodeFile = codeFile;

                this.CodeEditorComponent.Focus();
            }
        }

        private void HandleTabClose(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            this.CodeFiles.Remove(name);
        }

        private void HandleTabCreate(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            var nameWithoutExtension = Path.GetFileNameWithoutExtension(name);

            var newCodeFile = new CodeFile { Path = name };

            newCodeFile.Content = newCodeFile.Type == CodeFileType.CSharp
                ? string.Format(CoreConstants.DefaultCSharpFileContentFormat, nameWithoutExtension)
                : string.Format(CoreConstants.DefaultRazorFileContentFormat, nameWithoutExtension);

            this.CodeFiles.TryAdd(name, newCodeFile);

            this.JsRuntime.InvokeVoid(Try.Editor.SetLangugage, newCodeFile.Type == CodeFileType.CSharp ? "csharp" : "razor");
        }

        private void UpdateActiveCodeFileContent()
        {
            if (this.activeCodeFile == null)
            {
                Snackbar.Add("No active file to update.", Severity.Error);
                return;
            }

            this.activeCodeFile.Content = this.CodeEditorComponent.GetCode();
        }

        private Task UpdateLoaderTextAsync(string loaderText)
        {
            this.LoaderText = loaderText;

            this.StateHasChanged();

            return Task.Delay(10); // Ensure rendering has time to be called
        }

        private async void UpdateTheme()
        {
            await LayoutService.ToggleDarkMode();
            string theme = LayoutService.IsDarkMode ? "vs-dark" : "default";
            this.JsRuntime.InvokeVoid(Try.Editor.SetTheme, theme);
        }
    }
}
