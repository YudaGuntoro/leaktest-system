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

public class LeakTestParameter
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("channel_no")]
    public string ChannelNo { get; set; } = string.Empty;

    [JsonPropertyName("model_parameter")]
    public string ModelParameter { get; set; } = string.Empty;

    [JsonPropertyName("item_name")]
    public string ItemName { get; set; } = string.Empty;

    [JsonPropertyName("item_value")]
    public string ItemValue { get; set; } = string.Empty;

    [JsonPropertyName("machine_names")]
    public string? MachineNames { get; set; }

    [JsonPropertyName("is_deleted")]
    public bool? IsDeleted { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public class LeakTestJudgement
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("judgement_code")]
    public int JudgementCode { get; set; }

    [JsonPropertyName("judgement_name")]
    public string JudgementName { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public string Result { get; set; } = "NG";

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("is_deleted")]
    public bool? IsDeleted { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public class MeasurementUnit
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("unit_category")]
    public string UnitCategory { get; set; } = string.Empty;

    [JsonPropertyName("unit_symbol")]
    public string UnitSymbol { get; set; } = string.Empty;

    [JsonPropertyName("unit_name")]
    public string UnitName { get; set; } = string.Empty;

    [JsonPropertyName("is_deleted")]
    public bool? IsDeleted { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public class SystemSetting
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("pressure_unit_id")]
    public int PressureUnitId { get; set; }

    [JsonPropertyName("cycle_time_unit_id")]
    public int CycleTimeUnitId { get; set; }

    [JsonPropertyName("backup_db_location")]
    public string? BackupDbLocation { get; set; }

    [JsonPropertyName("backup_schedule")]
    public string BackupSchedule { get; set; } = "daily";

    [JsonPropertyName("plc_ip_address")]
    public string? PlcIpAddress { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [JsonIgnore]
    public MeasurementUnit? PressureUnit { get; set; }

    [JsonIgnore]
    public MeasurementUnit? CycleTimeUnit { get; set; }
}

public class SystemSettingsResponse
{
    [JsonPropertyName("pressure_unit")]
    public string PressureUnit { get; set; } = "MPa";

    [JsonPropertyName("cycle_time_unit")]
    public string CycleTimeUnit { get; set; } = "s";

    [JsonPropertyName("backup_db_location")]
    public string BackupDbLocation { get; set; } = string.Empty;

    [JsonPropertyName("backup_schedule")]
    public string BackupSchedule { get; set; } = "daily";

    [JsonPropertyName("plc_ip_address")]
    public string PlcIpAddress { get; set; } = string.Empty;
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

    [JsonPropertyName("barcode_scan")]
    public string? BarcodeScan { get; set; }

    [JsonPropertyName("check_date")]
    public DateTime CheckDate { get; set; } = DateTime.Today;

    [JsonPropertyName("check_time")]
    public string CheckTime { get; set; } = DateTime.Now.ToString("HH:mm:ss");

    [JsonPropertyName("machine_name")]
    public string MachineName { get; set; } = "Leak Tester Machine";

    [JsonPropertyName("operator_name")]
    public string? OperatorName { get; set; }

    [JsonPropertyName("parameter_pressure")]
    public decimal ParameterPressure { get; set; }

    [JsonPropertyName("channel_no")]
    public string? ChannelNo { get; set; }

    [JsonPropertyName("press_set_up")]
    public decimal? PressSetUp { get; set; }

    [JsonPropertyName("press_set_low")]
    public decimal? PressSetLow { get; set; }

    [JsonPropertyName("pressure_input")]
    public decimal PressureInput { get; set; }

    [JsonPropertyName("cycle_time_leak_test_minutes")]
    public decimal CycleTimeLeakTestMinutes { get; set; }

    [NotMapped]
    [JsonPropertyName("parameter_channel_no")]
    public string? ParameterChannelNo { get; set; }

    [NotMapped]
    [JsonPropertyName("parameter_standard")]
    public string? ParameterStandard { get; set; }

    [NotMapped]
    [JsonPropertyName("parameter_min")]
    public string? ParameterMin { get; set; }

    [NotMapped]
    [JsonPropertyName("parameter_max")]
    public string? ParameterMax { get; set; }

    [NotMapped]
    [JsonPropertyName("parameter_limit")]
    public string? ParameterLimit { get; set; }

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

    [JsonPropertyName("operator_name")]
    public string? OperatorName { get; set; }

    [JsonPropertyName("parameter_pressure")]
    public decimal ParameterPressure { get; set; }

    [JsonPropertyName("pressure_input")]
    public decimal PressureInput { get; set; }

    [NotMapped]
    [JsonPropertyName("parameter_channel_no")]
    public string? ParameterChannelNo { get; set; }

    [NotMapped]
    [JsonPropertyName("parameter_standard")]
    public string? ParameterStandard { get; set; }

    [NotMapped]
    [JsonPropertyName("parameter_min")]
    public string? ParameterMin { get; set; }

    [NotMapped]
    [JsonPropertyName("parameter_max")]
    public string? ParameterMax { get; set; }

