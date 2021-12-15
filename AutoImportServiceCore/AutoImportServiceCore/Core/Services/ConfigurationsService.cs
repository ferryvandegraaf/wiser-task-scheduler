﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoImportServiceCore.Core.Enums;
using AutoImportServiceCore.Core.Helpers;
using AutoImportServiceCore.Core.Interfaces;
using AutoImportServiceCore.Core.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using Microsoft.Extensions.Logging;

namespace AutoImportServiceCore.Core.Services
{
    public class ConfigurationsService : IConfigurationsService, IScopedService
    {
        private readonly ILogger<ConfigurationsService> logger;
        private readonly IActionsServiceFactory actionsServiceFactory;

        private readonly SortedList<int, ActionModel> actions;
        private readonly Dictionary<string, IActionsService> actionsServices;

        /// <inheritdoc />
        public LogSettings LogSettings { get; set; }

        /// <inheritdoc />
        public string Name { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="ConfigurationsService"/>.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="actionsServiceFactory"></param>
        public ConfigurationsService(ILogger<ConfigurationsService> logger, IActionsServiceFactory actionsServiceFactory)
        {
            this.logger = logger;
            this.actionsServiceFactory = actionsServiceFactory;

            actions = new SortedList<int, ActionModel>();
            actionsServices = new Dictionary<string, IActionsService>();
        }

        /// <inheritdoc />
        public void ExtractActionsFromConfiguration(int timeId, ConfigurationModel configuration)
        {
            var allActions = GetAllActionsFromConfiguration(configuration);

            foreach (ActionModel action in allActions.Where(action => action.TimeId == timeId))
            {
                action.LogSettings ??= LogSettings;
                actions.Add(action.Order, action);

                if (!actionsServices.ContainsKey(action.GetType().ToString()))
                {
                    actionsServices.Add(action.GetType().ToString(), actionsServiceFactory.GetActionsServiceForAction(action));
                }
            }

            LogHelper.LogInformation(logger, LogScopes.StartAndStop, LogSettings, $"{Name} has {actions.Count} action(s).");
        }

        /// <summary>
        /// Get all the provided action sets if they exist in a single list.
        /// </summary>
        /// <returns></returns>
        private List<ActionModel> GetAllActionsFromConfiguration(ConfigurationModel configuration)
        {
            var actionSets = new List<ActionModel[]>
            {
                configuration.Queries.ToArray<ActionModel>(),
                configuration.HttpApis.ToArray<ActionModel>()
            };

            var allActions = new List<ActionModel>();

            if (actions == null)
            {
                return allActions;
            }

            foreach (var actionSet in actionSets)
            {
                if (actionSet != null)
                {
                    allActions.AddRange(actionSet);
                }
            }

            return allActions;
        }

        /// <inheritdoc />
        public bool IsValidConfiguration(ConfigurationModel configuration)
        {
            var conflicts = 0;

            // Check for duplicate run scheme time ids.
            var runSchemeTimeIds = new List<int>();

            foreach (var runScheme in configuration.RunSchemes)
            {
                runSchemeTimeIds.Add(runScheme.TimeId);
            }

            var duplicateTimeIds = runSchemeTimeIds.GroupBy(id => id).Where(id => id.Count() > 1).Select(id => id.Key).ToList();

            if (duplicateTimeIds.Count > 0)
            {
                conflicts++;
                LogHelper.LogError(logger, LogScopes.RunStartAndStop, LogSettings, $"Configuration '{configuration.ServiceName}' has duplicate run scheme time ids: {String.Join(", ", duplicateTimeIds)}");
            }

            // Check for duplicate order in a single time id.
            var allActions = GetAllActionsFromConfiguration(configuration);

            foreach (var timeId in runSchemeTimeIds)
            {
                var duplicateOrders = allActions.Where(action => action.TimeId == timeId).GroupBy(action => action.Order).Where(action => action.Count() > 1).Select(action => action.Key).ToList();

                if (duplicateOrders.Count > 0)
                {
                    conflicts++;
                    LogHelper.LogError(logger, LogScopes.RunStartAndStop, LogSettings, $"Configuration '{configuration.ServiceName}' has duplicate orders within run scheme {timeId}. Orders: {String.Join(", ", duplicateOrders)}");
                }
            }

            return conflicts == 0;
        }

        /// <inheritdoc />
        public async Task ExecuteAsync()
        {
            foreach (var action in actions)
            {
                await actionsServices[action.Value.GetType().ToString()].Execute(action.Value);
            }
        }
    }
}