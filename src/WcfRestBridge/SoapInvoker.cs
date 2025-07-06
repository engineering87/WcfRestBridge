using System.ServiceModel;

namespace WcfRestBridge.Core
{

    public class SoapInvoker
    {
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
