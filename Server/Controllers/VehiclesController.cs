using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoDealerSphere.Server.Services;
using AutoDealerSphere.Shared.Models;
using System.Text.Json;
using System.Globalization;
using AutoDealerSphere.Server.Utils;

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

        private bool VehicleExists(int id)
        {
            return _context.Vehicles.Any(e => e.Id == id);
        }

        // POST: api/vehicles/{id}/import-json
        [HttpPost("{id}/import-json")]
        public async Task<IActionResult> ImportJson(int id, [FromBody] JsonDocument jsonDocument)
        {
            try
            {
                Console.WriteLine($"[ImportJson] 開始 - Vehicle ID: {id}");

                // 車両を取得
                var vehicle = await _context.Vehicles.FindAsync(id);
                if (vehicle == null)
                {
                    Console.WriteLine($"[ImportJson] エラー: 車両が見つかりません ID: {id}");
                    return NotFound($"Vehicle with ID {id} not found.");
                }

                Console.WriteLine($"[ImportJson] 車両を取得しました: {vehicle.VehicleName ?? "名前なし"}");

                // 取り込み前のデータを記録
                Console.WriteLine("[ImportJson] 取り込み前のデータ:");
                Console.WriteLine($"  - 車検証番号: {vehicle.InspectionCertificateNumber ?? "null"}");
                Console.WriteLine($"  - 車名: {vehicle.VehicleName ?? "null"}");
                Console.WriteLine($"  - 型式: {vehicle.VehicleModel ?? "null"}");
                Console.WriteLine($"  - 車台番号: {vehicle.ChassisNumber ?? "null"}");

                var root = jsonDocument.RootElement;
                var updatedFields = new List<string>();

                // 元のJSONを保存
                vehicle.OriginalData = jsonDocument.RootElement.GetRawText();
                vehicle.ImportSource = "JSON";
                vehicle.ImportDate = DateTime.Now;

                // CertInfoオブジェクトを取得
                JsonElement certInfo;
                if (root.TryGetProperty("CertInfo", out certInfo))
                {
                    Console.WriteLine($"[ImportJson] CertInfoオブジェクトを検出しました");
                }
                else
                {
                    // CertInfoがない場合はルートを使用（後方互換性）
                    certInfo = root;
                    Console.WriteLine($"[ImportJson] CertInfoが見つからないため、ルートオブジェクトを使用します");
                }

                // 車検証番号 (ElectCertMgNo)
                if (certInfo.TryGetProperty("ElectCertMgNo", out var electCertMgNo))
                {
                    var value = CharacterConverter.NormalizeVehicleData(electCertMgNo.GetString());
                    vehicle.InspectionCertificateNumber = value;
                    Console.WriteLine($"[ImportJson] 車検証番号をセット: {value}");
                    updatedFields.Add($"InspectionCertificateNumber = {value}");
                }
                // 後方互換性
                else if (certInfo.TryGetProperty("inspection_certificate_number", out var certNumber))
                {
                    var value = CharacterConverter.NormalizeVehicleData(certNumber.GetString());
                    vehicle.InspectionCertificateNumber = value;
                    Console.WriteLine($"[ImportJson] 車検証番号をセット(後方互換): {value}");
                    updatedFields.Add($"InspectionCertificateNumber = {value}");
                }

                // 車名 (CarName)
                if (certInfo.TryGetProperty("CarName", out var carName))
                {
                    var value = CharacterConverter.NormalizeVehicleData(carName.GetString());
                    vehicle.VehicleName = value;
                    Console.WriteLine($"[ImportJson] 車名をセット: {value}");
                    updatedFields.Add($"VehicleName = {value}");
                }
                else if (certInfo.TryGetProperty("vehicle_name", out var vehicleName))
                {
                    var value = CharacterConverter.NormalizeVehicleData(vehicleName.GetString());
                    vehicle.VehicleName = value;
                    Console.WriteLine($"[ImportJson] 車名をセット(後方互換): {value}");
                    updatedFields.Add($"VehicleName = {value}");
                }

                // 型式 (Model)
                if (certInfo.TryGetProperty("Model", out var model))
                {
                    var value = CharacterConverter.NormalizeVehicleData(model.GetString());
                    vehicle.VehicleModel = value;
                    Console.WriteLine($"[ImportJson] 型式をセット: {value}");
                    updatedFields.Add($"VehicleModel = {value}");
                }
                else if (certInfo.TryGetProperty("vehicle_model", out var vehicleModel))
                {
                    var value = CharacterConverter.NormalizeVehicleData(vehicleModel.GetString());
                    vehicle.VehicleModel = value;
                    Console.WriteLine($"[ImportJson] 型式をセット(後方互換): {value}");
                    updatedFields.Add($"VehicleModel = {value}");
                }

                // 車台番号 (CarNo)
                if (certInfo.TryGetProperty("CarNo", out var carNo))
                {
                    var value = CharacterConverter.NormalizeVehicleData(carNo.GetString());
                    vehicle.ChassisNumber = value;
                    Console.WriteLine($"[ImportJson] 車台番号をセット: {value}");
                    updatedFields.Add($"ChassisNumber = {value}");
                }
                else if (certInfo.TryGetProperty("chassis_number", out var chassisNumber))
                {
                    var value = CharacterConverter.NormalizeVehicleData(chassisNumber.GetString());
                    vehicle.ChassisNumber = value;
                    Console.WriteLine($"[ImportJson] 車台番号をセット(後方互換): {value}");
                    updatedFields.Add($"ChassisNumber = {value}");
                }

                // エンジン型式 (EngineModel)
                if (certInfo.TryGetProperty("EngineModel", out var engModel))
                {
                    var value = CharacterConverter.NormalizeVehicleData(engModel.GetString());
                    vehicle.EngineModel = value;
                    Console.WriteLine($"[ImportJson] エンジン型式をセット: {value}");
                    updatedFields.Add($"EngineModel = {value}");
                }
                else if (certInfo.TryGetProperty("engine_model", out var engineModel))
                {
                    var value = CharacterConverter.NormalizeVehicleData(engineModel.GetString());
                    vehicle.EngineModel = value;
                    Console.WriteLine($"[ImportJson] エンジン型式をセット(後方互換): {value}");
                    updatedFields.Add($"EngineModel = {value}");
                }

                // 型式指定番号 (ModelSpecifyNo)
                if (certInfo.TryGetProperty("ModelSpecifyNo", out var modelSpecifyNo))
                {
                    var value = CharacterConverter.NormalizeVehicleData(modelSpecifyNo.GetString());
                    vehicle.TypeCertificationNumber = value;
                    Console.WriteLine($"[ImportJson] 型式指定番号をセット: {value}");
                    updatedFields.Add($"TypeCertificationNumber = {value}");
                }
                else if (certInfo.TryGetProperty("type_certification_number", out var typeCertNumber))
                {
                    var value = CharacterConverter.NormalizeVehicleData(typeCertNumber.GetString());
                    vehicle.TypeCertificationNumber = value;
                    Console.WriteLine($"[ImportJson] 型式指定番号をセット(後方互換): {value}");
                    updatedFields.Add($"TypeCertificationNumber = {value}");
                }

                // 類別区分番号 (ClassifyAroundNo)
                if (certInfo.TryGetProperty("ClassifyAroundNo", out var classifyAroundNo))
                {
                    var value = CharacterConverter.NormalizeVehicleData(classifyAroundNo.GetString());
                    vehicle.CategoryNumber = value;
                    Console.WriteLine($"[ImportJson] 類別区分番号をセット: {value}");
                    updatedFields.Add($"CategoryNumber = {value}");
                }
                else if (certInfo.TryGetProperty("category_number", out var categoryNum))
                {
                    var value = CharacterConverter.NormalizeVehicleData(categoryNum.GetString());
                    vehicle.CategoryNumber = value;
                    Console.WriteLine($"[ImportJson] 類別区分番号をセット(後方互換): {value}");
                    updatedFields.Add($"CategoryNumber = {value}");
                }

                // ナンバープレート情報 (EntryNoCarNoから解析)
                if (certInfo.TryGetProperty("EntryNoCarNo", out var entryNoCarNo))
                {
                    var plateInfo = CharacterConverter.NormalizeVehicleData(entryNoCarNo.GetString());
                    if (!string.IsNullOrWhiteSpace(plateInfo))
                    {
                        // "香川 500 め 3924" のような形式を解析（正規化後は半角スペースと半角数字）
                        var parts = plateInfo.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 4)
                        {
                            vehicle.LicensePlateLocation = parts[0];  // 香川
                            vehicle.LicensePlateClassification = parts[1];  // 500
                            vehicle.LicensePlateHiragana = parts[2];  // め
                            vehicle.LicensePlateNumber = parts[3];  // 3924
                            Console.WriteLine($"[ImportJson] ナンバープレート情報をセット: {plateInfo}");
                            updatedFields.Add($"LicensePlate = {plateInfo}");
                        }
                    }
                }
                // 後方互換性（個別フィールド）
                else
                {
                    if (certInfo.TryGetProperty("license_plate_location", out var plateLocation))
                    {
                        var value = CharacterConverter.NormalizeVehicleData(plateLocation.GetString());
                        vehicle.LicensePlateLocation = value;
                        Console.WriteLine($"[ImportJson] ナンバープレート地域をセット: {value}");
                        updatedFields.Add($"LicensePlateLocation = {value}");
                    }

                    if (certInfo.TryGetProperty("license_plate_classification", out var plateClass))
                    {
                        var value = CharacterConverter.NormalizeVehicleData(plateClass.GetString());
                        vehicle.LicensePlateClassification = value;
                        Console.WriteLine($"[ImportJson] 分類番号をセット: {value}");
                        updatedFields.Add($"LicensePlateClassification = {value}");
                    }

                    if (certInfo.TryGetProperty("license_plate_hiragana", out var plateHiragana))
                    {
                        var value = CharacterConverter.NormalizeVehicleData(plateHiragana.GetString());
                        vehicle.LicensePlateHiragana = value;
                        Console.WriteLine($"[ImportJson] ひらがなをセット: {value}");
                        updatedFields.Add($"LicensePlateHiragana = {value}");
                    }

                    if (certInfo.TryGetProperty("license_plate_number", out var plateNumber))
                    {
                        var value = CharacterConverter.NormalizeVehicleData(plateNumber.GetString());
                        vehicle.LicensePlateNumber = value;
                        Console.WriteLine($"[ImportJson] ナンバーをセット: {value}");
                        updatedFields.Add($"LicensePlateNumber = {value}");
                    }
                }

                // 初度登録年月 (FirstregistdateE/Y/M)
                if (certInfo.TryGetProperty("FirstregistdateE", out var era) &&
                    certInfo.TryGetProperty("FirstregistdateY", out var year) &&
                    certInfo.TryGetProperty("FirstregistdateM", out var month))
                {
                    var eraStr = era.GetString();
                    var yearStr = year.GetString()?.Trim();
                    var monthStr = month.GetString()?.Trim();

                    // 平成/令和から西暦へ変換
                    if (!string.IsNullOrEmpty(yearStr) && !string.IsNullOrEmpty(monthStr))
                    {
                        if (int.TryParse(yearStr, out var yearNum) && int.TryParse(monthStr, out var monthNum))
                        {
                            int actualYear = 0;
                            if (eraStr == "令和")
                                actualYear = 2018 + yearNum;
                            else if (eraStr == "平成")
                                actualYear = 1988 + yearNum;

                            if (actualYear > 0)
                            {
                                var date = new DateTime(actualYear, monthNum, 1);
                                vehicle.FirstRegistrationDate = date;
                                Console.WriteLine($"[ImportJson] 初度登録年月をセット: {date:yyyy-MM}");
                                updatedFields.Add($"FirstRegistrationDate = {date:yyyy-MM}");
                            }
                        }
                    }
                }
                // 後方互換性
                else if (certInfo.TryGetProperty("first_registration_date", out var firstRegDate))
                {
                    var dateStr = firstRegDate.GetString();
                    if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParseExact(dateStr,
                        new[] { "yyyy-MM-dd", "yyyy/MM/dd", "yyyyMMdd" },
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    {
                        vehicle.FirstRegistrationDate = date;
                        Console.WriteLine($"[ImportJson] 初度登録年月をセット(後方互換): {date:yyyy-MM-dd}");
                        updatedFields.Add($"FirstRegistrationDate = {date:yyyy-MM-dd}");
                    }
                }

                // 用途 (Use)
                if (certInfo.TryGetProperty("Use", out var use))
                {
                    var value = CharacterConverter.NormalizeVehicleData(use.GetString());
                    vehicle.Purpose = value;
                    Console.WriteLine($"[ImportJson] 用途をセット: {value}");
                    updatedFields.Add($"Purpose = {value}");
                }
                else if (certInfo.TryGetProperty("purpose", out var purpose))
                {
                    var value = CharacterConverter.NormalizeVehicleData(purpose.GetString());
                    vehicle.Purpose = value;
                    Console.WriteLine($"[ImportJson] 用途をセット(後方互換): {value}");
                    updatedFields.Add($"Purpose = {value}");
                }

                // 自家用・事業用 (PrivateBusiness)
                if (certInfo.TryGetProperty("PrivateBusiness", out var privateBusiness))
                {
                    var value = CharacterConverter.NormalizeVehicleData(privateBusiness.GetString());
                    vehicle.PersonalBusinessUse = value;
                    Console.WriteLine($"[ImportJson] 自家用・事業用をセット: {value}");
                    updatedFields.Add($"PersonalBusinessUse = {value}");
                }
                else if (certInfo.TryGetProperty("personal_business_use", out var pbUse))
                {
                    var value = CharacterConverter.NormalizeVehicleData(pbUse.GetString());
                    vehicle.PersonalBusinessUse = value;
                    Console.WriteLine($"[ImportJson] 自家用・事業用をセット(後方互換): {value}");
                    updatedFields.Add($"PersonalBusinessUse = {value}");
                }

                // 車体の形状 (CarShape)
                if (certInfo.TryGetProperty("CarShape", out var carShape))
                {
                    var value = CharacterConverter.NormalizeVehicleData(carShape.GetString());
                    vehicle.BodyShape = value;
                    Console.WriteLine($"[ImportJson] 車体の形状をセット: {value}");
                    updatedFields.Add($"BodyShape = {value}");
                }
                else if (certInfo.TryGetProperty("body_shape", out var bodyShape))
                {
                    var value = CharacterConverter.NormalizeVehicleData(bodyShape.GetString());
                    vehicle.BodyShape = value;
                    Console.WriteLine($"[ImportJson] 車体の形状をセット(後方互換): {value}");
                    updatedFields.Add($"BodyShape = {value}");
                }

                // 乗車定員 (Cap)
                if (certInfo.TryGetProperty("Cap", out var cap))
                {
                    var capStr = CharacterConverter.NormalizeVehicleData(cap.GetString());
                    if (int.TryParse(capStr, out var capValue))
                    {
                        vehicle.SeatingCapacity = capValue;
                        vehicle.PassengerCapacity = capValue;
                        Console.WriteLine($"[ImportJson] 乗車定員をセット: {capValue}");
                        updatedFields.Add($"SeatingCapacity = {capValue}");
                    }
                }
                else if (certInfo.TryGetProperty("seating_capacity", out var seatingCap) && seatingCap.TryGetInt32(out var seatValue))
                {
                    vehicle.SeatingCapacity = seatValue;
                    vehicle.PassengerCapacity = seatValue;
                    Console.WriteLine($"[ImportJson] 乗車定員をセット(後方互換): {seatValue}");
                    updatedFields.Add($"SeatingCapacity = {seatValue}");
                }

                // 最大積載量 (Maxloadage)
                if (certInfo.TryGetProperty("Maxloadage", out var maxloadage))
                {
                    var loadStr = CharacterConverter.NormalizeVehicleData(maxloadage.GetString());
                    if (loadStr != "-" && int.TryParse(loadStr, out var loadValue))
                    {
                        vehicle.MaxLoadCapacity = loadValue;
                        Console.WriteLine($"[ImportJson] 最大積載量をセット: {loadValue}");
                        updatedFields.Add($"MaxLoadCapacity = {loadValue}");
                    }
                }
                else if (certInfo.TryGetProperty("max_load_capacity", out var maxLoad) && maxLoad.TryGetInt32(out var loadValue2))
                {
                    vehicle.MaxLoadCapacity = loadValue2;
                    Console.WriteLine($"[ImportJson] 最大積載量をセット(後方互換): {loadValue2}");
                    updatedFields.Add($"MaxLoadCapacity = {loadValue2}");
                }

                // 車両重量 (CarWgt)
                if (certInfo.TryGetProperty("CarWgt", out var carWgt))
                {
                    var wgtStr = CharacterConverter.NormalizeVehicleData(carWgt.GetString());
                    if (int.TryParse(wgtStr, out var wgtValue))
                    {
                        vehicle.VehicleWeight = wgtValue;
                        Console.WriteLine($"[ImportJson] 車両重量をセット: {wgtValue}");
                        updatedFields.Add($"VehicleWeight = {wgtValue}");
                    }
                }
                else if (certInfo.TryGetProperty("vehicle_weight", out var vWeight) && vWeight.TryGetInt32(out var weightValue))
                {
                    vehicle.VehicleWeight = weightValue;
                    Console.WriteLine($"[ImportJson] 車両重量をセット(後方互換): {weightValue}");
                    updatedFields.Add($"VehicleWeight = {weightValue}");
                }

                // 車両総重量 (CarTotalWgt)
                if (certInfo.TryGetProperty("CarTotalWgt", out var carTotalWgt))
                {
                    var totalWgtStr = CharacterConverter.NormalizeVehicleData(carTotalWgt.GetString());
                    if (int.TryParse(totalWgtStr, out var totalWgtValue))
                    {
                        vehicle.VehicleTotalWeight = totalWgtValue;
                        Console.WriteLine($"[ImportJson] 車両総重量をセット: {totalWgtValue}");
                        updatedFields.Add($"VehicleTotalWeight = {totalWgtValue}");
                    }
                }
                else if (certInfo.TryGetProperty("vehicle_total_weight", out var vTotalWeight) && vTotalWeight.TryGetInt32(out var totalWeightValue))
                {
                    vehicle.VehicleTotalWeight = totalWeightValue;
                    Console.WriteLine($"[ImportJson] 車両総重量をセット(後方互換): {totalWeightValue}");
                    updatedFields.Add($"VehicleTotalWeight = {totalWeightValue}");
                }

                // 長さ (Length)
                if (certInfo.TryGetProperty("Length", out var length))
                {
                    var lengthStr = CharacterConverter.NormalizeVehicleData(length.GetString());
                    if (int.TryParse(lengthStr, out var lengthValue))
                    {
                        vehicle.VehicleLength = lengthValue;
                        Console.WriteLine($"[ImportJson] 長さをセット: {lengthValue}");
                        updatedFields.Add($"VehicleLength = {lengthValue}");
                    }
                }
                else if (certInfo.TryGetProperty("vehicle_length", out var vLength) && vLength.TryGetInt32(out var lengthValue2))
                {
                    vehicle.VehicleLength = lengthValue2;
                    Console.WriteLine($"[ImportJson] 長さをセット(後方互換): {lengthValue2}");
                    updatedFields.Add($"VehicleLength = {lengthValue2}");
                }

                // 幅 (Width)
                if (certInfo.TryGetProperty("Width", out var width))
                {
                    var widthStr = CharacterConverter.NormalizeVehicleData(width.GetString());
                    if (int.TryParse(widthStr, out var widthValue))
                    {
                        vehicle.VehicleWidth = widthValue;
                        Console.WriteLine($"[ImportJson] 幅をセット: {widthValue}");
                        updatedFields.Add($"VehicleWidth = {widthValue}");
                    }
                }
                else if (certInfo.TryGetProperty("vehicle_width", out var vWidth) && vWidth.TryGetInt32(out var widthValue2))
                {
                    vehicle.VehicleWidth = widthValue2;
                    Console.WriteLine($"[ImportJson] 幅をセット(後方互換): {widthValue2}");
                    updatedFields.Add($"VehicleWidth = {widthValue2}");
                }

                // 高さ (Height)
                if (certInfo.TryGetProperty("Height", out var height))
                {
                    var heightStr = CharacterConverter.NormalizeVehicleData(height.GetString());
                    if (int.TryParse(heightStr, out var heightValue))
                    {
                        vehicle.VehicleHeight = heightValue;
                        Console.WriteLine($"[ImportJson] 高さをセット: {heightValue}");
                        updatedFields.Add($"VehicleHeight = {heightValue}");
                    }
                }
                else if (certInfo.TryGetProperty("vehicle_height", out var vHeight) && vHeight.TryGetInt32(out var heightValue2))
                {
                    vehicle.VehicleHeight = heightValue2;
                    Console.WriteLine($"[ImportJson] 高さをセット(後方互換): {heightValue2}");
                    updatedFields.Add($"VehicleHeight = {heightValue2}");
                }

                // 前前軸重 (FfAxWgt)
                if (certInfo.TryGetProperty("FfAxWgt", out var ffAxWgt))
                {
                    var wgtStr = CharacterConverter.NormalizeVehicleData(ffAxWgt.GetString());
                    if (int.TryParse(wgtStr, out var frontValue))
                    {
                        vehicle.FrontOverhang = frontValue;
                        vehicle.FrontAxleWeight = frontValue;
                        Console.WriteLine($"[ImportJson] 前前軸重をセット: {frontValue}");
                        updatedFields.Add($"FrontOverhang = {frontValue}");
                    }
                }
                else if (certInfo.TryGetProperty("front_overhang", out var frontOh) && frontOh.TryGetInt32(out var frontValue2))
                {
                    vehicle.FrontOverhang = frontValue2;
                    vehicle.FrontAxleWeight = frontValue2;
                    Console.WriteLine($"[ImportJson] 前前軸重をセット(後方互換): {frontValue2}");
                    updatedFields.Add($"FrontOverhang = {frontValue2}");
                }

                // 後後軸重 (RrAxWgt)
                if (certInfo.TryGetProperty("RrAxWgt", out var rrAxWgt))
                {
                    var wgtStr = CharacterConverter.NormalizeVehicleData(rrAxWgt.GetString());
                    if (wgtStr != "-" && int.TryParse(wgtStr, out var rearValue))
                    {
                        vehicle.RearOverhang = rearValue;
                        vehicle.RearAxleWeight = rearValue;
                        Console.WriteLine($"[ImportJson] 後後軸重をセット: {rearValue}");
                        updatedFields.Add($"RearOverhang = {rearValue}");
                    }
                }
                else if (certInfo.TryGetProperty("rear_overhang", out var rearOh) && rearOh.TryGetInt32(out var rearValue2))
                {
                    vehicle.RearOverhang = rearValue2;
                    vehicle.RearAxleWeight = rearValue2;
                    Console.WriteLine($"[ImportJson] 後後軸重をセット(後方互換): {rearValue2}");
                    updatedFields.Add($"RearOverhang = {rearValue2}");
                }

                // 排気量 (Displacement) - 既にL単位
                if (certInfo.TryGetProperty("Displacement", out var disp))
                {
                    var dispStr = CharacterConverter.NormalizeVehicleData(disp.GetString());
                    if (decimal.TryParse(dispStr, out var dispValue))
                    {
                        vehicle.Displacement = dispValue;
                        Console.WriteLine($"[ImportJson] 排気量をセット: {dispValue}L");
                        updatedFields.Add($"Displacement = {dispValue}L");
                    }
                }
                else if (certInfo.TryGetProperty("displacement", out var displacement))
                {
                    if (displacement.TryGetInt32(out var ccValue))
                    {
                        var literValue = ccValue / 1000m;
                        vehicle.Displacement = literValue;
                        Console.WriteLine($"[ImportJson] 排気量をセット(後方互換): {ccValue}cc → {literValue}L");
                        updatedFields.Add($"Displacement = {literValue}L");
                    }
                    else if (displacement.TryGetDecimal(out var decValue))
                    {
                        vehicle.Displacement = decValue;
                        Console.WriteLine($"[ImportJson] 排気量をセット: {decValue}L");
                        updatedFields.Add($"Displacement = {decValue}L");
                    }
                }

                // 定格出力
                if (root.TryGetProperty("rated_output", out var ratedOutput) && ratedOutput.TryGetDecimal(out var outputValue))
                {
                    vehicle.RatedOutput = outputValue;
                    Console.WriteLine($"[ImportJson] 定格出力をセット: {outputValue}kW");
                    updatedFields.Add($"RatedOutput = {outputValue}kW");
                }

                // 燃料の種類 (FuelClass)
                if (certInfo.TryGetProperty("FuelClass", out var fuelClass))
                {
                    var value = CharacterConverter.NormalizeVehicleData(fuelClass.GetString());
                    vehicle.FuelType = value;
                    Console.WriteLine($"[ImportJson] 燃料の種類をセット: {value}");
                    updatedFields.Add($"FuelType = {value}");
                }
                else if (certInfo.TryGetProperty("fuel_type", out var fuelType))
                {
                    var value = CharacterConverter.NormalizeVehicleData(fuelType.GetString());
                    vehicle.FuelType = value;
                    Console.WriteLine($"[ImportJson] 燃料の種類をセット(後方互換): {value}");
                    updatedFields.Add($"FuelType = {value}");
                }

                // 車検満了日 (ValidPeriodExpirdateE/Y/M/D)
                if (certInfo.TryGetProperty("ValidPeriodExpirdateE", out var validEra) &&
                    certInfo.TryGetProperty("ValidPeriodExpirdateY", out var validYear) &&
                    certInfo.TryGetProperty("ValidPeriodExpirdateM", out var validMonth) &&
                    certInfo.TryGetProperty("ValidPeriodExpirdateD", out var validDay))
                {
                    var eraStr = validEra.GetString();
                    var yearStr = validYear.GetString()?.Trim();
                    var monthStr = validMonth.GetString()?.Trim();
                    var dayStr = validDay.GetString()?.Trim();

                    if (!string.IsNullOrEmpty(yearStr) && !string.IsNullOrEmpty(monthStr) && !string.IsNullOrEmpty(dayStr))
                    {
                        if (int.TryParse(yearStr, out var yearNum) &&
                            int.TryParse(monthStr, out var monthNum) &&
                            int.TryParse(dayStr, out var dayNum))
                        {
                            int actualYear = 0;
                            if (eraStr == "令和")
                                actualYear = 2018 + yearNum;
                            else if (eraStr == "平成")
                                actualYear = 1988 + yearNum;

                            if (actualYear > 0)
                            {
                                var date = new DateTime(actualYear, monthNum, dayNum);
                                vehicle.InspectionExpiryDate = date;
                                Console.WriteLine($"[ImportJson] 車検満了日をセット: {date:yyyy-MM-dd}");
                                updatedFields.Add($"InspectionExpiryDate = {date:yyyy-MM-dd}");
                            }
                        }
                    }
                }
                // 後方互換性
                else if (certInfo.TryGetProperty("inspection_expiry_date", out var expiry))
                {
                    var dateStr = expiry.GetString();
                    if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParseExact(dateStr,
                        new[] { "yyyy-MM-dd", "yyyy/MM/dd", "yyyyMMdd" },
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    {
                        vehicle.InspectionExpiryDate = date;
                        Console.WriteLine($"[ImportJson] 車検満了日をセット(後方互換): {date:yyyy-MM-dd}");
                        updatedFields.Add($"InspectionExpiryDate = {date:yyyy-MM-dd}");
                    }
                }

                // 使用者の氏名又は名称 (UsernameLowLevelChar/UsernameHighLevelChar)
                if (certInfo.TryGetProperty("UsernameLowLevelChar", out var userLowLevel))
                {
                    var value = CharacterConverter.NormalizeVehicleData(userLowLevel.GetString());
                    // "***"の場合はスキップ（正規化後は半角）
                    if (value != null && value != "***")
                    {
                        vehicle.UserNameOrCompany = value;
                        Console.WriteLine($"[ImportJson] 使用者名をセット: {value}");
                        updatedFields.Add($"UserNameOrCompany = {value}");
                    }
                }
                else if (certInfo.TryGetProperty("user_name_or_company", out var userName))
                {
                    var value = CharacterConverter.NormalizeVehicleData(userName.GetString());
                    vehicle.UserNameOrCompany = value;
                    Console.WriteLine($"[ImportJson] 使用者名をセット(後方互換): {value}");
                    updatedFields.Add($"UserNameOrCompany = {value}");
                }

                // 使用者の住所 (UserAddressChar + UserAddressNumValue)
                if (certInfo.TryGetProperty("UserAddressChar", out var userAddrChar))
                {
                    var addrChar = CharacterConverter.NormalizeVehicleData(userAddrChar.GetString());
                    var addrNum = "";
                    if (certInfo.TryGetProperty("UserAddressNumValue", out var userAddrNum))
                    {
                        addrNum = CharacterConverter.NormalizeVehicleData(userAddrNum.GetString());
                    }

                    if (addrChar != null && addrChar != "***")
                    {
                        var fullAddr = addrChar + addrNum;
                        vehicle.UserAddress = fullAddr;
                        Console.WriteLine($"[ImportJson] 使用者住所をセット: {fullAddr}");
                        updatedFields.Add($"UserAddress = {fullAddr}");
                    }
                }
                else if (certInfo.TryGetProperty("user_address", out var userAddr))
                {
                    var value = CharacterConverter.NormalizeVehicleData(userAddr.GetString());
                    vehicle.UserAddress = value;
                    Console.WriteLine($"[ImportJson] 使用者住所をセット(後方互換): {value}");
                    updatedFields.Add($"UserAddress = {value}");
                }

                // 所有者の氏名又は名称 (OwnernameLowLevelChar/OwnernameHighLevelChar)
                if (certInfo.TryGetProperty("OwnernameLowLevelChar", out var ownerLowLevel))
                {
                    var value = CharacterConverter.NormalizeVehicleData(ownerLowLevel.GetString());
                    vehicle.OwnerNameOrCompany = value;
                    Console.WriteLine($"[ImportJson] 所有者名をセット: {value}");
                    updatedFields.Add($"OwnerNameOrCompany = {value}");
                }
                else if (certInfo.TryGetProperty("owner_name_or_company", out var ownerName))
                {
                    var value = CharacterConverter.NormalizeVehicleData(ownerName.GetString());
                    vehicle.OwnerNameOrCompany = value;
                    Console.WriteLine($"[ImportJson] 所有者名をセット(後方互換): {value}");
                    updatedFields.Add($"OwnerNameOrCompany = {value}");
                }

                // 所有者の住所 (OwnerAddressChar + OwnerAddressNumValue)
                if (certInfo.TryGetProperty("OwnerAddressChar", out var ownerAddrChar))
                {
                    var addrChar = CharacterConverter.NormalizeVehicleData(ownerAddrChar.GetString());
                    var addrNum = "";
                    if (certInfo.TryGetProperty("OwnerAddressNumValue", out var ownerAddrNum))
                    {
                        addrNum = CharacterConverter.NormalizeVehicleData(ownerAddrNum.GetString());
                    }
                    var fullAddr = addrChar + addrNum;
                    vehicle.OwnerAddress = fullAddr;
                    Console.WriteLine($"[ImportJson] 所有者住所をセット: {fullAddr}");
                    updatedFields.Add($"OwnerAddress = {fullAddr}");
                }
                else if (certInfo.TryGetProperty("owner_address", out var ownerAddr))
                {
                    var value = CharacterConverter.NormalizeVehicleData(ownerAddr.GetString());
                    vehicle.OwnerAddress = value;
                    Console.WriteLine($"[ImportJson] 所有者住所をセット(後方互換): {value}");
                    updatedFields.Add($"OwnerAddress = {value}");
                }

                // 使用の本拠の位置 (UseheadqrterChar + UseheadqrterNumValue)
                if (certInfo.TryGetProperty("UseheadqrterChar", out var headqrterChar))
                {
                    var hqChar = CharacterConverter.NormalizeVehicleData(headqrterChar.GetString());
                    var hqNum = "";
                    if (certInfo.TryGetProperty("UseheadqrterNumValue", out var headqrterNum))
                    {
                        hqNum = CharacterConverter.NormalizeVehicleData(headqrterNum.GetString());
                    }

                    if (hqChar != null && hqChar != "***")
                    {
                        var fullHq = hqChar + hqNum;
                        vehicle.BaseLocation = fullHq;
                        Console.WriteLine($"[ImportJson] 使用の本拠の位置をセット: {fullHq}");
                        updatedFields.Add($"BaseLocation = {fullHq}");
                    }
                }
                else if (certInfo.TryGetProperty("base_location", out var baseLoc))
                {
                    var value = CharacterConverter.NormalizeVehicleData(baseLoc.GetString());
                    vehicle.BaseLocation = value;
                    Console.WriteLine($"[ImportJson] 使用の本拠の位置をセット(後方互換): {value}");
                    updatedFields.Add($"BaseLocation = {value}");
                }

                // 走行距離
                if (root.TryGetProperty("mileage", out var mileage) && mileage.TryGetDecimal(out var mileageValue))
                {
                    vehicle.Mileage = mileageValue;
                    Console.WriteLine($"[ImportJson] 走行距離をセット: {mileageValue}");
                    updatedFields.Add($"Mileage = {mileageValue}");
                }

                // 走行距離更新日
                if (root.TryGetProperty("mileage_update_date", out var mileageUpdate))
                {
                    var dateStr = mileageUpdate.GetString();
                    if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParseExact(dateStr,
                        new[] { "yyyy-MM-dd", "yyyy/MM/dd", "yyyyMMdd" },
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    {
                        vehicle.MileageUpdateDate = date;
                        Console.WriteLine($"[ImportJson] 走行距離更新日をセット: {date:yyyy-MM-dd}");
                        updatedFields.Add($"MileageUpdateDate = {date:yyyy-MM-dd}");
                    }
                }

                // QRコード情報
                if (root.TryGetProperty("qr_code_data", out var qrCode))
                {
                    var value = qrCode.GetString();
                    vehicle.QRCodeData = value;
                    Console.WriteLine($"[ImportJson] QRコードデータをセット");
                    updatedFields.Add("QRCodeData = [データあり]");
                }

                // ICタグID
                if (root.TryGetProperty("ic_tag_id", out var icTag))
                {
                    var value = icTag.GetString();
                    vehicle.ICTagId = value;
                    Console.WriteLine($"[ImportJson] ICタグIDをセット: {value}");
                    updatedFields.Add($"ICTagId = {value}");
                }

                // 電子車検証フラグ
                if (root.TryGetProperty("electronic_certificate_flag", out var elecFlag))
                {
                    try
                    {
                        var flagValue = elecFlag.GetBoolean();
                        vehicle.ElectronicCertificateFlag = flagValue;
                        Console.WriteLine($"[ImportJson] 電子車検証フラグをセット: {flagValue}");
                        updatedFields.Add($"ElectronicCertificateFlag = {flagValue}");
                    }
                    catch (InvalidOperationException)
                    {
                        // Boolean値として解析できない場合は無視
                    }
                }

                // 発行年月日
                if (root.TryGetProperty("issue_date", out var issueDate))
                {
                    var dateStr = issueDate.GetString();
                    if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParseExact(dateStr,
                        new[] { "yyyy-MM-dd", "yyyy/MM/dd", "yyyyMMdd" },
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    {
                        vehicle.IssueDate = date;
                        Console.WriteLine($"[ImportJson] 発行年月日をセット: {date:yyyy-MM-dd}");
                        updatedFields.Add($"IssueDate = {date:yyyy-MM-dd}");
                    }
                }

                // 発行事務所
                if (root.TryGetProperty("issue_office", out var issueOffice))
                {
                    var value = issueOffice.GetString();
                    vehicle.IssueOffice = value;
                    Console.WriteLine($"[ImportJson] 発行事務所をセット: {value}");
                    updatedFields.Add($"IssueOffice = {value}");
                }

                // 車検証バージョン
                if (root.TryGetProperty("certificate_version", out var certVersion))
                {
                    var value = certVersion.GetString();
                    vehicle.CertificateVersion = value;
                    Console.WriteLine($"[ImportJson] 車検証バージョンをセット: {value}");
                    updatedFields.Add($"CertificateVersion = {value}");
                }

                // 更新日時を設定
                vehicle.UpdatedAt = DateTime.Now;

                // データベースを更新
                Console.WriteLine($"[ImportJson] データベース更新開始 - {updatedFields.Count}個のフィールドを更新");
                _context.Entry(vehicle).State = EntityState.Modified;

                var saveCount = await _context.SaveChangesAsync();
                Console.WriteLine($"[ImportJson] SaveChangesAsync完了 - {saveCount}件の変更を保存");

                // 保存後の検証 - データベースから再読み込み
                await _context.Entry(vehicle).ReloadAsync();
                Console.WriteLine("[ImportJson] データベースから再読み込み完了");

                // 保存後のデータを確認
                Console.WriteLine("[ImportJson] 保存後のデータ:");
                Console.WriteLine($"  - 車検証番号: {vehicle.InspectionCertificateNumber ?? "null"}");
                Console.WriteLine($"  - 車名: {vehicle.VehicleName ?? "null"}");
                Console.WriteLine($"  - 型式: {vehicle.VehicleModel ?? "null"}");
                Console.WriteLine($"  - 車台番号: {vehicle.ChassisNumber ?? "null"}");
                Console.WriteLine($"  - UpdatedAt: {vehicle.UpdatedAt:yyyy-MM-dd HH:mm:ss}");

                // 再度データベースからクエリして確認
                var verifiedVehicle = await _context.Vehicles.FindAsync(id);
                if (verifiedVehicle != null)
                {
                    Console.WriteLine("[ImportJson] 再検証 - FindAsync:");
                    Console.WriteLine($"  - 車検証番号: {verifiedVehicle.InspectionCertificateNumber ?? "null"}");
                    Console.WriteLine($"  - 車名: {verifiedVehicle.VehicleName ?? "null"}");
                    Console.WriteLine($"  - 型式: {verifiedVehicle.VehicleModel ?? "null"}");
                    Console.WriteLine($"  - 車台番号: {verifiedVehicle.ChassisNumber ?? "null"}");

                    if (string.IsNullOrEmpty(verifiedVehicle.InspectionCertificateNumber) &&
                        root.TryGetProperty("inspection_certificate_number", out var certCheck) &&
                        !string.IsNullOrEmpty(certCheck.GetString()))
                    {
                        Console.WriteLine("[ImportJson] ⚠️警告: 車検証番号が正しく保存されていません!");
                    }
                }

                Console.WriteLine($"[ImportJson] 完了 - {updatedFields.Count}個のフィールドを更新しました");
                return Ok(new {
                    message = "JSON import successful",
                    updatedFieldsCount = updatedFields.Count,
                    updatedFields = updatedFields,
                    vehicleId = id
                });
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[ImportJson] JSONパースエラー: {ex.Message}");
                return BadRequest($"JSON parse error: {ex.Message}");
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"[ImportJson] データベース更新エラー: {ex.Message}");
                Console.WriteLine($"[ImportJson] InnerException: {ex.InnerException?.Message}");
                return StatusCode(500, $"Database update error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ImportJson] 予期しないエラー: {ex.Message}");
                Console.WriteLine($"[ImportJson] StackTrace: {ex.StackTrace}");
                return StatusCode(500, $"Unexpected error: {ex.Message}");
            }
        }
    }
}