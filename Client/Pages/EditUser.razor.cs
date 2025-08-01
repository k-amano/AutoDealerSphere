using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace AutoDealerSphere.Client.Pages
{
	public partial class EditUser
	{
		[Parameter]
		public int UserId { get; set; }
		private User? _item;
		private bool _initialized = false;

		protected override async Task OnInitializedAsync()
		{
			var user = await Http.GetFromJsonAsync<User>($"/api/User/{this.UserId}");
			if (user != null)
			{
				_item = new()
				{
					Id = user.Id,
					Name = user.Name,
					Email = user.Email,
					Password = user.Password,
					Role = user.Role
				};
			}
			_initialized = true;
		}

		private async Task OnClickOK(User item)
		{
			var result = await Http.PostAsJsonAsync("/api/User/update", item);
			if (result?.StatusCode == System.Net.HttpStatusCode.OK)
			{
				this.NavigationManager.NavigateTo("/userlist");
			}
		}

		private async Task OnClickDelete()
		{
			var result = await Http.DeleteAsync($"/api/User/{this.UserId}");
			if (result?.StatusCode == System.Net.HttpStatusCode.NoContent)
			{
				this.NavigationManager.NavigateTo("/userlist");
			}
		}
	}
}
