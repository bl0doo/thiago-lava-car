namespace ThiagoLavaCar.IntegrationTests;

/// <summary>Uma única API e um único banco para todas as classes de teste (a configuração vai por variáveis de ambiente).</summary>
[CollectionDefinition(Nome)]
public sealed class ApiCompartilhada : ICollectionFixture<ApiFactory>
{
    public const string Nome = "api";
}
