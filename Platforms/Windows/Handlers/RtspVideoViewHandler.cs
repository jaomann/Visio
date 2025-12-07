using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using Visio.Controls;
using LibVLCSharp.Shared;
using System.Runtime.InteropServices;

namespace Visio.Platforms.Windows.Handlers;

public class RtspVideoViewHandler : ViewHandler<RtspVideoView, Microsoft.UI.Xaml.Controls.Grid>
{
    private MediaPlayer? _libVlcMediaPlayer;
    private SwapChainPanel? _swapChainPanel;
    private bool _isAttached;

    public static IPropertyMapper<RtspVideoView, RtspVideoViewHandler> Mapper = new PropertyMapper<RtspVideoView, RtspVideoViewHandler>(ViewHandler.ViewMapper)
    {
        [nameof(RtspVideoView.MediaPlayer)] = MapMediaPlayer
    };

    public RtspVideoViewHandler() : base(Mapper)
    {
    }

    protected override Microsoft.UI.Xaml.Controls.Grid CreatePlatformView()
    {
        var grid = new Microsoft.UI.Xaml.Controls.Grid();
        
        _swapChainPanel = new SwapChainPanel();
        grid.Children.Add(_swapChainPanel);

        return grid;
    }

    protected override void ConnectHandler(Microsoft.UI.Xaml.Controls.Grid platformView)
    {
        base.ConnectHandler(platformView);
        UpdateMediaPlayer();
    }

    protected override void DisconnectHandler(Microsoft.UI.Xaml.Controls.Grid platformView)
    {
        DetachPlayer();
        _libVlcMediaPlayer = null;
        _swapChainPanel = null;
        base.DisconnectHandler(platformView);
    }

    private static void MapMediaPlayer(RtspVideoViewHandler handler, RtspVideoView view)
    {
        handler.UpdateMediaPlayer();
    }

    private void UpdateMediaPlayer()
    {
        if (PlatformView == null || VirtualView == null)
            return;

        var newMediaPlayer = VirtualView.MediaPlayer;

        if (_libVlcMediaPlayer == newMediaPlayer)
            return;

        DetachPlayer();

        _libVlcMediaPlayer = newMediaPlayer;

        if (newMediaPlayer != null)
        {
            AttachPlayer();
        }
    }

    private void AttachPlayer()
    {
        if (_libVlcMediaPlayer == null || _swapChainPanel == null || _isAttached)
            return;

        try
        {
            System.Diagnostics.Debug.WriteLine("[Handler] Tentando obter handle do SwapChainPanel");
            var handle = GetSwapChainPanelHandle(_swapChainPanel);
            
            System.Diagnostics.Debug.WriteLine($"[Handler] Handle obtido: {handle}");
            
            if (handle != IntPtr.Zero)
            {
                _libVlcMediaPlayer.Hwnd = handle;
                _isAttached = true;
                System.Diagnostics.Debug.WriteLine($"[Handler] LibVLC Hwnd configurado: {handle}");
                
                System.Diagnostics.Debug.WriteLine("[Handler] Chamando Play()...");
                _libVlcMediaPlayer.Play();
                System.Diagnostics.Debug.WriteLine("[Handler] Play() chamado!");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[Handler] ERRO: Handle e zero!");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Handler] ERRO ao anexar player: {ex.Message}");
        }
    }

    private void DetachPlayer()
    {
        if (_libVlcMediaPlayer != null && _isAttached)
        {
            try
            {
                _libVlcMediaPlayer.Hwnd = IntPtr.Zero;
                System.Diagnostics.Debug.WriteLine("[Handler] LibVLC Hwnd removido");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Handler] ERRO ao desanexar: {ex.Message}");
            }
            
            _isAttached = false;
        }
    }

    private IntPtr GetSwapChainPanelHandle(SwapChainPanel panel)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[Handler] Tentando cast para ISwapChainPanelNative");
            var nativePanel = WinRT.CastExtensions.As<ISwapChainPanelNative>(panel);
            
            if (nativePanel == null)
            {
                System.Diagnostics.Debug.WriteLine("[Handler] ERRO: Cast retornou null");
                return IntPtr.Zero;
            }
            
            var handle = nativePanel.GetHandle();
            System.Diagnostics.Debug.WriteLine($"[Handler] Handle do painel: {handle}");
            return handle;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Handler] ERRO ao obter handle: {ex.Message}");
            return IntPtr.Zero;
        }
    }

    [ComImport]
    [Guid("63aad0b8-7c24-40ff-85a8-640d944cc325")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISwapChainPanelNative
    {
        IntPtr GetHandle();
    }
}
