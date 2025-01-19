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
        public IJSRuntime JsRuntime { get; set; }        

        [Parameter]
        public bool Visible { get; set; }

        [Parameter]
        public EventCallback<bool> VisibleChanged { get; set; }

        [Parameter]
        public List<StaticAsset> AddLinks { get; set; }

        [Parameter]
        public EventCallback<List<StaticAsset>> AddLinksChanged { get; set; }

        private bool Loading { get; set; }

        private string AddLink { get; set; } = string.Empty;

        private async Task PasteLinkFromClipboard()
        {
            string text = await JsRuntime.InvokeAsync<string>("copyFromClipboard");
            if (text != null)
            {
                AddLink = text;
            }
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
                    var name = AddLink.Substring(AddLink.LastIndexOf('/') + 1);
                    var link = new StaticAsset { Location=AddLink, IsIncluded=true, Name=name  };
                    if (AddLinks.Where(x => x.Name == name).ToList().Any())
                    {
                        Snackbar.Add("Can't have a link with the same name as another link.", Severity.Error);
                    }
                    else
                    {
                        if (link.Name.EndsWith(".js"))
                        {
                            link.FileType = FileType.JS;
                        } 
                        else if (link.Name.EndsWith(".css"))
                        {
                            link.FileType = FileType.CSS;
                        }
                        else
                        {
                            Snackbar.Add("Link must end in .js or .css", Severity.Warning);
                            return;
                        }
                        AddLinks.Add(link);
                        AddLink = string.Empty;
                    }                    
                }
                else
                {
                    Snackbar.Add($"Link does not exist or error occurred. Status code: {response.StatusCode}", Severity.Error);
                }
            }
            catch (Exception)
            {
                // Handle network errors or other exceptions
                Snackbar.Add("Unable to verify link, Link not added.", Severity.Error);
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