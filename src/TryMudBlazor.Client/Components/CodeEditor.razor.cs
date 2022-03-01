namespace TryMudBlazor.Client.Components
{
    using System;
    using System.Threading.Tasks;
    using Try.Core;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using System.Text.RegularExpressions;

    public partial class CodeEditor : IDisposable
    {
        private const string EditorId = "user-code-editor";

        private bool hasCodeChanged;

        [Inject]
        public IJSInProcessRuntime JsRuntime { get; set; }

        [Parameter]
        public string Code { get; set; }

        [Parameter]
        public CodeFileType CodeFileType { get; set; }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.TryGetValue<string>(nameof(this.Code), out var parameterValue))
            {
                this.hasCodeChanged = this.Code != parameterValue;
            }

            return base.SetParametersAsync(parameters);
        }

        public void Dispose() => this.JsRuntime.InvokeVoid("App.CodeEditor.dispose");

        internal void Focus() => this.JsRuntime.InvokeVoid("App.CodeEditor.focus");

        internal string GetCode() => this.JsRuntime.Invoke<string>("App.CodeEditor.getValue");

        protected override void OnAfterRender(bool firstRender)
        {
            var newCode = this.Code;
            if (newCode != null)
            {
                this.Code = Regex.Replace(newCode, @"@code\s*\r?\n\s*{", "@code {");
            }

            if (firstRender)
            {
                this.JsRuntime.InvokeVoid(
                   "App.CodeEditor.init",
                   EditorId,
                   this.Code ?? CoreConstants.MainComponentDefaultFileContent);
            }
            else if (this.hasCodeChanged)
            {
                var language = this.CodeFileType == CodeFileType.CSharp ? "csharp" : "razor";
                this.JsRuntime.InvokeVoid("App.CodeEditor.setValue", this.Code, language);
            }

            base.OnAfterRender(firstRender);
        }
    }
}
