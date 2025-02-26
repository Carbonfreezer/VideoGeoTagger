using System.Diagnostics;
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
   

    /// <summary>
    /// Asks for the current video position or sets it.
    /// </summary>
    public TimeSpan VideoPosition
    {
        get
        {
            if ((m_mediaClock == null) || (!m_mediaClock.CurrentTime.HasValue))
                return TimeSpan.Zero;
            return m_mediaClock.CurrentTime.Value;
        }
        set
        {
            double relativeValue = value / m_videoLength;
            m_slider.Value = relativeValue * 100.0f;
            ProcessSliderValue();
        }
    }    
 

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
        m_mediaClock.CurrentStateInvalidated += StateChange;
    }

    private void StateChange(object? sender, EventArgs e)
    {
        if (!m_mediaActive || (m_mediaClock == null) || (m_mediaClock.CurrentState != ClockState.Active))
            return;
        
        if (!m_mediaClock.IsPaused)
            m_mediaClock.Controller?.Pause();

        if (m_mediaClock.CurrentTime.HasValue)
        {
            m_slider.Value = m_mediaClock.CurrentTime.Value / m_videoLength * 100.0f;
        }
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
    }
}