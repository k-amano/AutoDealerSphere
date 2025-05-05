using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace AutoDealerSphere.Client.Shared
{
    public class PrefectureItem
    {
        public int Code { get; set; }
        public string Name { get; set; } = "";
    }

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
        
        private List<PrefectureItem> Prefectures { get; set; } = new List<PrefectureItem>
        {
            new PrefectureItem { Code = 1, Name = "北海道" },
            new PrefectureItem { Code = 2, Name = "青森県" },
            new PrefectureItem { Code = 3, Name = "岩手県" },
            new PrefectureItem { Code = 4, Name = "宮城県" },
            new PrefectureItem { Code = 5, Name = "秋田県" },
            new PrefectureItem { Code = 6, Name = "山形県" },
            new PrefectureItem { Code = 7, Name = "福島県" },
            new PrefectureItem { Code = 8, Name = "茨城県" },
            new PrefectureItem { Code = 9, Name = "栃木県" },
            new PrefectureItem { Code = 10, Name = "群馬県" },
            new PrefectureItem { Code = 11, Name = "埼玉県" },
            new PrefectureItem { Code = 12, Name = "千葉県" },
            new PrefectureItem { Code = 13, Name = "東京都" },
            new PrefectureItem { Code = 14, Name = "神奈川県" },
            new PrefectureItem { Code = 15, Name = "新潟県" },
            new PrefectureItem { Code = 16, Name = "富山県" },
            new PrefectureItem { Code = 17, Name = "石川県" },
            new PrefectureItem { Code = 18, Name = "福井県" },
            new PrefectureItem { Code = 19, Name = "山梨県" },
            new PrefectureItem { Code = 20, Name = "長野県" },
            new PrefectureItem { Code = 21, Name = "岐阜県" },
            new PrefectureItem { Code = 22, Name = "静岡県" },
            new PrefectureItem { Code = 23, Name = "愛知県" },
            new PrefectureItem { Code = 24, Name = "三重県" },
            new PrefectureItem { Code = 25, Name = "滋賀県" },
            new PrefectureItem { Code = 26, Name = "京都府" },
            new PrefectureItem { Code = 27, Name = "大阪府" },
            new PrefectureItem { Code = 28, Name = "兵庫県" },
            new PrefectureItem { Code = 29, Name = "奈良県" },
            new PrefectureItem { Code = 30, Name = "和歌山県" },
            new PrefectureItem { Code = 31, Name = "鳥取県" },
            new PrefectureItem { Code = 32, Name = "島根県" },
            new PrefectureItem { Code = 33, Name = "岡山県" },
            new PrefectureItem { Code = 34, Name = "広島県" },
            new PrefectureItem { Code = 35, Name = "山口県" },
            new PrefectureItem { Code = 36, Name = "徳島県" },
            new PrefectureItem { Code = 37, Name = "香川県" },
            new PrefectureItem { Code = 38, Name = "愛媛県" },
            new PrefectureItem { Code = 39, Name = "高知県" },
            new PrefectureItem { Code = 40, Name = "福岡県" },
            new PrefectureItem { Code = 41, Name = "佐賀県" },
            new PrefectureItem { Code = 42, Name = "長崎県" },
            new PrefectureItem { Code = 43, Name = "熊本県" },
            new PrefectureItem { Code = 44, Name = "大分県" },
            new PrefectureItem { Code = 45, Name = "宮崎県" },
            new PrefectureItem { Code = 46, Name = "鹿児島県" },
            new PrefectureItem { Code = 47, Name = "沖縄県" }
        };

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
