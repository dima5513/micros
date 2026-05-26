using System.Text.Json;
using System.Text.Json.Serialization;

namespace Micros.Core.Types;

public class PatchJsonConverter<T> : JsonConverter<Patch<T>>
{
    public override Patch<T> Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions opts)
    {
        // Сюда мы попадаем ТОЛЬКО если поле есть в JSON.
        // Значит IsSet = true. Дальше отличаем "null" от значения.
        if (reader.TokenType == JsonTokenType.Null)
            return new Patch<T>(default);

        var value = JsonSerializer.Deserialize<T>(ref reader, opts);
        return new Patch<T>(value);
    }

    public override void Write(Utf8JsonWriter writer, Patch<T> value, JsonSerializerOptions opts)
    {
        if (!value.IsSet) return; // на сериализации тоже пропускаем
        JsonSerializer.Serialize(writer, value.Value, opts);
    }
}