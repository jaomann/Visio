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
    private readonly Visio.Services.Interfaces.IImageProcessingService _imageProcessingService;
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

    [ObservableProperty]
    private bool _isGrayscaleEnabled;

    [ObservableProperty]
    private bool _isBlurEnabled;

    [ObservableProperty]
    private bool _isEdgeDetectionEnabled;

    [ObservableProperty]
    private bool _isFaceDetectionEnabled;

    public MainViewModel(IFrameCaptureService captureService, Visio.Services.Interfaces.IImageProcessingService imageProcessingService)
    {
        _captureService = captureService;
        _imageProcessingService = imageProcessingService;
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

    [RelayCommand]
    private async Task CaptureSnapshot()
    {
        if (!IsConnected)
        {
            StatusMessage = "Conecte ao RTSP primeiro";
            StatusColor = Colors.Orange;
            return;
        }

        var frame = _captureService.GetCurrentFrame();
        if (frame == null || frame.Empty())
        {
            StatusMessage = "Nenhum frame disponível";
            StatusColor = Colors.Orange;
            return;
        }

        try
        {
            using var processedFrame = ApplyFilters(frame);
            
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filename = $"snapshot_{timestamp}.png";
            var picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            var visioPath = Path.Combine(picturesPath, "Visio");
            
            Directory.CreateDirectory(visioPath);
            
            var fullPath = Path.Combine(visioPath, filename);
            
            Cv2.ImWrite(fullPath, processedFrame);
            
            StatusMessage = $"Foto salva: {filename}";
            StatusColor = Colors.Green;
            
            await Task.Delay(3000);
            
            if (IsConnected)
            {
                StatusMessage = "Conectado";
                StatusColor = Colors.Green;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro ao salvar: {ex.Message}";
            StatusColor = Colors.Red;
        }
        finally
        {
            frame.Dispose();
        }
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
                if (_captureService == null || !IsConnected)
                {
                    _conversionSemaphore.Release();
                    return;
                }

                var mat = _captureService.GetCurrentFrame();
            if (mat != null && !mat.Empty())
            {
                try
                {
                    var imageSource = await Task.Run(() => MatToImageSource(mat));
                    
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        if (IsConnected)
                        {
                            CurrentFrame = imageSource;
                        }
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

    public void OnPageAppearing()
    {
        Debug.WriteLine("[ViewModel] Página aparecendo");
        
        try
        {
            if (IsConnected && _frameUpdateTimer == null && _captureService != null)
            {
                StartFrameUpdate();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ViewModel] Erro ao retomar timer: {ex.Message}");
        }
    }

    public void OnPageDisappearing()
    {
        Debug.WriteLine("[ViewModel] Página desaparecendo");
        
        try
        {
            StopFrameUpdate();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ViewModel] Erro ao parar timer: {ex.Message}");
        }
    }

    private ImageSource MatToImageSource(Mat mat)
    {
        using var processed = ApplyFilters(mat);
        using var resized = new Mat();
        Cv2.Resize(processed, resized, new OpenCvSharp.Size(640, 360));
        
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

    private Mat ApplyFilters(Mat input)
    {
        var current = input.Clone();

        if (IsGrayscaleEnabled)
        {
            var temp = _imageProcessingService.ApplyGrayscale(current);
            current.Dispose();
            current = temp;
        }

        if (IsBlurEnabled)
        {
            var temp = _imageProcessingService.ApplyBlur(current);
            current.Dispose();
            current = temp;
        }

        if (IsEdgeDetectionEnabled)
        {
            var temp = _imageProcessingService.ApplyEdgeDetection(current);
            current.Dispose();
            current = temp;
        }

        if (IsFaceDetectionEnabled)
        {
            var temp = _imageProcessingService.ApplyFaceDetection(current);
            current.Dispose();
            current = temp;
        }

        return current;
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
