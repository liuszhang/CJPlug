using System;
using System.IO;
using PdfSharp.Pdf;
using MigraDocCore.DocumentObjectModel;
using Xceed.Words.NET;
using MigraDocCore.Rendering;
using MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes;

public class WordToPdfService2
{
    public static void ConvertToPdf(string wordFilePath, string pdfFilePath)
    {
        using (var document = DocX.Load(wordFilePath))
        {
            var migraDocDocument = new Document();
            var section = migraDocDocument.AddSection();

            // 处理段落
            foreach (var paragraph in document.Paragraphs)
            {
                var p = section.AddParagraph();
                p.AddText(paragraph.Text);
                p.Format.Font.Size = 12;
                p.Format.Font.Name = "Verdana";
            }

            // 处理图片
            foreach (var image in document.Images)
            {
                try
                {
                    // 将图片流保存到内存流中
                    using (var imageStream = image.GetStream(FileMode.Open, FileAccess.Read))
                    using (var memoryStream = new MemoryStream())
                    {
                        imageStream.CopyTo(memoryStream);

                        // 重置内存流的位置
                        memoryStream.Seek(0, SeekOrigin.Begin);

                        // 从内存流中加载图片
                        var imageSource = ImageSource.FromStream(image.FileName, () => memoryStream);
                        var img = section.AddImage(imageSource);
                        img.Width = Unit.FromCentimeter(15);
                        img.LockAspectRatio = true;
                    }
                }
                catch (Exception ex)
                {
                    // 记录错误或处理异常
                    Console.WriteLine($"error to add image: {ex.Message}");
                }
            }

            // 渲染并保存 PDF
            var pdfRenderer = new PdfDocumentRenderer(true)
            {
                Document = migraDocDocument
            };

            pdfRenderer.RenderDocument();
            pdfRenderer.PdfDocument.Save(pdfFilePath);
        }
    }
}