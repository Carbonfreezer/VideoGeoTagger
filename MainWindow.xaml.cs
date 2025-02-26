using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace VideoGeoTagger;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{

    private readonly VideoAdministrator m_videoAdmin;
    private readonly SplittingAdministrator m_splitting;

    public MainWindow()
    {
        InitializeComponent();
        m_videoAdmin = new VideoAdministrator(VideoImage, VideoSlider);
        m_splitting =
            new SplittingAdministrator(SplittingList, ButtonCreateSplitting, ButtonDeleteSplitting, m_videoAdmin);
    }


    /// <summary>
    ///     Gets called, when the zoom in button has been pressed
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnZoomIn(object sender, RoutedEventArgs e)
    {
    }


    /// <summary>
    ///     Gets called when the zoom out button has been pressed.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnZoomOut(object sender, RoutedEventArgs e)
    {
    }


    /// <summary>
    ///     Gets called when the mouse has been oved up un the gpx window.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnGpxMouseDown(object sender, MouseButtonEventArgs e)
    {
    }

    /// <summary>
    ///     Gets called when the mouse is released over the GPX window.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnGpxMouseUp(object sender, MouseButtonEventArgs e)
    {
    }

    /// <summary>
    ///     Gets called when the mouse is moved over the gpx window.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnGpxMouseMove(object sender, MouseEventArgs e)
    {
    }


    /// <summary>
    ///     Gets called when we press the load movie button.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnLoadVideo(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            DefaultExt = ".mp4", // Default file extension
            Filter = "Movie (.mp4)|*.mp4" // Filter files by extension
        };

        // Show open file dialog box
        bool? result = dialog.ShowDialog();

        // Process open file dialog box results
        if (result == true)
        {
            m_videoAdmin.LoadVideo(dialog.FileName);
            m_splitting.ResetData();

            // TODO: Here we set the movie information.
        }
    }

    /// <summary>
    ///     Gets called when we load the gpx information.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnLoadGpx(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            DefaultExt = ".gpx", // Default file extension
            Filter = "GPS Data (.gpx)|*.gpx" // Filter files by extension
        };

        // Show open file dialog box
        bool? result = dialog.ShowDialog();

        // Process open file dialog box results
        if (result == true)
        {
            // Open document
            string filename = dialog.FileName;

            // TODO: Here we set the movie information.
        }
    }
}