namespace Visio.Services.Interfaces;

/// <summary>
/// Interface para serviço de streaming RTSP
/// </summary>
public interface IRtspStreamService
{
    /// <summary>
    /// Conecta a uma URL RTSP
    /// </summary>
    /// <param name="rtspUrl">URL do stream RTSP (ex: rtsp://192.168.1.100/stream)</param>
    /// <returns>True se conectou com sucesso, False caso contrário</returns>
    Task<bool> ConnectAsync(string rtspUrl);
    
    /// <summary>
    /// Desconecta do stream atual
    /// </summary>
    Task DisconnectAsync();
    
    /// <summary>
    /// Indica se está conectado a um stream
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// Evento disparado quando ocorre erro de conexão
    /// </summary>
    event EventHandler<string>? ConnectionError;
}
