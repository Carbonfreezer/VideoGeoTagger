using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace VideoGeoTagger;

/// <summary>
///     System to scroll through the video.
/// </summary>
public class VideoAdministrator
{
    /// <summary>
    ///     A delegate that informs if the slider position has been changed.
    /// </summary>
    /// <param name="videoPosition">The position we are currently in the video.</param>
    public delegate void InformPositionChanged(TimeSpan videoPosition);

    /// <summary>
    ///     The canvas where the video gets displayed.
    /// </summary>
    private readonly Canvas m_displayCanvas;

    /// <summary>
    ///     The slider to scroll through the time line.
    /// </summary>
    private readonly Slider m_slider;

    /// <summary>
    ///     Indicates if the media is present.
    /// </summary>
    private bool m_mediaActive;

    /// <summary>
    ///     The media player to show the video.
    /// </summary>
    private MediaPlayer? m_mediaPlayer;

    /// <summary>
    ///     The total length of the video.
    /// </summary>
    private TimeSpan m_videoLength;

    /// <summary>
    ///     Generates the video administrator from the slider and the display canvas.
    /// </summary>
    /// <param name="displayCanvas">The canvas where we shoe the video in.</param>
    /// <param name="usedSlider">The slider we use.</param>
    public VideoAdministrator(Canvas displayCanvas, Slider usedSlider)
    {
        m_displayCanvas = displayCanvas;
        m_slider = usedSlider;
        m_slider.PreviewMouseUp += SliderValueChanged;
    }


    /// <summary>
    ///     Indicates the total length of the video.
    /// </summary>
    public TimeSpan TotalLength => m_videoLength;


    /// <summary>
    ///     Asks for the current video position or sets it.
    /// </summary>
    public TimeSpan VideoPosition
    {
        get
        {
            if ((m_mediaPlayer == null))
                return TimeSpan.Zero;
            return m_mediaPlayer.Position;
        }
        set
        {
            double relativeValue = value / m_videoLength;
            m_slider.Value = relativeValue * 100.0f;
            ProcessSliderValue();
        }
    }

    /// <summary>
    ///     The event that the video position has been changed.
    /// </summary>
    public event InformPositionChanged? OnVideoPositionChanged;


    /// <summary>
    ///     Gets called on mouse release of the slider.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SliderValueChanged(object sender, MouseButtonEventArgs e)
    {
        if (m_mediaActive)
            ProcessSliderValue();
    }


    /// <summary>
    ///     Called externally to load a video file.
    /// </summary>
    /// <param name="fileName">The name of the video file we want to load.</param>
    public void LoadVideo(string fileName)
    {
        m_mediaActive = false;
        m_mediaPlayer = new MediaPlayer();
        m_mediaPlayer.Open(new Uri(fileName));
        VideoDrawing drawing = new VideoDrawing
        {
            Player = m_mediaPlayer,
            Rect = new Rect(0, 0, 1, 1)
        };
        m_displayCanvas.Background = new DrawingBrush(drawing);
        m_slider.Value = 0.0f;
        m_mediaPlayer.MediaOpened += MediaOpened;
    }


    /// <summary>
    ///     Callback gets invoked as soon as the video is opened.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MediaOpened(object? sender, EventArgs e)
    {
        m_mediaActive = true;
        if (m_mediaPlayer == null)
            return;

        m_videoLength = m_mediaPlayer.NaturalDuration.TimeSpan;
        m_mediaPlayer.SpeedRatio = 0.0000000001;
        m_mediaPlayer.Play();
        ProcessSliderValue();
    }

    /// <summary>
    ///     We process the slider value and advance the video to the current state.
    /// </summary>
    private void ProcessSliderValue()
    {
        if (m_mediaPlayer == null)
            return;

        TimeSpan target = m_videoLength * (m_slider.Value / 100.0f);
        m_mediaPlayer.Position = target;
        m_mediaPlayer.Play();

        while (m_mediaPlayer.Position == target)
            Thread.Sleep(20);

        m_mediaPlayer.Pause();

        m_slider.Value = 100.0f * (m_mediaPlayer.Position / m_videoLength);
        OnVideoPositionChanged?.Invoke(m_mediaPlayer.Position);
    }
}