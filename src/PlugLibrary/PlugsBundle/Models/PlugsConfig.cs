using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PlugsBundle.Models
{
    [XmlRoot("PlugsConfig")]
    public class PlugsConfig
    {
        [XmlElement("BasePath")]
        public string BasePath { get; set; }

        [XmlArray("UserPlugs")]
        [XmlArrayItem("Plug")]
        public List<PlugConfig> Plugs { get; set; }
    }
}
