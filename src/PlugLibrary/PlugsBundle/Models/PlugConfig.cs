using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PlugsBundle.Models
{
    public class PlugConfig
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("DllPath")]
        public string DllPath { get; set; }
    }
}
