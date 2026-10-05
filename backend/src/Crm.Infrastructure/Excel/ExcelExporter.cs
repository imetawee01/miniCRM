using ClosedXML.Excel;
using Crm.Application.Abstractions;

namespace Crm.Infrastructure.Excel;

public sealed class ExcelExporter : IExcelExporter
{
    public byte[] Export<T>(string sheetName, IReadOnlyList<(string Header, Func<T, object?> Value)> columns, IEnumerable<T> rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(string.IsNullOrWhiteSpace(sheetName) ? "Sheet1" : sheetName[..Math.Min(sheetName.Length, 31)]);

        for (var c = 0; c < columns.Count; c++)
            ws.Cell(1, c + 1).Value = columns[c].Header;

        ws.Row(1).Style.Font.Bold = true;
        ws.SheetView.FreezeRows(1);

        var r = 2;
        foreach (var row in rows)
        {
            for (var c = 0; c < columns.Count; c++)
            {
                var value = columns[c].Value(row);
                var cell = ws.Cell(r, c + 1);
                switch (value)
                {
                    case null:
                        cell.Value = Blank.Value;
                        break;
                    case DateTime dt:
                        cell.Value = dt;
                        cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
                        break;
                    case decimal d:
                        cell.Value = d;
                        cell.Style.NumberFormat.Format = "#,##0.00";
                        break;
                    case int i:
                        cell.Value = i;
                        break;
                    case long l:
                        cell.Value = l;
                        break;
                    case double db:
                        cell.Value = db;
                        cell.Style.NumberFormat.Format = "0.0";
                        break;
                    case bool b:
                        cell.Value = b;
                        break;
                    default:
                        cell.Value = value.ToString();
                        break;
                }
            }
            r++;
        }

        if (r > 2 && columns.Count > 0)
            ws.Range(1, 1, r - 1, columns.Count).SetAutoFilter();

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
