using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace VideoGeoTagger;

public class VideoAdministrator
{
    private MediaClock? m_mediaClock;
    private bool m_mediaActive;
    private TimeSpan m_videoLength;
    private readonly Canvas m_displayCanvas;
    private readonly Slider m_slider;
    private readonly Timer m_resumeTimer;


    /// <summary>
    /// Asks for the current video position.
    /// </summary>
    public TimeSpan? VideoPosition => m_mediaClock?.CurrentTime;


    /// <summary>
    /// Generates the video administrator from the slider and the display canvas.
    /// </summary>
    /// <param name="displayCanvas">The canvas where we shoe the video in.</param>
    /// <param name="usedSlider">The slider we use.</param>
    public VideoAdministrator(Canvas displayCanvas, Slider usedSlider)
    {
        m_displayCanvas = displayCanvas;
        m_slider = usedSlider;
        m_slider.PreviewMouseUp += SliderValueChanged;
        m_resumeTimer = new Timer(PauseVideo);
    }

    /// <summary>
    /// Gets invoked after some time to pause the video again.
    /// </summary>
    /// <param name="state"></param>
    private void PauseVideo(object? state)
    {
        m_mediaClock?.Controller?.Pause();
    }

    /// <summary>
    /// Gets called on mouse release of the slider.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SliderValueChanged(object sender, MouseButtonEventArgs e)
    {
        if (m_mediaActive)
            ProcessSliderValue();
    }


    /// <summary>
    /// Called externally to load a video file.
    /// </summary>
    /// <param name="fileName">The name of the video file we want to load.</param>
    public void LoadVideo(string fileName)
    {
        m_mediaActive = false;
        MediaTimeline mediaTimeLine = new MediaTimeline(new Uri(fileName));
        m_mediaClock = mediaTimeLine.CreateClock();
        MediaPlayer mediaPlayer = new MediaPlayer
        {
            Clock = m_mediaClock
        };
        VideoDrawing drawing = new VideoDrawing
        {
            Player = mediaPlayer,
            Rect = new Rect(0, 0, 1, 1)
        };
        m_displayCanvas.Background = new DrawingBrush(drawing);
        m_slider.Value = 0.0f;
        mediaPlayer.MediaOpened += MediaOpened;
    }



    /// <summary>
    /// Callback gets invoked as soon as the video is opened.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MediaOpened(object? sender, EventArgs e)
    {
        m_mediaActive = true;
        if (m_mediaClock != null)
            m_videoLength = m_mediaClock.NaturalDuration.TimeSpan;
        ProcessSliderValue();
    }

    /// <summary>
    /// We process the slider value and advance the video to the current state.
    /// </summary>
    private void ProcessSliderValue()
    {
        if (m_mediaClock == null)
            return;

        if (m_mediaClock.IsPaused)
            m_mediaClock.Controller?.Resume();
        TimeSpan target = m_videoLength * ( m_slider.Value / 100.0f);
        m_mediaClock.Controller?.Seek(target, TimeSeekOrigin.BeginTime);
        m_resumeTimer.Change(300, int.MaxValue);
    }
}