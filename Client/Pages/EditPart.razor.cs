using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace AutoDealerSphere.Client.Pages
{
    public partial class EditPart
    {
        [Parameter]
        public int PartId { get; set; }
        private Part? _item;
        private bool _initialized = false;

        protected override async Task OnInitializedAsync()
        {
            if (PartId == 0)
            {
                // 新規作成
                _item = new Part()
                {
                    IsActive = true
                };
            }
            else
            {
                // 既存データの編集
                var part = await Http.GetFromJsonAsync<Part>($"/api/Parts/{PartId}");
                if (part != null)
                {
                    _item = new Part()
                    {
                        Id = part.Id,
                        PartName = part.PartName,
                        Type = part.Type,
                        UnitPrice = part.UnitPrice,
                        IsActive = part.IsActive,
                        CreatedAt = part.CreatedAt,
                        UpdatedAt = part.UpdatedAt
                    };
                }
            }
            _initialized = true;
        }

        private async Task OnClickOK(Part item)
        {
            if (PartId == 0)
            {
                // 新規作成
                var result = await Http.PostAsJsonAsync("/api/Parts", item);
                if (result?.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    NavigationManager.NavigateTo("/partlist");
                }
            }
            else
            {
                // 更新
                var result = await Http.PutAsJsonAsync($"/api/Parts/{PartId}", item);
                if (result?.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    NavigationManager.NavigateTo("/partlist");
                }
            }
        }

        private async Task OnClickDelete()
        {
            var result = await Http.DeleteAsync($"/api/Parts/{PartId}");
            if (result?.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                NavigationManager.NavigateTo("/partlist");
            }
        }
    }
}