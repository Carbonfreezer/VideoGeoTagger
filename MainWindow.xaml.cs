using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using VideoGeoTagger.GpxData;

namespace VideoGeoTagger;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{

    private readonly VideoAdministrator m_videoAdmin;
    private readonly SplittingAdministrator m_splitting;
    private readonly GpxRepresentation m_gpxRepresentation;
    private readonly GpxVisualizer m_gpxVisualizer;

    public MainWindow()
    {
        InitializeComponent();
        m_videoAdmin = new VideoAdministrator(VideoImage, VideoSlider);
        m_splitting =
            new SplittingAdministrator(SplittingList, ButtonCreateSplitting, ButtonDeleteSplitting, m_videoAdmin);
        m_gpxRepresentation = new GpxRepresentation();
        m_gpxVisualizer = new GpxVisualizer(GpxImage, GpxZoomSlider, m_gpxRepresentation);
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
            m_gpxRepresentation.LoadFromFile(filename);
            m_gpxVisualizer.UpdateRepresentation();

            // TODO: Here we set the movie information.
        }
    }
}