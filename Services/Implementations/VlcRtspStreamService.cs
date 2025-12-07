using LibVLCSharp.Shared;
using System.Diagnostics;
using Visio.Services.Interfaces;

namespace Visio.Services.Implementations;

/// <summary>
/// Implementação do serviço de streaming RTSP usando LibVLC
/// </summary>
public class VlcRtspStreamService : IRtspStreamService
{
    private LibVLC? _libVLC;
    private MediaPlayer? _mediaPlayer;
    private bool _isConnected;

    public event EventHandler<string>? ConnectionError;

    public VlcRtspStreamService()
    {
    }

    /// <summary>
    /// Conecta a uma URL RTSP e inicia o streaming
    /// </summary>
    public async Task<bool> ConnectAsync(string rtspUrl)
    {
        if (string.IsNullOrWhiteSpace(rtspUrl))
            throw new ArgumentException("URL não pode ser vazia", nameof(rtspUrl));

        if (!rtspUrl.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("URL deve começar com rtsp://", nameof(rtspUrl));

        try
        {
            Debug.WriteLine($"[RTSP] Tentando conectar: {rtspUrl}");

            if (_libVLC == null)
            {
                var options = new[]
                {
                    "--network-caching=300",
                    "--rtsp-tcp",
                    "--no-audio",
                    "--verbose=0"
                };

                _libVLC = new LibVLC(options);
                Debug.WriteLine("[RTSP] LibVLC inicializado");
            }

            var media = new Media(_libVLC, rtspUrl, FromType.FromLocation);

            if (_mediaPlayer == null)
            {
                _mediaPlayer = new MediaPlayer(_libVLC);

                _mediaPlayer.Playing += (s, e) =>
                    Debug.WriteLine("[RTSP] Reproduzindo");

                _mediaPlayer.EncounteredError += (s, e) =>
                {
                    Debug.WriteLine("[RTSP] Erro encontrado");
                    _isConnected = false;
                    ConnectionError?.Invoke(this, "Erro na reprodução do stream");
                };
            }

            _mediaPlayer.Media = media;
            _mediaPlayer.Play();

            await Task.Delay(2000);

            _isConnected = _mediaPlayer.IsPlaying;

            if (_isConnected)
                Debug.WriteLine("[RTSP] Conectado com sucesso");
            else
                Debug.WriteLine("[RTSP] Não está reproduzindo");

            return _isConnected;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RTSP] ✗ Exceção: {ex.Message}");
            _isConnected = false;
            throw new Exception($"Falha ao conectar: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Desconecta do stream RTSP atual
    /// </summary>
    public async Task DisconnectAsync()
    {
        Debug.WriteLine("[RTSP] Desconectando...");
        _mediaPlayer?.Stop();
        _isConnected = false;
        await Task.CompletedTask;
    }

    public bool IsConnected => _isConnected;

    public MediaPlayer? GetMediaPlayer() => _mediaPlayer;
}
