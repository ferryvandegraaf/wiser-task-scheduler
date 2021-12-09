﻿using System.Threading.Tasks;
using AutoImportServiceCore.Core.Models;

namespace AutoImportServiceCore.Core.Interfaces
{
    /// <summary>
    /// A service for a configuration.
    /// </summary>
    public interface  IConfigurationsService
    {
        /// <summary>
        /// Gets or sets the log settings that needs to be used.
        /// </summary>
        LogSettings LogSettings { get; set; }

        /// <summary>
        /// Gets or sets the name of the configuration run scheme.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Get all actions from the configuration that are associated with the time id.
        /// </summary>
        /// <param name="timeId"></param>
        /// <param name="configuration"></param>
        void ExtractActionsFromConfiguration(int timeId, ConfigurationModel configuration);

        /// <summary>
        /// Execute all actions that have been extracted from the configuration from the time id.
        /// </summary>
        /// <returns></returns>
        Task ExecuteAsync();
    }
}
