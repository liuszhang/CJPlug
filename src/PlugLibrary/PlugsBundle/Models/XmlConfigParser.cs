using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PlugsBundle.Models
{
    public static class XmlConfigParser
    {
        public static PlugsConfig Parse(string filePath)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(PlugsConfig));
                using var reader = new StreamReader(filePath);
                return (PlugsConfig)serializer.Deserialize(reader);
            }
            catch (FileNotFoundException)
            {
                throw new ArgumentException($"配置文件不存在: {filePath}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("解析配置文件失败", ex);
            }
        }
    }
}
