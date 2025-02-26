using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace VideoGeoTagger;

public class VideoAdministrator
{
    private MediaPlayer? m_mediaPlayer;
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
        m_mediaPlayer.BufferingEnded += Test;
    }

    private void Test(object? sender, EventArgs e)
    {
        Trace.WriteLine(e);
    }


    /// <summary>
    /// Callback gets invoked as soon as the video is opened.
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
    /// We process the slider value and advance the video to the current state.
    /// </summary>
    private void ProcessSliderValue()
    {
        if (m_mediaPlayer == null)
            return;
       
        TimeSpan target = m_videoLength * ( m_slider.Value / 100.0f);
        m_mediaPlayer.Position = target;
        m_mediaPlayer.Play();
        Thread.Sleep(300);
        m_mediaPlayer.Pause();
    }
}