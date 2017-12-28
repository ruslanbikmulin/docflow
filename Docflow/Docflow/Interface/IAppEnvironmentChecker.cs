using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Docflow.Interface
{
    interface IAppEnvironmentChecker
    {
        bool AppReadyToWork { get; set; }

        void CheckSettings();
    }
}
