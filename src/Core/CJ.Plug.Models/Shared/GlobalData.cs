using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Shared
{
    public class GlobalData
    {
        public static string MainDispatcherServer = "http://localhost:6660";
        public static string MainApiServer = "http://localhost:6661";
        public static string ElsaEngineServer = "http://localhost:5001";
        public static string ElsaEngineApiKey = "00000000-0000-0000-0000-000000000000";
        //public static string MainWebFileServer = "http://localhost:5066";

        public static string MainFileServerPathRoot = "C://tmp//FileServer";
        public static string MainWebFileServer = "C://tmp//Web";

        public static string StationFileRootPath = Path.Combine("C:", "tmp", "StationTmpFiles");
    }
}
