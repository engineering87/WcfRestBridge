// (c) 2025 Francesco Del Re <francesco.delre.87@gmail.com>
// This code is licensed under MIT license (see LICENSE.txt for details)
using System.ServiceModel;

namespace WcfRestBridge.Core
{
    /// <summary>
    /// Provides runtime invocation capabilities for WCF SOAP services via reflection and dynamic proxies.
    /// </summary>
    public class SoapInvoker
    {
        /// <summary>
        /// Asynchronously invokes a method on a WCF SOAP service using its contract interface.
        /// </summary>
        /// <param name="contractType">The interface type that defines the WCF service contract.</param>
        /// <param name="methodName">The name of the method to invoke.</param>
        /// <param name="args">An array of arguments to pass to the method.</param>
        /// <param name="endpointUrl">The endpoint URL of the WCF service.</param>
        /// <returns>The result of the method invocation, or null if the method is not found.</returns>
        public static async Task<object?> InvokeAsync(Type contractType, string methodName, object[] args, string endpointUrl)
        {
            var binding = new BasicHttpBinding();
            var endpoint = new EndpointAddress(endpointUrl);
            var factoryType = typeof(ChannelFactory<>).MakeGenericType(contractType);
            dynamic factory = Activator.CreateInstance(factoryType, binding, endpoint);
            dynamic channel = factory.CreateChannel();

            var method = contractType.GetMethod(methodName);
            return method?.Invoke(channel, args);
        }
    }
}
