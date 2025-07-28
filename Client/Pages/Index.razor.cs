using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using System.Net.Http.Json;

namespace AutoDealerSphere.Client.Pages
{
	public partial class Index
	{
		[Parameter]
		public UserSearch Item { get; set; } = new();
		private IEnumerable<User> _users = [];
		private bool _initialized = false;

		protected override async Task OnInitializedAsync()
		{
			_users = await Http.GetFromJsonAsync<User[]>("/api/User/") ?? [];
			_initialized = true;
		}

		public void AddUserHandler()
		{
			this.NavigationManager.NavigateTo("./user");

		}

		private void OnClickEdit(User user)
		{
			this.NavigationManager.NavigateTo($"./user/{user.Id}");
		}

		private async Task OnClickSearch(UserSearch item)
		{
			var result = await Http.PostAsJsonAsync<UserSearch>("/api/User/", item);
			_users = JsonConvert.DeserializeObject<User[]>(await result.Content.ReadAsStringAsync()) ?? [];
		}

	}
}
