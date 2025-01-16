namespace TryMudBlazor.Client.Components
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using MudBlazor;
    using TryMudBlazor.Client.Models;
    using TryMudBlazor.Client.Services;

    public partial class StaticAssetsPopup
    {
        [Inject]
        public ISnackbar Snackbar { get; set; }

        [Inject]
        public HttpClient httpClient { get; set; }

        [Inject]
        protected IJsApiService JsApiService { get; set; }

        [Inject]
        public IJSInProcessRuntime JsRuntime { get; set; }        

        [Parameter]
        public bool Visible { get; set; }

        [Parameter]
        public EventCallback<bool> VisibleChanged { get; set; }

        [Parameter]
        public List<StaticAsset> AddLinks { get; set; }

        [Parameter]
        public EventCallback<List<StaticAsset>> AddLinksChanged { get; set; }

        private bool Loading { get; set; }

        private string AddLink { get; set; }

        private async Task CopyLinkToClipboard()
        {
            await JsApiService.CopyToClipboardAsync(AddLink);
        }

        private async Task PasteLinkFromClipboard()
        {
            await Task.CompletedTask;
        }

        private async Task VerifyLink()
        {
            Loading = true;
            await Task.Yield();

            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, AddLink));

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Link exists.");
                }
                else
                {
                    Console.WriteLine($"Link does not exist or error occurred. Status code: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                // Handle network errors or other exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            finally
            {
                Loading = false;
            }
        }

        private void DeleteLink(StaticAsset item)
        {
            AddLinks.Remove(item);
        }

        private async void OnClose()
        {
            Loading = false;
            AddLink = string.Empty;

            await VisibleChanged.InvokeAsync(false);
        }

    }
}