using System.Text.Json.Serialization;

namespace ProductionControl.Domain.Production;

public enum CuttingListStatus
{
    OPEN,
    RELEASED,
    IN_PROGRESS,
    COMPLETED,
    CANCELLED
}

public enum ProductionWorkOrderStatus
{
    WAITING,
    READY,
    IN_PROGRESS,
    HOLD,
    COMPLETED,
    CANCELLED
}

public enum ProductionActivityType
{
    PIC_SCAN,
    OPERATOR_REMOVE,
    CUTTING_LIST_SCAN,
    WORK_START,
    PRODUCTION_UPDATE,
    WORK_HOLD,
    WORK_RESUME,
    WORK_COMPLETE
}

public class PicCard
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("card_uid")]
    public string CardUid { get; set; } = string.Empty;

    [JsonPropertyName("employee_no")]
    public string EmployeeNo { get; set; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("department")]
    public string Department { get; set; } = "Production";

    [JsonPropertyName("shift")]
    public string Shift { get; set; } = string.Empty;

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("last_scanned_at")]
    public DateTime? LastScannedAt { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class ShiftMaster
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("shift_code")]
    public string ShiftCode { get; set; } = string.Empty;

    [JsonPropertyName("shift_name")]
    public string ShiftName { get; set; } = string.Empty;

    [JsonPropertyName("sort_order")]
    public int SortOrder { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class CuttingList
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("cutting_list_no")]
    public string CuttingListNo { get; set; } = string.Empty;

    [JsonPropertyName("product_code")]
    public string ProductCode { get; set; } = string.Empty;

    [JsonPropertyName("product_name")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("line_code")]
    public string LineCode { get; set; } = string.Empty;

    [JsonPropertyName("planned_qty")]
    public int PlannedQty { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; } = "PCS";

    [JsonPropertyName("plan_date")]
    public DateTime PlanDate { get; set; }

    [JsonPropertyName("status")]
    public CuttingListStatus Status { get; set; } = CuttingListStatus.OPEN;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class ProductionWorkOrder
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("wo_number")]
    public string WoNumber { get; set; } = string.Empty;

    [JsonPropertyName("cutting_list_id")]
    public int CuttingListId { get; set; }

    [JsonPropertyName("pic_card_id")]
    public int? PicCardId { get; set; }

    [JsonPropertyName("line_code")]
    public string LineCode { get; set; } = string.Empty;

    [JsonIgnore]
    public int TargetQty { get; set; }

    [JsonPropertyName("actual_qty")]
    public int ActualQty { get; set; }

    [JsonPropertyName("reject_qty")]
    public int RejectQty { get; set; }

    [JsonPropertyName("status")]
    public ProductionWorkOrderStatus Status { get; set; } = ProductionWorkOrderStatus.WAITING;

    [JsonPropertyName("started_at")]
    public DateTime? StartedAt { get; set; }

    [JsonPropertyName("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [JsonIgnore]
    public CuttingList? CuttingList { get; set; }

    [JsonIgnore]
    public PicCard? PicCard { get; set; }

    [JsonIgnore]
    public ICollection<ProductionWorkOrderOperator> Operators { get; set; } = new List<ProductionWorkOrderOperator>();

    [JsonIgnore]
    public ICollection<ProductionActivityLog> ActivityLogs { get; set; } = new List<ProductionActivityLog>();
}

public class ProductionWorkOrderOperator
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("production_work_order_id")]
    public int ProductionWorkOrderId { get; set; }

    [JsonPropertyName("pic_card_id")]
    public int PicCardId { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("scanned_at")]
    public DateTime ScannedAt { get; set; } = DateTime.Now;

    [JsonPropertyName("removed_at")]
    public DateTime? RemovedAt { get; set; }

    [JsonIgnore]
    public ProductionWorkOrder? ProductionWorkOrder { get; set; }

    [JsonIgnore]
    public PicCard? PicCard { get; set; }
}

public class ProductionActivityLog
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("production_work_order_id")]
    public int ProductionWorkOrderId { get; set; }

    [JsonPropertyName("pic_card_id")]
    public int? PicCardId { get; set; }

    [JsonPropertyName("activity_type")]
    public ProductionActivityType ActivityType { get; set; }

    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonIgnore]
    public ProductionWorkOrder? ProductionWorkOrder { get; set; }

    [JsonIgnore]
    public PicCard? PicCard { get; set; }
}