    [NotMapped]
    [JsonPropertyName("parameter_limit")]
    public string? ParameterLimit { get; set; }

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

public class CreateLeakTestParameterRequest
{
    [JsonPropertyName("channel_no")]
    public string ChannelNo { get; set; } = string.Empty;

    [JsonPropertyName("model_parameter")]
    public string ModelParameter { get; set; } = string.Empty;

    [JsonPropertyName("item_name")]
    public string ItemName { get; set; } = string.Empty;

    [JsonPropertyName("item_value")]
    public string ItemValue { get; set; } = string.Empty;

    [JsonPropertyName("machine_names")]
    public string? MachineNames { get; set; }

    [JsonPropertyName("is_deleted")]
    public bool? IsDeleted { get; set; }
}

public class UpdateSystemSettingsRequest
{
    [JsonPropertyName("pressure_unit")]
    public string PressureUnit { get; set; } = "MPa";

    [JsonPropertyName("cycle_time_unit")]
    public string CycleTimeUnit { get; set; } = "s";

    [JsonPropertyName("backup_db_location")]
    public string? BackupDbLocation { get; set; }

    [JsonPropertyName("backup_schedule")]
    public string BackupSchedule { get; set; } = "daily";

    [JsonPropertyName("plc_ip_address")]
    public string? PlcIpAddress { get; set; }
}

public class LeakTestParameterImportResult
{
    [JsonPropertyName("imported")]
    public int Imported { get; set; }

    [JsonPropertyName("updated")]
    public int Updated { get; set; }

    [JsonPropertyName("skipped")]
    public int Skipped { get; set; }

    [JsonPropertyName("channels")]
    public int Channels { get; set; }
}

public class CreateLeakTestWorkRecordRequest
{
    [JsonPropertyName("engine_model_id")]
    public int EngineModelId { get; set; }

    [JsonPropertyName("engine_number")]
    public string EngineNumber { get; set; } = string.Empty;

    [JsonPropertyName("barcode_scan")]
    public string? BarcodeScan { get; set; }

    [JsonPropertyName("check_date")]
    public DateTime CheckDate { get; set; } = DateTime.Today;

    [JsonPropertyName("check_time")]
    public string CheckTime { get; set; } = DateTime.Now.ToString("HH:mm:ss");

    [JsonPropertyName("machine_name")]
    public string MachineName { get; set; } = "Leak Tester Machine";

    [JsonPropertyName("operator_name")]
    public string? OperatorName { get; set; }

    [JsonPropertyName("parameter_pressure")]
    public decimal ParameterPressure { get; set; }

    [JsonPropertyName("channel_no")]
    public string? ChannelNo { get; set; }

    [JsonPropertyName("press_set_up")]
    public decimal? PressSetUp { get; set; }

    [JsonPropertyName("press_set_low")]
    public decimal? PressSetLow { get; set; }

    [JsonPropertyName("pressure_input")]
    public decimal PressureInput { get; set; }

    [JsonPropertyName("cycle_time_leak_test_minutes")]
    public decimal CycleTimeLeakTestMinutes { get; set; }

    [JsonPropertyName("result")]
    public string Result { get; set; } = "OK";
}

public class CreateHmiLeakTestWorkRecordRequest
{
    [JsonPropertyName("barcode")]
    public string? Barcode { get; set; }

    [JsonPropertyName("barcode_scan")]
    public string? BarcodeScan { get; set; }

    [JsonPropertyName("engine_model")]
    public string? EngineModel { get; set; }

    [JsonPropertyName("serial_no")]
    public string? SerialNo { get; set; }

    [JsonPropertyName("serial no")]
    public string? SerialNoText { get; set; }

    [JsonPropertyName("engine_number")]
    public string? EngineNumber { get; set; }

    [JsonPropertyName("machine_name")]
    public string? MachineName { get; set; }

    [JsonPropertyName("operator")]
    public string? Operator { get; set; }

    [JsonPropertyName("channel_no")]
    public string? ChannelNo { get; set; }

    [JsonPropertyName("press_set_up")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? PressSetUp { get; set; }

    [JsonPropertyName("press_set_low")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? PressSetLow { get; set; }

    [JsonPropertyName("pressure_input")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal PressureInput { get; set; }

    [JsonPropertyName("cycle_time")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal CycleTime { get; set; }

    [JsonPropertyName("judgement")]
    public string? Judgement { get; set; }

    [JsonPropertyName("tested_at")]
    public DateTime? TestedAt { get; set; }
}

public class CreateReworkEngineRecordRequest
{
    [JsonPropertyName("barcode_scan")]
    public string BarcodeScan { get; set; } = string.Empty;

    [JsonPropertyName("rework_date")]
    public DateTime ReworkDate { get; set; } = DateTime.Today;

    [JsonPropertyName("rework_time")]
    public string ReworkTime { get; set; } = DateTime.Now.ToString("HH:mm:ss");

    [JsonPropertyName("operator_name")]
    public string? OperatorName { get; set; }

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
