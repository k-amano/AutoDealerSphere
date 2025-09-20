using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoDealerSphere.Server.Services;
using AutoDealerSphere.Shared.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        public async Task<ActionResult<Vehicle>> ImportVehicleJson(int id, [FromBody] JsonDocument jsonDocument)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null)
            {
                return NotFound();
            }

            try
            {
                var root = jsonDocument.RootElement;

                // 車名
                if (root.TryGetProperty("車名", out var vehicleName))
                {
                    vehicle.VehicleName = vehicleName.GetString();
                }

                // 型式
                if (root.TryGetProperty("型式", out var vehicleModel))
                {
                    vehicle.VehicleModel = vehicleModel.GetString();
                }

                // 車台番号
                if (root.TryGetProperty("車台番号", out var chassisNumber))
                {
                    vehicle.ChassisNumber = chassisNumber.GetString();
                }

                // 型式指定番号
                if (root.TryGetProperty("型式指定番号", out var typeCertNumber))
                {
                    vehicle.TypeCertificationNumber = typeCertNumber.GetString();
                }

                // 類別区分番号
                if (root.TryGetProperty("類別区分番号", out var categoryNumber))
                {
                    vehicle.CategoryNumber = categoryNumber.GetString();
                }

                // 初度登録年月
                if (root.TryGetProperty("初度登録年月", out var firstRegDate))
                {
                    if (DateTime.TryParse(firstRegDate.GetString(), out var parsedDate))
                    {
                        vehicle.FirstRegistrationDate = parsedDate;
                    }
                }

                // 登録年月日
                if (root.TryGetProperty("登録年月日", out var regDate))
                {
                    if (DateTime.TryParse(regDate.GetString(), out var parsedDate))
                    {
                        vehicle.RegistrationDate = parsedDate;
                    }
                }

                // 車検証有効期限
                if (root.TryGetProperty("有効期限の満了する日", out var inspExpiry))
                {
                    if (DateTime.TryParse(inspExpiry.GetString(), out var parsedDate))
                    {
                        vehicle.InspectionExpiryDate = parsedDate;
                    }
                }

                // 車検証番号
                if (root.TryGetProperty("車検証番号", out var inspCertNum))
                {
                    vehicle.InspectionCertificateNumber = inspCertNum.GetString();
                }

                // 用途
                if (root.TryGetProperty("用途", out var purpose))
                {
                    vehicle.Purpose = purpose.GetString();
                }

                // 自家用・事業用
                if (root.TryGetProperty("自家用・事業用の別", out var personalBusiness))
                {
                    vehicle.PersonalBusinessUse = personalBusiness.GetString();
                }

                // 車体の形状
                if (root.TryGetProperty("車体の形状", out var bodyShape))
                {
                    vehicle.BodyShape = bodyShape.GetString();
                }

                // 燃料の種類
                if (root.TryGetProperty("燃料の種類", out var fuelType))
                {
                    vehicle.FuelType = fuelType.GetString();
                }

                // 総排気量又は定格出力
                if (root.TryGetProperty("総排気量又は定格出力", out var displacement))
                {
                    var dispStr = displacement.GetString();
                    if (!string.IsNullOrEmpty(dispStr))
                    {
                        // "2.00L" のような形式から数値を抽出
                        var numStr = System.Text.RegularExpressions.Regex.Match(dispStr, @"[\d.]+").Value;
                        if (decimal.TryParse(numStr, out var dispValue))
                        {
                            vehicle.Displacement = dispValue;
                        }
                    }
                }

                // 原動機の型式
                if (root.TryGetProperty("原動機の型式", out var engineModel))
                {
                    vehicle.EngineModel = engineModel.GetString();
                }

                // 型式
                if (root.TryGetProperty("型式", out var modelCode))
                {
                    vehicle.ModelCode = modelCode.GetString();
                }

                // 車両重量
                if (root.TryGetProperty("車両重量", out var vehicleWeight))
                {
                    var weightStr = vehicleWeight.GetString();
                    if (!string.IsNullOrEmpty(weightStr))
                    {
                        var numStr = System.Text.RegularExpressions.Regex.Match(weightStr, @"\d+").Value;
                        if (int.TryParse(numStr, out var weight))
                        {
                            vehicle.VehicleWeight = weight;
                        }
                    }
                }

                // 車両総重量
                if (root.TryGetProperty("車両総重量", out var vehicleTotalWeight))
                {
                    var weightStr = vehicleTotalWeight.GetString();
                    if (!string.IsNullOrEmpty(weightStr))
                    {
                        var numStr = System.Text.RegularExpressions.Regex.Match(weightStr, @"\d+").Value;
                        if (int.TryParse(numStr, out var weight))
                        {
                            vehicle.VehicleTotalWeight = weight;
                        }
                    }
                }

                // 長さ
                if (root.TryGetProperty("長さ", out var vehicleLength))
                {
                    var lengthStr = vehicleLength.GetString();
                    if (!string.IsNullOrEmpty(lengthStr))
                    {
                        var numStr = System.Text.RegularExpressions.Regex.Match(lengthStr, @"\d+").Value;
                        if (int.TryParse(numStr, out var length))
                        {
                            vehicle.VehicleLength = length;
                        }
                    }
                }

                // 幅
                if (root.TryGetProperty("幅", out var vehicleWidth))
                {
                    var widthStr = vehicleWidth.GetString();
                    if (!string.IsNullOrEmpty(widthStr))
                    {
                        var numStr = System.Text.RegularExpressions.Regex.Match(widthStr, @"\d+").Value;
                        if (int.TryParse(numStr, out var width))
                        {
                            vehicle.VehicleWidth = width;
                        }
                    }
                }

                // 高さ
                if (root.TryGetProperty("高さ", out var vehicleHeight))
                {
                    var heightStr = vehicleHeight.GetString();
                    if (!string.IsNullOrEmpty(heightStr))
                    {
                        var numStr = System.Text.RegularExpressions.Regex.Match(heightStr, @"\d+").Value;
                        if (int.TryParse(numStr, out var height))
                        {
                            vehicle.VehicleHeight = height;
                        }
                    }
                }

                // 前前軸重
                if (root.TryGetProperty("前前軸重", out var frontAxle))
                {
                    var axleStr = frontAxle.GetString();
                    if (!string.IsNullOrEmpty(axleStr))
                    {
                        var numStr = System.Text.RegularExpressions.Regex.Match(axleStr, @"\d+").Value;
                        if (int.TryParse(numStr, out var axleWeight))
                        {
                            vehicle.FrontAxleWeight = axleWeight;
                        }
                    }
                }

                // 後後軸重
                if (root.TryGetProperty("後後軸重", out var rearAxle))
                {
                    var axleStr = rearAxle.GetString();
                    if (!string.IsNullOrEmpty(axleStr))
                    {
                        var numStr = System.Text.RegularExpressions.Regex.Match(axleStr, @"\d+").Value;
                        if (int.TryParse(numStr, out var axleWeight))
                        {
                            vehicle.RearAxleWeight = axleWeight;
                        }
                    }
                }

                // 乗車定員
                if (root.TryGetProperty("乗車定員", out var passengerCapacity))
                {
                    var capacityStr = passengerCapacity.GetString();
                    if (!string.IsNullOrEmpty(capacityStr))
                    {
                        var numStr = System.Text.RegularExpressions.Regex.Match(capacityStr, @"\d+").Value;
                        if (int.TryParse(numStr, out var capacity))
                        {
                            vehicle.PassengerCapacity = capacity;
                        }
                    }
                }

                // 最大積載量
                if (root.TryGetProperty("最大積載量", out var maxLoad))
                {
                    var loadStr = maxLoad.GetString();
                    if (!string.IsNullOrEmpty(loadStr))
                    {
                        var numStr = System.Text.RegularExpressions.Regex.Match(loadStr, @"\d+").Value;
                        if (int.TryParse(numStr, out var load))
                        {
                            vehicle.MaxLoadCapacity = load;
                        }
                    }
                }

                // 車体の色
                if (root.TryGetProperty("車体の色", out var bodyColor))
                {
                    vehicle.BodyColor = bodyColor.GetString();
                }

                // 使用者の氏名又は名称
                if (root.TryGetProperty("使用者の氏名又は名称", out var userName))
                {
                    vehicle.UserNameOrCompany = userName.GetString();
                }

                // 使用者の住所
                if (root.TryGetProperty("使用者の住所", out var userAddress))
                {
                    vehicle.UserAddress = userAddress.GetString();
                }

                // 使用の本拠の位置
                if (root.TryGetProperty("使用の本拠の位置", out var baseLocation))
                {
                    vehicle.BaseLocation = baseLocation.GetString();
                }

                // 所有者の氏名又は名称
                if (root.TryGetProperty("所有者の氏名又は名称", out var ownerName))
                {
                    vehicle.OwnerNameOrCompany = ownerName.GetString();
                }

                // 所有者の住所
                if (root.TryGetProperty("所有者の住所", out var ownerAddress))
                {
                    vehicle.OwnerAddress = ownerAddress.GetString();
                }

                // メタデータを更新
                vehicle.ImportSource = "JSON";
                vehicle.ImportDate = DateTime.Now;
                vehicle.OriginalData = jsonDocument.RootElement.GetRawText();
                vehicle.UpdatedAt = DateTime.Now;

                // データベースを更新
                _context.Entry(vehicle).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(vehicle);
            }
            catch (Exception ex)
            {
                return BadRequest($"JSONの解析に失敗しました: {ex.Message}");
            }
        }

        private bool VehicleExists(int id)
        {
            return _context.Vehicles.Any(e => e.Id == id);
        }
    }
}