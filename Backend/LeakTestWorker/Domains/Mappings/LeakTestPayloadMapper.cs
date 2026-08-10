using System.Globalization;
using LeakTestWorker.Domains.Models;
using LeakTestWorker.Domains.Payload;
using LeakTestWorker.Helper;
using Newtonsoft.Json.Linq;

namespace LeakTestWorker.Domains.Mappings;

public static class LeakTestPayloadMapper
{
    private static readonly string[] KnownPayloadFields =
    [
        "engine_model_id",
        "engineModelId",
        "EngineModelId",
        "engine_model",
        "engineModel",
        "EngineModel",
        "engine_number",
        "engineNumber",
        "EngineNumber",
        "serial_no",
        "serial no",
        "barcode",
        "Barcode",
        "channel_no",
        "press_set_up",
        "press_set_low",
        "pressure_input",
        "pressureInput",
        "PressureInput",
        "press_input",
        "judgement",
        "result",
        "Result",
        "ts"
    ];

    public static LeakTestHistoryRecord ToHistoryRecord(string topic, string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new FormatException("MQTT payload is empty.");
        }

        var root = JObject.Parse(payload);
        var message = new LeakTestPayload
        {
            Topic = topic,
            RawJson = payload,
            Data = SelectDataObject(root)
        };

        var serverNow = DateTime.Now;
        var checkDate = serverNow.Date;
        var checkTime = serverNow.ToString("HH:mm:ss", CultureInfo.InvariantCulture);

        var pressSetUp = ReadDecimal(message.Data, "press_set_up", "pressSetUp", "PressSetUp", "upper_press_limit", "UpperPressLimit", "tp_ul", "TP_UL");
        var pressSetLow = ReadDecimal(message.Data, "press_set_low", "pressSetLow", "PressSetLow", "lower_press_limit", "LowerPressLimit", "tp_ll", "TP_LL");
        var parameterPressure = ReadDecimal(message.Data, "parameter_pressure", "parameterPressure", "ParameterPressure", "set_pressure", "SetPressure", "target_pressure", "TargetPressure", "pressure_setting", "PressureSetting")
            ?? CalculateParameterPressure(pressSetLow, pressSetUp);

        var rawBarcode = ReadString(message.Data, "barcode", "Barcode", "barcode_scan", "barcodeScan", "BarcodeScan");
        var normalizedBarcode = NormalizeBarcodeScan(rawBarcode);
        var (barcodeEngineModel, barcodeEngineNumber) = ParseBarcodeScan(rawBarcode);
        var explicitEngineNumber = ReadString(message.Data, "engine_number", "engineNumber", "EngineNumber", "engine_no", "EngineNo", "serial_no", "serial no", "serial_number", "SerialNumber");
        var engineModel = FirstText(
            ReadString(message.Data, "engine_model", "engineModel", "EngineModel", "model", "Model", "engine_type", "EngineType"),
            barcodeEngineModel);
        var engineNumber = FirstText(explicitEngineNumber, barcodeEngineNumber, normalizedBarcode);
        var judgementCode = ReadInt(message.Data, "judgement", "Judgement", "judgement_code", "JudgementCode");
        var record = new LeakTestHistoryRecord
        {
            EngineModelId = ReadInt(message.Data, "engine_model_id", "engineModelId", "EngineModelId", "model_id", "ModelId"),
            EngineModel = engineModel,
            EngineNumber = engineNumber ?? string.Empty,
            BarcodeScan = normalizedBarcode,
            CheckDate = checkDate.Date,
            CheckTime = checkTime,
            MachineName = ReadString(message.Data, "machine_name", "machineName", "MachineName", "machine", "Machine", "line", "Line", "LineNo")
                ?? SignalHelper.TopicToMachineName(message.Topic),
            Operator = ReadString(message.Data, "operator", "Operator", "operator_name", "operatorName", "OperatorName", "operator_code", "operatorCode", "OperatorCode"),
            ChannelNo = ReadString(message.Data, "channel_no", "channelNo", "ChannelNo", "channel", "Channel"),
            ParameterPressure = parameterPressure ?? 0,
            PressSetUp = pressSetUp,
            PressSetLow = pressSetLow,
            PressureInput = NormalizeCosmoPressure(ReadDecimal(message.Data, "pressure_input", "pressureInput", "PressureInput", "press_input", "actual_pressure", "ActualPressure", "leak_pressure", "LeakPressure") ?? 0),
            CycleTimeLeakTestMinutes = ReadCycleTime(message.Data) ?? 0,
            JudgementCode = judgementCode
        };

