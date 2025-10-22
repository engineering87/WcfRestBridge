// (c) 2025 Francesco Del Re <francesco.delre.87@gmail.com>
// This code is licensed under MIT license (see LICENSE.txt for details)
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Reflection;
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
            if (body.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                return BadRequest("Request body is empty or invalid JSON.");

            var serviceDesc = _services.FirstOrDefault(s =>
                s.InterfaceType.Name.Equals(service, StringComparison.OrdinalIgnoreCase));
            if (serviceDesc is null)
                return NotFound($"Service '{service}' not found.");

            if (!serviceDesc.Methods.TryGetValue(method, out var candidateMethods) || candidateMethods.Length == 0)
                return NotFound($"Method '{method}' not found on service '{service}'.");

            try
            {
                // Pick the best overload based on JSON binding score
                MethodInfo? selected = null;
                object?[]? selectedArgs = null;
                int bestScore = -1;

                foreach (var mi in candidateMethods)
                {
                    if (TryBindArguments(mi, body, out var args, out var score) && score > bestScore)
                    {
                        bestScore = score;
                        selected = mi;
                        selectedArgs = args;
                    }
                }

                if (selected is null || selectedArgs is null)
                    return BadRequest($"Cannot bind request JSON to any overload of '{method}' on '{service}'.");

                var endpointUrl = _config[$"WcfEndpoints:{service}"];
                if (string.IsNullOrWhiteSpace(endpointUrl))
                    return StatusCode(500, $"WCF endpoint for '{service}' not configured.");

                var result = await SoapInvoker.InvokeAsync(
                    contractType: serviceDesc.InterfaceType,
                    methodName: selected.Name,
                    args: selectedArgs,
                    endpointUrl: endpointUrl,
                    parameterTypes: selected.GetParameters().Select(p => p.ParameterType).ToArray()
                );

                return Ok(result);
            }
            catch (JsonException ex)
            {
                return BadRequest($"JSON parsing error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        private static object? GetDefault(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

        private static bool TryBindArguments(MethodInfo methodInfo, JsonElement body, out object?[] args, out int score)
        {
            var parameters = methodInfo.GetParameters();
            args = new object?[parameters.Length];
            score = 0;

            if (body.ValueKind != JsonValueKind.Object)
                return false;

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                if (TryGetPropertyCaseInsensitive(body, p.Name!, out var prop))
                {
                    try
                    {
                        args[i] = JsonSerializer.Deserialize(prop.GetRawText(), p.ParameterType, options);
                        score++; // explicit match
                    }
                    catch
                    {
                        args = Array.Empty<object?>();
                        score = -1;
                        return false;
                    }
                }
                else
                {
                    args[i] = GetDefault(p.ParameterType);
                }
            }

            return true;
        }

        private static bool TryGetPropertyCaseInsensitive(JsonElement obj, string name, out JsonElement value)
        {
            foreach (var prop in obj.EnumerateObject())
            {
                if (prop.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    value = prop.Value;
                    return true;
                }
            }
            value = default;
            return false;
        }
    }
}