using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Docflow.Enums
{
    public enum UploadStatus
    {
        Starting,
        Uploading,
        Zipping,
        Paused,
        Finishing,
        Canceling,
        Completed,
        Canceled
    }
}