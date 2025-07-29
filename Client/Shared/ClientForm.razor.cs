using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace AutoDealerSphere.Client.Shared
{
    public partial class ClientForm
    {
        [Parameter]
        public AutoDealerSphere.Shared.Models.Client Item { get; set; } = new();
        [Parameter]
        public EventCallback<AutoDealerSphere.Shared.Models.Client> OnClickOK { get; set; }
        [Parameter]
        public EventCallback OnClickDelete { get; set; }
        private bool IsVisible { get; set; } = false;
        private bool _submitted = false;
        
        private List<Prefecture> Prefectures { get; set; } = Prefecture.GetAllWithEmpty();

        protected override void OnInitialized()
        {
            base.OnInitialized();
            
            // Itemが初期化されていない場合は新規作成
            if (Item == null)
            {
                Item = new AutoDealerSphere.Shared.Models.Client();
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
            // DropDownListの初期値を確実にセットするため
            StateHasChanged();
        }

        private async Task OnValidated()
        {
            if (_submitted)
            {
                await this.OnClickOK.InvokeAsync(this.Item);
            }
        }

        public void OnUnvalidated()
        {
            this._submitted = false;
        }

        public void OnCancel()
        {
            this.NavigationManager.NavigateTo("/clientlist");
        }

        private void OnRegister()
        {
            this._submitted = true;
        }

        private void OnOpenDialogue()
        {
            this.IsVisible = true;
        }

        private void OnCloseDialogue()
        {
            this.IsVisible = false;
        }

        private async Task OnDelete()
        {
            await this.OnClickDelete.InvokeAsync();
        }
    }
}
