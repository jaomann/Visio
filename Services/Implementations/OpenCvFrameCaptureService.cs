using OpenCvSharp;
using Visio.Services.Interfaces;
using System.Diagnostics;

namespace Visio.Services.Implementations;

/// <summary>
/// Serviço de captura de frames RTSP usando OpenCV
/// </summary>
public class OpenCvFrameCaptureService : IFrameCaptureService, IDisposable
{
    private VideoCapture? _capture;
    private Mat? _currentFrame;
    private bool _isCapturing;
    private CancellationTokenSource? _cts;
    private readonly object _frameLock = new();

    public bool IsConnected { get; private set; }

    public event EventHandler<string>? ConnectionError;

    public async Task<bool> ConnectAsync(string rtspUrl)
    {
        try
        {
            Debug.WriteLine($"[OpenCV] Conectando: {rtspUrl}");

            await Task.Run(() =>
            {
                _capture = new VideoCapture(rtspUrl, VideoCaptureAPIs.FFMPEG);
                
                if (!_capture.IsOpened())
                {
                    throw new Exception("Falha ao abrir stream RTSP");
                }

                _capture.Set(VideoCaptureProperties.BufferSize, 1);

                var width = _capture.FrameWidth;
                var height = _capture.FrameHeight;
                var fps = _capture.Fps;

                Debug.WriteLine($"[OpenCV] Stream aberto: {width}x{height} @ {fps}fps");
            });

            IsConnected = true;
            StartCapture();
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[OpenCV] Erro: {ex.Message}");
            ConnectionError?.Invoke(this, ex.Message);
            IsConnected = false;
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        StopCapture();
        
        await Task.Delay(100);
        
        await Task.Run(() =>
        {
            _capture?.Release();
            _capture?.Dispose();
            _capture = null;

            lock (_frameLock)
            {
                _currentFrame?.Dispose();
                _currentFrame = null;
            }

            IsConnected = false;
            Debug.WriteLine("[OpenCV] Desconectado");
        });
    }

    public Mat? GetCurrentFrame()
    {
        lock (_frameLock)
        {
            return _currentFrame?.Clone();
        }
    }

    public void StartCapture()
    {
        if (_isCapturing) return;

        _isCapturing = true;
        _cts = new CancellationTokenSource();

        Task.Run(() =>
        {
            Debug.WriteLine("[OpenCV] Iniciando captura de frames");

            using var frame = new Mat();

            while (_isCapturing && !_cts.Token.IsCancellationRequested)
            {
                try
                {
                    if (_capture == null || !_capture.IsOpened())
                    {
                        Debug.WriteLine("[OpenCV] Capture fechado, parando loop");
                        break;
                    }

                    if (!_capture.Grab())
                        continue;

                    _capture.Retrieve(frame);

                    if (!frame.Empty())
                    {
                        lock (_frameLock)
                        {
                            _currentFrame?.Dispose();
                            _currentFrame = frame.Clone();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[OpenCV] Erro na captura: {ex.Message}");
                    ConnectionError?.Invoke(this, ex.Message);
                    break;
                }
            }

            _isCapturing = false;
            Debug.WriteLine("[OpenCV] Loop de captura finalizado");

        }, _cts.Token);
    }

    public void StopCapture()
    {
        if (!_isCapturing) return;

        Debug.WriteLine("[OpenCV] Parando captura");
        _isCapturing = false;
        
        try
        {
            _cts?.Cancel();
        }
        catch { }
        
        Thread.Sleep(50);
        
        _cts?.Dispose();
        _cts = null;
    }

    public void Dispose()
    {
        StopCapture();
        _capture?.Dispose();
        
        lock (_frameLock)
        {
            _currentFrame?.Dispose();
        }
    }
}
