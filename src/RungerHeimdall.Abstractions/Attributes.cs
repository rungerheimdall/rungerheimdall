using System;

namespace RungerHeimdall;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class HotPathAttribute : Attribute
{
    public string? Reason { get; set; }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ColdPathAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class HeimdallSliceAttribute : Attribute
{
    public HeimdallSliceAttribute(string route)
    {
        Route = route;
    }

    public string Route { get; }

    public SliceHost Host { get; set; } = SliceHost.MinimalApi;
}

public enum SliceHost
{
    MinimalApi = 0,
    FastEndpoints = 1,
    MediatR = 2,
    Controller = 3,
}