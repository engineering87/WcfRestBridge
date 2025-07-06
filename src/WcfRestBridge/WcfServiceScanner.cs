using System.Reflection;

namespace WcfRestBridge.Core
{
    public static class WcfServiceScanner
    {
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