public class ProductionDashboardSummary
{
    [JsonPropertyName("total_work_orders")]
    public int TotalWorkOrders { get; set; }

    [JsonPropertyName("waiting_work_orders")]
    public int WaitingWorkOrders { get; set; }

    [JsonPropertyName("running_work_orders")]
    public int RunningWorkOrders { get; set; }

    [JsonPropertyName("completed_work_orders")]
    public int CompletedWorkOrders { get; set; }

    [JsonPropertyName("actual_qty")]
    public int ActualQty { get; set; }

    [JsonPropertyName("reject_qty")]
    public int RejectQty { get; set; }

    [JsonPropertyName("work_orders")]
    public List<ProductionWorkOrderResponse> WorkOrders { get; set; } = [];
}

public class ProductionWorkOrderResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("wo_number")]
    public string WoNumber { get; set; } = string.Empty;

    [JsonPropertyName("cutting_list_id")]
    public int CuttingListId { get; set; }

    [JsonPropertyName("cutting_list_no")]
    public string CuttingListNo { get; set; } = string.Empty;

    [JsonPropertyName("product_code")]
    public string ProductCode { get; set; } = string.Empty;

    [JsonPropertyName("product_name")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("pic_card_id")]
    public int? PicCardId { get; set; }

    [JsonPropertyName("pic_name")]
    public string? PicName { get; set; }

    [JsonPropertyName("employee_no")]
    public string? EmployeeNo { get; set; }

    [JsonPropertyName("operator_shift")]
    public string? OperatorShift { get; set; }

    [JsonPropertyName("operator_department")]
    public string? OperatorDepartment { get; set; }

    [JsonPropertyName("operators")]
    public List<ProductionOperatorResponse> Operators { get; set; } = [];

    [JsonPropertyName("line_code")]
    public string LineCode { get; set; } = string.Empty;

    [JsonPropertyName("actual_qty")]
    public int ActualQty { get; set; }

    [JsonPropertyName("reject_qty")]
    public int RejectQty { get; set; }

    [JsonPropertyName("status")]
    public ProductionWorkOrderStatus Status { get; set; }

    [JsonPropertyName("plan_date")]
    public DateTime PlanDate { get; set; }

    [JsonPropertyName("started_at")]
    public DateTime? StartedAt { get; set; }

    [JsonPropertyName("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class CreateProductionWorkOrderRequest
{
    [JsonPropertyName("wo_number")]
    public string WoNumber { get; set; } = string.Empty;

    [JsonPropertyName("cutting_list_id")]
    public int CuttingListId { get; set; }

    [JsonPropertyName("line_code")]
    public string? LineCode { get; set; }

}

public class ScanWorkOrderRequest
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
}

public class ScanPicRequest
{
    [JsonPropertyName("card_uid")]
    public string CardUid { get; set; } = string.Empty;
}

public class ProductionOperatorResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("pic_card_id")]
    public int PicCardId { get; set; }

    [JsonPropertyName("card_uid")]
    public string CardUid { get; set; } = string.Empty;

    [JsonPropertyName("employee_no")]
    public string EmployeeNo { get; set; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("department")]
    public string Department { get; set; } = string.Empty;

    [JsonPropertyName("shift")]
    public string Shift { get; set; } = string.Empty;

    [JsonPropertyName("scanned_at")]
    public DateTime ScannedAt { get; set; }
}

public class UpdateProductionRequest
{
    [JsonPropertyName("actual_qty")]
    public int? ActualQty { get; set; }

    [JsonPropertyName("reject_qty")]
    public int? RejectQty { get; set; }

    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }
}

public class CreateCuttingListRequest
{
    [JsonPropertyName("cutting_list_no")]
    public string CuttingListNo { get; set; } = string.Empty;

    [JsonPropertyName("product_code")]
    public string ProductCode { get; set; } = string.Empty;

    [JsonPropertyName("product_name")]
    public string ProductName { get; set; } = string.Empty;

    [JsonPropertyName("line_code")]
    public string LineCode { get; set; } = string.Empty;

    [JsonPropertyName("planned_qty")]
    public int PlannedQty { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; } = "PCS";

    [JsonPropertyName("plan_date")]
    public DateTime PlanDate { get; set; }
}
