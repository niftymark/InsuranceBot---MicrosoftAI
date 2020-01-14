using System;﻿
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InsuranceBot.Models
{
    public class TranslatorDetectResponse
    {
        public string Language { get; set; }

        public double Score { get; set; }
    }
}
