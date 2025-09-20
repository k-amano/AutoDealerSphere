using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoDealerSphere.Server.Services;
using AutoDealerSphere.Shared.Models;
using System.Text.Json;

namespace AutoDealerSphere.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehiclesController : ControllerBase
    {
        private readonly SQLDBContext _context;

        public VehiclesController(SQLDBContext context)
        {
            _context = context;
        }

        // GET: api/vehicles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehicles()
        {
            return await _context.Vehicles
                .Include(v => v.Client)
                .OrderBy(v => v.Id)
                .ToListAsync();
        }

        // GET: api/vehicles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Vehicle>> GetVehicle(int id)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.Client)
                .Include(v => v.VehicleCategory)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vehicle == null)
            {
                return NotFound();
            }

            return vehicle;
        }

        // GET: api/vehicles/byClient/5
        [HttpGet("byClient/{clientId}")]
        public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehiclesByClient(int clientId)
        {
            return await _context.Vehicles
                .Where(v => v.ClientId == clientId)
                .OrderBy(v => v.VehicleName)
                .ToListAsync();
        }

        // GET: api/vehicles/search
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Vehicle>>> SearchVehicles(
            [FromQuery] string? vehicleNameOrModel,
            [FromQuery] string? licensePlate,
            [FromQuery] string? clientName,
            [FromQuery] DateTime? inspectionExpiryDateFrom,
            [FromQuery] DateTime? inspectionExpiryDateTo)
        {
            var query = _context.Vehicles
                .Include(v => v.Client)
                .AsQueryable();

            // 車名または型式の部分一致
            if (!string.IsNullOrWhiteSpace(vehicleNameOrModel))
            {
                query = query.Where(v => 
                    (v.VehicleName != null && v.VehicleName.Contains(vehicleNameOrModel)) ||
                    (v.VehicleModel != null && v.VehicleModel.Contains(vehicleNameOrModel)));
            }

            // ナンバープレート情報の部分一致
            if (!string.IsNullOrWhiteSpace(licensePlate))
            {
                query = query.Where(v => 
                    (v.LicensePlateLocation != null && v.LicensePlateLocation.Contains(licensePlate)) ||
                    (v.LicensePlateClassification != null && v.LicensePlateClassification.Contains(licensePlate)) ||
                    (v.LicensePlateHiragana != null && v.LicensePlateHiragana.Contains(licensePlate)) ||
                    (v.LicensePlateNumber != null && v.LicensePlateNumber.Contains(licensePlate)));
            }

            // 所有者名の部分一致
            if (!string.IsNullOrWhiteSpace(clientName))
            {
                query = query.Where(v => v.Client != null && v.Client.Name.Contains(clientName));
            }

            // 車検満了日の範囲指定
            if (inspectionExpiryDateFrom.HasValue)
            {
                query = query.Where(v => v.InspectionExpiryDate >= inspectionExpiryDateFrom.Value);
            }

            if (inspectionExpiryDateTo.HasValue)
            {
                query = query.Where(v => v.InspectionExpiryDate <= inspectionExpiryDateTo.Value);
            }

            return await query.OrderBy(v => v.Id).ToListAsync();
        }

        // POST: api/vehicles
        [HttpPost]
        public async Task<ActionResult<Vehicle>> PostVehicle(Vehicle vehicle)
        {
            vehicle.CreatedAt = DateTime.Now;
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.Id }, vehicle);
        }

        // PUT: api/vehicles/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVehicle(int id, Vehicle vehicle)
        {
            if (id != vehicle.Id)
            {
                return BadRequest();
            }

            vehicle.UpdatedAt = DateTime.Now;
            _context.Entry(vehicle).State = EntityState.Modified;
            
            // Clientプロパティは更新しない
            _context.Entry(vehicle).Property(v => v.ClientId).IsModified = true;
            _context.Entry(vehicle).Reference(v => v.Client).IsModified = false;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VehicleExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/vehicles/{id}/import-json
        [HttpPost("{id}/import-json")]
        public async Task<IActionResult> ImportJson(int id, [FromBody] JsonDocument jsonData)
        {
            try
            {
                // Find the existing vehicle
                var vehicle = await _context.Vehicles.FindAsync(id);
                if (vehicle == null)
                {
                    return NotFound(new { error = "指定された車両が見つかりません。" });
                }

                // Parse JSON and update vehicle properties
                var root = jsonData.RootElement;

                // 基本情報
                if (root.TryGetProperty("inspection_certificate_number", out var inspectionCertNumber))
                    vehicle.InspectionCertificateNumber = inspectionCertNumber.GetString();

                if (root.TryGetProperty("vehicle_name", out var vehicleName))
                    vehicle.VehicleName = vehicleName.GetString();

                if (root.TryGetProperty("vehicle_model", out var vehicleModel))
                    vehicle.VehicleModel = vehicleModel.GetString();

                if (root.TryGetProperty("chassis_number", out var chassisNumber))
                    vehicle.ChassisNumber = chassisNumber.GetString();

                if (root.TryGetProperty("engine_model", out var engineModel))
                    vehicle.EngineModel = engineModel.GetString();

                if (root.TryGetProperty("type_certification_number", out var typeCertNumber))
                    vehicle.TypeCertificationNumber = typeCertNumber.GetString();

                if (root.TryGetProperty("category_number", out var categoryNumber))
                    vehicle.CategoryNumber = categoryNumber.GetString();

                // 車両諸元
                if (root.TryGetProperty("first_registration_date", out var firstRegDate) && firstRegDate.ValueKind == JsonValueKind.String)
                {
                    if (DateTime.TryParse(firstRegDate.GetString(), out var parsedDate))
                        vehicle.FirstRegistrationDate = parsedDate;
                }

                if (root.TryGetProperty("purpose", out var purpose))
                    vehicle.Purpose = purpose.GetString();

                if (root.TryGetProperty("personal_business_use", out var personalBusinessUse))
                    vehicle.PersonalBusinessUse = personalBusinessUse.GetString();

                if (root.TryGetProperty("body_shape", out var bodyShape))
                    vehicle.BodyShape = bodyShape.GetString();

                if (root.TryGetProperty("seating_capacity", out var seatingCapacity))
                {
                    if (seatingCapacity.TryGetInt32(out var seating))
                        vehicle.SeatingCapacity = seating;
                }

                if (root.TryGetProperty("max_load_capacity", out var maxLoadCapacity))
                {
                    if (maxLoadCapacity.TryGetInt32(out var maxLoad))
                        vehicle.MaxLoadCapacity = maxLoad;
                }

                if (root.TryGetProperty("vehicle_weight", out var vehicleWeight))
                {
                    if (vehicleWeight.TryGetInt32(out var weight))
                        vehicle.VehicleWeight = weight;
                }

                if (root.TryGetProperty("vehicle_total_weight", out var vehicleTotalWeight))
                {
                    if (vehicleTotalWeight.TryGetInt32(out var totalWeight))
                        vehicle.VehicleTotalWeight = totalWeight;
                }

                if (root.TryGetProperty("vehicle_length", out var vehicleLength))
                {
                    if (vehicleLength.TryGetInt32(out var length))
                        vehicle.VehicleLength = length;
                }

                if (root.TryGetProperty("vehicle_width", out var vehicleWidth))
                {
                    if (vehicleWidth.TryGetInt32(out var width))
                        vehicle.VehicleWidth = width;
                }

                if (root.TryGetProperty("vehicle_height", out var vehicleHeight))
                {
                    if (vehicleHeight.TryGetInt32(out var height))
                        vehicle.VehicleHeight = height;
                }

                if (root.TryGetProperty("front_overhang", out var frontOverhang))
                {
                    if (frontOverhang.TryGetInt32(out var front))
                        vehicle.FrontOverhang = front;
                }

                if (root.TryGetProperty("rear_overhang", out var rearOverhang))
                {
                    if (rearOverhang.TryGetInt32(out var rear))
                        vehicle.RearOverhang = rear;
                }

                // 排気量（ccからLへの変換）
                if (root.TryGetProperty("displacement", out var displacement))
                {
                    if (displacement.TryGetDecimal(out var cc))
                        vehicle.Displacement = cc / 1000m; // cc to L
                }

                if (root.TryGetProperty("fuel_type", out var fuelType))
                    vehicle.FuelType = fuelType.GetString();

                // 車検・使用者情報
                if (root.TryGetProperty("inspection_expiry_date", out var inspectionExpiry) && inspectionExpiry.ValueKind == JsonValueKind.String)
                {
                    if (DateTime.TryParse(inspectionExpiry.GetString(), out var expiryDate))
                        vehicle.InspectionExpiryDate = expiryDate;
                }

                if (root.TryGetProperty("user_name_or_company", out var userNameOrCompany))
                    vehicle.UserNameOrCompany = userNameOrCompany.GetString();

                if (root.TryGetProperty("user_address", out var userAddress))
                    vehicle.UserAddress = userAddress.GetString();

                if (root.TryGetProperty("base_location", out var baseLocation))
                    vehicle.BaseLocation = baseLocation.GetString();

                // ナンバープレート情報
                if (root.TryGetProperty("license_plate_location", out var licensePlateLocation))
                    vehicle.LicensePlateLocation = licensePlateLocation.GetString();

                if (root.TryGetProperty("license_plate_classification", out var licensePlateClassification))
                    vehicle.LicensePlateClassification = licensePlateClassification.GetString();

                if (root.TryGetProperty("license_plate_hiragana", out var licensePlateHiragana))
                    vehicle.LicensePlateHiragana = licensePlateHiragana.GetString();

                if (root.TryGetProperty("license_plate_number", out var licensePlateNumber))
                    vehicle.LicensePlateNumber = licensePlateNumber.GetString();

                // Update timestamp
                vehicle.UpdatedAt = DateTime.Now;

                // Mark entity as modified
                _context.Entry(vehicle).State = EntityState.Modified;

                // Save changes to database
                var changesSaved = await _context.SaveChangesAsync();

                // Verify that changes were actually saved
                if (changesSaved == 0)
                {
                    return StatusCode(500, new { error = "データベースへの保存に失敗しました。変更が保存されませんでした。" });
                }

                // Reload the entity from database to verify it was saved
                await _context.Entry(vehicle).ReloadAsync();

                // Double-check by querying the database again
                var verifiedVehicle = await _context.Vehicles.FindAsync(id);
                if (verifiedVehicle == null)
                {
                    return StatusCode(500, new { error = "データの保存確認に失敗しました。" });
                }

                return Ok(new
                {
                    success = true,
                    message = "JSONデータの取り込みに成功し、データベースへの保存を確認しました。",
                    changesSaved = changesSaved,
                    vehicleId = verifiedVehicle.Id,
                    updatedAt = verifiedVehicle.UpdatedAt
                });
            }
            catch (JsonException ex)
            {
                return BadRequest(new { error = $"JSONの解析に失敗しました: {ex.Message}" });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { error = $"データベースの更新に失敗しました: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"予期しないエラーが発生しました: {ex.Message}" });
            }
        }

        // DELETE: api/vehicles/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null)
            {
                return NotFound();
            }

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool VehicleExists(int id)
        {
            return _context.Vehicles.Any(e => e.Id == id);
        }
    }
}