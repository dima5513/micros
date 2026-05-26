using System.Text.Json;
using System.Text.Json.Serialization;

namespace Micros.Core.Types;

public class PatchJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Patch<>);

    public override JsonConverter CreateConverter(Type type, JsonSerializerOptions opts)
    {
        var inner = type.GetGenericArguments()[0];
        return (JsonConverter)Activator.CreateInstance(
            typeof(PatchJsonConverter<>).MakeGenericType(inner))!;
    }
}