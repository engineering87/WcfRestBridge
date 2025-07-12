// (c) 2025 Francesco Del Re <francesco.delre.87@gmail.com>
// This code is licensed under MIT license (see LICENSE.txt for details)
namespace WcfRestBridge.TestHost
{
    public class MySoapService : IMySoapService
    {
        public string Test() => "Test ok";

        public string Echo(string message) => $"Echo: {message}";

        public int Add(int a, int b) => a + b;
    }
}