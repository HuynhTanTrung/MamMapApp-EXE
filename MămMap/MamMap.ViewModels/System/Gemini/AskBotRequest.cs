﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.ViewModels.System.Gemini
{
    public class AskBotRequest
    {
        public string Prompt { get; set; } = string.Empty;
        public Guid? SessionId { get; set; }
    }
}
