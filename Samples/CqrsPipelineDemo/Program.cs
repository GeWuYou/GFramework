// Copyright (c) 2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using System.Threading;
using System;
using CqrsPipelineDemo;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Cqrs.Behaviors;
using GFramework.Cqrs;
using Microsoft.Extensions.DependencyInjection;

// #region scoped-pipeline
var services = new ServiceCollection();
services.AddScopedCqrsRuntime();
services.AddScoped<RequestScope>();
services.AddCqrsHandlers(GFramework.Generated.Cqrs.CqrsRegistration.CreateRegistry());
// Open generic behavior mapping follows the request/response parameter order.
services.AddCqrsPipelineBehavior(typeof(LoggingBehavior<,>));
services.AddCqrsPipelineBehavior(typeof(ScopeBehavior<,>), ServiceLifetime.Scoped);

var provider = services.BuildServiceProvider(
    new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
await using var providerLifetime = provider.ConfigureAwait(false);
RequestScope state;
var scope = provider.CreateAsyncScope();
await using (scope.ConfigureAwait(false))
{
    state = scope.ServiceProvider.GetRequiredService<RequestScope>();
    var runtime = scope.ServiceProvider.GetRequiredService<ICqrsRuntime>();
    if (await runtime.SendAsync(new GetLength("Alice")).ConfigureAwait(false) != 5 || state.Calls != 1 || state.Disposed)
        throw new InvalidOperationException("The generated handler or scoped behavior failed.");
}
if (!state.Disposed)
    throw new InvalidOperationException("The caller scope did not release its dependency.");
Console.WriteLine("CQRS_PIPELINE_PASS");
// #endregion scoped-pipeline
