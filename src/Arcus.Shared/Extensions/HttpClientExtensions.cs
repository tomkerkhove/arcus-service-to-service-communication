﻿
using System.Threading.Tasks;
using Arcus.Observability.Correlation;
using Arcus.Observability.Telemetry.Core;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace System.Net.Http
{
    public static class HttpClientExtensions
    {
        // TODO: Contribute Upstream - HTTP service-to-service automagical tracking - Option #2 - Use extension, but end-users need to specify all dependencies
        // Contribute Upstream means that this should be in the Arcus library ?
        public static async Task<HttpResponseMessage> SendAndTrackDependencyAsync(this HttpClient httpClient, string operationName, HttpRequestMessage request, ICorrelationInfoAccessor correlationInfoAccessor, ILogger logger)
        {
            using (var httpDependencyMeasurement = DurationMeasurement.Start())
            {
                var newDependencyId = Guid.NewGuid().ToString();
                var correlationInfo = correlationInfoAccessor.GetCorrelationInfo();
                var upstreamOperationParentId = $"|{correlationInfo?.OperationId}.{newDependencyId}";

                request.Headers.Add("Request-Id", upstreamOperationParentId);
                request.Headers.Add("X-Transaction-ID", correlationInfo?.TransactionId);

                var response = await httpClient.SendAsync(request);

                logger.LogHttpDependency(request, response.StatusCode, httpDependencyMeasurement, dependencyId: upstreamOperationParentId);

                return response;
            }
        }
    }
}
