using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rouyan.Interfaces
{
    public interface IPromptManagementService
    {
        public Task LoadPromptsAsync();
        public Task LoadConfigAsync();
        public Task SaveConfigAsync();
    }
}
