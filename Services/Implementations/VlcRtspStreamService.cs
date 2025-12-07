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
        Debug.WriteLine($"[RTSP] Tentando conectar: {rtspUrl}");

        if (_libVLC == null)
        {
            _libVLC = new LibVLC(
                "--rtsp-tcp",
                "--network-caching=1000",
                "--verbose=2"
            );
        }

        var media = new Media(_libVLC, new Uri(rtspUrl));
        media.AddOption(":rtsp-tcp");
        media.AddOption(":network-caching=1000");

        if (_mediaPlayer == null)
        {
            _mediaPlayer = new MediaPlayer(_libVLC);

            _mediaPlayer.Playing += (_, _) =>
            {
                Debug.WriteLine("[RTSP] Playing");
                _isConnected = true;
            };

            _mediaPlayer.EncounteredError += (_, _) =>
            {
                Debug.WriteLine("[RTSP] Error");
                _isConnected = false;
            };
        }

        _mediaPlayer.Media = media;
        _mediaPlayer.Play();

        var timeout = DateTime.Now.AddSeconds(10);
        while (DateTime.Now < timeout)
        {
            if (_mediaPlayer.State == VLCState.Playing)
                return true;

            if (_mediaPlayer.State == VLCState.Error)
                return false;

            await Task.Delay(300);
        }

        Debug.WriteLine("[RTSP] Timeout");
        return false;
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
