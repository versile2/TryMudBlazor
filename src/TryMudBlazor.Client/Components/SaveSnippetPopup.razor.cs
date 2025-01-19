namespace TryMudBlazor.Client.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using MudBlazor;
    using Try.Core;
    using TryMudBlazor.Client.Models;
    using TryMudBlazor.Client.Pages;
    using TryMudBlazor.Client.Services;

    public partial class SaveSnippetPopup
    {
        [Inject]
        public ISnackbar Snackbar { get; set; }

        [Inject]
        protected IJsApiService JsApiService { get; set; }

        [Inject]
        public IJSInProcessRuntime JsRuntime { get; set; }

        [Inject]
        public SnippetsService SnippetsService { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        [Parameter]
        public bool Visible { get; set; }

        [Parameter]
        public EventCallback<bool> VisibleChanged { get; set; }

        [Parameter]
        public List<CodeFile> CodeFiles { get; set; } = new();

        [Parameter]
        public IEnumerable<StaticAsset> StaticAssets { get; set; }

        [Parameter]
        public Action UpdateActiveCodeFileContentAction { get; set; }

        private bool Loading { get; set; }
        private string SnippetLink { get; set; }

        private async Task CopyLinkToClipboard()
        {
            await JsApiService.CopyToClipboardAsync(SnippetLink);
        }

        private async Task SaveAsync()
        {
            Loading = true;

            try
            {
                this.UpdateActiveCodeFileContentAction?.Invoke();
                CodeFile codeFile;
                if (this.StaticAssets.Any())
                {
                    codeFile = new CodeFile
                    {
                        Path = "cssOrJs.css",
                        Content = JsonSerializer.Serialize(this.StaticAssets),
                    };
                    this.CodeFiles.Add(codeFile);
                }

                var snippetId = await this.SnippetsService.SaveSnippetAsync(this.CodeFiles);
                var urlBuilder = new UriBuilder(this.NavigationManager.BaseUri) { Path = $"snippet/{snippetId}" };
                this.SnippetLink = urlBuilder.Uri.ToString();
                this.JsRuntime.InvokeVoid(Try.ChangeDisplayUrl, SnippetLink);
            }
            catch (InvalidOperationException ex)
            {
                Snackbar.Add(ex.Message, Severity.Error);
            }
            catch (Exception)
            {
                Snackbar.Add("Error while saving snippet. Please try again later.", Severity.Error);
            }
            finally
            {
                Loading = false;
            }
        }

        private async void OnClose()
        {
            Loading = false;
            SnippetLink = string.Empty;

            await VisibleChanged.InvokeAsync(false);
        }
    }
}
