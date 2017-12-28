using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Docflow.DAL.Models;

namespace Docflow.Interface
{
    interface IFileGetter : IDisposable
    {
        bool GetFileBytes(ScanPath scanPath);
    }
}
