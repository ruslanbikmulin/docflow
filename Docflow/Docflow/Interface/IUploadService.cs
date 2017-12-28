using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Docflow.DAL.Models;
using Docflow.DtoEntities;

namespace Docflow.Interface
{
    interface IUploadService
    {
        void RegisterUpload(UploadParams uploadParams);

        void UnregistrateRunningUpload();

        UploadEntity GetRunningUpload();
    }
}
