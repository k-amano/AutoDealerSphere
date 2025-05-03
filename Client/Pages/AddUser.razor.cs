using AutoDealerSphere.Shared.Models;
using System.Net.Http.Json;

namespace AutoDealerSphere.Client.Pages
{
	public partial class AddUser
	{
		public async void OnOK(User user)
		{
			var result = await Http.PostAsJsonAsync("/api/User/add", user);
			if (result?.StatusCode == System.Net.HttpStatusCode.OK)
			{
				this.NavigationManager.NavigateTo("./");
			}
		}
	}
}
