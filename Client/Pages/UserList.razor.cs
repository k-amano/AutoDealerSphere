using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using Newtonsoft.Json;

namespace AutoDealerSphere.Client.Pages
{
    public partial class UserList
    {
        private List<User> Users { get; set; } = new();
        private UserSearch Search { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        private void AddUser()
        {
            NavigationManager.NavigateTo("/user/0");
        }

        private async Task EditUser(int id)
        {
            NavigationManager.NavigateTo($"/user/{id}");
        }

        private async Task OnSearch(UserSearch search)
        {
            try
            {
                var result = await Http.PostAsJsonAsync<UserSearch>("/api/User/search", search);
                if (result.IsSuccessStatusCode)
                {
                    Users = JsonConvert.DeserializeObject<List<User>>(await result.Content.ReadAsStringAsync()) ?? new();
                }
                else
                {
                    // 検索APIがない場合はフロントエンドでフィルタリング
                    await LoadData();
                    
                    // 名前の部分一致
                    if (!string.IsNullOrEmpty(search.Name))
                    {
                        Users = Users.Where(u => u.Name.Contains(search.Name)).ToList();
                    }
                    
                    // メールアドレスの部分一致
                    if (!string.IsNullOrEmpty(search.Email))
                    {
                        Users = Users.Where(u => u.Email.Contains(search.Email)).ToList();
                    }
                }
            }
            catch
            {
                // エラーが発生した場合は全件取得
                await LoadData();
            }
        }

        private async Task LoadData()
        {
            var result = await Http.GetFromJsonAsync<List<User>>("/api/User");
            if (result != null)
            {
                Users = result;
            }
            else
            {
                Users = new();
            }
        }

        private void EditUserFromContext(object context)
        {
            var user = context as User;
            if (user != null)
            {
                NavigationManager.NavigateTo($"/user/{user.Id}");
            }
        }
    }
}