        Validate(record);
        return record;
    }

    private static JObject SelectDataObject(JObject root)
    {
        if (HasKnownPayloadField(root))
        {
            return root;
        }

        foreach (var property in root.Properties())
        {
            if (property.Value is JObject child && HasKnownPayloadField(child))
            {
                return child;
            }
        }

        return root;
    }

    private static bool HasKnownPayloadField(JObject source) =>
        KnownPayloadFields.Any(field => source.Property(field, StringComparison.OrdinalIgnoreCase) is not null);

    private static string? ReadString(JObject source, params string[] names)
    {
        var token = ReadToken(source, names);
        var value = token?.Type == JTokenType.String
            ? token.Value<string>()
            : token?.ToString();

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? FirstText(params string?[] values)
    {
        return values
            .Select(value => value?.Trim())
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static string? NormalizeBarcodeScan(string? barcodeScan)
    {
        if (string.IsNullOrWhiteSpace(barcodeScan))
        {
            return null;
        }

        var normalized = barcodeScan.Trim().TrimStart('.');
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static (string? EngineModel, string? EngineNumber) ParseBarcodeScan(string? barcodeScan)
    {
        var normalized = NormalizeBarcodeScan(barcodeScan);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return (null, null);
        }

        var separatorIndex = normalized.IndexOfAny([' ', '\t', '\r', '\n']);
        if (separatorIndex < 0)
        {
            return (normalized, null);
        }

        var engineModel = normalized[..separatorIndex].Trim();
        var engineNumber = normalized[(separatorIndex + 1)..].Trim();
        return (
            string.IsNullOrWhiteSpace(engineModel) ? null : engineModel,
            string.IsNullOrWhiteSpace(engineNumber) ? null : engineNumber);
    }

    private static int? ReadInt(JObject source, params string[] names)
    {
        var token = ReadToken(source, names);
        if (token is null || token.Type == JTokenType.Null)
        {
            return null;
        }

        if (token.Type == JTokenType.Integer)
        {
            return token.Value<int>();
        }

        var value = token.ToString();
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static decimal? ReadDecimal(JObject source, params string[] names)
    {
        var token = ReadToken(source, names);
        if (token is null || token.Type == JTokenType.Null)
        {
            return null;
        }

        if (token.Type is JTokenType.Float or JTokenType.Integer)
        {
            return token.Value<decimal>();
        }

        var value = token.ToString().Trim();
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariant))
        {
            return invariant;
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var local))
        {
            return local;
        }

        return decimal.TryParse(value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var normalized)
            ? normalized
            : null;
    }

    private static decimal? ReadCycleTime(JObject source)
    {
        var explicitValue = ReadDecimal(source, "cycle_time_leak_test_minutes", "cycleTimeLeakTestMinutes", "CycleTimeLeakTestMinutes", "test_minutes", "TestMinutes");
        if (explicitValue.HasValue)
        {
            return explicitValue.Value;
        }

        var rawCycleTime = ReadDecimal(source, "cycle_time", "cycleTime", "CycleTime");
        return rawCycleTime.HasValue
            ? Math.Round(rawCycleTime.Value / 10, 2)
            : null;
    }

    private static JToken? ReadToken(JObject source, params string[] names)
    {
        foreach (var name in names)
        {
            var property = source.Property(name, StringComparison.OrdinalIgnoreCase);
            if (property is not null)
            {
                return property.Value;
            }
        }

        return null;
    }

    private static decimal? CalculateParameterPressure(decimal? pressSetLow, decimal? pressSetUp)
    {
        if (pressSetLow.HasValue && pressSetUp.HasValue)
        {
            return Math.Round((NormalizeCosmoPressure(pressSetLow.Value) + NormalizeCosmoPressure(pressSetUp.Value)) / 2, 2);
        }

        if (pressSetLow.HasValue)
        {
            return NormalizeCosmoPressure(pressSetLow.Value);
        }

        return pressSetUp.HasValue ? NormalizeCosmoPressure(pressSetUp.Value) : null;
    }

    private static decimal NormalizeCosmoPressure(decimal value)
    {
        return Math.Abs(value) >= 10 ? Math.Round(value / 100, 2) : value;
    }

    private static void Validate(LeakTestHistoryRecord record)
    {
        var missingFields = new List<string>();

        if ((record.EngineModelId is null or <= 0) && string.IsNullOrWhiteSpace(record.EngineModel))
        {
            missingFields.Add("engine_model_id/engine_model");
        }

        if (string.IsNullOrWhiteSpace(record.EngineNumber))
        {
            missingFields.Add("engine_number");
        }

        if (record.ParameterPressure <= 0)
        {
            missingFields.Add("parameter_pressure");
        }

        if (record.PressureInput <= 0)
        {
            missingFields.Add("pressure_input");
        }

        if (record.CycleTimeLeakTestMinutes <= 0)
        {
            missingFields.Add("cycle_time_leak_test_minutes");
        }

        if (missingFields.Count > 0)
        {
            throw new FormatException($"MQTT payload missing/invalid fields: {string.Join(", ", missingFields)}.");
        }
    }
}
