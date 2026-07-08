using ProductionControl.Domain.Production;
using ProductionControl.Persistence.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ProductionControl.WebAPI.Controllers;

[ApiController]
[Route("api/production")]
public class ProductionControlController : ApiControllerBase
{
    private readonly ProductionControlDbContext _db;

    public ProductionControlController(ProductionControlDbContext db) => _db = db;

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] DateTime? date)
    {
        var selectedDate = (date ?? DateTime.Today).Date;
        var nextDate = selectedDate.AddDays(1);
        var orders = await WorkOrderQuery()
            .Where(x => x.CuttingList!.PlanDate >= selectedDate && x.CuttingList.PlanDate < nextDate)
            .OrderBy(x => x.LineCode)
            .ThenBy(x => x.WoNumber)
            .ToListAsync();

        var completedOrders = orders.Where(x => x.CompletedAt.HasValue).ToList();
        var actualQty = completedOrders.Sum(x => x.ActualQty);
        return ApiOk(new ProductionDashboardSummary
        {
            TotalWorkOrders = orders.Count,
            WaitingWorkOrders = orders.Count(x => x.Status is ProductionWorkOrderStatus.WAITING or ProductionWorkOrderStatus.READY),
            RunningWorkOrders = orders.Count(x => x.Status is ProductionWorkOrderStatus.IN_PROGRESS or ProductionWorkOrderStatus.HOLD),
            CompletedWorkOrders = orders.Count(x => x.Status == ProductionWorkOrderStatus.COMPLETED),
            ActualQty = actualQty,
            RejectQty = completedOrders.Sum(x => x.RejectQty),
            WorkOrders = orders.Select(ToResponse).ToList()
        });
    }

    [HttpGet("work-orders")]
    public async Task<IActionResult> WorkOrders([FromQuery] ProductionWorkOrderStatus? status, [FromQuery] DateTime? date)
    {
        var query = WorkOrderQuery();
        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (date.HasValue)
        {
            var selectedDate = date.Value.Date;
            var nextDate = selectedDate.AddDays(1);
            query = query.Where(x => x.CuttingList!.PlanDate >= selectedDate && x.CuttingList.PlanDate < nextDate);
        }

        var items = await query.OrderByDescending(x => x.UpdatedAt).ToListAsync();
        return ApiOk(items.Select(ToResponse).ToList());
    }

    [HttpPost("work-orders")]
    public async Task<IActionResult> CreateWorkOrder([FromBody] CreateProductionWorkOrderRequest request)
    {
        try
        {
            var cuttingList = await _db.CuttingLists.FindAsync(request.CuttingListId);
            if (cuttingList is null)
            {
                return ApiNotFound("Cutting list was not found.");
            }

            if (string.IsNullOrWhiteSpace(request.WoNumber))
            {
                throw new ArgumentException("Work order number is required.");
            }

            if (await _db.ProductionWorkOrders.AnyAsync(x => x.WoNumber == request.WoNumber.Trim()))
            {
                throw new InvalidOperationException("Work order number is already used.");
            }

            if (await _db.ProductionWorkOrders.AnyAsync(x => x.CuttingListId == cuttingList.Id))
            {
                throw new InvalidOperationException("This cutting list already has a work order.");
            }

            var order = new ProductionWorkOrder
            {
                WoNumber = request.WoNumber.Trim(),
                CuttingListId = cuttingList.Id,
                LineCode = string.IsNullOrWhiteSpace(request.LineCode) ? cuttingList.LineCode : request.LineCode.Trim(),
                TargetQty = 0,
                Status = ProductionWorkOrderStatus.WAITING
            };
            _db.ProductionWorkOrders.Add(order);
            await _db.SaveChangesAsync();
            await ReloadWorkOrder(order);
            return ApiCreated(ToResponse(order), "Work order created successfully.");
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [HttpPost("work-orders/scan")]
    public async Task<IActionResult> ScanWorkOrder([FromBody] ScanWorkOrderRequest request)
    {
        try
        {
            var code = request.Code.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Work order QR code is required.");
            }

            var existingOrder = await _db.ProductionWorkOrders
                .Include(x => x.CuttingList)
                .Include(x => x.PicCard)
                .Include(x => x.Operators)
                    .ThenInclude(x => x.PicCard)
                .FirstOrDefaultAsync(x => x.WoNumber == code || x.CuttingList!.CuttingListNo == code);

            if (existingOrder is not null)
            {
                AddLog(existingOrder.Id, existingOrder.PicCardId, ProductionActivityType.CUTTING_LIST_SCAN, $"WO scan {code}");
                await _db.SaveChangesAsync();
                return ApiOk(ToResponse(existingOrder), "Work order found.");
            }

            var cuttingList = await _db.CuttingLists.FirstOrDefaultAsync(x => x.CuttingListNo == code);
            if (cuttingList is null)
            {
                return ApiNotFound("Work order / cutting list QR was not found.");
            }

            var order = new ProductionWorkOrder
            {
                WoNumber = cuttingList.CuttingListNo,
                CuttingListId = cuttingList.Id,
                LineCode = cuttingList.LineCode,
                TargetQty = 0,
                Status = ProductionWorkOrderStatus.WAITING
            };

            _db.ProductionWorkOrders.Add(order);
            await _db.SaveChangesAsync();
            await ReloadWorkOrder(order);
            AddLog(order.Id, null, ProductionActivityType.CUTTING_LIST_SCAN, $"WO scan {code}");
            await _db.SaveChangesAsync();

            return ApiCreated(ToResponse(order), "Work order created from the cutting list.");
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [HttpPost("work-orders/{id:int}/scan-pic")]
    public async Task<IActionResult> ScanPic(int id, [FromBody] ScanPicRequest request)
    {
        try
        {
            var order = await FindWorkOrder(id);
            if (order is null)
            {
                return ApiNotFound("Work order was not found.");
            }

            var cardUid = request.CardUid.Trim();
            var pic = await _db.PicCards.FirstOrDefaultAsync(x => x.CardUid == cardUid && x.IsActive);
            if (pic is null)
            {
                return ApiNotFound("PIC is not registered or the card is inactive.");
            }

            if (order.Status is ProductionWorkOrderStatus.COMPLETED or ProductionWorkOrderStatus.CANCELLED)
            {
                throw new InvalidOperationException("Operators cannot be added to a finished or canceled work order.");
            }

            var activeOperator = order.Operators.FirstOrDefault(x => x.IsActive && x.PicCardId == pic.Id);
            if (activeOperator is null)
            {
                activeOperator = new ProductionWorkOrderOperator
                {
                    ProductionWorkOrderId = order.Id,
                    PicCardId = pic.Id,
                    IsActive = true,
                    ScannedAt = DateTime.Now
                };
                activeOperator.PicCard = pic;
                order.Operators.Add(activeOperator);
                _db.ProductionWorkOrderOperators.Add(activeOperator);
            }
            else
            {
                activeOperator.ScannedAt = DateTime.Now;
            }

            order.PicCardId ??= pic.Id;
            if (order.Status == ProductionWorkOrderStatus.WAITING)
            {
                order.Status = ProductionWorkOrderStatus.READY;
            }

            order.UpdatedAt = DateTime.Now;
            pic.LastScannedAt = DateTime.Now;
            AddLog(order.Id, pic.Id, ProductionActivityType.PIC_SCAN, $"Operator {pic.EmployeeNo} - {pic.FullName}");
            await _db.SaveChangesAsync();
            await ReloadWorkOrder(order);
            return ApiOk(ToResponse(order), "Operator added successfully.");
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [HttpPost("work-orders/{id:int}/operators/{operatorId:long}/remove")]
    public async Task<IActionResult> RemoveOperator(int id, long operatorId)
    {
        try
        {
            var order = await FindWorkOrder(id);
            if (order is null)
            {
                return ApiNotFound("Work order was not found.");
            }

            if (order.Status is ProductionWorkOrderStatus.COMPLETED or ProductionWorkOrderStatus.CANCELLED)
            {
                throw new InvalidOperationException("Operators cannot be removed from a finished or canceled work order.");
            }

            var activeOperator = order.Operators.FirstOrDefault(x => x.Id == operatorId && x.IsActive);
            if (activeOperator is null)
            {
                return ApiNotFound("Active operator was not found on this work order.");
            }

            activeOperator.IsActive = false;
            activeOperator.RemovedAt = DateTime.Now;
            order.UpdatedAt = DateTime.Now;

            var nextPrimaryOperator = order.Operators
                .Where(x => x.IsActive && x.Id != operatorId)
                .OrderBy(x => x.ScannedAt)
                .FirstOrDefault();
            order.PicCardId = nextPrimaryOperator?.PicCardId;

            if (!order.PicCardId.HasValue && order.Status == ProductionWorkOrderStatus.READY)
            {
                order.Status = ProductionWorkOrderStatus.WAITING;
            }

            AddLog(order.Id, activeOperator.PicCardId, ProductionActivityType.OPERATOR_REMOVE, $"Operator {activeOperator.PicCard?.EmployeeNo} - {activeOperator.PicCard?.FullName} removed");
            await _db.SaveChangesAsync();
            await ReloadWorkOrder(order);
            return ApiOk(ToResponse(order), "Operator removed successfully.");
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [HttpPost("work-orders/{id:int}/start")]
    public async Task<IActionResult> Start(int id)
    {
        try
        {
            var order = await FindWorkOrder(id);
            if (order is null)
            {
                return ApiNotFound("Work order was not found.");
            }

            if (!order.Operators.Any(x => x.IsActive))
            {
                throw new InvalidOperationException("Scan at least one operator before starting the work order.");
            }

            if (order.Status is ProductionWorkOrderStatus.COMPLETED or ProductionWorkOrderStatus.CANCELLED)
            {
                throw new InvalidOperationException("A finished or canceled work order cannot be started.");
            }

            order.Status = ProductionWorkOrderStatus.IN_PROGRESS;
            order.StartedAt ??= DateTime.Now;
            order.UpdatedAt = DateTime.Now;
            order.CuttingList!.Status = CuttingListStatus.IN_PROGRESS;
            AddLog(order.Id, order.PicCardId, ProductionActivityType.WORK_START, "Production work started");
            await _db.SaveChangesAsync();
            return ApiOk(ToResponse(order), "Work order started.");
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [HttpPost("work-orders/{id:int}/progress")]
    public async Task<IActionResult> UpdateProgress(int id, [FromBody] UpdateProductionRequest request)
    {
        try
        {
            var order = await FindWorkOrder(id);
            if (order is null)
            {
                return ApiNotFound("Work order was not found.");
            }

            if (order.Status != ProductionWorkOrderStatus.IN_PROGRESS)
            {
                throw new InvalidOperationException("Output can only be updated while the work order is running.");
            }

            var actualQty = request.ActualQty.GetValueOrDefault(order.ActualQty);
            var rejectQty = request.RejectQty.GetValueOrDefault(0);

            if (actualQty < 0 || rejectQty < 0)
            {
                throw new ArgumentException("Quantity cannot be negative.");
            }

            order.ActualQty = actualQty;
            order.RejectQty = rejectQty;
            order.UpdatedAt = DateTime.Now;
            AddLog(order.Id, order.PicCardId, ProductionActivityType.PRODUCTION_UPDATE, request.Remarks ?? $"Actual {actualQty}, reject {rejectQty}");
            await _db.SaveChangesAsync();
            return ApiOk(ToResponse(order), "Production output updated.");
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [HttpPost("work-orders/{id:int}/complete")]
    public async Task<IActionResult> Complete(int id, [FromBody] UpdateProductionRequest? request)
    {
        try
        {
            var order = await FindWorkOrder(id);
            if (order is null)
            {
                return ApiNotFound("Work order was not found.");
            }

            if (order.Status != ProductionWorkOrderStatus.IN_PROGRESS)
            {
                throw new InvalidOperationException("Work order must be IN_PROGRESS before it can be completed.");
            }

            if (request is not null)
            {
                var actualQty = request.ActualQty.GetValueOrDefault(order.ActualQty);
                var rejectQty = request.RejectQty.GetValueOrDefault(0);
                if (actualQty < 0 || rejectQty < 0)
                {
                    throw new ArgumentException("Quantity cannot be negative.");
                }

                order.ActualQty = actualQty;
                order.RejectQty = rejectQty;
            }

            order.Status = ProductionWorkOrderStatus.COMPLETED;
            order.CompletedAt = DateTime.Now;
            order.UpdatedAt = DateTime.Now;
            order.CuttingList!.Status = CuttingListStatus.COMPLETED;
            AddLog(order.Id, order.PicCardId, ProductionActivityType.WORK_COMPLETE, request?.Remarks ?? "Production work completed");
            await _db.SaveChangesAsync();
            return ApiOk(ToResponse(order), "Work order completed.");
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [HttpPost("work-orders/{id:int}/finish")]
    public async Task<IActionResult> Finish(int id)
    {
        try
        {
            var order = await FindWorkOrder(id);
            if (order is null)
            {
                return ApiNotFound("Work order was not found.");
            }

            if (order.Status != ProductionWorkOrderStatus.IN_PROGRESS)
            {
                throw new InvalidOperationException("Work order must be IN_PROGRESS before finish.");
            }

            order.Status = ProductionWorkOrderStatus.COMPLETED;
            order.CompletedAt = DateTime.Now;
            order.UpdatedAt = DateTime.Now;
            order.CuttingList!.Status = CuttingListStatus.COMPLETED;
            AddLog(order.Id, order.PicCardId, ProductionActivityType.WORK_COMPLETE, "Production work finished");
            await _db.SaveChangesAsync();
            return ApiOk(ToResponse(order), "Work order finished.");
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [HttpPost("work-orders/{id:int}/cancel-finish")]
    public async Task<IActionResult> CancelFinish(int id)
    {
        try
        {
            var order = await FindWorkOrder(id);
            if (order is null)
            {
                return ApiNotFound("Work order was not found.");
            }

            if (order.Status != ProductionWorkOrderStatus.COMPLETED || !order.CompletedAt.HasValue)
            {
                throw new InvalidOperationException("Work order is not finished or can no longer be canceled.");
            }

            order.Status = ProductionWorkOrderStatus.IN_PROGRESS;
            order.CompletedAt = null;
            order.UpdatedAt = DateTime.Now;
            order.CuttingList!.Status = CuttingListStatus.IN_PROGRESS;
            AddLog(order.Id, order.PicCardId, ProductionActivityType.WORK_RESUME, "Production finish cancelled");
            await _db.SaveChangesAsync();
            return ApiOk(ToResponse(order), "Finish canceled.");
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [HttpGet("cutting-lists")]
    public async Task<IActionResult> CuttingLists([FromQuery] DateTime? date)
    {
        var query = _db.CuttingLists.AsNoTracking();
        if (date.HasValue)
        {
            var selectedDate = date.Value.Date;
            var nextDate = selectedDate.AddDays(1);
            query = query.Where(x => x.PlanDate >= selectedDate && x.PlanDate < nextDate);
        }

        return ApiOk(await query.OrderByDescending(x => x.PlanDate).ThenBy(x => x.LineCode).ToListAsync());
    }

    [HttpPost("cutting-lists")]
    public async Task<IActionResult> CreateCuttingList([FromBody] CreateCuttingListRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CuttingListNo) ||
                string.IsNullOrWhiteSpace(request.ProductCode) ||
                string.IsNullOrWhiteSpace(request.ProductName) ||
                string.IsNullOrWhiteSpace(request.LineCode))
            {
                throw new ArgumentException("Cutting list number, product, and line are required.");
            }

            if (await _db.CuttingLists.AnyAsync(x => x.CuttingListNo == request.CuttingListNo.Trim()))
            {
                throw new InvalidOperationException("Cutting list number is already used.");
            }

            var item = new CuttingList
            {
                CuttingListNo = request.CuttingListNo.Trim(),
                ProductCode = request.ProductCode.Trim(),
                ProductName = request.ProductName.Trim(),
                LineCode = request.LineCode.Trim(),
                PlannedQty = Math.Max(0, request.PlannedQty),
                Unit = string.IsNullOrWhiteSpace(request.Unit) ? "PCS" : request.Unit.Trim(),
                PlanDate = request.PlanDate.Date,
                Status = CuttingListStatus.RELEASED
            };
            _db.CuttingLists.Add(item);
            await _db.SaveChangesAsync();
            return ApiCreated(item, "Cutting list created successfully.");
        }
        catch (Exception ex)
        {
            return ApiBadRequest(ex);
        }
    }

    [HttpGet("pic-cards")]
    public async Task<IActionResult> PicCards()
    {
        return ApiOk(await _db.PicCards.AsNoTracking().OrderBy(x => x.FullName).ToListAsync());
    }

    [HttpGet("shift-masters")]
    public async Task<IActionResult> ShiftMasters()
    {
        return ApiOk(await _db.ShiftMasters.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.ShiftName)
            .ToListAsync());
    }

    [HttpGet("activity-logs")]
    public async Task<IActionResult> ActivityLogs([FromQuery] int? workOrderId)
    {
        var query = _db.ProductionActivityLogs.AsNoTracking()
            .Include(x => x.ProductionWorkOrder)
            .Include(x => x.PicCard)
            .AsQueryable();
        if (workOrderId.HasValue)
        {
            query = query.Where(x => x.ProductionWorkOrderId == workOrderId.Value);
        }

        var logs = await query.OrderByDescending(x => x.CreatedAt).Take(250)
            .Select(x => new
            {
                id = x.Id,
                production_work_order_id = x.ProductionWorkOrderId,
                wo_number = x.ProductionWorkOrder != null ? x.ProductionWorkOrder.WoNumber : null,
                pic_name = x.PicCard != null ? x.PicCard.FullName : null,
                activity_type = x.ActivityType,
                remarks = x.Remarks,
                created_at = x.CreatedAt
            })
            .ToListAsync();
        return ApiOk(logs);
    }

    private IQueryable<ProductionWorkOrder> WorkOrderQuery() =>
        _db.ProductionWorkOrders.AsNoTracking()
            .Include(x => x.CuttingList)
            .Include(x => x.PicCard)
            .Include(x => x.Operators)
                .ThenInclude(x => x.PicCard);

    private Task<ProductionWorkOrder?> FindWorkOrder(int id) =>
        _db.ProductionWorkOrders
            .Include(x => x.CuttingList)
            .Include(x => x.PicCard)
            .Include(x => x.Operators)
                .ThenInclude(x => x.PicCard)
            .FirstOrDefaultAsync(x => x.Id == id);

    private async Task ReloadWorkOrder(ProductionWorkOrder order)
    {
        await _db.Entry(order).Reference(x => x.CuttingList).LoadAsync();
        await _db.Entry(order).Reference(x => x.PicCard).LoadAsync();
        _db.Entry(order).Collection(x => x.Operators).IsLoaded = false;
        await _db.Entry(order).Collection(x => x.Operators).LoadAsync();
        foreach (var workOrderOperator in order.Operators)
        {
            await _db.Entry(workOrderOperator).Reference(x => x.PicCard).LoadAsync();
        }
    }

    private void AddLog(int workOrderId, int? picCardId, ProductionActivityType activityType, string? remarks)
    {
        _db.ProductionActivityLogs.Add(new ProductionActivityLog
        {
            ProductionWorkOrderId = workOrderId,
            PicCardId = picCardId,
            ActivityType = activityType,
            Remarks = remarks,
            CreatedAt = DateTime.Now
        });
    }

    private static ProductionWorkOrderResponse ToResponse(ProductionWorkOrder order)
    {
        var activeOperators = order.Operators
            .Where(x => x.IsActive)
            .OrderBy(x => x.ScannedAt)
            .ToList();
        var primaryOperator = activeOperators.FirstOrDefault()?.PicCard ?? order.PicCard;

        return new ProductionWorkOrderResponse
        {
            Id = order.Id,
            WoNumber = order.WoNumber,
            CuttingListId = order.CuttingListId,
            CuttingListNo = order.CuttingList?.CuttingListNo ?? string.Empty,
            ProductCode = order.CuttingList?.ProductCode ?? string.Empty,
            ProductName = order.CuttingList?.ProductName ?? string.Empty,
            PicCardId = primaryOperator?.Id,
            PicName = primaryOperator?.FullName,
            EmployeeNo = primaryOperator?.EmployeeNo,
            OperatorShift = primaryOperator?.Shift,
            OperatorDepartment = primaryOperator?.Department,
            Operators = activeOperators
                .Where(x => x.PicCard is not null)
                .Select(x => new ProductionOperatorResponse
                {
                    Id = x.Id,
                    PicCardId = x.PicCardId,
                    CardUid = x.PicCard!.CardUid,
                    EmployeeNo = x.PicCard.EmployeeNo,
                    FullName = x.PicCard.FullName,
                    Department = x.PicCard.Department,
                    Shift = x.PicCard.Shift,
                    ScannedAt = x.ScannedAt
                })
                .ToList(),
            LineCode = order.LineCode,
            ActualQty = order.CompletedAt.HasValue ? order.ActualQty : 0,
            RejectQty = order.CompletedAt.HasValue ? order.RejectQty : 0,
            Status = order.Status,
            PlanDate = order.CuttingList?.PlanDate ?? order.CreatedAt.Date,
            StartedAt = order.StartedAt,
            CompletedAt = order.CompletedAt,
            UpdatedAt = order.UpdatedAt
        };
    }
}
