using Aspose.Cells;
using CJ.Plug.Models.Services;
using ExcelPlug.Models;

namespace ExcelPlug.Utils
{
    public static class ExcelService
    {
        /// <summary>
        /// 读取 Excel 文件预览数据（默认第一页，最多 200 行）
        /// </summary>
        public static ExcelPreviewData ReadSheetPreview(Stream fileStream, int maxRows = 200)
        {
            HookAspose();
            var workbook = new Workbook(fileStream);
            var worksheet = workbook.Worksheets[0];
            var cells = worksheet.Cells;

            var result = new ExcelPreviewData
            {
                SheetName = worksheet.Name
            };

            int maxRow = cells.MaxDataRow;
            int maxCol = cells.MaxDataColumn;

            if (maxRow < 0 || maxCol < 0)
                return result;

            result.TotalRows = maxRow + 1;
            result.TotalColumns = maxCol + 1;

            // 列头用 Excel 字母命名
            result.Headers = Enumerable.Range(0, maxCol + 1)
                .Select(ColumnIndexToLetter)
                .ToList();

            int displayCount = Math.Min(maxRow + 1, maxRows);
            result.IsTruncated = maxRow + 1 > maxRows;

            for (int row = 0; row < displayCount; row++)
            {
                var rowData = new List<string>();
                for (int col = 0; col <= maxCol; col++)
                {
                    var cell = cells.CheckCell(row, col);
                    rowData.Add(cell?.StringValue ?? "");
                }
                result.Rows.Add(rowData);
            }

            return result;
        }

        private static string ColumnIndexToLetter(int colIndex)
        {
            int dividend = colIndex + 1;
            var sb = new System.Text.StringBuilder();
            while (dividend > 0)
            {
                dividend--;
                sb.Insert(0, (char)('A' + dividend % 26));
                dividend /= 26;
            }
            return sb.ToString();
        }

        public static void SetCellValue(string filePath, string cellRef, string value)
        {
            HookAspose();
            var workbook = new Workbook(filePath);
            var cell = workbook.Worksheets[0].Cells[cellRef];
            cell.PutValue(value);
            workbook.Save(filePath);
        }

        public static string? GetCellValue(string filePath, string cellRef)
        {
            HookAspose();
            var workbook = new Workbook(filePath);
            var cell = workbook.Worksheets[0].Cells[cellRef];
            return cell.StringValue;
        }

        public static void SetCellValue(string filePath, string sheetName, string cellRef, string value)
        {
            HookAspose();
            var workbook = new Workbook(filePath);
            var worksheet = workbook.Worksheets[sheetName];
            var cell = worksheet.Cells[cellRef];
            cell.PutValue(value);
            workbook.Save(filePath);
        }

        static void HookAspose()
        {
            HookManager.ShowHookDetails(false);
            HookManager.StartHook();
        }
    }
}
