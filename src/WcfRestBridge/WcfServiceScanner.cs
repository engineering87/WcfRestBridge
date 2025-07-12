// (c) 2025 Francesco Del Re <francesco.delre.87@gmail.com>
// This code is licensed under MIT license (see LICENSE.txt for details)
using System.Reflection;
using WcfRestBridge.WcfAttribute;

namespace WcfRestBridge.Core
{
    /// <summary>
    /// Scans loaded assemblies to discover interfaces marked with the WcfRestServiceAttribute.
    /// Builds a list of service descriptors containing metadata for routing and invocation.
    /// </summary>
    public static class WcfServiceScanner
    {
        /// <summary>
        /// Discovers WCF service interfaces decorated with WcfRestServiceAttribute across the given assemblies.
        /// </summary>
        /// <param name="assemblies">Assemblies to scan for WCF service interfaces.</param>
        /// <returns>List of WcfServiceDescriptor representing discovered services and their methods.</returns>
        public static List<WcfServiceDescriptor> DiscoverServices(params Assembly[] assemblies)
        {
            return [.. assemblies.SelectMany(a => a.GetTypes())
                .Where(t => t.IsInterface && t.GetCustomAttribute<WcfRestServiceAttribute>() != null)
                .Select(t => new WcfServiceDescriptor
                {
                    InterfaceType = t,
                    RoutePrefix = t.GetCustomAttribute<WcfRestServiceAttribute>()!.RoutePrefix,
                    Methods = t.GetMethods().ToDictionary(m => m.Name, m => m)
                })];
        }
    }
}
