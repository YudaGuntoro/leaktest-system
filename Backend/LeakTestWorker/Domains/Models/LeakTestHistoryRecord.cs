namespace LeakTestWorker.Domains.Models;

public sealed class LeakTestHistoryRecord
{
    public int? EngineModelId { get; init; }

    public string? EngineModel { get; init; }

    public string EngineNumber { get; init; } = string.Empty;

    public string? BarcodeScan { get; init; }

    public DateTime CheckDate { get; init; }

    public string CheckTime { get; init; } = string.Empty;

    public string MachineName { get; init; } = string.Empty;

    public string? Operator { get; init; }

    public string? ChannelNo { get; init; }

    public decimal ParameterPressure { get; init; }

    public decimal? PressSetUp { get; init; }

    public decimal? PressSetLow { get; init; }

    public decimal PressureInput { get; init; }

    public decimal CycleTimeLeakTestMinutes { get; init; }

    public int? JudgementCode { get; init; }
}
