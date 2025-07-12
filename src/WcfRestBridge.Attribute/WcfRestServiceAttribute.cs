// (c) 2025 Francesco Del Re <francesco.delre.87@gmail.com>
// This code is licensed under MIT license (see LICENSE.txt for details)
using System;

namespace WcfRestBridge.WcfAttribute
{
    /// <summary>
    /// Custom attribute used to mark WCF service interfaces intended to be exposed as REST endpoints.
    /// Specifies a route prefix that maps the interface to a RESTful route.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public class WcfRestServiceAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the route prefix used to expose the interface via REST.
        /// </summary>
        public string RoutePrefix { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WcfRestServiceAttribute"/> class.
        /// </summary>
        /// <param name="routePrefix">The route prefix for REST exposure.</param>
        public WcfRestServiceAttribute(string routePrefix) => RoutePrefix = routePrefix;
    }
}