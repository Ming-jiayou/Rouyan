using Rouyan.Models;
using Rouyan.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rouyan.Interfaces
{
    public interface IEnvConfigService
    {
        public Task<EnvConfig> LoadConfigAsync();
        public Task SaveConfigAsync(EnvConfig config);
    }
}
