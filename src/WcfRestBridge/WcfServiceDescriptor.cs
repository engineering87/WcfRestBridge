using System.Reflection;

namespace WcfRestBridge.Core
{
    public class WcfServiceDescriptor
    {
        public Type InterfaceType { get; set; }
        public string RoutePrefix { get; set; }
        public Dictionary<string, MethodInfo> Methods { get; set; } = new();
    }
}
