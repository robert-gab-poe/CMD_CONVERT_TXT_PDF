using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ClosedXML.Excel;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace CMD_CONVERT_TXT_PDF
{
    class Program
    {
        static void Main(string[] args)
        {
            string sourceFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TXT_TO_PDF");
            string logFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files-txt");
            string logFile = Path.Combine(logFolder, "LOG_CMD_TXT_PDF");

            if (!Directory.Exists(sourceFolder))
            {
                Directory.CreateDirectory(sourceFolder);
                LogMessage(logFile, "ERROR", $"Carpeta origen no existe, se ha creado: {sourceFolder}", "SISTEMA");
            }

            if (!Directory.Exists(logFolder))
            {
                Directory.CreateDirectory(logFolder);
            }

            string fileName;

            if (args.Length > 0)
            {
                fileName = args[0];
                ProcessFile(fileName, sourceFolder, logFile);
            }
            else
            {
                Console.WriteLine("=== Conversor TXT/CSV/XLSX/BMP/JPG a PDF ===");
                Console.WriteLine($"Carpeta de trabajo: {sourceFolder}");
                Console.WriteLine("Extensiones soportadas: .txt, .csv, .xlsx, .bmp, .jpg, .jpeg");
                Console.WriteLine("El archivo original se ELIMINA tras la conversion exitosa.");
                Console.WriteLine();

                while (true)
                {
                    Console.Write("Nombre del archivo (o ENTER para salir): ");
                    fileName = Console.ReadLine()?.Trim();

                    if (string.IsNullOrEmpty(fileName))
                    {
                        break;
                    }

                    ProcessFile(fileName, sourceFolder, logFile);
                    Console.WriteLine();
                }

                Console.WriteLine("Presione cualquier tecla para salir...");
                Console.ReadKey();
            }
        }

        static void ProcessFile(string fileName, string sourceFolder, string logFile)
        {
            string filePath = Path.Combine(sourceFolder, fileName);

            if (!File.Exists(filePath))
            {
                LogMessage(logFile, "ERROR", $"Archivo no encontrado: {fileName}", fileName);
                Console.WriteLine($"Error: Archivo no encontrado: {fileName}");
                return;
            }

            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            string type = ext switch
            {
                ".txt" => "txt",
                ".csv" => "csv",
                ".xlsx" => "xlsx",
                ".bmp" => "bmp",
                ".jpg" => "jpg",
                ".jpeg" => "jpeg",
                _ => null
            };

            if (type == null)
            {
                LogMessage(logFile, "ERROR", $"Extension no soportada: {ext}", fileName);
                Console.WriteLine($"Error: Extension no soportada: {ext}");
                return;
            }

            string pdfFileName = Path.ChangeExtension(fileName, ".pdf");
            string pdfPath = Path.Combine(sourceFolder, pdfFileName);

            try
            {
                if (type == "txt")
                {
                    string content = File.ReadAllText(filePath, Encoding.UTF8);
                    ConvertTextToPdf(content, pdfPath);
                }
                else if (type == "csv")
                {
                    List<List<string>> rows = ReadCsv(filePath);
                    var sheets = new List<TableSheet>
                    {
                        new TableSheet { Name = Path.GetFileNameWithoutExtension(fileName), Rows = rows }
                    };
                    ConvertDataToPdf(sheets, pdfPath);
                }
                else if (type == "xlsx")
                {
                    ConvertDataToPdf(ReadExcel(filePath), pdfPath);
                }
                else
                {
                    ConvertImageToPdf(filePath, pdfPath);
                }

                File.Delete(filePath);
                LogMessage(logFile, "EXITO", "Conversion completada y archivo original eliminado", fileName);
                Console.WriteLine($"OK: {fileName} -> {pdfFileName}");
            }
            catch (Exception ex)
            {
                LogMessage(logFile, "ERROR", ex.Message, fileName);
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static void ConvertTextToPdf(string text, string outputPath)
        {
            PdfDocument document = new PdfDocument();
            PdfPage page = document.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);
            XFont font = new XFont("Verdana", 10);
            PdfSharp.Drawing.Layout.XTextFormatter tf = new PdfSharp.Drawing.Layout.XTextFormatter(gfx);

            double margin = 40;
            XRect rect = new XRect(margin, margin, page.Width - 2 * margin, page.Height - 2 * margin);
            tf.Alignment = PdfSharp.Drawing.Layout.XParagraphAlignment.Left;
            tf.DrawString(text, font, XBrushes.Black, rect);

            document.Save(outputPath);
        }

        static void ConvertImageToPdf(string imagePath, string outputPath)
        {
            using (var image = XImage.FromFile(imagePath))
            {
                PdfDocument document = new PdfDocument();
                PdfPage page = document.AddPage();
                XGraphics gfx = XGraphics.FromPdfPage(page);

                double margin = 20;
                double maxWidth = page.Width - 2 * margin;
                double maxHeight = page.Height - 2 * margin;

                double imgWidth = image.PixelWidth * 72.0 / image.HorizontalResolution;
                double imgHeight = image.PixelHeight * 72.0 / image.VerticalResolution;

                double scale = Math.Min(maxWidth / imgWidth, maxHeight / imgHeight);
                double drawWidth = imgWidth * scale;
                double drawHeight = imgHeight * scale;

                double x = (page.Width - drawWidth) / 2;
                double y = (page.Height - drawHeight) / 2;

                gfx.DrawImage(image, x, y, drawWidth, drawHeight);

                document.Save(outputPath);
            }
        }

        static List<List<string>> ReadCsv(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            Encoding encoding = Encoding.UTF8;
            int start = 0;

            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                encoding = new UTF8Encoding(false);
                start = 3;
            }
            else if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                encoding = Encoding.Unicode;
                start = 2;
            }
            else if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                encoding = Encoding.BigEndianUnicode;
                start = 2;
            }

            string content = encoding.GetString(bytes, start, bytes.Length - start);

            if (content.IndexOf('\uFFFD') >= 0)
            {
                content = Encoding.GetEncoding(1252).GetString(bytes, start, bytes.Length - start);
            }

            char delimiter = DetectDelimiter(content);

            var result = new List<List<string>>();
            string[] lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                result.Add(SplitCsvLine(line, delimiter));
            }

            return result;
        }

        static char DetectDelimiter(string content)
        {
            int firstLineEnd = content.IndexOfAny(new[] { '\r', '\n' });
            string firstLine = firstLineEnd >= 0 ? content.Substring(0, firstLineEnd) : content;

            int commas = firstLine.Count(c => c == ',');
            int semicolons = firstLine.Count(c => c == ';');

            return semicolons > commas ? ';' : ',';
        }

        static List<string> SplitCsvLine(string line, char delimiter)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == delimiter)
                    {
                        result.Add(current.ToString().Trim());
                        current.Clear();
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
            }

            result.Add(current.ToString().Trim());
            return result;
        }

        static List<TableSheet> ReadExcel(string path)
        {
            var sheets = new List<TableSheet>();

            using (var workbook = new XLWorkbook(path))
            {
                int sheetIndex = 0;
                foreach (var ws in workbook.Worksheets)
                {
                    sheets.Add(ReadWorksheet(ws, sheetIndex++));
                }
            }

            return sheets;
        }

        static TableSheet ReadWorksheet(IXLWorksheet ws, int sheetIndex)
        {
            var rows = new List<List<string>>();
            var usedRange = ws.RangeUsed();

            if (usedRange != null)
            {
                int firstRow = usedRange.FirstRow().RowNumber();
                int lastRow = usedRange.LastRow().RowNumber();
                int firstCol = usedRange.FirstColumn().ColumnNumber();
                int lastCol = usedRange.LastColumn().ColumnNumber();

                for (int r = firstRow; r <= lastRow; r++)
                {
                    var row = new List<string>();
                    bool hasValue = false;

                    for (int c = firstCol; c <= lastCol; c++)
                    {
                        string value = ws.Cell(r, c).GetFormattedString().Trim();
                        if (!string.IsNullOrEmpty(value))
                        {
                            hasValue = true;
                        }
                        row.Add(value);
                    }

                    if (hasValue)
                    {
                        rows.Add(row);
                    }
                }
            }

            return new TableSheet { Name = string.IsNullOrWhiteSpace(ws.Name) ? $"Hoja {sheetIndex + 1}" : ws.Name, Rows = rows };
        }

        static void ConvertDataToPdf(List<TableSheet> sheets, string outputPath)
        {
            var document = new PdfDocument();
            var renderer = new PdfTableRenderer(document);

            bool anyDrawn = false;
            foreach (var sheet in sheets)
            {
                if (sheet.Rows == null || sheet.Rows.Count == 0)
                {
                    continue;
                }
                renderer.Render(sheet);
                anyDrawn = true;
            }

            renderer.Finish();

            if (!anyDrawn)
            {
                PdfPage page = document.AddPage();
                XGraphics gfx = XGraphics.FromPdfPage(page);
                XFont font = new XFont("Verdana", 12);
                gfx.DrawString("El archivo no contiene datos para convertir.", font, XBrushes.Black, 30, 60);
            }

            document.Save(outputPath);
        }

        static void LogMessage(string logFile, string status, string message, string fileName)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logEntry = $"{timestamp} | {status} | {fileName} | {message}{Environment.NewLine}";

            try
            {
                File.AppendAllText(logFile, logEntry, Encoding.UTF8);
            }
            catch
            {
            }
        }
    }

    class TableSheet
    {
        public string Name { get; set; }
        public List<List<string>> Rows { get; set; }
    }

    class PdfTableRenderer
    {
        private readonly PdfDocument _document;
        private PdfPage _page;
        private XGraphics _gfx;
        private double _y;

        private readonly XFont _titleFont = new XFont("Verdana", 12, XFontStyleEx.Bold);
        private readonly XFont _headerFont = new XFont("Verdana", 8.5, XFontStyleEx.Bold);
        private readonly XFont _bodyFont = new XFont("Verdana", 8);

        private const double Margin = 30;
        private const double CellPadding = 4;

        public PdfTableRenderer(PdfDocument document)
        {
            _document = document;
        }

        public void Render(TableSheet sheet)
        {
            if (sheet.Rows == null || sheet.Rows.Count == 0)
            {
                return;
            }

            if (_page == null || _y > Margin + 1)
            {
                NewPage();
            }

            double titleHeight = _titleFont.GetHeight();
            if (_y + titleHeight > _page.Height - Margin)
            {
                NewPage();
            }

            _gfx.DrawString(sheet.Name, _titleFont, XBrushes.Black, Margin, _y);
            _y += titleHeight + 6;

            int colCount = sheet.Rows.Max(r => r.Count);
            if (colCount == 0)
            {
                return;
            }

            double tableWidth = _page.Width - 2 * Margin;
            double[] colWidths = ComputeColumnWidths(sheet.Rows, colCount, tableWidth);
            double textLineHeight = _bodyFont.GetHeight();

            int index = 0;
            bool isFirst = true;

            foreach (var rawRow in sheet.Rows)
            {
                var cells = BuildRowCells(rawRow, colCount);
                XFont rowFont = isFirst ? _headerFont : _bodyFont;
                double rowHeight = ComputeRowHeight(cells, colWidths, rowFont, textLineHeight);

                if (_y + rowHeight > _page.Height - Margin)
                {
                    NewPage();
                    DrawRow(BuildRowCells(sheet.Rows[0], colCount), colWidths, _headerFont, true);
                }

                DrawRow(cells, colWidths, rowFont, isFirst);
                isFirst = false;
                index++;
            }
        }

        public void Finish()
        {
            if (_gfx != null)
            {
                _gfx.Dispose();
                _gfx = null;
            }
        }

        private void NewPage()
        {
            if (_gfx != null)
            {
                _gfx.Dispose();
            }

            _page = _document.AddPage();
            _gfx = XGraphics.FromPdfPage(_page);
            _y = Margin;
        }

        private void DrawRow(List<string> cells, double[] colWidths, XFont font, bool isHeader)
        {
            double rowHeight = ComputeRowHeight(cells, colWidths, font, font.GetHeight());

            double x = Margin;
            for (int c = 0; c < colWidths.Length && c < cells.Count; c++)
            {
                double cellWidth = colWidths[c];
                var rect = new XRect(x, _y, cellWidth, rowHeight);

                _gfx.DrawRectangle(isHeader ? XBrushes.LightGray : XBrushes.White, rect);
                _gfx.DrawRectangle(XPens.Gray, rect);

                var lines = WrapText(cells[c], font, cellWidth - 2 * CellPadding);
                double textLineHeight = font.GetHeight();

                for (int i = 0; i < lines.Count; i++)
                {
                    double lineY = _y + CellPadding + i * textLineHeight;
                    if (lineY + textLineHeight > _y + rowHeight)
                    {
                        break;
                    }

                    _gfx.DrawString(
                        lines[i],
                        font,
                        XBrushes.Black,
                        new XRect(x + CellPadding, lineY, cellWidth - 2 * CellPadding, textLineHeight),
                        XStringFormats.TopLeft);
                }

                x += cellWidth;
            }

            _y += rowHeight;
        }

        private double ComputeRowHeight(List<string> cells, double[] colWidths, XFont font, double textLineHeight)
        {
            double maxLines = 1;
            for (int c = 0; c < cells.Count; c++)
            {
                if (c >= colWidths.Length)
                {
                    break;
                }
                int lines = WrapText(cells[c], font, colWidths[c] - 2 * CellPadding).Count;
                maxLines = Math.Max(maxLines, lines);
            }

            double height = maxLines * textLineHeight + 2 * CellPadding;
            return Math.Max(height, textLineHeight + 2 * CellPadding);
        }

        private double[] ComputeColumnWidths(List<List<string>> rows, int colCount, double tableWidth)
        {
            double[] widths = new double[colCount];

            for (int c = 0; c < colCount; c++)
            {
                double maxWidth = 0;
                foreach (var row in rows)
                {
                    if (c < row.Count)
                    {
                        double w1 = _gfx.MeasureString(row[c], _bodyFont).Width;
                        double w2 = _gfx.MeasureString(row[c], _headerFont).Width;
                        maxWidth = Math.Max(maxWidth, Math.Max(w1, w2));
                    }
                }
                widths[c] = maxWidth + 2 * CellPadding;
            }

            NormalizeWidths(widths, tableWidth);

            for (int i = 0; i < widths.Length; i++)
            {
                widths[i] = Math.Max(widths[i], 25);
                widths[i] = Math.Min(widths[i], 220);
            }

            NormalizeWidths(widths, tableWidth);
            return widths;
        }

        private void NormalizeWidths(double[] widths, double tableWidth)
        {
            double total = widths.Sum();
            if (total > tableWidth)
            {
                double factor = tableWidth / total;
                for (int i = 0; i < widths.Length; i++)
                {
                    widths[i] *= factor;
                }
            }
        }

        private List<string> BuildRowCells(List<string> row, int colCount)
        {
            var cells = new List<string>(colCount);
            for (int c = 0; c < colCount; c++)
            {
                cells.Add(c < row.Count ? (row[c] ?? "").Replace("\r\n", " ").Replace("\n", " ") : "");
            }
            return cells;
        }

        private List<string> WrapText(string text, XFont font, double maxWidth)
        {
            var result = new List<string>();

            if (string.IsNullOrEmpty(text))
            {
                result.Add("");
                return result;
            }

            string[] segments = text.Replace("\r\n", "\n").Split('\n');

            foreach (string segment in segments)
            {
                if (segment.Length == 0)
                {
                    result.Add("");
                    continue;
                }

                string[] words = segment.Split(' ');
                var current = new StringBuilder();

                foreach (string word in words)
                {
                    if (_gfx.MeasureString(word, font).Width > maxWidth)
                    {
                        if (current.Length > 0)
                        {
                            result.Add(current.ToString());
                            current.Clear();
                        }

                        var chunks = new List<string>();
                        var chunk = new StringBuilder();
                        foreach (char c in word)
                        {
                            double testWidth = _gfx.MeasureString(chunk.ToString() + c, font).Width;
                            if (chunk.Length > 0 && testWidth > maxWidth)
                            {
                                chunks.Add(chunk.ToString());
                                chunk.Clear();
                            }
                            chunk.Append(c);
                        }
                        if (chunk.Length > 0)
                        {
                            current.Append(chunk.ToString());
                        }
                        else if (chunks.Count > 0)
                        {
                            current.Append(chunks[chunks.Count - 1]);
                            chunks.RemoveAt(chunks.Count - 1);
                        }
                        foreach (string ch in chunks)
                        {
                            result.Add(ch);
                        }
                        continue;
                    }

                    if (current.Length == 0)
                    {
                        current.Append(word);
                    }
                    else
                    {
                        double currentWidth = _gfx.MeasureString(current.ToString(), font).Width;
                        double spaceWidth = _gfx.MeasureString(" ", font).Width;
                        double wordWidth = _gfx.MeasureString(word, font).Width;

                        if (currentWidth + spaceWidth + wordWidth <= maxWidth)
                        {
                            current.Append(' ').Append(word);
                        }
                        else
                        {
                            result.Add(current.ToString());
                            current.Clear();
                            current.Append(word);
                        }
                    }
                }

                if (current.Length > 0)
                {
                    result.Add(current.ToString());
                }
            }

            return result.Count > 0 ? result : new List<string> { "" };
        }
    }
}
