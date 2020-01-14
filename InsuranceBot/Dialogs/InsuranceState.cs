using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InsuranceBot.Dialogs
{
    public class InsuranceState
    {
        public string InsuranceType { get; set; }

        public string CarType { get; set; }

        public string CarMake { get; set; }

        public string CarModel { get; set; }

        public int CarYear { get; set; }

        public string Language { get; set; }
    }
}
