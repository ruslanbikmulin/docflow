using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using Docflow.Enums;

namespace Docflow.DAL.Models
{
    /// <summary>
    /// Класс выгрузки
    /// </summary>
    [Table("UploadEntity")]
    public class UploadEntity
    {
        public UploadEntity()
        {
            this.UploadProgressRows = new List<UploadProgress>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Id { get; set; }

        public string JobIdentity { get; set; }

        public string UploadPath { get; set; }

        public DateTime UploadStartDate { get; set; }

        public string Email { get; set; }

        public string FilePath { get; set; }
        
        public UploadStatus UploadStatus { get; set; }

        [NotMapped]
        public int ContractCount
        {
            get
            {
                return this.UploadProgressRows.Count;
            }
        }

        [NotMapped]
        public int ErrorCount
        {
            get
            {
                return this.UploadProgressRows.Count(up => up.ProgressStatus == ProgressStatus.Error);
            }
        }

        [NotMapped]
        public int ZippedCount
        {
            get
            {
                return this.UploadProgressRows.Count(up => up.ProgressStatus == ProgressStatus.Zipped);
            }
        }

        public DateTime UploadEndDate { get; set; }

        public string UserNameStart { get; set; }

        public string UserNameCancel { get; set; }

        public virtual ICollection<UploadProgress> UploadProgressRows { get; set; }
    }
}