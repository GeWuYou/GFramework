// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Routing;

/// <summary>Marshals framework-owned callbacks without assuming a particular engine or creating a global dispatcher.</summary>
internal static class RouterCallbackDispatcher
{
    /// <summary>Dispatches the whole asynchronous callback onto the original host context.</summary>
    /// <param name="context">Host context captured before middleware can leave it.</param>
    /// <param name="operation">Framework callback to execute on that context.</param>
    /// <returns>The callback completion, including cancellation or failure.</returns>
    public static Task InvokeAsync(SynchronizationContext? context, Func<Task> operation)
    {
        if (context is null || SynchronizationContext.Current == context)
            return operation();

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executionContext = ExecutionContext.Capture();
        try
        {
            context.Post(_ =>
            {
                if (executionContext is null)
                    Start();
                else
                    ExecutionContext.Run(executionContext, _ => Start(), null);
            }, null);
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
        return completion.Task;

        async void Start()
        {
            try
            {
                await operation().ConfigureAwait(true);
                completion.TrySetResult();
            }
            catch (OperationCanceledException exception)
            {
                completion.TrySetCanceled(exception.CancellationToken);
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
        }
    }
}
