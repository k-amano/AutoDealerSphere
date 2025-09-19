using Microsoft.AspNetCore.Components;
using AutoDealerSphere.Shared.Models;
using System.Net.Http.Json;

namespace AutoDealerSphere.Client.Pages
{
    public partial class VehicleList : ComponentBase
    {
        private List<Vehicle>? Vehicles = null;
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
            
            if (!string.IsNullOrWhiteSpace(search.VehicleNameOrModel))
                query.Add($"vehicleNameOrModel={Uri.EscapeDataString(search.VehicleNameOrModel)}");
            
            if (!string.IsNullOrWhiteSpace(search.LicensePlate))
                query.Add($"licensePlate={Uri.EscapeDataString(search.LicensePlate)}");
            
            if (!string.IsNullOrWhiteSpace(search.ClientName))
                query.Add($"clientName={Uri.EscapeDataString(search.ClientName)}");
                
            if (search.InspectionExpiryDateFrom.HasValue)
                query.Add($"inspectionExpiryDateFrom={search.InspectionExpiryDateFrom.Value:yyyy-MM-dd}");
                
            if (search.InspectionExpiryDateTo.HasValue)
                query.Add($"inspectionExpiryDateTo={search.InspectionExpiryDateTo.Value:yyyy-MM-dd}");

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

        private void EditClientFromVehicle(Vehicle vehicle)
        {
            if (vehicle.ClientId > 0)
            {
                NavigationManager.NavigateTo($"/client/{vehicle.ClientId}");
            }
        }

        public class VehicleSearchModel
        {
            public string? VehicleNameOrModel { get; set; }
            public string? LicensePlate { get; set; }
            public string? ClientName { get; set; }
            public DateTime? InspectionExpiryDateFrom { get; set; }
            public DateTime? InspectionExpiryDateTo { get; set; }
        }
    }
}