namespace WcfRestBridge.Core
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class WcfRestServiceAttribute : Attribute
    {
        public string RoutePrefix { get; set; }
        public WcfRestServiceAttribute(string routePrefix) => RoutePrefix = routePrefix;
    }
}
