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

    [JsonPropertyName("parameter_pressure")]
    public decimal ParameterPressure { get; set; }

    [JsonPropertyName("pressure_input")]
    public decimal PressureInput { get; set; }

    [JsonPropertyName("cycle_time_leak_test_minutes")]
    public decimal CycleTimeLeakTestMinutes { get; set; }

    [JsonPropertyName("result")]
    public string Result { get; set; } = "OK";
}
