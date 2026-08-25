using System;
using System.IO;
using System.Text;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
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

            string[] txtFiles = Directory.GetFiles(sourceFolder, "*.txt", SearchOption.TopDirectoryOnly);
            string[] bmpFiles = Directory.GetFiles(sourceFolder, "*.bmp", SearchOption.TopDirectoryOnly);
            string[] jpgFiles = Directory.GetFiles(sourceFolder, "*.jpg", SearchOption.TopDirectoryOnly);
            string[] jpegFiles = Directory.GetFiles(sourceFolder, "*.jpeg", SearchOption.TopDirectoryOnly);

            int totalFiles = txtFiles.Length + bmpFiles.Length + jpgFiles.Length + jpegFiles.Length;

            if (totalFiles == 0)
            {
                LogMessage(logFile, "INFO", "No se encontraron archivos .txt, .bmp, .jpg o .jpeg para convertir", "SISTEMA");
                return;
            }

            foreach (string file in txtFiles)
                ProcessFile(file, logFile, "txt");

            foreach (string file in bmpFiles)
                ProcessFile(file, logFile, "bmp");

            foreach (string file in jpgFiles)
                ProcessFile(file, logFile, "jpg");

            foreach (string file in jpegFiles)
                ProcessFile(file, logFile, "jpeg");
        }

        static void ProcessFile(string filePath, string logFile, string type)
        {
            string fileName = Path.GetFileName(filePath);
            string pdfFileName = Path.ChangeExtension(fileName, ".pdf");
            string pdfPath = Path.Combine(Path.GetDirectoryName(filePath)!, pdfFileName);

            try
            {
                if (type == "txt")
                {
                    string content = File.ReadAllText(filePath, Encoding.UTF8);
                    ConvertTextToPdf(content, pdfPath);
                }
                else
                {
                    ConvertImageToPdf(filePath, pdfPath);
                }
                LogMessage(logFile, "EXITO", "Conversión completada", fileName);
            }
            catch (Exception ex)
            {
                LogMessage(logFile, "ERROR", ex.Message, fileName);
            }
        }

        static void ConvertTextToPdf(string text, string outputPath)
        {
            PdfDocument document = new PdfDocument();
            PdfPage page = document.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);
            XFont font = new XFont("Verdana", 10);
            XTextFormatter tf = new XTextFormatter(gfx);

            double margin = 40;
            XRect rect = new XRect(margin, margin, page.Width - 2 * margin, page.Height - 2 * margin);
            tf.Alignment = XParagraphAlignment.Left;
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
}