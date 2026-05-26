using Micros.Core.Types;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Micros.Api.Infrastructure.OpenApi;

public class PatchOpenApiSchemaTransformer : IOpenApiSchemaTransformer
{
    public async Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;

        if (IsPatchType(type))
        {
            var innerType = type.GetGenericArguments()[0];
            var innerSchema = await context.GetOrCreateSchemaAsync(innerType, null, cancellationToken);

            schema.Type = innerSchema.Type | JsonSchemaType.Null;
            schema.Format = innerSchema.Format;
            schema.Items = innerSchema.Items;
            schema.Properties = innerSchema.Properties;
            schema.Required = innerSchema.Required;
            schema.Enum = innerSchema.Enum;
            schema.Description = innerSchema.Description;
            schema.AdditionalProperties = innerSchema.AdditionalProperties;
            return;
        }

        if (schema.Required is { Count: > 0 })
        {
            foreach (var prop in context.JsonTypeInfo.Properties)
            {
                if (IsPatchType(prop.PropertyType))
                    schema.Required.Remove(prop.Name);
            }
        }
    }

    private static bool IsPatchType(Type t) =>
        t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Patch<>);
}
