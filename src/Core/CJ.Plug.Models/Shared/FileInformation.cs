using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Shared
{
    public class FileInformation
    {
        public int Id { get; set; }
        public string? FileId { get; set; }
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public string? FileSize { get; set; }
        public string? FileType { get; set; }
        public string? FileExtension { get; set; }
        public string? FileUploadDate { get; set; }
        public string? FileUploadTime { get; set; }
        public string? FileUploader { get; set; }
        public string? FileUploadType { get; set; }
        public string? FileUploadPath { get; set; }
        public string? FileDescription { get; set; }
        public string? FileStatus { get; set; }


        public string? MinioPath { get; set; }
        public string? SandBoxPath { get; set; }

        public FileInformation()
        {
            FileUploadDate = DateTime.Now.ToString("yyyy-MM-dd");
            FileUploadTime = DateTime.Now.ToString("HH:mm:ss");
        }

    }
}
