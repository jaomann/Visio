using LibVLCSharp.Shared;

namespace Visio.Controls;

/// <summary>
/// Controle cross-platform para exibir vídeo RTSP usando LibVLC
/// </summary>
public class RtspVideoView : View
{
    public static readonly BindableProperty MediaPlayerProperty = BindableProperty.Create(
        nameof(MediaPlayer),
        typeof(MediaPlayer),
        typeof(RtspVideoView),
        null);

    public MediaPlayer? MediaPlayer
    {
        get => (MediaPlayer?)GetValue(MediaPlayerProperty);
        set => SetValue(MediaPlayerProperty, value);
    }
}
