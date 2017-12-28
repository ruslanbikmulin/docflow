using Docflow.DtoEntities;
using System;

namespace Docflow.Interface
{
    interface IFileInfoFiller : IDisposable
    {
        FileInfoFillerResult FillFileInfo(string contractName);
    }
}
