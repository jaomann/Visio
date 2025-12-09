using OpenCvSharp;

namespace Visio.Services.Interfaces;

/// <summary>
/// Interface para serviço de captura de frames via OpenCV
/// </summary>
public interface IFrameCaptureService
{
    /// <summary>
    /// Indica se está conectado ao stream
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Evento disparado quando ocorre erro de conexão
    /// </summary>
    event EventHandler<string>? ConnectionError;

    /// <summary>
    /// Conecta ao stream RTSP
    /// </summary>
    Task<bool> ConnectAsync(string rtspUrl);

    /// <summary>
    /// Desconecta do stream RTSP
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Obtém o frame atual capturado
    /// </summary>
    Mat? GetCurrentFrame();

    /// <summary>
    /// Inicia a captura contínua de frames
    /// </summary>
    void StartCapture();

    /// <summary>
    /// Para a captura de frames
    /// </summary>
    void StopCapture();
}
