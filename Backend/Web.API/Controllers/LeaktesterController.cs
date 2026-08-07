using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
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
        var records = await WorkRecordQuery(date, dateFrom, dateTo, engineModel, engineNumber, barcodeScan, result)
            .OrderByDescending(x => x.CheckDate)
            .ThenByDescending(x => x.CheckTime)
            .ThenByDescending(x => x.Id)
            .Take(500)
            .ToListAsync();
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
            var records = await WorkRecordQuery(date, dateFrom, dateTo, engineModel, engineNumber, barcodeScan, result)
                .OrderByDescending(x => x.CheckDate)
                .ThenByDescending(x => x.CheckTime)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

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
            var record = await _db.LeakTestWorkRecords.AsNoTracking()
                .Include(x => x.EngineModel)
                .Include(x => x.Operator)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (record is null)
            {
                return ApiNotFound("Leak test work record was not found.");
            }

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
                CheckDate = request.CheckDate.Date,
                CheckTime = NormalizeCheckTime(request.CheckTime),
                MachineName = request.MachineName.Trim(),
                OperatorId = operatorItem?.Id,
                ParameterPressure = request.ParameterPressure,
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
            return ApiCreated(record, "Leak test work record saved successfully.");
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
        var modelTerm = engineModel?.Trim();
        if (!string.IsNullOrWhiteSpace(modelTerm))
        {
            query = query.Where(x => x.EngineModel != null && x.EngineModel.ModelName.Contains(modelTerm));
        }

        if (!string.IsNullOrWhiteSpace(barcodeEngineModel))
        {
            query = query.Where(x => x.EngineModel != null && x.EngineModel.ModelName.Contains(barcodeEngineModel));
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
