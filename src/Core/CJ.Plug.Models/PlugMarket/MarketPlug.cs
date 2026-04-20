using CJ.Plug.Models.Plug;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


    public class MarketPlug : Plug
    {
        public int? RootPlugID { get; set; }
        public string? MarketName { get; set; }
        public byte[]? MarketImageBytes { get; set; } = Array.Empty<byte>(); // 用于存储图片的字节数组
        public string? Base64ImageUrl { get; set; }
        public int UsersCount { get; set; } = 0;
        //public List<User>? DownloadUsers { get; set; } = new List<User>();

    }

