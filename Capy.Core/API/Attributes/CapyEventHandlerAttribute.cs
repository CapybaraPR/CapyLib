using System;

namespace Capy.Core.API.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class CapyEventHandlerAttribute : Attribute
{
}
