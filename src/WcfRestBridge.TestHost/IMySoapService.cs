// (c) 2025 Francesco Del Re <francesco.delre.87@gmail.com>
// This code is licensed under MIT license (see LICENSE.txt for details)
using System.ServiceModel;
using WcfRestBridge.WcfAttribute;

namespace WcfRestBridge.TestHost
{
    [WcfRestService("MySoapService")]
    [ServiceContract]
    public interface IMySoapService
    {
        [OperationContract]
        string Test();

        [OperationContract]
        string Echo(string message);

        [OperationContract]
        int Add(int a, int b);
    }
}
