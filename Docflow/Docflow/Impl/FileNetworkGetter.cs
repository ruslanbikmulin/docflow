using Docflow.DAL.Models;

namespace Docflow.Impl
{
    using Docflow.Interface;

    public class FileNetworkGetter : IFileGetter
    {
        public FileNetworkGetter()
        {
            
        }
        
        

        public void Dispose()
        {

        }
        

        bool IFileGetter.GetFileBytes(ScanPath scanPath)
        {


            return true;
        }
    }
}