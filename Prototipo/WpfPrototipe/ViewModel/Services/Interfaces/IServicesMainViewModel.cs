using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegistroPerf.ViewModel.Services.Interfaces
{
    public interface IServicesMainViewModel
    {
        public bool EmptyInformation();
        public bool IncorrectInformation(string parameter);
    }
}
