using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Shared;
using Visio.Services.Interfaces;

namespace Visio.ViewModels;

/// <summary>
/// ViewModel principal para a tela de streaming RTSP
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IRtspStreamService _rtspService;

    [ObservableProperty]
    private string _rtspUrl = "rtsp://";

    [ObservableProperty]
    private string _statusMessage = "Desconectado";

    [ObservableProperty]
    private Color _statusColor = Colors.Gray;

    [ObservableProperty]
    private bool _isConnected = false;

    [ObservableProperty]
    private MediaPlayer? _mediaPlayer;

    public MainViewModel(IRtspStreamService rtspService)
    {
        _rtspService = rtspService;
        _rtspService.ConnectionError += OnConnectionError;
    }

    /// <summary>
    /// Comando para conectar ao stream RTSP
    /// </summary>
    [RelayCommand]
    private async Task ConnectAsync()
    {
        try
        {
            StatusMessage = "Conectando...";
            StatusColor = Colors.Orange;

            var success = await _rtspService.ConnectAsync(RtspUrl);

            if (success)
            {
                MediaPlayer = _rtspService.GetMediaPlayer();
                IsConnected = true;
                StatusMessage = "Conectado";
                StatusColor = Colors.Green;
            }
            else
            {
                StatusMessage = "Falha na conexão";
                StatusColor = Colors.Red;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
            StatusColor = Colors.Red;
        }
    }

    /// <summary>
    /// Comando para desconectar do stream RTSP
    /// </summary>
    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await _rtspService.DisconnectAsync();
        MediaPlayer = null;
        IsConnected = false;
        StatusMessage = "Desconectado";
        StatusColor = Colors.Gray;
    }

    private void OnConnectionError(object? sender, string error)
    {
        StatusMessage = $"Erro: {error}";
        StatusColor = Colors.Red;
        IsConnected = false;
    }
}
