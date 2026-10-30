using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ThiagoLavaCar.Api.Infrastructure;

/// <summary>
/// Propriedades não anuláveis dos DTOs saem como obrigatórias no contrato. Sem isso o openapi-typescript
/// gera todos os campos como opcionais e o frontend perde a checagem de tipos.
/// </summary>
internal sealed class PropriedadesNaoNulasSaoObrigatorias : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema { Properties.Count: > 0 } objeto)
        {
            return;
        }

        objeto.Required ??= new HashSet<string>();
        foreach (var (nome, propriedade) in objeto.Properties)
        {
            var aceitaNulo = propriedade.Type is { } tipo && tipo.HasFlag(JsonSchemaType.Null);
            if (!aceitaNulo)
            {
                objeto.Required.Add(nome);
            }
        }
    }
}
