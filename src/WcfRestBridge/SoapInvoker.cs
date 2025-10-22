// (c) 2025 Francesco Del Re <francesco.delre.87@gmail.com>
// This code is licensed under the MIT license (see LICENSE.txt for details)
using System.Collections.Concurrent;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace WcfRestBridge.Core
{
    /// <summary>
    /// Elegant and safe runtime invoker for WCF SOAP services.
    /// Provides both strongly-typed and reflection-based invocation,
    /// with channel lifecycle management and factory caching.
    /// </summary>
    public static class SoapInvoker
    {
        /// <summary>
        /// Caches ChannelFactory instances keyed by (contract, endpoint, bindingKey) to avoid costly re-creation.
        /// </summary>
        private static readonly ConcurrentDictionary<(Type Contract, string Endpoint, string BindingKey), object> Factories = new();

        /// <summary>
        /// Executes a strongly-typed asynchronous call on a WCF service contract and returns a result.
        /// </summary>
        /// <typeparam name="TContract">The WCF contract interface type.</typeparam>
        /// <typeparam name="TResult">The expected result type.</typeparam>
        /// <param name="endpointUrl">The WCF endpoint URL.</param>
        /// <param name="call">A lambda that performs the service call using the generated channel.</param>
        /// <param name="binding">Optional binding (defaults to HTTPS BasicHttpBinding).</param>
        /// <param name="cancellationToken">A cancellation token to abort the call.</param>
        /// <returns>A task that completes with the service call result.</returns>
        public static Task<TResult> ExecuteAsync<TContract, TResult>(
            string endpointUrl,
            Func<TContract, Task<TResult>> call,
            Binding? binding = null,
            CancellationToken cancellationToken = default)
            where TContract : class
            => WithChannelAsync(endpointUrl, call, binding, cancellationToken);

        /// <summary>
        /// Executes a strongly-typed asynchronous call on a WCF service contract without a return value.
        /// </summary>
        /// <typeparam name="TContract">The WCF contract interface type.</typeparam>
        /// <param name="endpointUrl">The WCF endpoint URL.</param>
        /// <param name="call">A lambda that performs the service call using the generated channel.</param>
        /// <param name="binding">Optional binding (defaults to HTTPS BasicHttpBinding).</param>
        /// <param name="cancellationToken">A cancellation token to abort the call.</param>
        /// <returns>A task that completes when the call finishes.</returns>
        public static Task ExecuteAsync<TContract>(
            string endpointUrl,
            Func<TContract, Task> call,
            Binding? binding = null,
            CancellationToken cancellationToken = default)
            where TContract : class
            => WithChannelAsync<TContract, object?>(endpointUrl,
                async ch => { await call(ch).ConfigureAwait(false); return null; },
                binding, cancellationToken);

        /// <summary>
        /// Invokes a WCF method dynamically by name using reflection.
        /// If the method returns Task or Task&lt;T&gt;, the invocation is awaited automatically.
        /// </summary>
        /// <param name="contractType">The WCF contract interface type.</param>
        /// <param name="methodName">The method name to invoke.</param>
        /// <param name="args">The method arguments (can be empty).</param>
        /// <param name="endpointUrl">The WCF endpoint URL.</param>
        /// <param name="binding">Optional binding (defaults to HTTPS BasicHttpBinding).</param>
        /// <param name="parameterTypes">Optional explicit parameter types to disambiguate overloads.</param>
        /// <param name="cancellationToken">A cancellation token to abort the call.</param>
        /// <returns>The method result or null for void/Task methods.</returns>
        /// <exception cref="MissingMethodException">Thrown when no matching overload is found.</exception>
        public static async Task<object?> InvokeAsync(
            Type contractType,
            string methodName,
            object?[]? args,
            string endpointUrl,
            Binding? binding = null,
            Type[]? parameterTypes = null,
            CancellationToken cancellationToken = default)
        {
            if (contractType is null)
                throw new ArgumentNullException(nameof(contractType));
            if (string.IsNullOrWhiteSpace(methodName))
                throw new ArgumentException("Method name required.", nameof(methodName));

            args ??= Array.Empty<object?>();
            var method = ResolveMethod(contractType, methodName, parameterTypes, args);
            if (method is null)
                throw new MissingMethodException($"No overload of '{methodName}' on {contractType.FullName} matches the provided arguments.");

            return await WithChannelAsync(endpointUrl, async (dynamic channel) =>
            {
                object? result = method.Invoke(channel, args);
                if (result is Task t)
                {
                    using var _ = cancellationToken.Register(() => TryAbort(channel));
                    await t.ConfigureAwait(false);
                    if (t.GetType().IsGenericType)
                        return t.GetType().GetProperty("Result")!.GetValue(t);
                    return null;
                }
                return result;
            }, binding, cancellationToken, contractType).ConfigureAwait(false);
        }

        // ---------------- Internal Helpers ----------------

        /// <summary>
        /// Creates a channel for a typed contract, executes the provided function, and ensures proper channel shutdown.
        /// </summary>
        /// <typeparam name="TContract">The WCF contract interface type.</typeparam>
        /// <typeparam name="TResult">The result type returned by the function.</typeparam>
        /// <param name="endpointUrl">The WCF endpoint URL.</param>
        /// <param name="call">A function that uses the channel to perform the operation.</param>
        /// <param name="binding">Optional binding.</param>
        /// <param name="ct">A cancellation token to abort the call.</param>
        /// <returns>The result of the function execution.</returns>
        private static async Task<TResult> WithChannelAsync<TContract, TResult>(
            string endpointUrl,
            Func<TContract, Task<TResult>> call,
            Binding? binding,
            CancellationToken ct)
            where TContract : class
        {
            var (_, channel) = CreateChannel<TContract>(endpointUrl, binding);
            try
            {
                using var _ = ct.Register(() => TryAbort(channel));
                var result = await call(channel).ConfigureAwait(false);
                TryClose(channel);
                return result;
            }
            catch
            {
                TryAbort(channel);
                throw;
            }
        }

        /// <summary>
        /// Creates a channel for a dynamic contract type, executes the provided function, and ensures proper shutdown.
        /// </summary>
        /// <typeparam name="TResult">The result type returned by the function.</typeparam>
        /// <param name="endpointUrl">The WCF endpoint URL.</param>
        /// <param name="call">A function that uses the dynamic channel to perform the operation.</param>
        /// <param name="binding">Optional binding.</param>
        /// <param name="ct">A cancellation token to abort the call.</param>
        /// <param name="contractOverride">The contract interface type for channel creation.</param>
        /// <returns>The result of the function execution.</returns>
        private static async Task<TResult> WithChannelAsync<TResult>(
            string endpointUrl,
            Func<dynamic, Task<TResult>> call,
            Binding? binding,
            CancellationToken ct,
            Type contractOverride)
        {
            var (_, channel) = CreateChannel(endpointUrl, binding, contractOverride);
            try
            {
                using var _ = ct.Register(() => TryAbort(channel));
                var result = await call(channel).ConfigureAwait(false);
                TryClose(channel);
                return result;
            }
            catch
            {
                TryAbort(channel);
                throw;
            }
        }

        /// <summary>
        /// Creates (or reuses) a ChannelFactory for the specified typed contract and returns a new channel.
        /// </summary>
        /// <typeparam name="TContract">The WCF contract interface type.</typeparam>
        /// <param name="endpointUrl">The WCF endpoint URL.</param>
        /// <param name="binding">Optional binding.</param>
        /// <returns>The factory used and the created channel.</returns>
        private static (ChannelFactory<TContract> Factory, TContract Channel) CreateChannel<TContract>(
            string endpointUrl,
            Binding? binding)
            where TContract : class
        {
            binding ??= DefaultBinding();
            var key = (typeof(TContract), endpointUrl, BindingKey(binding));
            var factoryObj = Factories.GetOrAdd(key, _ => new ChannelFactory<TContract>(binding, new EndpointAddress(endpointUrl)));
            var factory = (ChannelFactory<TContract>)factoryObj;
            return (factory, factory.CreateChannel());
        }

        /// <summary>
        /// Creates (or reuses) a ChannelFactory for the specified contract Type and returns a new dynamic channel.
        /// </summary>
        /// <param name="endpointUrl">The WCF endpoint URL.</param>
        /// <param name="binding">Optional binding.</param>
        /// <param name="contractType">The WCF contract interface type.</param>
        /// <returns>The factory used and the created dynamic channel.</returns>
        private static (object Factory, dynamic Channel) CreateChannel(
            string endpointUrl,
            Binding? binding,
            Type contractType)
        {
            binding ??= DefaultBinding();
            var key = (contractType, endpointUrl, BindingKey(binding));
            var factoryObj = Factories.GetOrAdd(key, _ =>
            {
                var ft = typeof(ChannelFactory<>).MakeGenericType(contractType);
                return Activator.CreateInstance(ft, binding, new EndpointAddress(endpointUrl))!;
            });
            dynamic factory = factoryObj;
            dynamic channel = factory.CreateChannel();
            return (factoryObj, channel);
        }

        /// <summary>
        /// Provides a secure default binding (HTTPS BasicHttpBinding) with sensible limits and timeouts.
        /// </summary>
        /// <returns>A configured <see cref="BasicHttpBinding"/> instance.</returns>
        private static Binding DefaultBinding() => new BasicHttpBinding(BasicHttpSecurityMode.Transport)
        {
            MaxReceivedMessageSize = 16 * 1024 * 1024,
            OpenTimeout = TimeSpan.FromSeconds(15),
            CloseTimeout = TimeSpan.FromSeconds(15),
            SendTimeout = TimeSpan.FromSeconds(120),
            ReceiveTimeout = TimeSpan.FromSeconds(120),
        };

        /// <summary>
        /// Builds a stable key that identifies a binding configuration for factory caching.
        /// </summary>
        /// <param name="binding">The binding instance.</param>
        /// <returns>A unique string key representing the binding.</returns>
        private static string BindingKey(Binding binding)
            => $"{binding.GetType().FullName}:{binding.MessageVersion}:{binding.Scheme}";

        /// <summary>
        /// Attempts to gracefully close a WCF channel; aborts if the channel is faulted or closing fails.
        /// </summary>
        /// <param name="channel">The channel to close.</param>
        private static void TryClose(dynamic channel)
        {
            try
            {
                var comm = (ICommunicationObject)channel;
                if (comm.State == CommunicationState.Faulted)
                    comm.Abort();
                else
                    comm.Close();
            }
            catch
            {
                TryAbort(channel);
            }
        }

        /// <summary>
        /// Aborts a WCF channel, ignoring any exceptions. Used as a last-resort shutdown.
        /// </summary>
        /// <param name="channel">The channel to abort.</param>
        private static void TryAbort(dynamic channel)
        {
            try { ((ICommunicationObject)channel).Abort(); } catch { /* ignored */ }
        }

        /// <summary>
        /// Resolves a method by name and parameters on the specified contract type.
        /// If explicit parameter types are provided, they are used to disambiguate overloads.
        /// Otherwise, a soft runtime-compatibility check is performed against the provided arguments.
        /// </summary>
        /// <param name="contractType">The WCF contract interface type.</param>
        /// <param name="name">The method name.</param>
        /// <param name="parameterTypes">Optional explicit method parameter types.</param>
        /// <param name="args">Runtime arguments to match against method parameters.</param>
        /// <returns>The matching <see cref="MethodInfo"/> or null if not found.</returns>
        private static MethodInfo? ResolveMethod(Type contractType, string name, Type[]? parameterTypes, object?[] args)
        {
            if (parameterTypes is { Length: > 0 })
                return contractType.GetMethod(name, parameterTypes);

            var candidates = contractType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                         .Where(m => string.Equals(m.Name, name, StringComparison.Ordinal))
                                         .ToArray();

            foreach (var m in candidates)
            {
                var ps = m.GetParameters();
                if (ps.Length != args.Length) continue;

                bool match = true;
                for (int i = 0; i < ps.Length; i++)
                {
                    var p = ps[i].ParameterType;
                    var a = args[i];
                    if (a is null)
                    {
                        if (p.IsValueType && Nullable.GetUnderlyingType(p) is null)
                        {
                            match = false;
                            break;
                        }
                    }
                    else if (!p.IsInstanceOfType(a))
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return m;
            }
            return null;
        }
    }
}