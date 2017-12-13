using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Docflow.Enums
{
    public enum ErrorCode
    {
        ContractNotFound,
        ContractDublicate,
        DbAccessDisable,
        ZippingError,
        ScanFileNotFound,
        PartOfScansNotFound,
        FileSystemAccessDisable,
        NetworkError,
        UndefinedError,
        /// <summary>
        /// Нет ошибок по линии обработки
        /// </summary>
        None
    }
}