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

        // POST: api/vehicles/{id}/import-json
        [HttpPost("{id}/import-json")]
        public async Task<IActionResult> ImportVehicleJson(int id, [FromBody] JsonDocument jsonDocument)
        {
            try
            {
                // 既存の車両を取得
                var vehicle = await _context.Vehicles.FindAsync(id);
                if (vehicle == null)
                {
                    return NotFound("車両が見つかりません。");
                }

                // JSONから各フィールドをマッピング
                var root = jsonDocument.RootElement;

                // 車検証番号
                if (root.TryGetProperty("inspection_certificate_number", out var certNumber))
                {
                    vehicle.InspectionCertificateNumber = certNumber.GetString();
                }

                // 車名
                if (root.TryGetProperty("vehicle_name", out var vehicleName))
                {
                    vehicle.VehicleName = vehicleName.GetString();
                }

                // 型式
                if (root.TryGetProperty("vehicle_model", out var vehicleModel))
                {
                    vehicle.VehicleModel = vehicleModel.GetString();
                }

                // 車台番号
                if (root.TryGetProperty("chassis_number", out var chassisNumber))
                {
                    vehicle.ChassisNumber = chassisNumber.GetString();
                }

                // エンジン型式
                if (root.TryGetProperty("engine_model", out var engineModel))
                {
                    vehicle.EngineModel = engineModel.GetString();
                }

                // 型式指定番号
                if (root.TryGetProperty("type_certification_number", out var typeCert))
                {
                    vehicle.TypeCertificationNumber = typeCert.GetString();
                }

                // 類別区分番号
                if (root.TryGetProperty("category_number", out var categoryNum))
                {
                    vehicle.CategoryNumber = categoryNum.GetString();
                }

                // 初度登録年月
                if (root.TryGetProperty("first_registration_date", out var firstReg))
                {
                    if (DateTime.TryParse(firstReg.GetString(), out var regDate))
                    {
                        vehicle.FirstRegistrationDate = regDate;
                    }
                }

                // 用途
                if (root.TryGetProperty("purpose", out var purpose))
                {
                    vehicle.Purpose = purpose.GetString();
                }

                // 自家用・事業用
                if (root.TryGetProperty("personal_business_use", out var personalBusiness))
                {
                    vehicle.PersonalBusinessUse = personalBusiness.GetString();
                }

                // 車体の形状
                if (root.TryGetProperty("body_shape", out var bodyShape))
                {
                    vehicle.BodyShape = bodyShape.GetString();
                }

                // 乗車定員
                if (root.TryGetProperty("seating_capacity", out var seatingCapacity))
                {
                    if (seatingCapacity.TryGetInt32(out var capacity))
                    {
                        vehicle.SeatingCapacity = capacity;
                    }
                }

                // 最大積載量
                if (root.TryGetProperty("max_load_capacity", out var maxLoad))
                {
                    if (maxLoad.TryGetInt32(out var load))
                    {
                        vehicle.MaxLoadCapacity = load;
                    }
                }

                // 車両重量
                if (root.TryGetProperty("vehicle_weight", out var vehicleWeight))
                {
                    if (vehicleWeight.TryGetInt32(out var weight))
                    {
                        vehicle.VehicleWeight = weight;
                    }
                }

                // 車両総重量
                if (root.TryGetProperty("vehicle_total_weight", out var totalWeight))
                {
                    if (totalWeight.TryGetInt32(out var total))
                    {
                        vehicle.VehicleTotalWeight = total;
                    }
                }

                // 長さ
                if (root.TryGetProperty("vehicle_length", out var length))
                {
                    if (length.TryGetInt32(out var len))
                    {
                        vehicle.VehicleLength = len;
                    }
                }

                // 幅
                if (root.TryGetProperty("vehicle_width", out var width))
                {
                    if (width.TryGetInt32(out var wid))
                    {
                        vehicle.VehicleWidth = wid;
                    }
                }

                // 高さ
                if (root.TryGetProperty("vehicle_height", out var height))
                {
                    if (height.TryGetInt32(out var hei))
                    {
                        vehicle.VehicleHeight = hei;
                    }
                }

                // 前前軸重
                if (root.TryGetProperty("front_overhang", out var frontOverhang))
                {
                    if (frontOverhang.TryGetInt32(out var front))
                    {
                        vehicle.FrontOverhang = front;
                    }
                }

                // 後後軸重
                if (root.TryGetProperty("rear_overhang", out var rearOverhang))
                {
                    if (rearOverhang.TryGetInt32(out var rear))
                    {
                        vehicle.RearOverhang = rear;
                    }
                }

                // 排気量
                if (root.TryGetProperty("displacement", out var displacement))
                {
                    if (displacement.TryGetDecimal(out var disp))
                    {
                        vehicle.Displacement = disp / 1000; // ccをLに変換
                    }
                }

                // 燃料の種類
                if (root.TryGetProperty("fuel_type", out var fuelType))
                {
                    vehicle.FuelType = fuelType.GetString();
                }

                // 車検満了日
                if (root.TryGetProperty("inspection_expiry_date", out var inspectionExpiry))
                {
                    if (DateTime.TryParse(inspectionExpiry.GetString(), out var expiryDate))
                    {
                        vehicle.InspectionExpiryDate = expiryDate;
                    }
                }

                // 使用者の氏名又は名称
                if (root.TryGetProperty("user_name_or_company", out var userName))
                {
                    vehicle.UserNameOrCompany = userName.GetString();
                }

                // 使用者の住所
                if (root.TryGetProperty("user_address", out var userAddress))
                {
                    vehicle.UserAddress = userAddress.GetString();
                }

                // 使用の本拠の位置
                if (root.TryGetProperty("base_location", out var baseLocation))
                {
                    vehicle.BaseLocation = baseLocation.GetString();
                }

                // ナンバープレート情報
                if (root.TryGetProperty("license_plate_location", out var plateLocation))
                {
                    vehicle.LicensePlateLocation = plateLocation.GetString();
                }

                if (root.TryGetProperty("license_plate_classification", out var plateClass))
                {
                    vehicle.LicensePlateClassification = plateClass.GetString();
                }

                if (root.TryGetProperty("license_plate_hiragana", out var plateHiragana))
                {
                    vehicle.LicensePlateHiragana = plateHiragana.GetString();
                }

                if (root.TryGetProperty("license_plate_number", out var plateNumber))
                {
                    vehicle.LicensePlateNumber = plateNumber.GetString();
                }

                // 更新日時を設定
                vehicle.UpdatedAt = DateTime.Now;

                // 変更を保存
                _context.Entry(vehicle).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new { message = "JSONデータの取り込みに成功しました。", vehicleId = vehicle.Id });
            }
            catch (JsonException ex)
            {
                return BadRequest($"JSONの解析エラー: {ex.Message}");
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, $"データベース更新エラー: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"予期しないエラーが発生しました: {ex.Message}");
            }
        }

        private bool VehicleExists(int id)
        {
            return _context.Vehicles.Any(e => e.Id == id);
        }
    }
}