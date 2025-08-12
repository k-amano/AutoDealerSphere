using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace AutoDealerSphere.Client.Shared
{
    public partial class IssuerInfoForm
    {
        [Parameter]
        public IssuerInfo Item { get; set; } = new();
        [Parameter]
        public EventCallback<IssuerInfo> OnClickOK { get; set; }
        
        private bool _submitted = false;

        private class AccountType
        {
            public string Value { get; set; }
            public string Text { get; set; }
        }

        private List<AccountType> AccountTypes { get; set; } = new List<AccountType>
        {
            new AccountType { Value = "", Text = "選択してください" },
            new AccountType { Value = "普通", Text = "普通" },
            new AccountType { Value = "当座", Text = "当座" },
            new AccountType { Value = "貯蓄", Text = "貯蓄" }
        };

        protected override void OnInitialized()
        {
            base.OnInitialized();
            
            // Itemが初期化されていない場合は新規作成
            if (Item == null)
            {
                Item = new IssuerInfo();
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
            this.NavigationManager.NavigateTo("/");
        }

        private void OnRegister()
        {
            this._submitted = true;
        }
    }
}