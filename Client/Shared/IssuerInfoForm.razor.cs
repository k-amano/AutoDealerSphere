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

        // Email settings parameters
        [Parameter]
        public AutoDealerSphere.Shared.Models.EmailSettings EmailSettings { get; set; }
        [Parameter]
        public string Password { get; set; }
        [Parameter]
        public bool IsEmailProcessing { get; set; }
        [Parameter]
        public EventCallback OnTestConnection { get; set; }

        private bool _submitted = false;

        private class AccountType
        {
            public string Value { get; set; }
            public string Text { get; set; }
        }

        private class SmtpPortOption
        {
            public int Value { get; set; }
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

            // EmailSettingsが初期化されていない場合は新規作成
            if (EmailSettings == null)
            {
                EmailSettings = new AutoDealerSphere.Shared.Models.EmailSettings();
            }

            // 初期ポート設定（デフォルトはSSL/TLS使用のポート）
            if (EmailSettings.SmtpPort == 0)
            {
                EmailSettings.SmtpPort = EmailSettings.EnableSsl ? 587 : 25;
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
            // DropDownListの初期値を確実にセットするため
            StateHasChanged();
        }

        private List<SmtpPortOption> GetSmtpPortOptions()
        {
            if (EmailSettings?.EnableSsl == true)
            {
                // SSL/TLS使用時のポート
                return new List<SmtpPortOption>
                {
                    new SmtpPortOption { Value = 465, Text = "465 (SMTPS)" },
                    new SmtpPortOption { Value = 587, Text = "587 (STARTTLS)" }
                };
            }
            else
            {
                // SSL/TLS未使用時のポート
                return new List<SmtpPortOption>
                {
                    new SmtpPortOption { Value = 25, Text = "25 (標準SMTP)" },
                    new SmtpPortOption { Value = 587, Text = "587 (STARTTLS)" }
                };
            }
        }

        private void OnSslChanged(bool isChecked)
        {
            EmailSettings.EnableSsl = isChecked;

            // SSL/TLS設定変更時にポートを適切なデフォルト値に変更
            if (isChecked)
            {
                // SSL/TLS有効時は587をデフォルトに
                if (EmailSettings.SmtpPort == 25)
                {
                    EmailSettings.SmtpPort = 587;
                }
            }
            else
            {
                // SSL/TLS無効時は25をデフォルトに
                if (EmailSettings.SmtpPort == 465)
                {
                    EmailSettings.SmtpPort = 25;
                }
            }
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

        private async Task HandleTestConnection()
        {
            await OnTestConnection.InvokeAsync();
        }
    }
}