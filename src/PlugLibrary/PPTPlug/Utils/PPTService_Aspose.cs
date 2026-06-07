using Aspose.Slides;
using Aspose.Slides.Export;

namespace PPTPlug.Utils
{
    public static class PPTService_Aspose
    {
        /// <summary>
        /// 替换PPT中的文本
        /// </summary>
        /// <param name="filePath">PPT文件路径</param>
        /// <param name="oldText">要替换的文本</param>
        /// <param name="newText">新文本</param>
        public static void ReplaceTextInPresentation(string filePath, string oldText, string newText)
        {
            using var presentation = new Presentation(filePath);

            foreach (var slide in presentation.Slides)
            {
                foreach (var shape in slide.Shapes)
                {
                    if (shape is IAutoShape autoShape)
                    {
                        if (autoShape.TextFrame != null)
                        {
                            ReplaceTextInTextFrame(autoShape.TextFrame, oldText, newText);
                        }
                    }
                }
            }

            presentation.Save(filePath, SaveFormat.Pptx);
        }

        /// <summary>
        /// 批量替换PPT中的文本
        /// </summary>
        /// <param name="filePath">PPT文件路径</param>
        /// <param name="textMappings">文本映射字典（key: 原文本, value: 新文本）</param>
        public static void BatchReplaceText(string filePath, Dictionary<string, string> textMappings)
        {
            using var presentation = new Presentation(filePath);

            foreach (var slide in presentation.Slides)
            {
                foreach (var shape in slide.Shapes)
                {
                    if (shape is IAutoShape autoShape)
                    {
                        if (autoShape.TextFrame != null)
                        {
                            foreach (var mapping in textMappings)
                            {
                                ReplaceTextInTextFrame(autoShape.TextFrame, mapping.Key, mapping.Value);
                            }
                        }
                    }
                }
            }

            presentation.Save(filePath, SaveFormat.Pptx);
        }

        /// <summary>
        /// 在TextFrame中替换文本
        /// </summary>
        private static void ReplaceTextInTextFrame(ITextFrame textFrame, string oldText, string newText)
        {
            for (int i = 0; i < textFrame.Paragraphs.Count; i++)
            {
                var paragraph = textFrame.Paragraphs[i];
                for (int j = 0; j < paragraph.Portions.Count; j++)
                {
                    var portion = paragraph.Portions[j];
                    if (portion.Text.Contains(oldText))
                    {
                        portion.Text = portion.Text.Replace(oldText, newText);
                    }
                }
            }
        }

        /// <summary>
        /// PPT转PDF
        /// </summary>
        /// <param name="pptFilePath">PPT文件路径</param>
        /// <param name="pdfFilePath">PDF输出路径</param>
        public static void ConvertToPdf(string pptFilePath, string pdfFilePath)
        {
            using var presentation = new Presentation(pptFilePath);
            presentation.Save(pdfFilePath, SaveFormat.Pdf);
        }

        /// <summary>
        /// 获取幻灯片数量
        /// </summary>
        /// <param name="filePath">PPT文件路径</param>
        /// <returns>幻灯片数量</returns>
        public static int GetSlideCount(string filePath)
        {
            using var presentation = new Presentation(filePath);
            return presentation.Slides.Count;
        }

        /// <summary>
        /// 获取所有幻灯片的文本内容
        /// </summary>
        /// <param name="filePath">PPT文件路径</param>
        /// <returns>每张幻灯片的文本列表</returns>
        public static List<string> GetSlideTexts(string filePath)
        {
            var result = new List<string>();
            using var presentation = new Presentation(filePath);

            foreach (var slide in presentation.Slides)
            {
                var slideText = new System.Text.StringBuilder();
                foreach (var shape in slide.Shapes)
                {
                    if (shape is IAutoShape autoShape && autoShape.TextFrame != null)
                    {
                        slideText.AppendLine(autoShape.TextFrame.Text);
                    }
                }
                result.Add(slideText.ToString().Trim());
            }

            return result;
        }

        /// <summary>
        /// 获取PPT中所有可替换的占位符文本（以{{}}包围的文本）
        /// </summary>
        /// <param name="filePath">PPT文件路径</param>
        /// <returns>占位符列表</returns>
        public static List<string> GetPlaceholders(string filePath)
        {
            var placeholders = new HashSet<string>();
            using var presentation = new Presentation(filePath);

            foreach (var slide in presentation.Slides)
            {
                foreach (var shape in slide.Shapes)
                {
                    if (shape is IAutoShape autoShape && autoShape.TextFrame != null)
                    {
                        var text = autoShape.TextFrame.Text;
                        var matches = System.Text.RegularExpressions.Regex.Matches(text, @"\{\{(.+?)\}\}");
                        foreach (System.Text.RegularExpressions.Match match in matches)
                        {
                            placeholders.Add(match.Groups[1].Value);
                        }
                    }
                }
            }

            return placeholders.ToList();
        }
    }
}
