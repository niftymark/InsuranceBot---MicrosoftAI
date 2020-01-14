using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Configuration;

namespace InsuranceBot
{
    public class BotServices
    {
        public BotServices(BotConfiguration botConfiguration)
        {
            foreach (var service in botConfiguration.Services)
            {
                switch (service.Type)
                {
                    // Add LUIS case here
                }
            }
        }

        // Add LUIS services property here
    }
}
