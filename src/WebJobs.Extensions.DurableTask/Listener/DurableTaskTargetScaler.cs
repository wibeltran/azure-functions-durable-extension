﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if !FUNCTIONS_V1
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Castle.Components.DictionaryAdapter;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.DurableTask.Listener
{
    internal class DurableTaskTargetScaler : ITargetScaler
    {
        private readonly DurableTaskMetricsProvider metricsProvider;
        private readonly TargetScalerResult cachedTargetScaler;
        private readonly int maxConcurrentActivities;
        private readonly int maxConcurrentOrchestrators;

        public DurableTaskTargetScaler(string functionId, DurableTaskMetricsProvider metricsProvider, DurabilityProvider durabilityProvider)
        {
            this.metricsProvider = metricsProvider;
            this.cachedTargetScaler = new TargetScalerResult();
            this.TargetScalerDescriptor = new TargetScalerDescriptor(functionId);
            this.maxConcurrentActivities = durabilityProvider.MaxConcurrentTaskActivityWorkItems;
            this.maxConcurrentOrchestrators = durabilityProvider.MaxConcurrentTaskOrchestrationWorkItems;
        }

        public TargetScalerDescriptor TargetScalerDescriptor { get; private set; }

        public async Task<TargetScalerResult> GetScaleResultAsync(TargetScalerContext context)
        {
            var metrics = await this.metricsProvider.GetMetricsAsync();

            var workItemQueueLength = metrics.WorkItemQueueLength;
            double activityWorkers = Math.Ceiling((double)(workItemQueueLength / this.maxConcurrentActivities));

            var controlQueueLengths = metrics.ControlQueueLengthsNumbers;
            var controlQueueMessages = controlQueueLengths.Sum();
            var activeControlQueues = controlQueueLengths.Count(x => x > 0);

            var upperBoundControlWorkers = Math.Ceiling((double)(controlQueueMessages / this.maxConcurrentOrchestrators));
            var orchestratorWorkers = Math.Min(activeControlQueues, upperBoundControlWorkers);

            int numWorkersToRequest = (int)Math.Max(activityWorkers, orchestratorWorkers);
            this.cachedTargetScaler.TargetWorkerCount = numWorkersToRequest;
            return this.cachedTargetScaler;
        }
    }
}
#endif