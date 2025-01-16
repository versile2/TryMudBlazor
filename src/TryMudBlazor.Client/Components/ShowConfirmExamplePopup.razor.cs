namespace TryMudBlazor.Client.Components
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using MudBlazor;
    using TryMudBlazor.Client.Services;

    public partial class ShowConfirmExamplePopup
    {
        [Parameter]
        public bool Visible { get; set; }

        [Parameter]
        public EventCallback<bool> VisibleChanged { get; set; }

        [Parameter]
        public EventCallback<bool> ReturnResult { get; set; }

        private async Task OnClose()
        {
            await VisibleChanged.InvokeAsync(false);
        }

        private async Task ConfirmClick()
        {
            await ReturnResult.InvokeAsync(true);
            await OnClose();
        }

        private async Task CancelClick()
        {
            await ReturnResult.InvokeAsync(false);
            await OnClose();
        }

    }
}