using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace AutoDealerSphere.Client.Pages
{
    public partial class AddClient
    {
        private AutoDealerSphere.Shared.Models.Client Client { get; set; } = new();

        private async Task OnOK(AutoDealerSphere.Shared.Models.Client client)
        {
            var result = await Http.PostAsJsonAsync("/api/Client/add", client);
            if (result?.StatusCode == System.Net.HttpStatusCode.OK)
            {
                // 顧客一覧画面に遷移
                this.NavigationManager.NavigateTo("/clientlist");
            }
        }
    }
}
