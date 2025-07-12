// (c) 2025 Francesco Del Re <francesco.delre.87@gmail.com>
// This code is licensed under MIT license (see LICENSE.txt for details)
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WcfRestBridge.Core;

namespace WcfRestBridge.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RestProxyController : ControllerBase
    {
        private readonly IEnumerable<WcfServiceDescriptor> _services;
        private readonly IConfiguration _config;

        public RestProxyController(IConfiguration config)
        {
            _config = config;
            _services = WcfServiceScanner.DiscoverServices(AppDomain.CurrentDomain.GetAssemblies());
        }

        [HttpPost("{service}/{method}")]
        public async Task<IActionResult> Invoke(string service, string method, [FromBody] JsonElement body)
        {
            if (body.ValueKind == JsonValueKind.Undefined || body.ValueKind == JsonValueKind.Null)
                return BadRequest("Request body is empty or invalid JSON.");

            var serviceDesc = _services.FirstOrDefault(s =>
                s.InterfaceType.Name.Equals(service, StringComparison.OrdinalIgnoreCase));
            if (serviceDesc == null)
                return NotFound($"Service '{service}' not found");

            if (!serviceDesc.Methods.TryGetValue(method, out var methodInfo))
                return NotFound($"Method '{method}' not found on service '{service}'");

            var parameters = methodInfo.GetParameters();
            var argsArray = new object?[parameters.Length];

            try
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    if (body.TryGetProperty(param.Name!, out var prop))
                    {
                        var obj = JsonSerializer.Deserialize(prop.GetRawText(), param.ParameterType, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        argsArray[i] = obj;
                    }
                    else
                    {
                        argsArray[i] = GetDefault(param.ParameterType);
                    }
                }

                var endpointUrl = _config[$"WcfEndpoints:{service}"];
                if (string.IsNullOrEmpty(endpointUrl))
                    return StatusCode(500, $"WCF endpoint for '{service}' not configured");

                var result = await SoapInvoker.InvokeAsync(serviceDesc.InterfaceType, method, argsArray!, endpointUrl);
                return Ok(result);
            }
            catch (JsonException jsonEx)
            {
                return BadRequest($"JSON parsing error: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        private static object? GetDefault(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}
