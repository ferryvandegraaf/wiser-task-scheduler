﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoImportServiceCore.Core.Interfaces
{
    /// <summary>
    /// A service to handle the communication with the Wiser API.
    /// </summary>
    public interface IWiserService
    {
        /// <summary>
        /// Make a request to the API to get all XML configurations.
        /// </summary>
        /// <returns></returns>
        Task<List<string>> RequestConfigurations();
    }
}
