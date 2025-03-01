using System.Windows;
using Microsoft.Win32;
using VideoGeoTagger.GpxData;
using VideoGeoTagger.SegmentSystem;

namespace VideoGeoTagger;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    ///     Contains the gps track.
    /// </summary>
    private readonly GpxRepresentation m_gpxRepresentation;

    /// <summary>
    ///     Visualization module for the gpx track.
    /// </summary>
    private readonly GpxVisualizer m_gpxVisualizer;

    /// <summary>
    ///     Administrates the video segments and does the bul of the work.
    /// </summary>
    private readonly SegmentAdministrator m_segmentAdministrator;

    /// <summary>
    ///     System administrating the splitting points.
    /// </summary>
    private readonly SplittingAdministrator m_splitting;

    /// <summary>
    ///     Module to load and display videos.
    /// </summary>
    private readonly VideoAdministrator m_videoAdmin;

    public MainWindow()
    {
        InitializeComponent();
        m_videoAdmin = new VideoAdministrator(VideoImage, VideoSlider);
        m_splitting =
            new SplittingAdministrator(SplittingList, ButtonCreateSplitting, ButtonDeleteSplitting, m_videoAdmin);
        m_gpxRepresentation = new GpxRepresentation();
        m_gpxVisualizer = new GpxVisualizer(GpxImage, GpxZoomSlider, m_gpxRepresentation);
        m_segmentAdministrator = new SegmentAdministrator(SegmentList, Synchronize, SaveButton, m_videoAdmin,
            m_splitting, m_gpxVisualizer, m_gpxRepresentation);
    }


    /// <summary>
    ///     Gets called when we press the load movie button.
    /// </summary>
    private void OnLoadVideo(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new OpenFileDialog
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
        }
    }

    /// <summary>
    ///     Gets called when we load the gpx information.
    /// </summary>
    private void OnLoadGpx(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new OpenFileDialog
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
            m_segmentAdministrator.Flush();
        }
    }
}