// (c) 2025 Francesco Del Re <francesco.delre.87@gmail.com>
// This code is licensed under MIT license (see LICENSE.txt for details)
using System.Reflection;

namespace WcfRestBridge.Core
{
    /// <summary>
    /// Describes a WCF service interface discovered at runtime.
    /// Contains metadata necessary for routing and method invocation.
    /// </summary>
    public class WcfServiceDescriptor
    {
        /// <summary>
        /// The interface type of the WCF service.
        /// </summary>
        public Type InterfaceType { get; set; }

        /// <summary>
        /// The route prefix used to map REST endpoints to this service.
        /// </summary>
        public string RoutePrefix { get; set; }

        /// <summary>
        /// Dictionary of method names to MethodInfo objects for the service interface.
        /// </summary>
        public Dictionary<string, MethodInfo> Methods { get; set; } = new();
    }
}