using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace AutoDealerSphere.Client.Shared
{
    public class PrefectureItem
    {
        public int Code { get; set; }
        public string Name { get; set; } = string.Empty;
        
        public PrefectureItem()
        {
            Name = string.Empty;
        }
        
        public PrefectureItem(int code, string name)
        {
            Code = code;
            Name = name ?? string.Empty;
        }
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
            new PrefectureItem(0, ""),
            new PrefectureItem(1, "北海道"),
            new PrefectureItem(2, "青森県"),
            new PrefectureItem(3, "岩手県"),
            new PrefectureItem(4, "宮城県"),
            new PrefectureItem(5, "秋田県"),
            new PrefectureItem(6, "山形県"),
            new PrefectureItem(7, "福島県"),
            new PrefectureItem(8, "茨城県"),
            new PrefectureItem(9, "栃木県"),
            new PrefectureItem(10, "群馬県"),
            new PrefectureItem(11, "埼玉県"),
            new PrefectureItem(12, "千葉県"),
            new PrefectureItem(13, "東京都"),
            new PrefectureItem(14, "神奈川県"),
            new PrefectureItem(15, "新潟県"),
            new PrefectureItem(16, "富山県"),
            new PrefectureItem(17, "石川県"),
            new PrefectureItem(18, "福井県"),
            new PrefectureItem(19, "山梨県"),
            new PrefectureItem(20, "長野県"),
            new PrefectureItem(21, "岐阜県"),
            new PrefectureItem(22, "静岡県"),
            new PrefectureItem(23, "愛知県"),
            new PrefectureItem(24, "三重県"),
            new PrefectureItem(25, "滋賀県"),
            new PrefectureItem(26, "京都府"),
            new PrefectureItem(27, "大阪府"),
            new PrefectureItem(28, "兵庫県"),
            new PrefectureItem(29, "奈良県"),
            new PrefectureItem(30, "和歌山県"),
            new PrefectureItem(31, "鳥取県"),
            new PrefectureItem(32, "島根県"),
            new PrefectureItem(33, "岡山県"),
            new PrefectureItem(34, "広島県"),
            new PrefectureItem(35, "山口県"),
            new PrefectureItem(36, "徳島県"),
            new PrefectureItem(37, "香川県"),
            new PrefectureItem(38, "愛媛県"),
            new PrefectureItem(39, "高知県"),
            new PrefectureItem(40, "福岡県"),
            new PrefectureItem(41, "佐賀県"),
            new PrefectureItem(42, "長崎県"),
            new PrefectureItem(43, "熊本県"),
            new PrefectureItem(44, "大分県"),
            new PrefectureItem(45, "宮崎県"),
            new PrefectureItem(46, "鹿児島県"),
            new PrefectureItem(47, "沖縄県")
        };

        protected override void OnInitialized()
        {
            base.OnInitialized();
            
            // Itemが初期化されていない場合は新規作成
            if (Item == null)
            {
                Item = new AutoDealerSphere.Shared.Models.Client();
            }
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
