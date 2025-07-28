using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Components;

namespace AutoDealerSphere.Client.Shared
{
	public partial class UserForm
	{
		[Parameter]
		public User Item { get; set; } = new();
		[Parameter]
		public EventCallback<User> OnClickOK { get; set; }
		[Parameter]
		public EventCallback OnClickDelete { get; set; }
		private bool IsVisible { get; set; } = false;
		private bool _submitted = false;


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
			this.NavigationManager.NavigateTo("./");
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
