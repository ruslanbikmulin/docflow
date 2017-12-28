namespace Docflow.DAL.Models
{
    using Docflow.Enums;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("ScanPath")]
    public class ScanPath
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public long Id { get; set; }

        public string ContractName { get; set; }

        public ScanStatus ScanStatus { get; set; }

        /// <summary>
        /// Имя файла с расширением 
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Исходный путь до файла, как правило в виде идентификатора
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Итоговый путь до файла после копирования, как правило с нормальным именем и расширением
        /// </summary>
        public string ResultFileName { get; set; }

        /// <summary>
        /// Признак того что нужно создать подпапку для ДМЛ
        /// </summary>
        public bool NeedPathForDml { get; set; }

        public long UploadProgressId { get; set; }

        public UploadProgress UploadProgress { get; set; }
    }
}