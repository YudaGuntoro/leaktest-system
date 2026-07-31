using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        [FromQuery(Name = "date_to")] DateTime? dateTo)
    {
        var records = await WorkRecordQuery(date, dateFrom, dateTo)
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
        [FromQuery(Name = "date_to")] DateTime? dateTo)
    {
        try
        {
            var records = await WorkRecordQuery(date, dateFrom, dateTo)
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

    [HttpGet("work-records/{id:long}/export")]
    [Produces(LeakTestWorkRecordReportBuilder.ContentType)]
    public async Task<IActionResult> ExportWorkRecord(long id)
    {
        try
        {
            var record = await _db.LeakTestWorkRecords.AsNoTracking()
                .Include(x => x.EngineModel)
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
            return ApiCreated(record, "Leak test work record saved successfully.");
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

    private IQueryable<LeakTestWorkRecord> WorkRecordQuery(DateTime? date, DateTime? dateFrom, DateTime? dateTo)
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

        return query;
    }

    private static string NormalizeCheckTime(string checkTime)
    {
        var trimmed = checkTime.Trim();
        return trimmed.Length == 5 ? $"{trimmed}:00" : trimmed;
    }
}
