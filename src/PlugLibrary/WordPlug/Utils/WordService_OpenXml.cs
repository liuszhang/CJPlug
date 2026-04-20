using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using W = DocumentFormat.OpenXml.Wordprocessing;
using D = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using System.Text;
using Serilog;
using System.Text.Json;
using DocumentFormat.OpenXml.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using Path = System.IO.Path;

public class WordService_OpenXml
{
    public static string ConvertToHtml(string filePath)
    {
        StringBuilder html = new StringBuilder();

        using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false))
        {
            W.Body body = wordDoc.MainDocumentPart.Document.Body;

            foreach (var element in body.Elements())
            {
                html.Append(ConvertElementToHtml(element, wordDoc));
            }

            // 读取书签信息
            var bookmarks = GetBookmarks(wordDoc);
            if (bookmarks.Count > 0)
            {
                html.Append("<h3>书签信息</h3><ul>");
                foreach (var bookmark in bookmarks)
                {
                    html.Append($"<li>{bookmark.Key}: {bookmark.Value}</li>");
                }
                html.Append("</ul>");
            }
        }

        return html.ToString();
    }

    private static string ConvertElementToHtml(OpenXmlElement element, WordprocessingDocument wordDoc)
    {
        //Log.Information(element.GetType().FullName??element.GetType().Name);
        foreach (var item in element.Descendants())
        {
            //Log.Information(item.GetType().FullName ?? item.GetType().Name);
        }
        //if(element is D.Blip)
        //{
        //    Log.Information("ConvertDrawingToHtml");
        //    return ConvertDrawingToHtml(element, wordDoc);
        //}
        //Log.Information(JsonSerializer.Serialize(element.Descendants()));
        if (element.Descendants<D.Blip>().Any())
        {
            Log.Information("ConvertDrawingToHtml");
            return ConvertDrawingToHtml(element, wordDoc);
        }
        else if (element is W.Paragraph)
        {
            return $"<p>{element.InnerText}</p>";
        }
        else if (element is W.Table)
        {
            StringBuilder tableHtml = new StringBuilder("<table>");

            foreach (var row in element.Elements<W.TableRow>())
            {
                tableHtml.Append("<tr>");
                foreach (var cell in row.Elements<W.TableCell>())
                {
                    tableHtml.Append($"<td>{cell.InnerText}</td>");
                }
                tableHtml.Append("</tr>");
            }

            tableHtml.Append("</table>");
            return tableHtml.ToString();
        }
        

        return element.InnerText;
    }

    private static string ConvertDrawingToHtml(OpenXmlElement element, WordprocessingDocument wordDoc)
    {
        var blip = element.Descendants<D.Blip>().FirstOrDefault();
        if (blip == null)
            return string.Empty;

        var imagePart = wordDoc.MainDocumentPart.GetPartById(blip.Embed.Value) as ImagePart;
        if (imagePart == null)
            return string.Empty;

        // 提取图片并保存到本地路径
        var imageFileName = Path.GetFileName(imagePart.Uri.ToString());
        var imageFilePath = Path.Combine("wwwroot", "images", imageFileName);

        using (var stream = new FileStream(imageFilePath, FileMode.Create))
        {
            imagePart.GetStream().CopyTo(stream);
        }

        var extent = element.Descendants<DW.Extent>().FirstOrDefault();
        var width = extent?.Cx ?? 0;
        var height = extent?.Cy ?? 0;

        // 在HTML中引用本地路径
        return $"<img src=\"/images/{imageFileName}\" width=\"{width / 9525}\" height=\"{height / 9525}\" />";
    }



    public static List<string> GetBookmarksFromWordFile(string filePath)
    {
        List<string> bmList= new List<string>();
        using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath.Trim('"'), false))
        {            
            // 读取书签信息
            var bookmarks = GetBookmarks(wordDoc);
            if (bookmarks.Count > 0)
            {
                foreach (var bookmark in bookmarks)
                {
                    bmList.Add(bookmark.Key);
                    //bmList.Add(bookmark.Value);
                }
            }
        }
        return bmList;
    }

    private static Dictionary<string, string> GetBookmarks(WordprocessingDocument wordDoc)
    {
        Dictionary<string, string> bookmarks = new Dictionary<string, string>();

        foreach (W.BookmarkStart bookmarkStart in wordDoc.MainDocumentPart.RootElement.Descendants<W.BookmarkStart>())
        {
            string bookmarkName = bookmarkStart.Name;
            string bookmarkText = GetBookmarkText(bookmarkStart);
            bookmarks.Add(bookmarkName, bookmarkText);
        }

        return bookmarks;
    }

    private static string GetBookmarkText(W.BookmarkStart bookmarkStart)
    {
        StringBuilder bookmarkText = new StringBuilder();
        OpenXmlElement element = bookmarkStart.NextSibling();

        while (element != null && !(element is W.BookmarkEnd) && !(element is W.BookmarkStart))
        {
            bookmarkText.Append(element.InnerText);
            element = element.NextSibling();
        }

        return bookmarkText.ToString();
    }

    public static void InsertTextAtBookmark(string filePath, string bookmarkName, string textToInsert)
    {
        using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, true))
        {
            var bookmarkStart = wordDoc.MainDocumentPart.RootElement.Descendants<W.BookmarkStart>()
                .FirstOrDefault(b => b.Name == bookmarkName);

            if (bookmarkStart != null)
            {
                var parent = bookmarkStart.Parent;
                var run = new W.Run(new W.Text(textToInsert));
                parent.InsertAfter(run, bookmarkStart);
            }
        }
        Log.Information("插入文字至书签成功");
    }

    public static void InsertImageAtBookmark(string filePath, string bookmarkName, string imagePath)
    {
        using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, true))
        {
            var bookmarkStart = wordDoc.MainDocumentPart.RootElement.Descendants<W.BookmarkStart>()
                .FirstOrDefault(b => b.Name == bookmarkName);

            if (bookmarkStart != null)
            {
                var mainPart = wordDoc.MainDocumentPart;
                var imagePart = mainPart.AddImagePart(ImagePartType.Jpeg);

                using (var stream = new FileStream(imagePath, FileMode.Open))
                {
                    imagePart.FeedData(stream);
                }

                var imageId = mainPart.GetIdOfPart(imagePart);
                var element = new W.Drawing(
                    new DW.Inline(
                        new DW.Extent() { Cx = 990000L, Cy = 792000L },
                        new DW.EffectExtent()
                        {
                            LeftEdge = 0L,
                            TopEdge = 0L,
                            RightEdge = 0L,
                            BottomEdge = 0L
                        },
                        new DW.DocProperties()
                        {
                            Id = (UInt32Value)1U,
                            Name = "Picture 1"
                        },
                        new DW.NonVisualGraphicFrameDrawingProperties(
                            new D.GraphicFrameLocks() { NoChangeAspect = true }),
                        new D.Graphic(
                            new D.GraphicData(
                                new PIC.Picture(
                                    new PIC.NonVisualPictureProperties(
                                        new PIC.NonVisualDrawingProperties()
                                        {
                                            Id = (UInt32Value)0U,
                                            Name = Path.GetFileName(imagePath)
                                        },
                                        new PIC.NonVisualPictureDrawingProperties()),
                                    new PIC.BlipFill(
                                        new D.Blip(
                                            new D.BlipExtensionList(
                                                new D.BlipExtension()
                                                {
                                                    Uri =
                                                    "{28A0092B-C50C-407E-A947-70E740481C1C}"
                                                })
                                        )
                                        {
                                            Embed = imageId,
                                            CompressionState =
                                            D.BlipCompressionValues.Print
                                        },
                                        new D.Stretch(
                                            new D.FillRectangle())),
                                    new PIC.ShapeProperties(
                                        new D.Transform2D(
                                            new D.Offset() { X = 0L, Y = 0L },
                                            new D.Extents() { Cx = 990000L, Cy = 792000L }),
                                        new D.PresetGeometry(
                                            new D.AdjustValueList()
                                        )
                                        { Preset = D.ShapeTypeValues.Rectangle })))
                            )
                        { 
                            //Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" 
                        }))
                {
                    //DistanceFromTop = (UInt32Value)0U,
                    //DistanceFromBottom = (UInt32Value)0U,
                    //DistanceFromLeft = (UInt32Value)0U,
                    //DistanceFromRight = (UInt32Value)0U,
                    //EditId = "50D07946"
                };

                var parent = bookmarkStart.Parent;
                parent.InsertAfter(new W.Run(element), bookmarkStart);
            }
        }
        Log.Information("插入图片至书签成功");
    }


    public static void ConvertToPdf(string inputFilePath, string outputFilePath)
    {
        using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(inputFilePath, false))
        {
            PdfDocument pdfDoc = new PdfDocument();
            PdfPage pdfPage = pdfDoc.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(pdfPage);

            // 读取Word文档内容并绘制到PDF
            var body = wordDoc.MainDocumentPart.Document.Body;
            int yPosition = 0;
            foreach (var element in body.Elements())
            {
                gfx.DrawString(element.InnerText, new XFont("Arial", 12), XBrushes.Black, new XRect(0, yPosition, pdfPage.Width, 0));
                yPosition += 20;
            }

            pdfDoc.Save(outputFilePath);
        }
    }
}

