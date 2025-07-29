using Microsoft.AspNetCore.Components;
using AutoDealerSphere.Shared.Models;
using System.Net.Http.Json;

namespace AutoDealerSphere.Client.Pages
{
    public partial class VehicleList : ComponentBase
    {
        private List<Vehicle> Vehicles = new();
        private VehicleSearchModel Search = new();

        protected override async Task OnInitializedAsync()
        {
            await GetVehicles();
        }

        private async Task GetVehicles()
        {
            var response = await Http.GetAsync("api/vehicles");
            if (response.IsSuccessStatusCode)
            {
                Vehicles = await response.Content.ReadFromJsonAsync<List<Vehicle>>() ?? new();
            }
        }

        private async Task OnSearch(VehicleSearchModel search)
        {
            var query = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(search.VehicleName))
                query.Add($"vehicleName={Uri.EscapeDataString(search.VehicleName)}");
            
            if (!string.IsNullOrWhiteSpace(search.LicensePlateNumber))
                query.Add($"licensePlateNumber={Uri.EscapeDataString(search.LicensePlateNumber)}");
            
            if (!string.IsNullOrWhiteSpace(search.ClientName))
                query.Add($"clientName={Uri.EscapeDataString(search.ClientName)}");

            var queryString = query.Any() ? "?" + string.Join("&", query) : "";
            var response = await Http.GetAsync($"api/vehicles/search{queryString}");
            
            if (response.IsSuccessStatusCode)
            {
                Vehicles = await response.Content.ReadFromJsonAsync<List<Vehicle>>() ?? new();
            }
        }

        private void AddVehicle()
        {
            NavigationManager.NavigateTo("/vehicle/0");
        }

        private void EditVehicle(int id)
        {
            NavigationManager.NavigateTo($"/vehicle/{id}");
        }

        private void EditVehicleFromContext(object context)
        {
            if (context is Vehicle vehicle)
            {
                EditVehicle(vehicle.Id);
            }
        }

        private string GetLicensePlateFromContext(object context)
        {
            if (context is Vehicle vehicle)
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(vehicle.LicensePlateLocation))
                    parts.Add(vehicle.LicensePlateLocation);
                if (!string.IsNullOrWhiteSpace(vehicle.LicensePlateClassification))
                    parts.Add(vehicle.LicensePlateClassification);
                if (!string.IsNullOrWhiteSpace(vehicle.LicensePlateHiragana))
                    parts.Add(vehicle.LicensePlateHiragana);
                if (!string.IsNullOrWhiteSpace(vehicle.LicensePlateNumber))
                    parts.Add(vehicle.LicensePlateNumber);
                
                return string.Join(" ", parts);
            }
            return "";
        }

        private string GetClientNameFromContext(object context)
        {
            if (context is Vehicle vehicle && vehicle.Client != null)
            {
                return vehicle.Client.Name;
            }
            return "";
        }

        public class VehicleSearchModel
        {
            public string? VehicleName { get; set; }
            public string? LicensePlateNumber { get; set; }
            public string? ClientName { get; set; }
        }
    }
}