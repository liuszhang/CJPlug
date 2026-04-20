namespace CJ.Plug.Models.Extensions
{
    public static class StringExtensions
    {
        public static string GetPDZType(this string PDZId)
        {
            if(string.IsNullOrEmpty(PDZId)) return string.Empty;
            // PDZId格式为：UserName+PlugDefinitionId+PDZType+JobDefinitionId
            if(PDZId.EndsWith(PDZTypeEnum.Desi.ToString()))
            {
                return PDZTypeEnum.Desi.ToString();
            }
            else
            {
                return PDZId.Substring(PDZId.Length - 8, 4);
            }            
        }

        public static string GetPlugDefinitionId(this string PDZId)
        {
            if (string.IsNullOrEmpty(PDZId)) return string.Empty;
            // PDZId格式为：UserName+PlugDefinitionId+PDZType+JobDefinitionId
            if (PDZId.EndsWith(PDZTypeEnum.Desi.ToString()))
            {
                return PDZId.Substring(PDZId.Length - 8, 4);
            }
            else
            {
                return PDZId.Substring(PDZId.Length - 12, 4);
            }
        }

        public static string GetDesignPDZId(this string PDZId)
        {
            if (string.IsNullOrEmpty(PDZId)) return string.Empty;
            // PDZId格式为：UserName+PlugDefinitionId+PDZType+JobDefinitionId
            if (PDZId.EndsWith(PDZTypeEnum.Desi.ToString()))
            {
                return PDZId;
            }
            else
            {
                return PDZId.Substring(0,PDZId.Length - 8)+PDZTypeEnum.Desi.ToString();
            }
        }


        public static string GetFileIdFromFileVariable(this string fileVariable)
        {
            if (string.IsNullOrEmpty(fileVariable)) return string.Empty;
            // 假设fileVariable格式为 "asdf.txt:asdfadf"
            var fileId = fileVariable.Split(':').Last();
            return fileId;
        }
        public static string GetFileNameFromFileVariable(this string fileVariable)
        {
            if (string.IsNullOrEmpty(fileVariable)) return string.Empty;
            // 假设fileVariable格式为 "asdf.txt:asdfadf"
            var fileName = fileVariable.Split(':').First();
            return fileName;
        }




    }
}
