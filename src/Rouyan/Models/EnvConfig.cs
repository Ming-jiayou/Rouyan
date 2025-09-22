using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rouyan.Models
{
    public class EnvConfig
    {
        public string ChatApiKey { get; set; } = "";
        public string ChatBaseUrl { get; set; } = "";
        public string ChatModel { get; set; } = "";

        public string VisionApiKey { get; set; } = "";
        public string VisionBaseUrl { get; set; } = "";
        public string VisionModel { get; set; } = "";
    }
}
