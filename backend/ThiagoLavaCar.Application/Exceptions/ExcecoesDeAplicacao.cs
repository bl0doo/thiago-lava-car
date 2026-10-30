namespace ThiagoLavaCar.Application.Exceptions;

/// <summary>Recurso inexistente ou de outro dono; a API responde 404 sem revelar qual dos dois (HTTP 404).</summary>
public sealed class NaoEncontradoException(string message) : Exception(message);

/// <summary>Credenciais erradas, usuário inativo ou token de renovação inválido (HTTP 401).</summary>
public sealed class NaoAutenticadoException(string message) : Exception(message);

/// <summary>Usuário bloqueado por tentativas seguidas de login (HTTP 423).</summary>
public sealed class UsuarioBloqueadoException(string message) : Exception(message);

/// <summary>Telefone já cadastrado (HTTP 409); devolve o id existente para o frontend oferecer o cadastro encontrado.</summary>
public sealed class ClienteDuplicadoException(int clienteId) : Exception("Já existe cliente com este telefone.")
{
    public int ClienteId { get; } = clienteId;
}
