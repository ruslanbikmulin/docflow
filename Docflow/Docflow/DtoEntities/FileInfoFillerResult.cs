using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Docflow.DAL.Models;

namespace Docflow.DtoEntities
{
    public class FileInfoFillerResult
    {
        public bool Success { get; set; }

        public List<ScanPath> ScanPaths { get; set; }

        public string ErrorMessage { get; set; }
    }
}