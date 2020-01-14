using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InsuranceBot.Options
{
    public class BotOptions
    {
        public string MicrosoftAppId { get; set; }

        public string MicrosoftAppPassword { get; set; }

        public string LuisAppId { get; set; }

        public string LuisAPIKey { get; set; }

        public string LuisAPIHostName { get; set; }
    }
}
