using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Shared
{
    public class FileUploadRequest
    {
        public IFormFile? FileStream { get; set; } // 使用IFormFile而不是Stream,用于API使用
        //public Stream? FileStream { get; set; } //用于客户端使用
        public string? UploadPath { get; set; } //客户端上传时的文件路径信息，用于数据隔离区分
        public string? FileServerPath { get; set; }  //文件上传到服务器后的真实服务器路径，用于文件下载
        public string? FileName { get; set; }

        public string? FileId { get; set; }
        public string? FileCreator { get; set; }
        public string? FileUploadType { get; set; }

        public string? ProcessDefinitionId { get; set; }
        public string? ProcessInstanceId { get; set; }
        public string? PlugDefinitionId { get; set; }
        public string? PlugInstanceId { get; set; }

        public long Offset { get; set; } = 0; //分块上传偏移标识
        public int? ChunkSize { get; set; } //分块上传大小
        //public IFormFile? FileChunk { get; set; } //分块上传时使用的文件识别名称
    }
}
