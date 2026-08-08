using System.Globalization;
using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using Web.API.Domain.Production;

namespace Web.API.Reports;

public static class LeakTestWorkRecordReportBuilder
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string TemplateFileName = "LeakTestWorkRecordTemplate.xlsx";
    public const string LogoFileName = "YanmarLogo.png";
    public const string FrontendLogoRelativePath = "Frontend/public/images/logo/yanmar-logo.png";

    private static readonly CultureInfo ReportCulture = CultureInfo.InvariantCulture;

    public static byte[] Build(LeakTestWorkRecord record, string templatePath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Leak Test Report");
        var templateDirectory = Path.GetDirectoryName(templatePath) ?? AppContext.BaseDirectory;
        var logoPath = ResolveLogoPath(templateDirectory);

        ApplyTemplatePolish(worksheet);
        FillRecord(worksheet, record, logoPath);

        workbook.Properties.Title = $"Leak Test Result - {record.EngineNumber}";
        workbook.Properties.Author = "Leaktester Work Record";
        workbook.Properties.Company = "PT. Yanmar Diesel Indonesia";
        workbook.Properties.Subject = "Leak Test Work Record Export";
        workbook.Properties.Created = DateTime.Now;
        workbook.Properties.Modified = DateTime.Now;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static string BuildFileName(LeakTestWorkRecord record)
    {
        var modelName = SanitizeFileName(record.EngineModelName);
        var engineNumber = SanitizeFileName(record.EngineNumber);
        var judgement = SanitizeFileName(record.Result).ToUpperInvariant();
        return $"{modelName}_{engineNumber}_{record.CheckDate:yyyyMMdd}_Judgement_{judgement}.xlsx";
    }

    private static void FillRecord(IXLWorksheet worksheet, LeakTestWorkRecord record, string logoPath)
    {
        TryAddYanmarLogo(worksheet, logoPath);

        SetText(worksheet, "D2:K3", "PT. Yanmar Diesel Indonesia");
        SetText(worksheet, "D4:K4", "LEAK TEST RESULT");

        SetText(worksheet, "B7:K7", "ENGINE INFORMATION");
        SetLabelRow(worksheet, 8, "Engine Model", record.EngineModelName);
        SetLabelRow(worksheet, 9, "Serial Number", record.EngineNumber);
        SetLabelRow(worksheet, 10, "Barcode Scan", record.BarcodeScan ?? "-");
        SetLabelRow(worksheet, 11, "Date", FormatDate(record.CheckDate));
        SetLabelRow(worksheet, 12, "Time", FormatTime(record.CheckTime));
        SetLabelRow(worksheet, 13, "Operator Name", string.IsNullOrWhiteSpace(record.OperatorName) ? "-" : record.OperatorName);

        SetText(worksheet, "B16:K16", "LEAK TEST");
        SetLabelRow(worksheet, 17, "Parameter Range (TP LL ~ TP UL)", record.ParameterLimit ?? "-");
        SetLabelRow(worksheet, 18, "Pressure Input (Result)", FormatPressure(record.PressureInput));
        SetLabelRow(worksheet, 19, "Cycle Time", FormatMinutes(record.CycleTimeLeakTestMinutes));
        SetLabelRow(worksheet, 20, "Judgement", $"{record.Result} (by Cosmo)");

        SetLabelRow(worksheet, 23, "Created At", FormatDateTime(record.CreatedAt));
        SetLabelRow(worksheet, 24, "Updated At", FormatDateTime(record.UpdatedAt));
        SetLabelRow(worksheet, 25, "Generated At", FormatDateTime(DateTime.Now));

        SetText(worksheet, "B45:D45", "Foreman");
        SetText(worksheet, "E45:G45", "Supervisor");
        SetText(worksheet, "H45:K45", "Manager");

        var resultRange = worksheet.Range("E20:K20");
        var passed = string.Equals(record.Result, "OK", StringComparison.OrdinalIgnoreCase);
        resultRange.Style.Fill.BackgroundColor = passed ? XLColor.FromHtml("#DCFCE7") : XLColor.FromHtml("#FFE4E6");
        resultRange.Style.Font.FontColor = passed ? XLColor.FromHtml("#166534") : XLColor.FromHtml("#BE123C");
        resultRange.Style.Font.Bold = true;
        resultRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static bool TryAddYanmarLogo(IXLWorksheet worksheet, string logoPath)
    {
        if (!File.Exists(logoPath))
        {
            return false;
        }

        using var logoStream = File.OpenRead(logoPath);
        worksheet.AddPicture(logoStream, XLPictureFormat.Png, "Yanmar Mark")
            .MoveTo(worksheet.Cell("B2"), 54, 6)
            .WithSize(92, 68);
        return true;
    }

    public static string ResolveLogoPath(string templateDirectory)
    {
        var frontendLogoPath = Path.GetFullPath(Path.Combine(templateDirectory, "..", "..", "..", FrontendLogoRelativePath));
        return File.Exists(frontendLogoPath)
            ? frontendLogoPath
            : Path.Combine(templateDirectory, LogoFileName);
    }

    private static void ApplyTemplatePolish(IXLWorksheet worksheet)
    {
        foreach (var picture in worksheet.Pictures.ToList())
        {
            picture.Delete();
        }

        worksheet.Range("A1:M60").Unmerge();
        worksheet.Range("A1:M60").Clear(XLClearOptions.All);

        worksheet.Style.Font.FontName = "Calibri";
        worksheet.Style.Font.FontSize = 10;
        worksheet.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.PageSetup.PageOrientation = XLPageOrientation.Portrait;
        worksheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
        worksheet.PageSetup.FitToPages(1, 1);
        worksheet.PageSetup.CenterHorizontally = true;
        worksheet.PageSetup.CenterVertically = true;
        worksheet.PageSetup.Margins.Top = 0.25;
        worksheet.PageSetup.Margins.Bottom = 0.25;
        worksheet.PageSetup.Margins.Left = 0.2;
        worksheet.PageSetup.Margins.Right = 0.2;
        worksheet.PageSetup.Margins.Header = 0.1;
        worksheet.PageSetup.Margins.Footer = 0.1;
        worksheet.PageSetup.PrintAreas.Clear();
        worksheet.PageSetup.PrintAreas.Add("B2:K54");

        foreach (var address in new[]
        {
            "B2:C4", "D2:K3", "D4:K4",
            "B7:K7",
            "B8:C8", "E8:K8",
            "B9:C9", "E9:K9",
            "B10:C10", "E10:K10",
            "B11:C11", "E11:K11",
            "B12:C12", "E12:K12",
            "B13:C13", "E13:K13",
            "B16:K16",
            "B17:C17", "E17:K17",
            "B18:C18", "E18:K18",
            "B19:C19", "E19:K19",
            "B20:C20", "E20:K20",
            "B23:C23", "E23:K23",
            "B24:C24", "E24:K24",
            "B25:C25", "E25:K25",
            "B45:D45", "E45:G45", "H45:K45",
            "B46:D54", "E46:G54", "H46:K54"
        })
        {
            worksheet.Range(address).Merge();
        }

        worksheet.Columns("A:L").Width = 3;
        worksheet.Column("B").Width = 18;
        worksheet.Column("C").Width = 14;
        worksheet.Column("D").Width = 3;
        foreach (var column in new[] { "E", "F", "G", "H", "I", "J", "K" })
        {
            worksheet.Column(column).Width = 8.6;
        }

        worksheet.Row(1).Height = 8;
        worksheet.Row(2).Height = 24;
        worksheet.Row(3).Height = 24;
        worksheet.Row(4).Height = 24;
        worksheet.Row(5).Height = 10;
        worksheet.Row(6).Height = 8;
        worksheet.Row(7).Height = 23;
        worksheet.Rows("8:13").Height = 22;
        worksheet.Row(14).Height = 12;
        worksheet.Row(15).Height = 8;
        worksheet.Row(16).Height = 23;
        worksheet.Rows("17:20").Height = 22;
        worksheet.Row(21).Height = 12;
        worksheet.Row(22).Height = 8;
        worksheet.Rows("23:25").Height = 20;
        worksheet.Rows("26:44").Height = 10;
        worksheet.Row(45).Height = 18;
        worksheet.Rows("46:54").Height = 18;

        var reportFrame = worksheet.Range("B2:K54");
        reportFrame.Style.Fill.BackgroundColor = XLColor.White;

        var headerFrame = worksheet.Range("B2:K4");
        headerFrame.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        headerFrame.Style.Border.OutsideBorderColor = XLColor.Black;
        headerFrame.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        headerFrame.Style.Border.InsideBorderColor = XLColor.Black;

        var middleSpacer = worksheet.Range("B26:K44");
        middleSpacer.Style.Border.OutsideBorder = XLBorderStyleValues.None;
        middleSpacer.Style.Border.InsideBorder = XLBorderStyleValues.None;

        worksheet.Range("B2:C4").Style.Fill.BackgroundColor = XLColor.White;
        worksheet.Range("B2:C4").Style.Font.FontColor = XLColor.FromHtml("#0F172A");
        worksheet.Range("B2:C4").Style.Font.Bold = true;
        worksheet.Range("B2:C4").Style.Font.FontSize = 14;
        worksheet.Range("B2:C4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Range("B2:C4").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        worksheet.Range("D2:K3").Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
        worksheet.Range("D2:K3").Style.Font.FontColor = XLColor.FromHtml("#0F172A");
        worksheet.Range("D2:K3").Style.Font.Bold = true;
        worksheet.Range("D2:K3").Style.Font.FontSize = 15;
        worksheet.Range("D2:K3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Range("D4:K4").Style.Fill.BackgroundColor = XLColor.FromHtml("#0F172A");
        worksheet.Range("D4:K4").Style.Font.FontColor = XLColor.White;
        worksheet.Range("D4:K4").Style.Font.Bold = true;
        worksheet.Range("D4:K4").Style.Font.FontSize = 14;
        worksheet.Range("D4:K4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        foreach (var address in new[] { "B7:K7", "B16:K16" })
        {
            var section = worksheet.Range(address);
            section.Style.Fill.BackgroundColor = XLColor.FromHtml("#D71920");
            section.Style.Font.FontColor = XLColor.White;
            section.Style.Font.Bold = true;
            section.Style.Font.FontSize = 10;
            section.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            section.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        foreach (var row in new[] { 8, 9, 10, 11, 12, 13, 17, 18, 19, 20, 23, 24, 25 })
        {
            var labelRange = worksheet.Range($"B{row}:C{row}");
            var colonCell = worksheet.Cell($"D{row}");
            var valueRange = worksheet.Range($"E{row}:K{row}");

            labelRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
            labelRange.Style.Font.FontColor = XLColor.FromHtml("#334155");
            labelRange.Style.Font.Bold = true;
            labelRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            labelRange.Style.Alignment.Indent = 1;
            colonCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            colonCell.Style.Font.FontColor = XLColor.FromHtml("#64748B");
            valueRange.Style.Fill.BackgroundColor = XLColor.White;
            valueRange.Style.Font.FontColor = XLColor.FromHtml("#0F172A");
            valueRange.Style.Font.Bold = true;
            valueRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            valueRange.Style.Alignment.Indent = 1;

            var rowRange = worksheet.Range($"B{row}:K{row}");
            rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            rowRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
            rowRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");
            rowRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        foreach (var row in new[] { 9, 10, 13, 17, 18, 19, 20 })
        {
            worksheet.Range($"B{row}:C{row}").Style.Font.FontColor = XLColor.Black;
            worksheet.Cell($"D{row}").Style.Font.FontColor = XLColor.Black;
        }

        worksheet.Range("B23:K25").Style.Font.FontSize = 9;
        worksheet.Range("B45:K54").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        worksheet.Range("B45:K54").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        worksheet.Range("B45:K54").Style.Border.OutsideBorderColor = XLColor.FromHtml("#64748B");
        worksheet.Range("B45:K54").Style.Border.InsideBorderColor = XLColor.FromHtml("#94A3B8");
        worksheet.Range("B45:K45").Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
        worksheet.Range("B45:K45").Style.Font.Bold = true;
        worksheet.Range("B45:K45").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Range("B46:K54").Style.Fill.BackgroundColor = XLColor.White;

        worksheet.ShowGridLines = false;
        worksheet.ActiveCell = worksheet.Cell("B2");
        worksheet.SheetView.View = XLSheetViewOptions.Normal;
        worksheet.SheetView.ZoomScale = 85;
        worksheet.SheetView.ZoomScalePageLayoutView = 85;
    }

    private static void SetLabelRow(IXLWorksheet worksheet, int row, string label, string value)
    {
        SetText(worksheet, $"B{row}:C{row}", label);
        SetText(worksheet, $"D{row}:D{row}", ":");
        SetText(worksheet, $"E{row}:K{row}", value);
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

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(character =>
            invalidChars.Contains(character) || char.IsWhiteSpace(character) ? '-' : character).ToArray());

        sanitized = string.Join("-", sanitized.Split('-', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(sanitized) ? "work-record" : sanitized;
    }
}
