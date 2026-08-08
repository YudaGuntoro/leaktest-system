using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.Globalization;
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

        var records = await WorkRecordQuery(date, dateFrom, dateTo, engineModel, engineNumber, barcodeScan, result)
            .OrderByDescending(x => x.CheckDate)
            .ThenByDescending(x => x.CheckTime)
            .ThenByDescending(x => x.Id)
            .Take(500)
            .ToListAsync();

        await HydrateWorkRecordParameterContextAsync(records);
        return ApiOk(records);
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

            var records = await WorkRecordQuery(date, dateFrom, dateTo, engineModel, engineNumber, barcodeScan, result)
                .OrderByDescending(x => x.CheckDate)
                .ThenByDescending(x => x.CheckTime)
                .ThenByDescending(x => x.Id)
                .ToListAsync();
            await HydrateWorkRecordParameterContextAsync(records);

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
            .Where(x => x.CheckDate >= startDate && x.CheckDate < endDate)
            .Select(x => new
            {
                x.CheckDate,
                x.EngineNumber,
                x.Result
            })
            .ToListAsync();

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
                .Include(x => x.Operator)
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

            Operator? operatorItem = null;
            if (request.OperatorId is > 0)
            {
                operatorItem = await _db.Operators
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == request.OperatorId.Value && x.IsDeleted != true);
                if (operatorItem is null)
                {
                    throw new ArgumentException("Operator was not found or is inactive.");
                }
            }

            var result = request.Result.Trim().ToUpperInvariant();
            if (result is not ("OK" or "NG"))
            {
                throw new ArgumentException("Result must be OK or NG.");
            }

            var record = new LeakTestWorkRecord
            {
                EngineModelId = engineModel.Id,
                EngineNumber = request.EngineNumber.Trim(),
                BarcodeScan = FirstText(request.BarcodeScan, BuildBarcodeScan(engineModel.ModelName, request.EngineNumber)),
                CheckDate = request.CheckDate.Date,
                CheckTime = NormalizeCheckTime(request.CheckTime),
                MachineName = request.MachineName.Trim(),
                OperatorId = operatorItem?.Id,
                ParameterPressure = request.ParameterPressure,
                ChannelNo = string.IsNullOrWhiteSpace(request.ChannelNo) ? null : TrimTo(request.ChannelNo, 20),
                PressSetUp = request.PressSetUp,
                PressSetLow = request.PressSetLow,
                PressureInput = request.PressureInput,
                CycleTimeLeakTestMinutes = request.CycleTimeLeakTestMinutes,
                Result = result,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _db.LeakTestWorkRecords.Add(record);
            await _db.SaveChangesAsync();
            record.EngineModel = engineModel;
            record.Operator = operatorItem;
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

            var barcode = FirstText(request.BarcodeScan, request.Barcode);
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

            var result = NormalizeResult(request.Judgement);
            if (result is null)
            {
                throw new ArgumentException("Judgement must be OK or NG.");
            }

            var engineModel = await FindOrCreateEngineModelAsync(engineModelText);
            var operatorItem = await FindOrCreateOperatorAsync(request.Operator);
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
                OperatorId = operatorItem?.Id,
                ParameterPressure = parameterPressure,
                ChannelNo = string.IsNullOrWhiteSpace(request.ChannelNo) ? null : TrimTo(request.ChannelNo, 20),
                PressSetUp = request.PressSetUp,
                PressSetLow = request.PressSetLow,
                PressureInput = request.PressureInput,
                CycleTimeLeakTestMinutes = request.CycleTime,
                Result = result,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _db.LeakTestWorkRecords.Add(record);
            await _db.SaveChangesAsync();
            record.EngineModel = engineModel;
            record.Operator = operatorItem;
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
            var record = await _db.ReworkEngineRecords.AsNoTracking()
                .Include(x => x.EngineModel)
                .Include(x => x.Operator)
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

            Operator? operatorItem = null;
            if (request.OperatorId is > 0)
            {
                operatorItem = await _db.Operators
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == request.OperatorId.Value && x.IsDeleted != true);
                if (operatorItem is null)
                {
                    throw new ArgumentException("Operator was not found or is inactive.");
                }
            }

            var (barcodeEngineModel, barcodeEngineNumber) = ParseBarcodeScan(request.BarcodeScan);
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
                BarcodeScan = request.BarcodeScan.Trim(),
                ReworkDate = request.ReworkDate.Date,
                ReworkTime = NormalizeCheckTime(request.ReworkTime),
                OperatorId = operatorItem?.Id,
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
            record.Operator = operatorItem;
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
            if (string.IsNullOrWhiteSpace(request.OperatorCode))
            {
                throw new ArgumentException("Operator code is required.");
            }

            if (string.IsNullOrWhiteSpace(request.OperatorName))
            {
                throw new ArgumentException("Operator name is required.");
            }

            var item = new Operator
            {
                OperatorCode = request.OperatorCode.Trim(),
                OperatorName = request.OperatorName.Trim(),
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
        await EnsureIndexAsync(
            "leak_test_work_records",
            "ix_leak_test_work_records_barcode_scan",
            "CREATE INDEX ix_leak_test_work_records_barcode_scan ON leak_test_work_records (barcode_scan)");
        await EnsureIndexAsync(
            "leak_test_work_records",
            "ix_leak_test_work_records_channel_no",
            "CREATE INDEX ix_leak_test_work_records_channel_no ON leak_test_work_records (channel_no)");
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
        var model = engineModel?.Trim();
        var serial = serialNo?.Trim();

        if (string.IsNullOrWhiteSpace(model) || string.IsNullOrWhiteSpace(serial))
        {
            return null;
        }

        return TrimTo($"{model} {serial}", 180);
    }

    private static string? NormalizeResult(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToUpperInvariant() switch
        {
            "OK" or "PASS" or "PASSED" or "TRUE" or "1" => "OK",
            "NG" or "NOK" or "FAIL" or "FAILED" or "FALSE" or "0" => "NG",
            _ => null
        };
    }

    private async Task HydrateWorkRecordParameterContextAsync(IReadOnlyCollection<LeakTestWorkRecord> records)
    {
        if (records.Count == 0)
        {
            return;
        }

        var parameters = await GetActiveLeakTestParametersAsync();
        foreach (var record in records)
        {
            var context = FindParameterContext(parameters, record.EngineModelName);
            record.BarcodeScan = FirstText(record.BarcodeScan, BuildBarcodeScan(record.EngineModelName, record.EngineNumber));
            record.ParameterChannelNo = context?.ChannelNo ?? FirstText(record.ChannelNo);
            record.ParameterStandard = context?.Standard ?? FormatNormalizedPressure(record.ParameterPressure);
            record.ParameterMin = context?.Min ?? (record.PressSetLow.HasValue ? FormatNormalizedPressure(record.PressSetLow.Value) : null);
            record.ParameterMax = context?.Max ?? (record.PressSetUp.HasValue ? FormatNormalizedPressure(record.PressSetUp.Value) : null);
            record.ParameterLimit = context?.Limit ?? FormatHmiPressureLimit(record.PressSetLow, record.PressSetUp);
        }
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
        string? barcodeScan,
        string? result)
    {
        IQueryable<LeakTestWorkRecord> query = _db.LeakTestWorkRecords.AsNoTracking()
            .Include(x => x.EngineModel)
            .Include(x => x.Operator);

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
        var barcodeTerm = barcodeScan?.Trim();
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

        var resultTerm = result?.Trim().ToUpperInvariant();
        if (resultTerm is "OK" or "NG")
        {
            query = query.Where(x => x.Result == resultTerm);
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
            .Include(x => x.EngineModel)
            .Include(x => x.Operator);

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

        var normalized = barcodeScan.Trim().TrimStart('.');
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
}
