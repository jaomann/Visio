using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Visio.Services.Interfaces;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.IO;
using System.Diagnostics;

namespace Visio.ViewModels;

/// <summary>
/// ViewModel principal para a tela de streaming RTSP
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IFrameCaptureService _captureService;
    private Timer? _frameUpdateTimer;
    private readonly SemaphoreSlim _conversionSemaphore = new SemaphoreSlim(1, 1);

    [ObservableProperty]
    private string _rtspUrl = "rtsp://";

    [ObservableProperty]
    private string _statusMessage = "Desconectado";

    [ObservableProperty]
    private Color _statusColor = Colors.Gray;

    [ObservableProperty]
    private bool _isConnected = false;

    [ObservableProperty]
    private ImageSource? _currentFrame;

    public MainViewModel(IFrameCaptureService captureService)
    {
        _captureService = captureService;
        _captureService.ConnectionError += OnConnectionError;
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

            var success = await _captureService.ConnectAsync(RtspUrl);

            if (success)
            {
                IsConnected = true;
                StatusMessage = "Conectado";
                StatusColor = Colors.Green;
                StartFrameUpdate();
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
        StopFrameUpdate();
        await _captureService.DisconnectAsync();
        
        CurrentFrame = null;
        IsConnected = false;
        StatusMessage = "Desconectado";
        StatusColor = Colors.Gray;
    }

    private void StartFrameUpdate()
    {
        Debug.WriteLine("[ViewModel] Iniciando timer de atualização de frames (20 FPS)");
        
        _frameUpdateTimer = new Timer(async _ =>
        {
            if (!_conversionSemaphore.Wait(0))
            {
                return;
            }

            try
            {
                var mat = _captureService.GetCurrentFrame();
            if (mat != null && !mat.Empty())
            {
                try
                {
                    var imageSource = await Task.Run(() => MatToImageSource(mat));
                    
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        CurrentFrame = imageSource;
                    });
                }
                finally
                {
                        mat.Dispose();
                    }
                }
            }
            finally
            {
                _conversionSemaphore.Release();
            }
        }, null, 0, 33);
    }

    private void StopFrameUpdate()
    {
        Debug.WriteLine("[ViewModel] Parando timer de frames");
        _frameUpdateTimer?.Dispose();
        _frameUpdateTimer = null;
    }

    private ImageSource MatToImageSource(Mat mat)
    {
        using var resized = new Mat();
        Cv2.Resize(mat, resized, new OpenCvSharp.Size(640, 360));
        
        var bitmap = BitmapConverter.ToBitmap(resized);
        
        using var ms = new MemoryStream();
        
        var jpegEncoder = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders()
            .First(c => c.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid);
        var encoderParams = new System.Drawing.Imaging.EncoderParameters(1);
        encoderParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(
            System.Drawing.Imaging.Encoder.Quality, 70L);
        
        bitmap.Save(ms, jpegEncoder, encoderParams);
        bitmap.Dispose();
        
        ms.Position = 0;
        return ImageSource.FromStream(() => new MemoryStream(ms.ToArray()));
    }

    private void OnConnectionError(object? sender, string error)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusMessage = $"Erro: {error}";
            StatusColor = Colors.Red;
            IsConnected = false;
            StopFrameUpdate();
        });
    }
}
