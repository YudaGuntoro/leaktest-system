using System.Globalization;
using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using Web.API.Domain.Production;

namespace Web.API.Reports;

public static class LeakTestWorkRecordListReportBuilder
{
    public const string ContentType = LeakTestWorkRecordReportBuilder.ContentType;

    private const int HeaderRow = 12;
    private const int FirstDataRow = HeaderRow + 1;
    private static readonly CultureInfo ReportCulture = CultureInfo.InvariantCulture;

    public static byte[] Build(
        IReadOnlyList<LeakTestWorkRecord> records,
        DateTime? dateFrom,
        DateTime? dateTo,
        string templatePath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Leak Test List");
        var templateDirectory = Path.GetDirectoryName(templatePath) ?? AppContext.BaseDirectory;
        var logoPath = LeakTestWorkRecordReportBuilder.ResolveLogoPath(templateDirectory);

        ApplyLayout(worksheet);
        FillHeader(worksheet, records, dateFrom, dateTo, logoPath);
        var lastRow = FillTable(worksheet, records);

        worksheet.PageSetup.PrintAreas.Clear();
        worksheet.PageSetup.PrintAreas.Add($"B2:M{lastRow}");

        workbook.Properties.Title = "Leak Test Work Record List";
        workbook.Properties.Author = "Leaktester Work Record";
        workbook.Properties.Company = "PT. Yanmar Diesel Indonesia";
        workbook.Properties.Subject = "Leak Test Work Record List Export";
        workbook.Properties.Created = DateTime.Now;
        workbook.Properties.Modified = DateTime.Now;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static string BuildFileName(DateTime? dateFrom, DateTime? dateTo)
    {
        var period = FormatFilePeriod(dateFrom, dateTo);
        return $"LeakTest_WorkRecord_List_{period}.xlsx";
    }

    private static void ApplyLayout(IXLWorksheet worksheet)
    {
        worksheet.Style.Font.FontName = "Calibri";
        worksheet.Style.Font.FontSize = 10;
        worksheet.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.ShowGridLines = false;
        worksheet.SheetView.View = XLSheetViewOptions.Normal;
        worksheet.SheetView.ZoomScale = 90;

        worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        worksheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
        worksheet.PageSetup.CenterHorizontally = true;
        worksheet.PageSetup.Margins.Top = 0.28;
        worksheet.PageSetup.Margins.Bottom = 0.28;
        worksheet.PageSetup.Margins.Left = 0.2;
        worksheet.PageSetup.Margins.Right = 0.2;
        worksheet.PageSetup.Margins.Header = 0.1;
        worksheet.PageSetup.Margins.Footer = 0.1;
        worksheet.PageSetup.PagesWide = 1;
        worksheet.PageSetup.SetRowsToRepeatAtTop(HeaderRow, HeaderRow);

        worksheet.Column("A").Width = 2;
        worksheet.Column("B").Width = 5;
        worksheet.Column("C").Width = 16;
        worksheet.Column("D").Width = 20;
        worksheet.Column("E").Width = 23;
        worksheet.Column("F").Width = 16;
        worksheet.Column("G").Width = 12;
        worksheet.Column("H").Width = 10;
        worksheet.Column("I").Width = 24;
        worksheet.Column("J").Width = 15;
        worksheet.Column("K").Width = 15;
        worksheet.Column("L").Width = 15;
        worksheet.Column("M").Width = 11;
        worksheet.Column("N").Width = 2;

        worksheet.Row(1).Height = 8;
        worksheet.Row(2).Height = 24;
        worksheet.Row(3).Height = 24;
        worksheet.Row(4).Height = 22;
        worksheet.Row(5).Height = 10;
        worksheet.Row(6).Height = 22;
        worksheet.Rows("7:8").Height = 20;
        worksheet.Row(9).Height = 8;
        worksheet.Row(10).Height = 22;
        worksheet.Row(11).Height = 8;
        worksheet.Row(HeaderRow).Height = 24;

        foreach (var address in new[]
        {
            "B2:C4", "D2:M3", "D4:M4",
            "B6:M6",
            "B7:C7", "E7:M7",
            "B8:C8", "E8:M8",
            "B10:C10", "E10:F10", "H10:I10"
        })
        {
            worksheet.Range(address).Merge();
        }

        worksheet.Range("B2:M4").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        worksheet.Range("B2:M4").Style.Border.OutsideBorderColor = XLColor.Black;
        worksheet.Range("B2:M4").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        worksheet.Range("B2:M4").Style.Border.InsideBorderColor = XLColor.Black;

        worksheet.Range("B2:C4").Style.Fill.BackgroundColor = XLColor.White;
        worksheet.Range("B2:C4").Style.Font.FontColor = XLColor.FromHtml("#0F172A");
        worksheet.Range("B2:C4").Style.Font.Bold = true;
        worksheet.Range("B2:C4").Style.Font.FontSize = 14;
        worksheet.Range("B2:C4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Range("D2:M3").Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
        worksheet.Range("D2:M3").Style.Font.FontColor = XLColor.FromHtml("#0F172A");
        worksheet.Range("D2:M3").Style.Font.Bold = true;
        worksheet.Range("D2:M3").Style.Font.FontSize = 15;
        worksheet.Range("D2:M3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Range("D4:M4").Style.Fill.BackgroundColor = XLColor.FromHtml("#0F172A");
        worksheet.Range("D4:M4").Style.Font.FontColor = XLColor.White;
        worksheet.Range("D4:M4").Style.Font.Bold = true;
        worksheet.Range("D4:M4").Style.Font.FontSize = 12;
        worksheet.Range("D4:M4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        var section = worksheet.Range("B6:M6");
        section.Style.Fill.BackgroundColor = XLColor.FromHtml("#D71920");
        section.Style.Font.FontColor = XLColor.White;
        section.Style.Font.Bold = true;
        section.Style.Font.FontSize = 10;
        section.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        foreach (var row in new[] { 7, 8 })
        {
            var labelRange = worksheet.Range($"B{row}:C{row}");
            var colonCell = worksheet.Cell($"D{row}");
            var valueRange = worksheet.Range($"E{row}:M{row}");

            labelRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
            labelRange.Style.Font.FontColor = XLColor.FromHtml("#334155");
            labelRange.Style.Font.Bold = true;
            labelRange.Style.Alignment.Indent = 1;
            colonCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            colonCell.Style.Font.FontColor = XLColor.FromHtml("#64748B");
            valueRange.Style.Fill.BackgroundColor = XLColor.White;
            valueRange.Style.Font.FontColor = XLColor.FromHtml("#0F172A");
            valueRange.Style.Font.Bold = true;
            valueRange.Style.Alignment.Indent = 1;

            var rowRange = worksheet.Range($"B{row}:M{row}");
            rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            rowRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
            rowRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");
        }

        var summary = worksheet.Range("B10:J10");
        summary.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
        summary.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        summary.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        summary.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
        summary.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");
        summary.Style.Font.Bold = true;
        summary.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        summary.Style.Font.FontColor = XLColor.Black;

        worksheet.Range("B10:D10").Style.Fill.BackgroundColor = XLColor.FromHtml("#FFC000");
        worksheet.Range("E10:G10").Style.Fill.BackgroundColor = XLColor.FromHtml("#92D050");
        worksheet.Range("H10:J10").Style.Fill.BackgroundColor = XLColor.FromHtml("#FF0000");
        worksheet.Range("B10:J10").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        worksheet.Range("B10:J10").Style.Border.OutsideBorderColor = XLColor.FromHtml("#FF0000");
        worksheet.Range("B10:J10").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        worksheet.Range("B10:J10").Style.Border.InsideBorderColor = XLColor.White;
        worksheet.Range("B10:C10").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        worksheet.Range("B10:C10").Style.Alignment.Indent = 1;

        var tableHeader = worksheet.Range("B12:M12");
        tableHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#D71920");
        tableHeader.Style.Font.FontColor = XLColor.White;
        tableHeader.Style.Font.Bold = true;
        tableHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        tableHeader.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableHeader.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        tableHeader.Style.Border.OutsideBorderColor = XLColor.FromHtml("#D71920");
        tableHeader.Style.Border.InsideBorderColor = XLColor.FromHtml("#FCA5A5");
    }

    private static void FillHeader(
        IXLWorksheet worksheet,
        IReadOnlyList<LeakTestWorkRecord> records,
        DateTime? dateFrom,
        DateTime? dateTo,
        string logoPath)
    {
        TryAddYanmarLogo(worksheet, logoPath);

        SetText(worksheet, "D2:M3", "PT. Yanmar Diesel Indonesia");
        SetText(worksheet, "D4:M4", "LEAK TEST WORK RECORD LIST");

        SetText(worksheet, "B6:M6", "REPORT FILTER");
        SetLabelRow(worksheet, 7, "Period", FormatPeriod(dateFrom, dateTo));
        SetLabelRow(worksheet, 8, "Generated At", FormatDateTime(DateTime.Now));

        SetText(worksheet, "B10:C10", "Total Records");
        SetText(worksheet, "D10:D10", records.Count.ToString(ReportCulture));
        SetText(worksheet, "E10:F10", "OK");
        SetText(worksheet, "G10:G10", records.Count(x => string.Equals(x.Result, "OK", StringComparison.OrdinalIgnoreCase)).ToString(ReportCulture));
        SetText(worksheet, "H10:I10", "NG");
        SetText(worksheet, "J10:J10", records.Count(x => string.Equals(x.Result, "NG", StringComparison.OrdinalIgnoreCase)).ToString(ReportCulture));
    }

    private static int FillTable(IXLWorksheet worksheet, IReadOnlyList<LeakTestWorkRecord> records)
    {
        var headers = new[]
        {
            "No",
            "Engine Model",
            "Serial No",
            "Barcode Scan",
            "Operator",
            "Date",
            "Time",
            "Parameter Range (TP LL ~ TP UL)",
            "Pressure Input",
            "Cycle Time",
            "Judgement",
            "Result"
        };

        for (var index = 0; index < headers.Length; index++)
        {
            worksheet.Cell(HeaderRow, 2 + index).Value = headers[index];
        }

        if (records.Count == 0)
        {
            var emptyRange = worksheet.Range("B13:M15");
            emptyRange.Merge();
            emptyRange.FirstCell().Value = "No work records for selected filter.";
            emptyRange.Style.Fill.BackgroundColor = XLColor.White;
            emptyRange.Style.Font.FontColor = XLColor.FromHtml("#64748B");
            emptyRange.Style.Font.Bold = true;
            emptyRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            emptyRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            emptyRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            emptyRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
            worksheet.Rows("13:15").Height = 22;
            worksheet.ActiveCell = worksheet.Cell("B2");
            return 15;
        }

        for (var index = 0; index < records.Count; index++)
        {
            var record = records[index];
            var row = FirstDataRow + index;

            worksheet.Cell(row, 2).Value = index + 1;
            worksheet.Cell(row, 3).Value = record.EngineModelName;
            worksheet.Cell(row, 4).Value = record.EngineNumber;
            worksheet.Cell(row, 5).Value = string.IsNullOrWhiteSpace(record.BarcodeScan) ? "-" : record.BarcodeScan;
            worksheet.Cell(row, 6).Value = string.IsNullOrWhiteSpace(record.OperatorName) ? "-" : record.OperatorName;
            worksheet.Cell(row, 7).Value = FormatDate(record.CheckDate);
            worksheet.Cell(row, 8).Value = FormatTime(record.CheckTime);
            worksheet.Cell(row, 9).Value = string.IsNullOrWhiteSpace(record.ParameterLimit) ? "-" : record.ParameterLimit;
            worksheet.Cell(row, 10).Value = FormatPressure(record.PressureInput);
            worksheet.Cell(row, 11).Value = FormatMinutes(record.CycleTimeLeakTestMinutes);
            worksheet.Cell(row, 12).Value = FormatJudgement(record);
            worksheet.Cell(row, 13).Value = record.Result;

            var rowRange = worksheet.Range(row, 2, row, 13);
            rowRange.Style.Fill.BackgroundColor = index % 2 == 0 ? XLColor.White : XLColor.FromHtml("#F8FAFC");
            rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            rowRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
            rowRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");
            rowRange.Style.Font.FontSize = 9;
            rowRange.Style.Font.FontColor = XLColor.FromHtml("#0F172A");
            rowRange.Style.Alignment.WrapText = true;

            worksheet.Range(row, 2, row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range(row, 7, row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range(row, 9, row, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range(row, 3, row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            var passed = string.Equals(record.Result, "OK", StringComparison.OrdinalIgnoreCase);
            var resultCell = worksheet.Cell(row, 13);
            resultCell.Style.Fill.BackgroundColor = passed ? XLColor.FromHtml("#DCFCE7") : XLColor.FromHtml("#FFE4E6");
            resultCell.Style.Font.FontColor = passed ? XLColor.FromHtml("#166534") : XLColor.FromHtml("#BE123C");
            resultCell.Style.Font.Bold = true;
            resultCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Row(row).Height = 22;
        }

        var lastRow = FirstDataRow + records.Count - 1;
        worksheet.Range(HeaderRow, 2, lastRow, 13).SetAutoFilter();
        worksheet.SheetView.FreezeRows(HeaderRow);
        worksheet.ActiveCell = worksheet.Cell("B2");
        return lastRow;
    }

    private static bool TryAddYanmarLogo(IXLWorksheet worksheet, string logoPath)
    {
        if (!File.Exists(logoPath))
        {
            return false;
        }

        using var logoStream = File.OpenRead(logoPath);
        worksheet.AddPicture(logoStream, XLPictureFormat.Png, "Yanmar Mark")
            .MoveTo(worksheet.Cell("B2"), 25, 6)
            .WithSize(92, 68);
        return true;
    }

    private static void SetLabelRow(IXLWorksheet worksheet, int row, string label, string value)
    {
        SetText(worksheet, $"B{row}:C{row}", label);
        SetText(worksheet, $"D{row}:D{row}", ":");
        SetText(worksheet, $"E{row}:M{row}", value);
    }

    private static string FormatJudgement(LeakTestWorkRecord record)
    {
        if (!record.JudgementCode.HasValue)
        {
            return string.IsNullOrWhiteSpace(record.JudgementName) ? "-" : record.JudgementName;
        }

        return string.IsNullOrWhiteSpace(record.JudgementName)
            ? record.JudgementCode.Value.ToString(ReportCulture)
            : $"{record.JudgementCode.Value.ToString(ReportCulture)} - {record.JudgementName}";
    }

    private static void SetText(IXLWorksheet worksheet, string address, string value)
    {
        var range = worksheet.Range(address);
        range.FirstCell().Value = string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    private static string FormatPressure(decimal value) =>
        $"{value.ToString("0.00", ReportCulture)} MPa";

    private static string FormatMinutes(decimal value) =>
        $"{value.ToString("0.##", ReportCulture)} menit";

    private static string FormatDate(DateTime value) =>
        value.ToString("dd MMM yyyy", ReportCulture);

    private static string FormatDateTime(DateTime value) =>
        value.ToString("dd MMM yyyy, HH:mm", ReportCulture);

    private static string FormatTime(string value)
    {
        return TimeSpan.TryParse(value, ReportCulture, out var time)
            ? time.ToString(@"hh\:mm", ReportCulture)
            : value;
    }

    private static string FormatPeriod(DateTime? dateFrom, DateTime? dateTo)
    {
        if (dateFrom.HasValue && dateTo.HasValue)
        {
            return dateFrom.Value.Date == dateTo.Value.Date
                ? FormatDate(dateFrom.Value)
                : $"{FormatDate(dateFrom.Value)} - {FormatDate(dateTo.Value)}";
        }

        if (dateFrom.HasValue)
        {
            return $"From {FormatDate(dateFrom.Value)}";
        }

        if (dateTo.HasValue)
        {
            return $"Until {FormatDate(dateTo.Value)}";
        }

        return "All Dates";
    }

    private static string FormatFilePeriod(DateTime? dateFrom, DateTime? dateTo)
    {
        if (dateFrom.HasValue && dateTo.HasValue)
        {
            var start = dateFrom.Value.ToString("yyyyMMdd", ReportCulture);
            var end = dateTo.Value.ToString("yyyyMMdd", ReportCulture);
            return start == end ? start : $"{start}-{end}";
        }

        if (dateFrom.HasValue)
        {
            return $"from-{dateFrom.Value.ToString("yyyyMMdd", ReportCulture)}";
        }

        if (dateTo.HasValue)
        {
            return $"until-{dateTo.Value.ToString("yyyyMMdd", ReportCulture)}";
        }

        return "all";
    }
}
