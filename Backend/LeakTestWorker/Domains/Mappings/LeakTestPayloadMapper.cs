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
        "pressure_input",
        "pressureInput",
        "PressureInput",
        "result",
        "Result"
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

        var timestamp = ReadDateTime(message.Data, "timestamp", "Timestamp", "created_at", "CreatedAt", "date_time", "DateTime", "test_datetime", "TestDateTime");
        var checkDate = ReadDate(message.Data, "check_date", "checkDate", "CheckDate", "date", "Date", "test_date", "TestDate")
            ?? timestamp?.Date
            ?? DateTime.Today;
        var checkTime = ReadString(message.Data, "check_time", "checkTime", "CheckTime", "time", "Time", "test_time", "TestTime");
        if (string.IsNullOrWhiteSpace(checkTime))
        {
            checkTime = (timestamp ?? DateTime.Now).ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        }

        var record = new LeakTestHistoryRecord
        {
            EngineModelId = ReadInt(message.Data, "engine_model_id", "engineModelId", "EngineModelId", "model_id", "ModelId"),
            EngineModel = ReadString(message.Data, "engine_model", "engineModel", "EngineModel", "model", "Model", "engine_type", "EngineType"),
            EngineNumber = ReadString(message.Data, "engine_number", "engineNumber", "EngineNumber", "engine_no", "EngineNo", "serial_number", "SerialNumber", "barcode", "Barcode") ?? string.Empty,
            CheckDate = checkDate.Date,
            CheckTime = NormalizeTime(checkTime),
            MachineName = ReadString(message.Data, "machine_name", "machineName", "MachineName", "machine", "Machine", "line", "Line", "LineNo")
                ?? SignalHelper.TopicToMachineName(message.Topic),
            ParameterPressure = ReadDecimal(message.Data, "parameter_pressure", "parameterPressure", "ParameterPressure", "set_pressure", "SetPressure", "target_pressure", "TargetPressure") ?? 0,
            PressureInput = ReadDecimal(message.Data, "pressure_input", "pressureInput", "PressureInput", "actual_pressure", "ActualPressure", "leak_pressure", "LeakPressure") ?? 0,
            CycleTimeLeakTestMinutes = ReadDecimal(message.Data, "cycle_time_leak_test_minutes", "cycleTimeLeakTestMinutes", "CycleTimeLeakTestMinutes", "cycle_time", "cycleTime", "CycleTime", "test_minutes", "TestMinutes") ?? 0,
            Result = NormalizeResult(ReadString(message.Data, "result", "Result", "judgement", "Judgement", "status", "Status")) ?? string.Empty
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

    private static DateTime? ReadDate(JObject source, params string[] names)
    {
        var dateTime = ReadDateTime(source, names);
        return dateTime?.Date;
    }

    private static DateTime? ReadDateTime(JObject source, params string[] names)
    {
        var token = ReadToken(source, names);
        if (token is null || token.Type == JTokenType.Null)
        {
            return null;
        }

        if (token.Type == JTokenType.Date)
        {
            return token.Value<DateTime>();
        }

        var value = token.ToString().Trim();
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var invariant))
        {
            return invariant;
        }

        return DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var local)
            ? local
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

    private static string NormalizeTime(string value)
    {
        if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var time))
        {
            return time.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dateTime))
        {
            return dateTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        }

        return value.Length > 8 ? value[..8] : value;
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

        if (record.Result is not ("OK" or "NG"))
        {
            missingFields.Add("result");
        }

        if (missingFields.Count > 0)
        {
            throw new FormatException($"MQTT payload missing/invalid fields: {string.Join(", ", missingFields)}.");
        }
    }
}
