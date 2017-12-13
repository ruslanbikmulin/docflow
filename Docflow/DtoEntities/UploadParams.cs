using System;
using System.IO;
using Docflow.Enums;

namespace Docflow.DtoEntities
{
    public class UploadParams
    {
        public string UploadPath { get; set; }

        public DateTime UploadStartDate { get; set; }

        public string Email { get; set; }

        public string FilePath { get; set; }

        public UploadStatus UploadStatus { get; set; }

        public int ContractCount { get; set; }

        public int ErrorCount { get; set; }

        public int ZippedCount { get; set; }

        public DateTime? UploadEndDate { get; set; }

        public string UserNameStart { get; set; }

        public string UserNameCancel { get; set; }

        public Stream ContractIdentifierFileStream { get; set; }
    }
}