using System;
using System.Collections.Generic;

namespace RungerHeimdall.Engine.Json;

internal abstract class JsonValue
{
    public static JsonValue Parse(string text)
    {
        return new JsonParser(text).ParseDocument();
    }
}

internal sealed class JsonObject : JsonValue
{
    public JsonObject(Dictionary<string, JsonValue> properties)
    {
        Properties = properties;
    }

    public IReadOnlyDictionary<string, JsonValue> Properties { get; }

    public JsonValue? Get(string name)
    {
        if (!Properties.TryGetValue(name, out var value))
        {
            return null;
        }

        return value is JsonNull ? null : value;
    }

    public string? GetString(string name)
    {
        return Get(name) is JsonString text ? text.Value : null;
    }

    public int? GetInt(string name)
    {
        return Get(name) is JsonNumber number && number.Value >= int.MinValue && number.Value <= int.MaxValue
            ? (int)number.Value
            : null;
    }

    public bool? GetBool(string name)
    {
        return Get(name) is JsonBoolean boolean ? boolean.Value : null;
    }

    public JsonArray? GetArray(string name)
    {
        return Get(name) as JsonArray;
    }
}

internal sealed class JsonArray : JsonValue
{
    public JsonArray(List<JsonValue> items)
    {
        Items = items;
    }

    public IReadOnlyList<JsonValue> Items { get; }
}

internal sealed class JsonString : JsonValue
{
    public JsonString(string value)
    {
        Value = value;
    }

    public string Value { get; }
}

internal sealed class JsonNumber : JsonValue
{
    public JsonNumber(double value)
    {
        Value = value;
    }

    public double Value { get; }
}

internal sealed class JsonBoolean : JsonValue
{
    public JsonBoolean(bool value)
    {
        Value = value;
    }

    public bool Value { get; }
}

internal sealed class JsonNull : JsonValue
{
    public static readonly JsonNull Instance = new JsonNull();

    private JsonNull()
    {
    }
}