using System.Collections.Generic;
using Docflow.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web;

namespace Docflow.DAL.Models
{
    [Table("UploadProgress")]
    public class UploadProgress
    {
        public UploadProgress()
        {
            this.ScanPaths = new List<ScanPath>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public long Id { get; set; }

        public string ContractName { get; set; }

        public ProgressStatus ProgressStatus { get; set; }

        public ErrorCode ErrorCode { get; set; }

        /// <summary>
        /// Путь до файла который позже используется для архивации
        /// </summary>
        public string FullFilePath { get; set; }

        public int UploadEntityId { get; set; }

        public UploadEntity UploadEntity { get; set; }

        public virtual ICollection<ScanPath> ScanPaths { get; set; }
    }
}