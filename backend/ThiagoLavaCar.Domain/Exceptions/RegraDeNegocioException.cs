namespace ThiagoLavaCar.Domain.Exceptions;

/// <summary>Violação de regra de negócio. A API traduz cada subtipo para um código HTTP.</summary>
public abstract class RegraDeNegocioException(string message) : Exception(message);

/// <summary>Estado do recurso não permite a operação (HTTP 409).</summary>
public sealed class ConflitoException(string message) : RegraDeNegocioException(message);

/// <summary>Dados válidos no formato, mas que violam uma regra (HTTP 422).</summary>
public sealed class RegraInvalidaException(string message) : RegraDeNegocioException(message);
