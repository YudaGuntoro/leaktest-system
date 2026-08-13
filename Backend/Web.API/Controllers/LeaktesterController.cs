using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.Globalization;
using System.Net.NetworkInformation;
using Web.API.Domain.Production;
using Web.API.Persistence.Context;
using Web.API.Reports;

namespace Web.API.Controllers;

[ApiController]
[Route("api/leaktester")]
public class LeaktesterController : ApiControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public LeaktesterController(AppDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    [HttpGet("work-records")]
    public async Task<IActionResult> WorkRecords(
        [FromQuery] DateTime? date,
        [FromQuery(Name = "date_from")] DateTime? dateFrom,
        [FromQuery(Name = "date_to")] DateTime? dateTo,
        [FromQuery(Name = "engine_model")] string? engineModel,
        [FromQuery(Name = "engine_number")] string? engineNumber,
        [FromQuery(Name = "barcode_scan")] string? barcodeScan,
        [FromQuery] string? result)
    {
        await EnsureLeakTestWorkRecordHmiColumnsAsync();

        var records = await WorkRecordQuery(date, dateFrom, dateTo, engineModel, engineNumber, barcodeScan)
            .OrderByDescending(x => x.CheckDate)
            .ThenByDescending(x => x.CheckTime)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        await HydrateWorkRecordParameterContextAsync(records);
        return ApiOk(FilterWorkRecordsByResult(records, result).Take(500).ToList());
    }

    [HttpGet("work-records/export")]
    [Produces(LeakTestWorkRecordListReportBuilder.ContentType)]
    public async Task<IActionResult> ExportWorkRecords(
        [FromQuery] DateTime? date,
        [FromQuery(Name = "date_from")] DateTime? dateFrom,
        [FromQuery(Name = "date_to")] DateTime? dateTo,
        [FromQuery(Name = "engine_model")] string? engineModel,
        [FromQuery(Name = "engine_number")] string? engineNumber,
        [FromQuery(Name = "barcode_scan")] string? barcodeScan,
        [FromQuery] string? result)
    {
        try
        {
            await EnsureLeakTestWorkRecordHmiColumnsAsync();
            await EnsureLeakTestJudgementTableAsync();

            var records = await WorkRecordQuery(date, dateFrom, dateTo, engineModel, engineNumber, barcodeScan)
                .OrderByDescending(x => x.CheckDate)
                .ThenByDescending(x => x.CheckTime)
                .ThenByDescending(x => x.Id)
                .ToListAsync();
            await HydrateWorkRecordParameterContextAsync(records);
            records = FilterWorkRecordsByResult(records, result).ToList();

            var effectiveDateFrom = dateFrom ?? date;
            var effectiveDateTo = dateTo ?? date;
            var templatePath = Path.Combine(_environment.ContentRootPath, "Templates", LeakTestWorkRecordReportBuilder.TemplateFileName);
            var content = LeakTestWorkRecordListReportBuilder.Build(
                records,
                effectiveDateFrom?.Date,
                effectiveDateTo?.Date,
                templatePath);

            return File(
                content,
                LeakTestWorkRecordListReportBuilder.ContentType,
                LeakTestWorkRecordListReportBuilder.BuildFileName(effectiveDateFrom?.Date, effectiveDateTo?.Date));
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [HttpGet("work-records/monthly-summary")]
    public async Task<IActionResult> WorkRecordMonthlySummary([FromQuery] int? year)
    {
        var selectedYear = year is >= 1 and <= 9999 ? year.Value : DateTime.Today.Year;
        var startDate = new DateTime(selectedYear, 1, 1);
        var endDate = startDate.AddYears(1);

        var records = await _db.LeakTestWorkRecords.AsNoTracking()
            .Include(x => x.EngineModel)
            .Where(x => x.CheckDate >= startDate && x.CheckDate < endDate)
            .ToListAsync();
        await HydrateWorkRecordParameterContextAsync(records);

        var summaries = Enumerable.Range(1, 12)
            .Select(month =>
            {
                var monthRecords = records
                    .Where(x => x.CheckDate.Month == month)
                    .ToList();

                return new LeakTestMonthlySummary
                {
                    Year = selectedYear,
                    Month = month,
                    MonthLabel = CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(month),
                    TotalEngineInspect = monthRecords
                        .Select(x => x.EngineNumber.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count(),
                    Ok = monthRecords
                        .Where(x => x.Result == "OK")
                        .Select(x => x.EngineNumber.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count(),
                    Ng = monthRecords
                        .Where(x => x.Result == "NG")
                        .Select(x => x.EngineNumber.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count()
                };
            })
            .ToList();

        return ApiOk(summaries);
    }

    [HttpGet("work-records/{id:long}/export")]
    [Produces(LeakTestWorkRecordReportBuilder.ContentType)]
    public async Task<IActionResult> ExportWorkRecord(long id)
    {
        try
        {
            await EnsureLeakTestWorkRecordHmiColumnsAsync();

            var record = await _db.LeakTestWorkRecords.AsNoTracking()
                .Include(x => x.EngineModel)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (record is null)
            {
                return ApiNotFound("Leak test work record was not found.");
            }

            await HydrateWorkRecordParameterContextAsync(new[] { record });
            var templatePath = Path.Combine(_environment.ContentRootPath, "Templates", LeakTestWorkRecordReportBuilder.TemplateFileName);
            var content = LeakTestWorkRecordReportBuilder.Build(record, templatePath);
            return File(content, LeakTestWorkRecordReportBuilder.ContentType, LeakTestWorkRecordReportBuilder.BuildFileName(record));
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [HttpPost("work-records")]
    public async Task<IActionResult> CreateWorkRecord([FromBody] CreateLeakTestWorkRecordRequest request)
    {
        try
        {
            await EnsureLeakTestWorkRecordHmiColumnsAsync();

            if (request.EngineModelId <= 0 ||
                string.IsNullOrWhiteSpace(request.EngineNumber) ||
                string.IsNullOrWhiteSpace(request.MachineName) ||
                string.IsNullOrWhiteSpace(request.CheckTime))
            {
                throw new ArgumentException("Engine information and leak test pressure fields are required.");
            }

            var engineModel = await _db.EngineModels
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.EngineModelId && x.IsDeleted != true);
            if (engineModel is null)
            {
                throw new ArgumentException("Engine model was not found or is inactive.");
            }

            if (request.ParameterPressure <= 0 || request.PressureInput <= 0)
            {
                throw new ArgumentException("Leak test pressure values must be greater than zero.");
            }

            if (request.CycleTimeLeakTestMinutes <= 0)
            {
                throw new ArgumentException("Cycle time leak test must be greater than zero.");
            }

            var operatorName = FirstText(request.OperatorName);

            var record = new LeakTestWorkRecord
            {
                EngineModelId = engineModel.Id,
                EngineNumber = request.EngineNumber.Trim(),
                BarcodeScan = FirstText(request.BarcodeScan, BuildBarcodeScan(engineModel.ModelName, request.EngineNumber)),
                CheckDate = request.CheckDate.Date,
                CheckTime = NormalizeCheckTime(request.CheckTime),
                MachineName = request.MachineName.Trim(),
                OperatorName = string.IsNullOrWhiteSpace(operatorName) ? null : TrimTo(operatorName, 150),
                ParameterPressure = request.ParameterPressure,
                ChannelNo = string.IsNullOrWhiteSpace(request.ChannelNo) ? null : TrimTo(request.ChannelNo, 20),
                PressSetUp = request.PressSetUp,
                PressSetLow = request.PressSetLow,
                PressureInput = request.PressureInput,
                CycleTimeLeakTestMinutes = request.CycleTimeLeakTestMinutes,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _db.LeakTestWorkRecords.Add(record);
            await _db.SaveChangesAsync();
            record.EngineModel = engineModel;
            await HydrateWorkRecordParameterContextAsync(new[] { record });
            return ApiCreated(record, "Leak test work record saved successfully.");
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [AllowAnonymous]
    [HttpPost("work-records/hmi")]
    public async Task<IActionResult> CreateHmiWorkRecord([FromBody] CreateHmiLeakTestWorkRecordRequest request)
    {
        try
        {
            await EnsureLeakTestWorkRecordHmiColumnsAsync();

            var barcode = NormalizeBarcodeScan(FirstText(request.BarcodeScan, request.Barcode));
            var (barcodeEngineModel, barcodeEngineNumber) = ParseBarcodeScan(barcode);
            var engineModelText = FirstText(request.EngineModel, barcodeEngineModel);
            var engineNumber = FirstText(request.SerialNo, request.SerialNoText, request.EngineNumber, barcodeEngineNumber, barcode);

            if (string.IsNullOrWhiteSpace(engineModelText))
            {
                throw new ArgumentException("Engine model is required from HMI payload.");
            }

            if (string.IsNullOrWhiteSpace(engineNumber))
            {
                throw new ArgumentException("Serial no / engine number is required from HMI payload.");
            }

            if (request.PressureInput <= 0)
            {
                throw new ArgumentException("Pressure input must be greater than zero.");
            }

            if (request.CycleTime <= 0)
            {
                throw new ArgumentException("Cycle time must be greater than zero.");
            }

            var parameterPressure = CalculateHmiParameterPressure(request.PressSetLow, request.PressSetUp);
            if (parameterPressure <= 0)
            {
                throw new ArgumentException("Press set low/up is required from HMI payload.");
            }

            var judgement = await ResolveJudgementSnapshotAsync(request.Judgement);

            var engineModel = await FindOrCreateEngineModelAsync(engineModelText);
            var testedAt = request.TestedAt ?? DateTime.Now;

            var record = new LeakTestWorkRecord
            {
                EngineModelId = engineModel.Id,
                EngineNumber = TrimTo(engineNumber, 120),
                BarcodeScan = FirstText(barcode, BuildBarcodeScan(engineModel.ModelName, engineNumber)),
                CheckDate = testedAt.Date,
                CheckTime = testedAt.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                MachineName = string.IsNullOrWhiteSpace(request.MachineName)
                    ? "Leak Tester Machine 1"
                    : TrimTo(request.MachineName, 150),
                OperatorName = string.IsNullOrWhiteSpace(request.Operator) ? null : TrimTo(request.Operator, 150),
                ParameterPressure = parameterPressure,
                ChannelNo = string.IsNullOrWhiteSpace(request.ChannelNo) ? null : TrimTo(request.ChannelNo, 20),
                PressSetUp = request.PressSetUp,
                PressSetLow = request.PressSetLow,
                PressureInput = request.PressureInput,
                CycleTimeLeakTestMinutes = request.CycleTime,
                JudgementCode = judgement.JudgementCode,
                JudgementName = judgement.JudgementName,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _db.LeakTestWorkRecords.Add(record);
            await _db.SaveChangesAsync();
            record.EngineModel = engineModel;
            await HydrateWorkRecordParameterContextAsync(new[] { record });
            return ApiCreated(record, "HMI leak test work record saved successfully.");
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [AllowAnonymous]
    [HttpGet("rework-engine-records")]
    public async Task<IActionResult> ReworkEngineRecords(
        [FromQuery] DateTime? date,
        [FromQuery(Name = "date_from")] DateTime? dateFrom,
        [FromQuery(Name = "date_to")] DateTime? dateTo,
        [FromQuery(Name = "engine_model")] string? engineModel,
        [FromQuery(Name = "engine_number")] string? engineNumber,
        [FromQuery(Name = "barcode_scan")] string? barcodeScan,
        [FromQuery] string? result)
    {
        await EnsureReworkEngineRecordOperatorSnapshotColumnAsync();

        var records = await ReworkEngineRecordQuery(date, dateFrom, dateTo, engineModel, engineNumber, barcodeScan, result)
            .OrderByDescending(x => x.ReworkDate)
            .ThenByDescending(x => x.ReworkTime)
            .ThenByDescending(x => x.Id)
            .Take(500)
            .ToListAsync();
        await HydrateReworkEngineParameterContextAsync(records);
        return ApiOk(records);
    }

    [AllowAnonymous]
    [HttpGet("rework-engine-records/export")]
    [Produces(ReworkEngineRecordListReportBuilder.ContentType)]
    public async Task<IActionResult> ExportReworkEngineRecords(
        [FromQuery] DateTime? date,
        [FromQuery(Name = "date_from")] DateTime? dateFrom,
        [FromQuery(Name = "date_to")] DateTime? dateTo,
        [FromQuery(Name = "engine_model")] string? engineModel,
        [FromQuery(Name = "engine_number")] string? engineNumber,
        [FromQuery(Name = "barcode_scan")] string? barcodeScan,
        [FromQuery] string? result)
    {
        try
        {
            await EnsureReworkEngineRecordOperatorSnapshotColumnAsync();

            var records = await ReworkEngineRecordQuery(date, dateFrom, dateTo, engineModel, engineNumber, barcodeScan, result)
                .OrderByDescending(x => x.ReworkDate)
                .ThenByDescending(x => x.ReworkTime)
                .ThenByDescending(x => x.Id)
                .ToListAsync();
            await HydrateReworkEngineParameterContextAsync(records);

            var effectiveDateFrom = dateFrom ?? date;
            var effectiveDateTo = dateTo ?? date;
            var templatePath = Path.Combine(_environment.ContentRootPath, "Templates", LeakTestWorkRecordReportBuilder.TemplateFileName);
            var content = ReworkEngineRecordListReportBuilder.Build(
                records,
                effectiveDateFrom?.Date,
                effectiveDateTo?.Date,
                templatePath);

            return File(
                content,
                ReworkEngineRecordListReportBuilder.ContentType,
                ReworkEngineRecordListReportBuilder.BuildFileName(effectiveDateFrom?.Date, effectiveDateTo?.Date));
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [AllowAnonymous]
    [HttpGet("rework-engine-records/{id:long}/export")]
    [Produces(ReworkEngineRecordReportBuilder.ContentType)]
    public async Task<IActionResult> ExportReworkEngineRecord(long id)
    {
        try
        {
            await EnsureReworkEngineRecordOperatorSnapshotColumnAsync();

            var record = await _db.ReworkEngineRecords.AsNoTracking()
                .Include(x => x.EngineModel)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (record is null)
            {
                return ApiNotFound("Rework engine record was not found.");
            }

            await HydrateReworkEngineParameterContextAsync(new[] { record });
            var templatePath = Path.Combine(_environment.ContentRootPath, "Templates", LeakTestWorkRecordReportBuilder.TemplateFileName);
            var content = ReworkEngineRecordReportBuilder.Build(record, templatePath);
            return File(content, ReworkEngineRecordReportBuilder.ContentType, ReworkEngineRecordReportBuilder.BuildFileName(record));
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [AllowAnonymous]
    [HttpPost("rework-engine-records")]
    public async Task<IActionResult> CreateReworkEngineRecord([FromBody] CreateReworkEngineRecordRequest request)
    {
        try
        {
            await EnsureReworkEngineRecordOperatorSnapshotColumnAsync();

            if (string.IsNullOrWhiteSpace(request.BarcodeScan))
            {
                throw new ArgumentException("Barcode scan is required.");
            }

            if (request.ParameterPressure <= 0 || request.PressureInput <= 0)
            {
                throw new ArgumentException("Rework pressure values must be greater than zero.");
            }

            var result = request.Result.Trim().ToUpperInvariant();
            if (result is not ("OK" or "NG"))
            {
                throw new ArgumentException("Result must be OK or NG.");
            }

            var operatorName = FirstText(request.OperatorName);

            var barcodeScan = NormalizeBarcodeScan(request.BarcodeScan);
            var (barcodeEngineModel, barcodeEngineNumber) = ParseBarcodeScan(barcodeScan);
            if (string.IsNullOrWhiteSpace(barcodeEngineNumber))
            {
                barcodeEngineNumber = barcodeEngineModel;
                barcodeEngineModel = null;
            }

            if (string.IsNullOrWhiteSpace(barcodeEngineNumber))
            {
                throw new ArgumentException("Engine number could not be read from barcode.");
            }

            EngineModel? engineModel = null;
            if (!string.IsNullOrWhiteSpace(barcodeEngineModel))
            {
                engineModel = await _db.EngineModels
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.ModelName == barcodeEngineModel && x.IsDeleted != true);
            }

            var record = new ReworkEngineRecord
            {
                EngineModelId = engineModel?.Id,
                EngineModelText = engineModel is null ? barcodeEngineModel : null,
                EngineNumber = barcodeEngineNumber.Trim(),
                BarcodeScan = barcodeScan ?? request.BarcodeScan.Trim(),
                ReworkDate = request.ReworkDate.Date,
                ReworkTime = NormalizeCheckTime(request.ReworkTime),
                OperatorName = string.IsNullOrWhiteSpace(operatorName) ? null : TrimTo(operatorName, 150),
                ParameterPressure = request.ParameterPressure,
                PressureInput = request.PressureInput,
                Result = result,
                Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _db.ReworkEngineRecords.Add(record);
            await _db.SaveChangesAsync();
            record.EngineModel = engineModel;
            await HydrateReworkEngineParameterContextAsync(new[] { record });
            return ApiCreated(record, "Rework engine record saved successfully.");
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [HttpGet("engine-models")]
    public async Task<IActionResult> EngineModels(
        [FromQuery] string? search,
        [FromQuery(Name = "search_by")] string? searchBy,
        [FromQuery] string? status)
    {
        var query = _db.EngineModels.AsNoTracking().AsQueryable();
        var normalizedStatus = status?.Trim().ToLowerInvariant();

        query = normalizedStatus switch
        {
            "all" => query,
            "deleted" => query.Where(x => x.IsDeleted == true),
            _ => query.Where(x => x.IsDeleted != true)
        };

        var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            var normalizedSearchBy = searchBy?.Trim().ToLowerInvariant();
            query = normalizedSearchBy switch
            {
                "engine_model" => query.Where(x => x.ModelName.Contains(term)),
                "description" => query.Where(x => x.Description != null && x.Description.Contains(term)),
                _ => query.Where(x =>
                    x.ModelName.Contains(term) ||
                    (x.Description != null && x.Description.Contains(term)))
            };
        }

        return ApiOk(await query
            .OrderBy(x => x.ModelName)
            .ToListAsync());
    }

    [AllowAnonymous]
    [HttpGet("operators")]
    public async Task<IActionResult> Operators(
        [FromQuery] string? search,
        [FromQuery(Name = "search_by")] string? searchBy,
        [FromQuery] string? status)
    {
        var query = _db.Operators.AsNoTracking().AsQueryable();
        var normalizedStatus = status?.Trim().ToLowerInvariant();

        query = normalizedStatus switch
        {
            "all" => query,
            "deleted" => query.Where(x => x.IsDeleted == true),
            _ => query.Where(x => x.IsDeleted != true)
        };

        var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            var normalizedSearchBy = searchBy?.Trim().ToLowerInvariant();
            query = normalizedSearchBy switch
            {
                "operator_code" => query.Where(x => x.OperatorCode.Contains(term)),
                "operator_name" => query.Where(x => x.OperatorName.Contains(term)),
                "department" => query.Where(x => x.Department != null && x.Department.Contains(term)),
                _ => query.Where(x =>
                    x.OperatorCode.Contains(term) ||
                    x.OperatorName.Contains(term) ||
                    (x.Department != null && x.Department.Contains(term)))
            };
        }

        return ApiOk(await query
            .OrderBy(x => x.OperatorCode)
            .ToListAsync());
    }

    [AllowAnonymous]
    [HttpGet("settings")]
    public async Task<IActionResult> Settings()
    {
        try
        {
            await EnsureSystemSettingsTablesAsync();
            return ApiOk(await GetSystemSettingsResponseAsync());
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateSystemSettingsRequest request)
    {
        try
        {
            await EnsureSystemSettingsTablesAsync();

            var pressureUnitId = await FindOrCreateMeasurementUnitAsync("pressure", request.PressureUnit, request.PressureUnit);
            var cycleTimeUnitId = await FindOrCreateMeasurementUnitAsync("cycle_time", request.CycleTimeUnit, request.CycleTimeUnit);
            var schedule = NormalizeBackupSchedule(request.BackupSchedule);

            var setting = await _db.SystemSettings.FirstOrDefaultAsync(x => x.Id == 1);
            if (setting is null)
            {
                setting = new SystemSetting
                {
                    Id = 1,
                    CreatedAt = DateTime.Now
                };
                _db.SystemSettings.Add(setting);
            }

            setting.PressureUnitId = pressureUnitId;
            setting.CycleTimeUnitId = cycleTimeUnitId;
            setting.BackupDbLocation = string.IsNullOrWhiteSpace(request.BackupDbLocation)
                ? null
                : TrimTo(request.BackupDbLocation, 500);
            setting.BackupSchedule = schedule;
            setting.PlcIpAddress = string.IsNullOrWhiteSpace(request.PlcIpAddress)
                ? null
                : TrimTo(request.PlcIpAddress, 80);
            setting.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return ApiOk(await GetSystemSettingsResponseAsync(), "Settings updated successfully.");
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [HttpGet("parameters")]
    public async Task<IActionResult> Parameters(
        [FromQuery] string? search,
        [FromQuery(Name = "search_by")] string? searchBy,
        [FromQuery] string? status)
    {
        await EnsureLeakTestParameterTableAsync();

        var query = _db.LeakTestParameters.AsNoTracking().AsQueryable();
        var normalizedStatus = status?.Trim().ToLowerInvariant();

        query = normalizedStatus switch
        {
            "all" => query,
            "deleted" => query.Where(x => x.IsDeleted == true),
            _ => query.Where(x => x.IsDeleted != true)
        };

        var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            var normalizedSearchBy = searchBy?.Trim().ToLowerInvariant();
            query = normalizedSearchBy switch
            {
                "channel_no" => query.Where(x => x.ChannelNo.Contains(term)),
                "model_parameter" => query.Where(x => x.ModelParameter.Contains(term)),
                "item_name" => query.Where(x => x.ItemName.Contains(term)),
                "item_value" => query.Where(x => x.ItemValue.Contains(term)),
                "machine_names" => query.Where(x => x.MachineNames != null && x.MachineNames.Contains(term)),
                _ => query.Where(x =>
                    x.ChannelNo.Contains(term) ||
                    x.ModelParameter.Contains(term) ||
                    x.ItemName.Contains(term) ||
                    x.ItemValue.Contains(term) ||
                    (x.MachineNames != null && x.MachineNames.Contains(term)))
            };
        }

        return ApiOk(await query
            .OrderBy(x => x.ChannelNo)
            .ThenBy(x => x.Id)
            .ToListAsync());
    }

    [HttpGet("judgements")]
    public async Task<IActionResult> Judgements()
    {
        try
        {
            await EnsureLeakTestJudgementTableAsync();

            var items = await _db.LeakTestJudgements
                .AsNoTracking()
                .Where(x => x.IsDeleted != true)
                .OrderBy(x => x.JudgementCode)
                .ToListAsync();

            if (items.Count == 0)
            {
                await SeedDefaultHmiJudgementsAsync();
                items = await _db.LeakTestJudgements
                    .AsNoTracking()
                    .Where(x => x.IsDeleted != true)
                    .OrderBy(x => x.JudgementCode)
                    .ToListAsync();
            }

            return ApiOk(items);
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [HttpPut("judgements/{id:int}")]
    public async Task<IActionResult> UpdateJudgement(int id, [FromBody] UpdateLeakTestJudgementRequest request)
    {
        try
        {
            await EnsureLeakTestJudgementTableAsync();

            var result = request.Result.Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(result) && result is not ("OK" or "NG"))
            {
                throw new ArgumentException("Result must be empty, OK, or NG.");
            }

            var item = await _db.LeakTestJudgements.FirstOrDefaultAsync(x => x.Id == id);
            if (item is null)
            {
                return ApiNotFound("Judgement was not found.");
            }

            item.JudgementName = string.IsNullOrWhiteSpace(request.JudgementName) ? string.Empty : TrimTo(request.JudgementName, 80);
            item.Result = result;
            item.Note = string.IsNullOrWhiteSpace(request.Note) ? string.Empty : TrimTo(request.Note, 150);
            item.IsDeleted = request.IsDeleted ?? false;
            item.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return ApiOk(item, "Judgement updated successfully.");
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [HttpPost("parameters")]
    public async Task<IActionResult> CreateParameter([FromBody] CreateLeakTestParameterRequest request)
    {
        try
        {
            await EnsureLeakTestParameterTableAsync();

            if (string.IsNullOrWhiteSpace(request.ChannelNo))
            {
                throw new ArgumentException("Channel no is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ModelParameter))
            {
                throw new ArgumentException("Model parameter is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ItemName))
            {
                throw new ArgumentException("Item name is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ItemValue))
            {
                throw new ArgumentException("Value is required.");
            }

            var item = new LeakTestParameter
            {
                ChannelNo = TrimTo(request.ChannelNo, 20),
                ModelParameter = TrimTo(request.ModelParameter, 150),
                ItemName = TrimTo(request.ItemName, 120),
                ItemValue = TrimTo(request.ItemValue, 80),
                MachineNames = string.IsNullOrWhiteSpace(request.MachineNames) ? null : TrimTo(request.MachineNames, 1000),
                IsDeleted = request.IsDeleted ?? false,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _db.LeakTestParameters.Add(item);
            await _db.SaveChangesAsync();
            return ApiCreated(item, "Parameter created successfully.");
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [HttpPut("parameters/{id:int}")]
    public async Task<IActionResult> UpdateParameter(int id, [FromBody] CreateLeakTestParameterRequest request)
    {
        try
        {
            await EnsureLeakTestParameterTableAsync();

            var item = await _db.LeakTestParameters.FirstOrDefaultAsync(x => x.Id == id);
            if (item is null)
            {
                return ApiNotFound("Parameter was not found.");
            }

            if (string.IsNullOrWhiteSpace(request.ChannelNo))
            {
                throw new ArgumentException("Channel no is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ModelParameter))
            {
                throw new ArgumentException("Model parameter is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ItemName))
            {
                throw new ArgumentException("Item name is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ItemValue))
            {
                throw new ArgumentException("Value is required.");
            }

            item.ChannelNo = TrimTo(request.ChannelNo, 20);
            item.ModelParameter = TrimTo(request.ModelParameter, 150);
            item.ItemName = TrimTo(request.ItemName, 120);
            item.ItemValue = TrimTo(request.ItemValue, 80);
            item.MachineNames = string.IsNullOrWhiteSpace(request.MachineNames) ? null : TrimTo(request.MachineNames, 1000);
            item.IsDeleted = request.IsDeleted ?? false;
            item.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return ApiOk(item, "Parameter updated successfully.");
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [HttpPost("parameters/import")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> ImportParameters([FromForm] IFormFile? file)
    {
        try
        {
            await EnsureLeakTestParameterTableAsync();

            if (file is null || file.Length <= 0)
            {
                throw new ArgumentException("Excel file is required.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension is not ".xlsx" and not ".xlsm")
            {
                throw new ArgumentException("Only .xlsx and .xlsm Excel files are supported.");
            }

            var rows = ReadParameterRowsFromExcel(file);
            if (rows.Count == 0)
            {
                throw new ArgumentException("No parameter rows were found in the Excel file.");
            }

            var existingRows = await _db.LeakTestParameters.ToListAsync();
            var existingMap = existingRows.ToDictionary(
                x => ParameterKey(x.ChannelNo, x.ItemName),
                x => x);

            var imported = 0;
            var updated = 0;
            var skipped = 0;
            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                var key = ParameterKey(row.ChannelNo, row.ItemName);
                if (!seenKeys.Add(key))
                {
                    skipped++;
                    continue;
                }

                if (existingMap.TryGetValue(key, out var existing))
                {
                    existing.ModelParameter = row.ModelParameter;
                    existing.ItemValue = row.ItemValue;
                    existing.MachineNames = row.MachineNames;
                    existing.IsDeleted = false;
                    existing.UpdatedAt = DateTime.Now;
                    updated++;
                    continue;
                }

                _db.LeakTestParameters.Add(new LeakTestParameter
                {
                    ChannelNo = row.ChannelNo,
                    ModelParameter = row.ModelParameter,
                    ItemName = row.ItemName,
                    ItemValue = row.ItemValue,
                    MachineNames = row.MachineNames,
                    IsDeleted = false,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
                imported++;
            }

            await _db.SaveChangesAsync();
            return ApiOk(new LeakTestParameterImportResult
            {
                Imported = imported,
                Updated = updated,
                Skipped = skipped,
                Channels = rows.Select(x => x.ChannelNo).Distinct(StringComparer.OrdinalIgnoreCase).Count()
            }, "Parameter Excel imported successfully.");
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [HttpPost("operators")]
    public async Task<IActionResult> CreateOperator([FromBody] CreateOperatorRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.OperatorName))
            {
                throw new ArgumentException("Operator name is required.");
            }

            var operatorCode = await BuildNextOperatorCodeAsync();
            var operatorName = request.OperatorName.Trim();

            var item = new Operator
            {
                OperatorCode = operatorCode,
                OperatorName = operatorName,
                Department = string.IsNullOrWhiteSpace(request.Department) ? null : request.Department.Trim(),
                Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                IsDeleted = request.IsDeleted ?? false,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _db.Operators.Add(item);
            await _db.SaveChangesAsync();
            return ApiCreated(item, "Operator created successfully.");
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [HttpPut("operators/{id:int}")]
    public async Task<IActionResult> UpdateOperator(int id, [FromBody] CreateOperatorRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.OperatorName))
            {
                throw new ArgumentException("Operator name is required.");
            }

            if (string.IsNullOrWhiteSpace(request.OperatorCode))
            {
                throw new ArgumentException("Operator code is required.");
            }

            var item = await _db.Operators.FirstOrDefaultAsync(x => x.Id == id);
            if (item is null)
            {
                return ApiNotFound("Operator was not found.");
            }

            var operatorCode = TrimTo(request.OperatorCode.Trim(), 50);
            var operatorName = request.OperatorName.Trim();
            var codeExists = await _db.Operators.AnyAsync(x => x.Id != id && x.OperatorCode == operatorCode);
            if (codeExists)
            {
                throw new ArgumentException("Operator code already exists.");
            }

            item.OperatorCode = operatorCode;
            item.OperatorName = operatorName;
            item.Department = string.IsNullOrWhiteSpace(request.Department) ? null : request.Department.Trim();
            item.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
            item.IsDeleted = request.IsDeleted ?? false;
            item.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return ApiOk(item, "Operator updated successfully.");
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [HttpDelete("operators/{id:int}")]
    public async Task<IActionResult> DeleteOperator(int id)
    {
        try
        {
            var item = await _db.Operators.FirstOrDefaultAsync(x => x.Id == id);
            if (item is null)
            {
                return ApiNotFound("Operator was not found.");
            }

            item.IsDeleted = true;
            item.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            return ApiOk(item, "Operator deleted successfully.");
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    private async Task<string> BuildNextOperatorCodeAsync()
    {
        const string prefix = "LT-OP-";
        var codes = await _db.Operators
            .AsNoTracking()
            .Where(x => x.OperatorCode.StartsWith(prefix))
            .Select(x => x.OperatorCode)
            .ToListAsync();

        var maxNumber = codes
            .Select(code => code[prefix.Length..])
            .Select(value => int.TryParse(value, out var number) ? number : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}{maxNumber + 1:0000}";
    }

    [HttpPost("engine-models")]
    public async Task<IActionResult> CreateEngineModel([FromBody] CreateEngineModelRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ModelName))
            {
                throw new ArgumentException("Engine model is required.");
            }

            var item = new EngineModel
            {
                ModelName = request.ModelName.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                IsDeleted = request.IsDeleted ?? false
            };

            _db.EngineModels.Add(item);
            await _db.SaveChangesAsync();
            return ApiCreated(item, "Engine model created successfully.");
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var lastMqttAt = await _db.LeakTestWorkRecords.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => (DateTime?)x.CreatedAt)
            .FirstOrDefaultAsync();

        return ApiOk(new
        {
            last_mqtt_at = lastMqttAt,
            server_time = DateTime.Now
        });
    }

    [AllowAnonymous]
    [HttpGet("plc/status")]
    public async Task<IActionResult> PlcStatus()
    {
        try
        {
            await EnsureSystemSettingsTablesAsync();
            var settings = await GetSystemSettingsResponseAsync();
            var plcIpAddress = settings.PlcIpAddress.Trim();
            var isOnline = await CheckPlcReachableAsync(plcIpAddress);

            return ApiOk(new
            {
                plc_ip_address = plcIpAddress,
                configured = !string.IsNullOrWhiteSpace(plcIpAddress),
                online = isOnline,
                checked_at = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    private async Task EnsureLeakTestWorkRecordHmiColumnsAsync()
    {
        await EnsureColumnAsync(
            "leak_test_work_records",
            "barcode_scan",
            "ALTER TABLE leak_test_work_records ADD COLUMN barcode_scan VARCHAR(180) NULL AFTER engine_number");
        await EnsureColumnAsync(
            "leak_test_work_records",
            "channel_no",
            "ALTER TABLE leak_test_work_records ADD COLUMN channel_no VARCHAR(20) NULL AFTER parameter_pressure");
        await EnsureColumnAsync(
            "leak_test_work_records",
            "press_set_up",
            "ALTER TABLE leak_test_work_records ADD COLUMN press_set_up DECIMAL(8, 2) NULL AFTER channel_no");
        await EnsureColumnAsync(
            "leak_test_work_records",
            "press_set_low",
            "ALTER TABLE leak_test_work_records ADD COLUMN press_set_low DECIMAL(8, 2) NULL AFTER press_set_up");
        await EnsureColumnAsync(
            "leak_test_work_records",
            "operator_name",
            "ALTER TABLE leak_test_work_records ADD COLUMN operator_name VARCHAR(150) NULL AFTER machine_name");
        await EnsureColumnAsync(
            "leak_test_work_records",
            "judgement_code",
            "ALTER TABLE leak_test_work_records ADD COLUMN judgement_code INT NULL AFTER cycle_time_leak_test_minutes");
        await DropHistoryOperatorIdColumnsAsync();
        await DropWorkRecordJudgementNameColumnAsync();
        await DropWorkRecordResultColumnAsync();
        await EnsureIndexAsync(
            "leak_test_work_records",
            "ix_leak_test_work_records_barcode_scan",
            "CREATE INDEX ix_leak_test_work_records_barcode_scan ON leak_test_work_records (barcode_scan)");
        await EnsureIndexAsync(
            "leak_test_work_records",
            "ix_leak_test_work_records_channel_no",
            "CREATE INDEX ix_leak_test_work_records_channel_no ON leak_test_work_records (channel_no)");
        await EnsureIndexAsync(
            "leak_test_work_records",
            "ix_leak_test_work_records_judgement_code",
            "CREATE INDEX ix_leak_test_work_records_judgement_code ON leak_test_work_records (judgement_code)");
    }

    private async Task EnsureReworkEngineRecordOperatorSnapshotColumnAsync()
    {
        await EnsureColumnAsync(
            "rework_engine_records",
            "operator_name",
            "ALTER TABLE rework_engine_records ADD COLUMN operator_name VARCHAR(150) NULL AFTER rework_time");
        await DropHistoryOperatorIdColumnsAsync();
    }

    private async Task DropHistoryOperatorIdColumnsAsync()
    {
        await _db.Database.ExecuteSqlRawAsync(@"
SET @has_work_operator_id := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'leak_test_work_records'
      AND COLUMN_NAME = 'operator_id'
);
SET @sql := IF(
    @has_work_operator_id > 0,
    'UPDATE leak_test_work_records records JOIN operators operators_master ON operators_master.id = records.operator_id SET records.operator_name = operators_master.operator_name WHERE records.operator_name IS NULL',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_rework_operator_id := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'rework_engine_records'
      AND COLUMN_NAME = 'operator_id'
);
SET @sql := IF(
    @has_rework_operator_id > 0,
    'UPDATE rework_engine_records records JOIN operators operators_master ON operators_master.id = records.operator_id SET records.operator_name = operators_master.operator_name WHERE records.operator_name IS NULL',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_work_fk := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'leak_test_work_records'
      AND CONSTRAINT_NAME = 'fk_leak_test_work_records_operator'
);
SET @sql := IF(@has_work_fk > 0, 'ALTER TABLE leak_test_work_records DROP FOREIGN KEY fk_leak_test_work_records_operator', 'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_rework_fk := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'rework_engine_records'
      AND CONSTRAINT_NAME = 'fk_rework_engine_records_operator'
);
SET @sql := IF(@has_rework_fk > 0, 'ALTER TABLE rework_engine_records DROP FOREIGN KEY fk_rework_engine_records_operator', 'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_work_index := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'leak_test_work_records'
      AND INDEX_NAME = 'ix_leak_test_work_records_operator_id'
);
SET @sql := IF(@has_work_index > 0, 'DROP INDEX ix_leak_test_work_records_operator_id ON leak_test_work_records', 'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_rework_index := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'rework_engine_records'
      AND INDEX_NAME = 'ix_rework_engine_records_operator_id'
);
SET @sql := IF(@has_rework_index > 0, 'DROP INDEX ix_rework_engine_records_operator_id ON rework_engine_records', 'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql := IF(@has_work_operator_id > 0, 'ALTER TABLE leak_test_work_records DROP COLUMN operator_id', 'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql := IF(@has_rework_operator_id > 0, 'ALTER TABLE rework_engine_records DROP COLUMN operator_id', 'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;");
    }

    private async Task DropWorkRecordResultColumnAsync()
    {
        await _db.Database.ExecuteSqlRawAsync(@"
SET @has_work_result_index := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'leak_test_work_records'
      AND INDEX_NAME = 'ix_leak_test_work_records_result'
);

SET @sql := IF(@has_work_result_index > 0, 'DROP INDEX ix_leak_test_work_records_result ON leak_test_work_records', 'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @has_work_result := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'leak_test_work_records'
      AND COLUMN_NAME = 'result'
);

SET @sql := IF(@has_work_result > 0, 'ALTER TABLE leak_test_work_records DROP COLUMN result', 'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;");
    }

    private async Task DropWorkRecordJudgementNameColumnAsync()
    {
        await _db.Database.ExecuteSqlRawAsync(@"
SET @has_work_judgement_name := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'leak_test_work_records'
      AND COLUMN_NAME = 'judgement_name'
);

SET @sql := IF(@has_work_judgement_name > 0, 'ALTER TABLE leak_test_work_records DROP COLUMN judgement_name', 'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;");
    }

    private async Task EnsureColumnAsync(string tableName, string columnName, string alterSql)
    {
        var exists = await _db.Database
            .SqlQueryRaw<int>(
                """
                SELECT COUNT(*) AS Value
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = {0}
                  AND COLUMN_NAME = {1}
                """,
                tableName,
                columnName)
            .SingleAsync();

        if (exists == 0)
        {
            await _db.Database.ExecuteSqlRawAsync(alterSql);
        }
    }

    private async Task EnsureIndexAsync(string tableName, string indexName, string createSql)
    {
        var exists = await _db.Database
            .SqlQueryRaw<int>(
                """
                SELECT COUNT(*) AS Value
                FROM INFORMATION_SCHEMA.STATISTICS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = {0}
                  AND INDEX_NAME = {1}
                """,
                tableName,
                indexName)
            .SingleAsync();

        if (exists == 0)
        {
            await _db.Database.ExecuteSqlRawAsync(createSql);
        }
    }

    private async Task<EngineModel> FindOrCreateEngineModelAsync(string engineModelName)
    {
        var modelName = TrimTo(engineModelName, 45);
        var engineModel = await _db.EngineModels
            .FirstOrDefaultAsync(x => x.ModelName == modelName);

        if (engineModel is not null)
        {
            if (engineModel.IsDeleted == true)
            {
                engineModel.IsDeleted = false;
            }

            return engineModel;
        }

        engineModel = new EngineModel
        {
            ModelName = modelName,
            Description = "HMI",
            Note = "Created by HMI payload",
            IsDeleted = false
        };
        _db.EngineModels.Add(engineModel);
        await _db.SaveChangesAsync();
        return engineModel;
    }

    private async Task<Operator?> FindOrCreateOperatorAsync(string? operatorText)
    {
        var value = FirstText(operatorText);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var operatorItem = await _db.Operators
            .FirstOrDefaultAsync(x => x.OperatorCode == value || x.OperatorName == value);
        if (operatorItem is not null)
        {
            if (operatorItem.IsDeleted == true)
            {
                operatorItem.IsDeleted = false;
            }

            return operatorItem;
        }

        var operatorCode = await BuildUniqueOperatorCodeAsync(value);
        operatorItem = new Operator
        {
            OperatorCode = operatorCode,
            OperatorName = TrimTo(value, 150),
            Department = "Production",
            Note = "Created by HMI payload",
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        _db.Operators.Add(operatorItem);
        await _db.SaveChangesAsync();
        return operatorItem;
    }

    private async Task<string> BuildUniqueOperatorCodeAsync(string operatorText)
    {
        var alphanumeric = new string(operatorText
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
        var baseCode = TrimTo($"HMI-{(string.IsNullOrWhiteSpace(alphanumeric) ? "OPERATOR" : alphanumeric)}", 50);
        var code = baseCode;
        var suffix = 1;

        while (await _db.Operators.AnyAsync(x => x.OperatorCode == code))
        {
            var suffixText = $"-{suffix}";
            var prefixLength = Math.Min(baseCode.Length, 50 - suffixText.Length);
            code = $"{baseCode[..prefixLength]}{suffixText}";
            suffix++;
        }

        return code;
    }

    private static decimal CalculateHmiParameterPressure(decimal? pressSetLow, decimal? pressSetUp)
    {
        if (pressSetLow.HasValue && pressSetUp.HasValue)
        {
            return Math.Round((NormalizeCosmoPressure(pressSetLow.Value) + NormalizeCosmoPressure(pressSetUp.Value)) / 2, 2);
        }

        if (pressSetLow.HasValue)
        {
            return NormalizeCosmoPressure(pressSetLow.Value);
        }

        return pressSetUp.HasValue ? NormalizeCosmoPressure(pressSetUp.Value) : 0;
    }

    private static decimal NormalizeCosmoPressure(decimal value)
    {
        return Math.Abs(value) >= 10 ? Math.Round(value / 100, 2) : value;
    }

    private static string FormatNormalizedPressure(decimal value)
    {
        return $"{NormalizeCosmoPressure(value).ToString("0.00", CultureInfo.InvariantCulture)} MPa";
    }

    private static string? FormatHmiPressureLimit(decimal? pressSetLow, decimal? pressSetUp)
    {
        if (pressSetLow.HasValue && pressSetUp.HasValue)
        {
            return $"{FormatNormalizedPressureAmount(pressSetLow.Value)} ~ {FormatNormalizedPressureAmount(pressSetUp.Value)} MPa";
        }

        if (pressSetLow.HasValue)
        {
            return $"Min {FormatNormalizedPressure(pressSetLow.Value)}";
        }

        if (pressSetUp.HasValue)
        {
            return $"Max {FormatNormalizedPressure(pressSetUp.Value)}";
        }

        return null;
    }

    private static string FormatNormalizedPressureAmount(decimal value)
    {
        return NormalizeCosmoPressure(value).ToString("0.00", CultureInfo.InvariantCulture);
    }

    private static string? FirstText(params string?[] values)
    {
        return values
            .Select(value => value?.Trim())
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static string? BuildBarcodeScan(string? engineModel, string? serialNo)
    {
        var model = engineModel?.Trim().TrimStart('.');
        var serial = serialNo?.Trim();

        if (string.IsNullOrWhiteSpace(model) || string.IsNullOrWhiteSpace(serial))
        {
            return null;
        }

        return TrimTo($"{model} {serial}", 180);
    }

    private static string? NormalizeBarcodeScan(string? barcodeScan)
    {
        if (string.IsNullOrWhiteSpace(barcodeScan))
        {
            return null;
        }

        var normalized = barcodeScan.Trim().TrimStart('.');
        return string.IsNullOrWhiteSpace(normalized) ? null : TrimTo(normalized, 180);
    }

    private static string? NormalizeResult(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToUpperInvariant() switch
        {
            "OK" or "PASS" or "PASSED" or "TRUE" or "2" => "OK",
            "NG" or "NOK" or "FAIL" or "FAILED" or "FALSE" or "0" or "1" or "3" or "4" or "5" or "6" or "7" => "NG",
            _ => null
        };
    }

    private sealed record LeakTestJudgementSnapshot(int? JudgementCode, string? JudgementName, string? Result);

    private async Task<LeakTestJudgementSnapshot> ResolveJudgementSnapshotAsync(string? value)
    {
        if (int.TryParse(value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var judgementCode))
        {
            var masterJudgement = await _db.LeakTestJudgements
                .AsNoTracking()
                .Where(x => x.JudgementCode == judgementCode && x.IsDeleted != true)
                .Select(x => new { x.JudgementCode, x.JudgementName, x.Result })
                .FirstOrDefaultAsync();

            if (masterJudgement?.Result is "OK" or "NG")
            {
                return new LeakTestJudgementSnapshot(
                    masterJudgement.JudgementCode,
                    string.IsNullOrWhiteSpace(masterJudgement.JudgementName) ? null : masterJudgement.JudgementName,
                    masterJudgement.Result);
            }

            return new LeakTestJudgementSnapshot(
                judgementCode,
                string.IsNullOrWhiteSpace(masterJudgement?.JudgementName) ? null : masterJudgement.JudgementName,
                NormalizeResult(value));
        }

        return new LeakTestJudgementSnapshot(null, null, NormalizeResult(value));
    }

    private async Task HydrateWorkRecordParameterContextAsync(IReadOnlyCollection<LeakTestWorkRecord> records)
    {
        if (records.Count == 0)
        {
            return;
        }

        await HydrateWorkRecordJudgementsAsync(records);
        await HydrateWorkRecordOperatorsAsync(records);

        var parameters = await GetActiveLeakTestParametersAsync();
        foreach (var record in records)
        {
            var context = FindParameterContext(parameters, record.EngineModelName);
            record.BarcodeScan = FirstText(record.BarcodeScan, BuildBarcodeScan(record.EngineModelName, record.EngineNumber));
            record.ParameterChannelNo = context?.ChannelNo ?? FirstText(record.ChannelNo);
            record.ParameterStandard = context?.Standard ?? FormatNormalizedPressure(record.ParameterPressure);
            record.ParameterMin = FirstText(context?.Min, record.PressSetLow.HasValue ? FormatNormalizedPressure(record.PressSetLow.Value) : null);
            record.ParameterMax = FirstText(context?.Max, record.PressSetUp.HasValue ? FormatNormalizedPressure(record.PressSetUp.Value) : null);
            record.ParameterLimit = context?.Limit ?? FormatHmiPressureLimit(record.PressSetLow, record.PressSetUp);
            record.Result = EvaluateWorkRecordResult(record);
        }
    }

    private static IEnumerable<LeakTestWorkRecord> FilterWorkRecordsByResult(
        IEnumerable<LeakTestWorkRecord> records,
        string? result)
    {
        var resultTerm = result?.Trim().ToUpperInvariant();
        return resultTerm is "OK" or "NG"
            ? records.Where(x => string.Equals(x.Result, resultTerm, StringComparison.OrdinalIgnoreCase))
            : records;
    }

    private static string EvaluateWorkRecordResult(LeakTestWorkRecord record)
    {
        var lowerLimit = ParsePressureValue(record.ParameterMin) ??
            (record.PressSetLow.HasValue ? NormalizeCosmoPressure(record.PressSetLow.Value) : null);
        var upperLimit = ParsePressureValue(record.ParameterMax) ??
            (record.PressSetUp.HasValue ? NormalizeCosmoPressure(record.PressSetUp.Value) : null);

        return EvaluateWorkRecordResult(record.PressureInput, lowerLimit, upperLimit);
    }

    private static string EvaluateWorkRecordResult(decimal pressureInput, decimal? lowerLimit, decimal? upperLimit)
    {
        var normalizedInput = NormalizeCosmoPressure(pressureInput);
        var normalizedLowerLimit = lowerLimit.HasValue ? NormalizeCosmoPressure(lowerLimit.Value) : (decimal?)null;
        var normalizedUpperLimit = upperLimit.HasValue ? NormalizeCosmoPressure(upperLimit.Value) : (decimal?)null;

        if (normalizedLowerLimit.HasValue && normalizedInput < normalizedLowerLimit.Value)
        {
            return "NG";
        }

        if (normalizedUpperLimit.HasValue && normalizedInput > normalizedUpperLimit.Value)
        {
            return "NG";
        }

        return "OK";
    }

    private static decimal? ParsePressureValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var chars = value
            .Trim()
            .TakeWhile(character => char.IsDigit(character) || character is '-' or '+' or '.' or ',')
            .ToArray();
        if (chars.Length == 0)
        {
            return null;
        }

        var numberText = new string(chars).Replace(',', '.');
        return decimal.TryParse(numberText, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? NormalizeCosmoPressure(parsed)
            : null;
    }

    private async Task HydrateWorkRecordJudgementsAsync(IReadOnlyCollection<LeakTestWorkRecord> records)
    {
        var judgementCodes = records
            .Select(x => x.JudgementCode)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        if (judgementCodes.Count == 0)
        {
            return;
        }

        await EnsureLeakTestJudgementTableAsync();
        var judgementMap = await _db.LeakTestJudgements
            .AsNoTracking()
            .Where(x => judgementCodes.Contains(x.JudgementCode) && x.IsDeleted != true)
            .Select(x => new { x.JudgementCode, x.JudgementName })
            .ToDictionaryAsync(x => x.JudgementCode, x => x.JudgementName);

        foreach (var record in records)
        {
            if (record.JudgementCode.HasValue &&
                judgementMap.TryGetValue(record.JudgementCode.Value, out var judgementName) &&
                !string.IsNullOrWhiteSpace(judgementName))
            {
                record.JudgementName = judgementName;
            }
        }
    }

    private async Task HydrateWorkRecordOperatorsAsync(IReadOnlyCollection<LeakTestWorkRecord> records)
    {
        var operatorTexts = records
            .Select(x => FirstText(x.OperatorName))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (operatorTexts.Count == 0)
        {
            return;
        }

        var operators = await _db.Operators
            .AsNoTracking()
            .Where(x => x.IsDeleted != true &&
                (operatorTexts.Contains(x.OperatorCode) || operatorTexts.Contains(x.OperatorName)))
            .Select(x => new { x.OperatorCode, x.OperatorName })
            .ToListAsync();

        foreach (var record in records)
        {
            var operatorText = FirstText(record.OperatorName);
            if (string.IsNullOrWhiteSpace(operatorText))
            {
                continue;
            }

            var matchedOperator = operators.FirstOrDefault(x =>
                string.Equals(x.OperatorCode, operatorText, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.OperatorName, operatorText, StringComparison.OrdinalIgnoreCase));

            if (matchedOperator is not null)
            {
                record.OperatorCode = matchedOperator.OperatorCode;
                record.OperatorName = matchedOperator.OperatorName;
                continue;
            }

            if (LooksLikeOperatorCode(operatorText))
            {
                record.OperatorCode = operatorText;
                record.OperatorName = null;
            }
        }
    }

    private static bool LooksLikeOperatorCode(string value)
    {
        return value.StartsWith("LT-OP-", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("HMI-", StringComparison.OrdinalIgnoreCase) ||
               value.All(char.IsDigit);
    }

    private async Task HydrateReworkEngineParameterContextAsync(IReadOnlyCollection<ReworkEngineRecord> records)
    {
        if (records.Count == 0)
        {
            return;
        }

        var parameters = await GetActiveLeakTestParametersAsync();
        foreach (var record in records)
        {
            var context = FindParameterContext(parameters, record.EngineModelName);
            record.ParameterChannelNo = context?.ChannelNo;
            record.ParameterStandard = context?.Standard;
            record.ParameterMin = context?.Min;
            record.ParameterMax = context?.Max;
            record.ParameterLimit = context?.Limit;
        }
    }

    private async Task<List<LeakTestParameter>> GetActiveLeakTestParametersAsync()
    {
        await EnsureLeakTestParameterTableAsync();
        return await _db.LeakTestParameters
            .AsNoTracking()
            .Where(x => x.IsDeleted != true)
            .ToListAsync();
    }

    private static LeakTestParameterContext? FindParameterContext(
        IReadOnlyList<LeakTestParameter> parameters,
        string engineModelName)
    {
        var modelKey = NormalizeModelKey(engineModelName);
        if (string.IsNullOrWhiteSpace(modelKey) || parameters.Count == 0)
        {
            return null;
        }

        var groups = parameters
            .Where(x => !string.IsNullOrWhiteSpace(x.ChannelNo))
            .GroupBy(x => x.ChannelNo.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToList();

        var machineMatch = groups.FirstOrDefault(group =>
            group.Any(parameter => MachineNamesContainModel(parameter.MachineNames, modelKey)));
        if (machineMatch is not null)
        {
            return BuildParameterContext(machineMatch);
        }

        var modelParameterMatch = groups
            .Select(group => new
            {
                Group = group,
                Score = ModelParameterScore(group, modelKey)
            })
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .FirstOrDefault();

        return modelParameterMatch is null
            ? null
            : BuildParameterContext(modelParameterMatch.Group);
    }

    private static LeakTestParameterContext BuildParameterContext(IEnumerable<LeakTestParameter> parameters)
    {
        var rows = parameters.ToList();
        var channelNo = rows.First().ChannelNo.Trim();
        var standard = FindParameterValue(rows, "pressure setting");
        var min = FindParameterValue(rows, "lower press limit");
        var max = FindParameterValue(rows, "upper press limit");

        return new LeakTestParameterContext(
            channelNo,
            standard,
            min,
            max,
            FormatParameterLimit(min, max));
    }

    private static string? FindParameterValue(IEnumerable<LeakTestParameter> parameters, string itemNameTerm)
    {
        return parameters
            .FirstOrDefault(x => NormalizeSpaces(x.ItemName).Contains(itemNameTerm, StringComparison.OrdinalIgnoreCase))
            ?.ItemValue;
    }

    private static bool MachineNamesContainModel(string? machineNames, string modelKey)
    {
        if (string.IsNullOrWhiteSpace(machineNames))
        {
            return false;
        }

        return machineNames
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeModelKey)
            .Any(machineKey => machineKey == modelKey);
    }

    private static int ModelParameterScore(IEnumerable<LeakTestParameter> parameters, string modelKey)
    {
        return parameters
            .SelectMany(parameter => parameter.ModelParameter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(NormalizeModelKey)
            .Where(parameterKey => !string.IsNullOrWhiteSpace(parameterKey) && modelKey.StartsWith(parameterKey, StringComparison.Ordinal))
            .Select(parameterKey => parameterKey.Length)
            .DefaultIfEmpty(0)
            .Max();
    }

    private static string NormalizeModelKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
    }

    private static string? FormatParameterLimit(string? minValue, string? maxValue)
    {
        var min = NormalizeSpaces(minValue);
        var max = NormalizeSpaces(maxValue);

        if (!string.IsNullOrWhiteSpace(min) && !string.IsNullOrWhiteSpace(max))
        {
            var (minAmount, minUnit) = SplitParameterValue(min);
            var (maxAmount, maxUnit) = SplitParameterValue(max);

            return !string.IsNullOrWhiteSpace(minUnit) &&
                   minUnit.Equals(maxUnit, StringComparison.OrdinalIgnoreCase)
                ? $"{minAmount} ~ {maxAmount} {minUnit}"
                : $"{min} ~ {max}";
        }

        if (!string.IsNullOrWhiteSpace(min))
        {
            return $"Min {min}";
        }

        if (!string.IsNullOrWhiteSpace(max))
        {
            return $"Max {max}";
        }

        return null;
    }

    private static (string Amount, string Unit) SplitParameterValue(string value)
    {
        var normalized = NormalizeSpaces(value);
        var lastSpaceIndex = normalized.LastIndexOf(' ');

        return lastSpaceIndex <= 0 || lastSpaceIndex >= normalized.Length - 1
            ? (normalized, string.Empty)
            : (normalized[..lastSpaceIndex].Trim(), normalized[(lastSpaceIndex + 1)..].Trim());
    }

    private IQueryable<LeakTestWorkRecord> WorkRecordQuery(
        DateTime? date,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? engineModel,
        string? engineNumber,
        string? barcodeScan)
    {
        IQueryable<LeakTestWorkRecord> query = _db.LeakTestWorkRecords.AsNoTracking()
            .Include(x => x.EngineModel);

        if (dateFrom.HasValue || dateTo.HasValue)
        {
            if (dateFrom.HasValue)
            {
                var startDate = dateFrom.Value.Date;
                query = query.Where(x => x.CheckDate >= startDate);
            }

            if (dateTo.HasValue)
            {
                var endDate = dateTo.Value.Date.AddDays(1);
                query = query.Where(x => x.CheckDate < endDate);
            }
        }
        else if (date.HasValue)
        {
            var selectedDate = date.Value.Date;
            var nextDate = selectedDate.AddDays(1);
            query = query.Where(x => x.CheckDate >= selectedDate && x.CheckDate < nextDate);
        }

        var (barcodeEngineModel, barcodeEngineNumber) = ParseBarcodeScan(barcodeScan);
        var barcodeTerm = NormalizeBarcodeScan(barcodeScan);
        var hasBarcodeEngineModel = !string.IsNullOrWhiteSpace(barcodeEngineModel);
        var hasBarcodeEngineNumber = !string.IsNullOrWhiteSpace(barcodeEngineNumber);
        var parsedBarcodeEngineModel = barcodeEngineModel ?? string.Empty;
        var parsedBarcodeEngineNumber = barcodeEngineNumber ?? string.Empty;

        var modelTerm = engineModel?.Trim();
        if (!string.IsNullOrWhiteSpace(modelTerm))
        {
            query = query.Where(x => x.EngineModel != null && x.EngineModel.ModelName.Contains(modelTerm));
        }

        var engineNumberTerm = engineNumber?.Trim();
        if (!string.IsNullOrWhiteSpace(engineNumberTerm))
        {
            query = query.Where(x => x.EngineNumber.Contains(engineNumberTerm));
        }

        if (!string.IsNullOrWhiteSpace(barcodeTerm))
        {
            query = hasBarcodeEngineModel && hasBarcodeEngineNumber
                ? query.Where(x =>
                    (x.BarcodeScan != null && x.BarcodeScan.Contains(barcodeTerm)) ||
                    (x.EngineModel != null &&
                        x.EngineModel.ModelName.Contains(parsedBarcodeEngineModel) &&
                        x.EngineNumber.Contains(parsedBarcodeEngineNumber)))
                : query.Where(x =>
                    (x.BarcodeScan != null && x.BarcodeScan.Contains(barcodeTerm)) ||
                    x.EngineNumber.Contains(barcodeTerm) ||
                    (x.EngineModel != null && x.EngineModel.ModelName.Contains(barcodeTerm)));
        }

        return query;
    }

    private IQueryable<ReworkEngineRecord> ReworkEngineRecordQuery(
        DateTime? date,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? engineModel,
        string? engineNumber,
        string? barcodeScan,
        string? result)
    {
        IQueryable<ReworkEngineRecord> query = _db.ReworkEngineRecords.AsNoTracking()
            .Include(x => x.EngineModel);

        if (dateFrom.HasValue || dateTo.HasValue)
        {
            if (dateFrom.HasValue)
            {
                var startDate = dateFrom.Value.Date;
                query = query.Where(x => x.ReworkDate >= startDate);
            }

            if (dateTo.HasValue)
            {
                var endDate = dateTo.Value.Date.AddDays(1);
                query = query.Where(x => x.ReworkDate < endDate);
            }
        }
        else if (date.HasValue)
        {
            var selectedDate = date.Value.Date;
            var nextDate = selectedDate.AddDays(1);
            query = query.Where(x => x.ReworkDate >= selectedDate && x.ReworkDate < nextDate);
        }

        var (barcodeEngineModel, barcodeEngineNumber) = ParseBarcodeScan(barcodeScan);
        var modelTerm = engineModel?.Trim();
        if (!string.IsNullOrWhiteSpace(modelTerm))
        {
            query = query.Where(x =>
                (x.EngineModel != null && x.EngineModel.ModelName.Contains(modelTerm)) ||
                (x.EngineModelText != null && x.EngineModelText.Contains(modelTerm)));
        }

        if (!string.IsNullOrWhiteSpace(barcodeEngineModel))
        {
            query = query.Where(x =>
                (x.EngineModel != null && x.EngineModel.ModelName.Contains(barcodeEngineModel)) ||
                (x.EngineModelText != null && x.EngineModelText.Contains(barcodeEngineModel)));
        }

        var engineNumberTerm = engineNumber?.Trim();
        if (!string.IsNullOrWhiteSpace(engineNumberTerm))
        {
            query = query.Where(x => x.EngineNumber.Contains(engineNumberTerm));
        }

        if (!string.IsNullOrWhiteSpace(barcodeEngineNumber))
        {
            query = query.Where(x => x.EngineNumber.Contains(barcodeEngineNumber));
        }

        var resultTerm = result?.Trim().ToUpperInvariant();
        if (resultTerm is "OK" or "NG")
        {
            query = query.Where(x => x.Result == resultTerm);
        }

        return query;
    }

    private async Task EnsureLeakTestParameterTableAsync()
    {
        await _db.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS leak_test_parameters (
    id INT AUTO_INCREMENT PRIMARY KEY,
    channel_no VARCHAR(20) NOT NULL,
    model_parameter VARCHAR(150) NOT NULL,
    item_name VARCHAR(120) NOT NULL,
    item_value VARCHAR(80) NOT NULL,
    machine_names VARCHAR(1000) NULL,
    is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_leak_test_parameters_channel_item (channel_no, item_name),
    KEY ix_leak_test_parameters_channel_no (channel_no),
    KEY ix_leak_test_parameters_model_parameter (model_parameter)
)");
    }

    private async Task EnsureLeakTestJudgementTableAsync()
    {
        await _db.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS leak_test_judgements (
    id INT AUTO_INCREMENT PRIMARY KEY,
    judgement_code INT NOT NULL,
    judgement_name VARCHAR(80) NOT NULL,
    result VARCHAR(10) NOT NULL,
    note VARCHAR(150) NULL,
    is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_leak_test_judgements_code (judgement_code),
    KEY ix_leak_test_judgements_result (result)
)");

        await _db.Database.ExecuteSqlRawAsync(@"
INSERT INTO leak_test_judgements
    (judgement_code, judgement_name, result, note, is_deleted)
VALUES
    (1, 'LL NG', 'NG', 'HMI judgement', 0),
    (2, 'PASS', 'OK', 'HMI judgement', 0),
    (3, 'UL NG', 'NG', 'HMI judgement', 0),
    (4, 'LL2 NG', 'NG', 'HMI judgement', 0),
    (5, 'UL2 NG', 'NG', 'HMI judgement', 0),
    (6, 'ERROR', 'NG', 'HMI judgement', 0),
    (7, '', '', '', 0),
    (8, '', '', '', 0),
    (9, '', '', '', 0),
    (10, '', '', '', 0)
ON DUPLICATE KEY UPDATE
    result = IF(is_deleted = 1 OR judgement_name LIKE 'DUMMY-%' OR judgement_name IN ('OK', 'NG'), VALUES(result), result),
    note = IF(is_deleted = 1 OR note LIKE 'Temporary dummy%' OR note IN ('Gateway judgement OK', 'Gateway judgement NG'), VALUES(note), note),
    is_deleted = VALUES(is_deleted),
    judgement_name = IF(is_deleted = 1 OR judgement_name LIKE 'DUMMY-%' OR judgement_name IN ('OK', 'NG'), VALUES(judgement_name), judgement_name),
    updated_at = CURRENT_TIMESTAMP");

        await _db.Database.ExecuteSqlRawAsync(@"
UPDATE leak_test_judgements
SET is_deleted = 1, updated_at = CURRENT_TIMESTAMP
WHERE judgement_code > 10");
    }

    private async Task SeedDefaultHmiJudgementsAsync()
    {
        await _db.Database.ExecuteSqlRawAsync(@"
INSERT INTO leak_test_judgements
    (judgement_code, judgement_name, result, note, is_deleted)
VALUES
    (1, 'LL NG', 'NG', 'HMI judgement', 0),
    (2, 'PASS', 'OK', 'HMI judgement', 0),
    (3, 'UL NG', 'NG', 'HMI judgement', 0),
    (4, 'LL2 NG', 'NG', 'HMI judgement', 0),
    (5, 'UL2 NG', 'NG', 'HMI judgement', 0),
    (6, 'ERROR', 'NG', 'HMI judgement', 0),
    (7, '', '', '', 0),
    (8, '', '', '', 0),
    (9, '', '', '', 0),
    (10, '', '', '', 0)
ON DUPLICATE KEY UPDATE
    result = IF(is_deleted = 1 OR judgement_name LIKE 'DUMMY-%' OR judgement_name IN ('OK', 'NG'), VALUES(result), result),
    note = IF(is_deleted = 1 OR note LIKE 'Temporary dummy%' OR note IN ('Gateway judgement OK', 'Gateway judgement NG'), VALUES(note), note),
    is_deleted = VALUES(is_deleted),
    judgement_name = IF(is_deleted = 1 OR judgement_name LIKE 'DUMMY-%' OR judgement_name IN ('OK', 'NG'), VALUES(judgement_name), judgement_name),
    updated_at = CURRENT_TIMESTAMP");

        await _db.Database.ExecuteSqlRawAsync(@"
UPDATE leak_test_judgements
SET is_deleted = 1, updated_at = CURRENT_TIMESTAMP
WHERE judgement_code > 10");
    }

    private static List<ParameterExcelRow> ReadParameterRowsFromExcel(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var rows = new List<ParameterExcelRow>();
        string? currentChannelNo = null;
        string? currentModelParameter = null;
        string? currentMachineNames = null;

        for (var rowNumber = 1; rowNumber <= lastRow; rowNumber++)
        {
            var channelNo = CellText(worksheet, rowNumber, 1);
            var modelParameter = CellText(worksheet, rowNumber, 2);
            var itemName = CellText(worksheet, rowNumber, 3);
            var itemValue = CellText(worksheet, rowNumber, 4);

            if (IsHeaderCell(channelNo) || IsHeaderCell(itemName))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(channelNo))
            {
                currentChannelNo = channelNo;
                currentModelParameter = modelParameter;
                currentMachineNames = ReadMachineNames(worksheet, rowNumber);
            }

            if (string.IsNullOrWhiteSpace(currentChannelNo) ||
                string.IsNullOrWhiteSpace(itemName) ||
                string.IsNullOrWhiteSpace(itemValue))
            {
                continue;
            }

            rows.Add(new ParameterExcelRow(
                TrimTo(currentChannelNo, 20),
                TrimTo(currentModelParameter ?? string.Empty, 150),
                TrimTo(itemName, 120),
                TrimTo(itemValue, 80),
                string.IsNullOrWhiteSpace(currentMachineNames) ? null : TrimTo(currentMachineNames, 1000)));
        }

        return rows;
    }

    private static string ReadMachineNames(IXLWorksheet worksheet, int rowNumber)
    {
        var lastColumn = worksheet.Row(rowNumber).LastCellUsed()?.Address.ColumnNumber ?? 5;
        var names = new List<string>();

        for (var column = 5; column <= lastColumn; column++)
        {
            var value = CellText(worksheet, rowNumber, column);
            if (!string.IsNullOrWhiteSpace(value))
            {
                names.Add(value);
            }
        }

        return string.Join(", ", names);
    }

    private static string CellText(IXLWorksheet worksheet, int rowNumber, int columnNumber)
    {
        var value = worksheet.Cell(rowNumber, columnNumber).GetFormattedString();
        return NormalizeSpaces(value);
    }

    private static string NormalizeSpaces(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static bool IsHeaderCell(string value)
    {
        return value.Equals("CHANNEL NO", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("ITEM NAME", StringComparison.OrdinalIgnoreCase);
    }

    private static string ParameterKey(string channelNo, string itemName)
    {
        return $"{channelNo.Trim().ToUpperInvariant()}|{itemName.Trim().ToUpperInvariant()}";
    }

    private static string TrimTo(string value, int maxLength)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private sealed record ParameterExcelRow(
        string ChannelNo,
        string ModelParameter,
        string ItemName,
        string ItemValue,
        string? MachineNames);

    private sealed record LeakTestParameterContext(
        string ChannelNo,
        string? Standard,
        string? Min,
        string? Max,
        string? Limit);

    private static (string? EngineModel, string? EngineNumber) ParseBarcodeScan(string? barcodeScan)
    {
        if (string.IsNullOrWhiteSpace(barcodeScan))
        {
            return (null, null);
        }

        var normalized = NormalizeBarcodeScan(barcodeScan);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return (null, null);
        }

        var separatorIndex = normalized.IndexOfAny(new[] { ' ', '\t', '\r', '\n' });
        if (separatorIndex < 0)
        {
            return (string.IsNullOrWhiteSpace(normalized) ? null : normalized, null);
        }

        var engineModel = normalized[..separatorIndex].Trim();
        var engineNumber = normalized[(separatorIndex + 1)..].Trim();
        return (
            string.IsNullOrWhiteSpace(engineModel) ? null : engineModel,
            string.IsNullOrWhiteSpace(engineNumber) ? null : engineNumber);
    }

    private static string NormalizeCheckTime(string checkTime)
    {
        var trimmed = checkTime.Trim();
        return trimmed.Length == 5 ? $"{trimmed}:00" : trimmed;
    }

    private async Task EnsureSystemSettingsTablesAsync()
    {
        await _db.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS measurement_units (
    id INT AUTO_INCREMENT PRIMARY KEY,
    unit_category VARCHAR(50) NOT NULL,
    unit_symbol VARCHAR(20) NOT NULL,
    unit_name VARCHAR(80) NOT NULL,
    is_deleted TINYINT(1) NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_measurement_units_category_symbol (unit_category, unit_symbol)
)");

        await _db.Database.ExecuteSqlRawAsync(@"
INSERT INTO measurement_units
    (unit_category, unit_symbol, unit_name, is_deleted)
VALUES
    ('pressure', 'MPa', 'Megapascal', 0),
    ('cycle_time', 's', 'Second', 0)
ON DUPLICATE KEY UPDATE
    unit_name = VALUES(unit_name),
    is_deleted = VALUES(is_deleted),
    updated_at = CURRENT_TIMESTAMP");

        await _db.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS system_settings (
    id INT PRIMARY KEY,
    pressure_unit_id INT NOT NULL,
    cycle_time_unit_id INT NOT NULL,
    backup_db_location VARCHAR(500) NULL,
    backup_schedule VARCHAR(20) NOT NULL DEFAULT 'daily',
    plc_ip_address VARCHAR(80) NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_system_settings_pressure_unit
        FOREIGN KEY (pressure_unit_id) REFERENCES measurement_units (id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,
    CONSTRAINT fk_system_settings_cycle_time_unit
        FOREIGN KEY (cycle_time_unit_id) REFERENCES measurement_units (id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
)");

        await EnsureColumnAsync(
            "system_settings",
            "plc_ip_address",
            "ALTER TABLE system_settings ADD COLUMN plc_ip_address VARCHAR(80) NULL AFTER backup_schedule");

        await EnsureDefaultSystemSettingAsync();
    }

    private async Task EnsureDefaultSystemSettingAsync()
    {
        var exists = await _db.SystemSettings.AsNoTracking().AnyAsync(x => x.Id == 1);
        if (exists)
        {
            return;
        }

        var pressureUnitId = await FindOrCreateMeasurementUnitAsync("pressure", "MPa", "Megapascal");
        var cycleTimeUnitId = await FindOrCreateMeasurementUnitAsync("cycle_time", "s", "Second");

        _db.SystemSettings.Add(new SystemSetting
        {
            Id = 1,
            PressureUnitId = pressureUnitId,
            CycleTimeUnitId = cycleTimeUnitId,
            BackupSchedule = "daily",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        });
        await _db.SaveChangesAsync();
    }

    private async Task<SystemSettingsResponse> GetSystemSettingsResponseAsync()
    {
        var setting = await _db.SystemSettings
            .AsNoTracking()
            .Include(x => x.PressureUnit)
            .Include(x => x.CycleTimeUnit)
            .FirstOrDefaultAsync(x => x.Id == 1);

        return new SystemSettingsResponse
        {
            PressureUnit = setting?.PressureUnit?.UnitSymbol ?? "MPa",
            CycleTimeUnit = setting?.CycleTimeUnit?.UnitSymbol ?? "s",
            BackupDbLocation = setting?.BackupDbLocation ?? string.Empty,
            BackupSchedule = setting?.BackupSchedule ?? "daily",
            PlcIpAddress = setting?.PlcIpAddress ?? string.Empty
        };
    }

    private static async Task<bool> CheckPlcReachableAsync(string plcIpAddress)
    {
        if (string.IsNullOrWhiteSpace(plcIpAddress))
        {
            return false;
        }

        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(plcIpAddress, 1000);
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }

    private async Task<int> FindOrCreateMeasurementUnitAsync(string category, string? symbol, string? name)
    {
        var unitSymbol = string.IsNullOrWhiteSpace(symbol) ? (category == "pressure" ? "MPa" : "s") : TrimTo(symbol, 20);
        var unitName = string.IsNullOrWhiteSpace(name) ? unitSymbol : TrimTo(name, 80);

        var existing = await _db.MeasurementUnits
            .FirstOrDefaultAsync(x => x.UnitCategory == category && x.UnitSymbol == unitSymbol);
        if (existing is not null)
        {
            if (existing.IsDeleted == true)
            {
                existing.IsDeleted = false;
                existing.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();
            }

            return existing.Id;
        }

        var unit = new MeasurementUnit
        {
            UnitCategory = category,
            UnitSymbol = unitSymbol,
            UnitName = unitName,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        _db.MeasurementUnits.Add(unit);
        await _db.SaveChangesAsync();
        return unit.Id;
    }

    private static string NormalizeBackupSchedule(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "weekly" => "weekly",
            "monthly" => "monthly",
            _ => "daily"
        };
    }
}
