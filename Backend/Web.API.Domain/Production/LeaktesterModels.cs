using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Web.API.Domain.Production;

public class EngineModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("engine_model")]
    public string ModelName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("is_deleted")]
    public bool? IsDeleted { get; set; }
}

public class Operator
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("operator_code")]
    public string OperatorCode { get; set; } = string.Empty;

    [JsonPropertyName("operator_name")]
    public string OperatorName { get; set; } = string.Empty;

    [JsonPropertyName("department")]
    public string? Department { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("is_deleted")]
    public bool? IsDeleted { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public class LeakTestWorkRecord
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("engine_model_id")]
    public int EngineModelId { get; set; }

    [NotMapped]
    [JsonPropertyName("engine_model")]
    public string EngineModelName => EngineModel?.ModelName ?? string.Empty;

    [JsonIgnore]
    public EngineModel? EngineModel { get; set; }

    [JsonPropertyName("engine_number")]
    public string EngineNumber { get; set; } = string.Empty;

    [JsonPropertyName("check_date")]
    public DateTime CheckDate { get; set; } = DateTime.Today;

    [JsonPropertyName("check_time")]
    public string CheckTime { get; set; } = DateTime.Now.ToString("HH:mm:ss");

    [JsonPropertyName("machine_name")]
    public string MachineName { get; set; } = "Leak Tester Machine";

    [JsonPropertyName("operator_id")]
    public int? OperatorId { get; set; }

    [NotMapped]
    [JsonPropertyName("operator_name")]
    public string OperatorName => Operator?.OperatorName ?? string.Empty;

    [JsonIgnore]
    public Operator? Operator { get; set; }

    [JsonPropertyName("parameter_pressure")]
    public decimal ParameterPressure { get; set; }

    [JsonPropertyName("pressure_input")]
    public decimal PressureInput { get; set; }

    [JsonPropertyName("cycle_time_leak_test_minutes")]
    public decimal CycleTimeLeakTestMinutes { get; set; }

    [JsonPropertyName("result")]
    public string Result { get; set; } = "OK";

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public class ReworkEngineRecord
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("engine_model_id")]
    public int? EngineModelId { get; set; }

    [NotMapped]
    [JsonPropertyName("engine_model")]
    public string EngineModelName => EngineModel?.ModelName ?? EngineModelText ?? string.Empty;

    [JsonIgnore]
    public EngineModel? EngineModel { get; set; }

    [JsonPropertyName("engine_model_text")]
    public string? EngineModelText { get; set; }

    [JsonPropertyName("engine_number")]
    public string EngineNumber { get; set; } = string.Empty;

    [JsonPropertyName("barcode_scan")]
    public string BarcodeScan { get; set; } = string.Empty;

    [JsonPropertyName("rework_date")]
    public DateTime ReworkDate { get; set; } = DateTime.Today;

    [JsonPropertyName("rework_time")]
    public string ReworkTime { get; set; } = DateTime.Now.ToString("HH:mm:ss");

    [JsonPropertyName("operator_id")]
    public int? OperatorId { get; set; }

    [NotMapped]
    [JsonPropertyName("operator_name")]
    public string OperatorName => Operator?.OperatorName ?? string.Empty;

    [JsonIgnore]
    public Operator? Operator { get; set; }

    [JsonPropertyName("parameter_pressure")]
    public decimal ParameterPressure { get; set; }

    [JsonPropertyName("pressure_input")]
    public decimal PressureInput { get; set; }

    [JsonPropertyName("result")]
    public string Result { get; set; } = "OK";

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public class CreateEngineModelRequest
{
    [JsonPropertyName("engine_model")]
    public string ModelName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("is_deleted")]
    public bool? IsDeleted { get; set; }
}

public class CreateOperatorRequest
{
    [JsonPropertyName("operator_code")]
    public string OperatorCode { get; set; } = string.Empty;

    [JsonPropertyName("operator_name")]
    public string OperatorName { get; set; } = string.Empty;

    [JsonPropertyName("department")]
    public string? Department { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("is_deleted")]
    public bool? IsDeleted { get; set; }
}

public class CreateLeakTestWorkRecordRequest
{
    [JsonPropertyName("engine_model_id")]
    public int EngineModelId { get; set; }

    [JsonPropertyName("engine_number")]
    public string EngineNumber { get; set; } = string.Empty;

    [JsonPropertyName("check_date")]
    public DateTime CheckDate { get; set; } = DateTime.Today;

    [JsonPropertyName("check_time")]
    public string CheckTime { get; set; } = DateTime.Now.ToString("HH:mm:ss");

    [JsonPropertyName("machine_name")]
    public string MachineName { get; set; } = "Leak Tester Machine";

    [JsonPropertyName("operator_id")]
    public int? OperatorId { get; set; }

    [JsonPropertyName("parameter_pressure")]
    public decimal ParameterPressure { get; set; }

    [JsonPropertyName("pressure_input")]
    public decimal PressureInput { get; set; }

    [JsonPropertyName("cycle_time_leak_test_minutes")]
    public decimal CycleTimeLeakTestMinutes { get; set; }

    [JsonPropertyName("result")]
    public string Result { get; set; } = "OK";
}

public class CreateReworkEngineRecordRequest
{
    [JsonPropertyName("barcode_scan")]
    public string BarcodeScan { get; set; } = string.Empty;

    [JsonPropertyName("rework_date")]
    public DateTime ReworkDate { get; set; } = DateTime.Today;

    [JsonPropertyName("rework_time")]
    public string ReworkTime { get; set; } = DateTime.Now.ToString("HH:mm:ss");

    [JsonPropertyName("operator_id")]
    public int? OperatorId { get; set; }

    [JsonPropertyName("parameter_pressure")]
    public decimal ParameterPressure { get; set; }

    [JsonPropertyName("pressure_input")]
    public decimal PressureInput { get; set; }

    [JsonPropertyName("result")]
    public string Result { get; set; } = "OK";

    [JsonPropertyName("note")]
    public string? Note { get; set; }
}

public class LeakTestMonthlySummary
{
    [JsonPropertyName("year")]
    public int Year { get; set; }

    [JsonPropertyName("month")]
    public int Month { get; set; }

    [JsonPropertyName("month_label")]
    public string MonthLabel { get; set; } = string.Empty;

    [JsonPropertyName("total_engine_inspect")]
    public int TotalEngineInspect { get; set; }

    [JsonPropertyName("ok")]
    public int Ok { get; set; }

    [JsonPropertyName("ng")]
    public int Ng { get; set; }
}
