using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Xml.Serialization;
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


    /// <summary>
    ///     The plain gpx filename used for saving.
    /// </summary>
    private string m_plainGpxFilename = "";

    /// <summary>
    ///     The plain video filename used for saving.
    /// </summary>
    private string m_plainVideoFilename = "";

    /// <summary>
    ///     The save struct we have from loading the data.
    /// </summary>
    private ProjectSaveStruct? m_saveStruct;

    public MainWindow()
    {
        InitializeComponent();
        m_videoAdmin = new VideoAdministrator(VideoImage, VideoSlider, TimeBox);
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
            LoadVideo(dialog.FileName);
    }

    /// <summary>
    ///     Gets called when we want to load a video.
    /// </summary>
    /// <param name="fileName">The filename we want to load.</param>
    private void LoadVideo(string fileName)
    {
        m_videoAdmin.LoadVideo(fileName);
        m_splitting.ResetData();
        m_segmentAdministrator.Flush();
        m_plainVideoFilename = Path.GetFileName(fileName);
        if (m_plainGpxFilename != "")
            SaveProjectButton.IsEnabled = true;
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
            LoadGpx(dialog.FileName);
    }


    /// <summary>
    ///     Loads the gpx data from the indicated file.
    /// </summary>
    /// <param name="filename">The name of the file to load gpx from.</param>
    private void LoadGpx(string filename)
    {
        m_gpxRepresentation.LoadFromFile(filename);
        m_gpxVisualizer.UpdateRepresentation();
        m_segmentAdministrator.Flush();
        m_splitting.ResetData();
        m_plainGpxFilename = Path.GetFileName(filename);
        if (m_plainVideoFilename != "")
            SaveProjectButton.IsEnabled = true;
    }


    /// <summary>
    ///     Gets called when the load button got pressed.
    /// </summary>
    private void OnLoadProject(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new OpenFileDialog
        {
            DefaultExt = ".gtp", // Default file extension
            Filter = "Video Geo Tag Project (.gtp)|*.gtp" // Filter files by extension
        };

        // Show open file dialog box
        bool? result = dialog.ShowDialog();

        // Process open file dialog box results
        if (result != true)
            return;

        string fileName = dialog.FileName;
        XmlSerializer serial = new XmlSerializer(typeof(ProjectSaveStruct));
        using TextReader reader = new StreamReader(fileName);
        m_saveStruct = (ProjectSaveStruct?)serial.Deserialize(reader);
        reader.Close();

        if (m_saveStruct == null)
            return;


        // First we deal with loading the files.
        string? baseName = Path.GetDirectoryName(fileName);
        Debug.Assert(baseName != null, "Should be a directory.");
        LoadVideo(Path.Combine(baseName, m_saveStruct.m_videoFilename));
        LoadGpx(Path.Combine(baseName, m_saveStruct.m_gpxFilename));

        // For the rest we have to wait till the video got finished.
        m_videoAdmin.OnVideoReadyForTiming += LoadRestOfData;
    }

    /// <summary>
    ///     Gets invoked after the video is finished.
    /// </summary>
    private void LoadRestOfData(TimeSpan _)
    {
        m_videoAdmin.OnVideoReadyForTiming -= LoadRestOfData;
        if (m_saveStruct == null)
            return;
        // Now set the rest of the data.
        m_splitting.SetSplittingPoints(m_saveStruct.m_splittingPoints);
        m_segmentAdministrator.SetLoadingInfo(m_saveStruct.m_videoSegments);
        m_saveStruct = null;
    }

    /// <summary>
    ///     Gets called when the save button got pressed.
    /// </summary>
    private void OnSaveProject(object sender, RoutedEventArgs e)
    {
        // First prepare the data.
        ProjectSaveStruct saveStruct = new ProjectSaveStruct();
        saveStruct.m_gpxFilename = m_plainGpxFilename;
        saveStruct.m_videoFilename = m_plainVideoFilename;
        saveStruct.m_splittingPoints = m_splitting.SplittingPoints;
        saveStruct.m_videoSegments = m_segmentAdministrator.SafeInfo;

        // Create the save dialog.
        SaveFileDialog dialog = new SaveFileDialog
        {
            FileName = "VideoGeoTag",
            DefaultExt = ".gtp", // Default file extension
            Filter = "Video Geo Tag Project (.gtp)|*.gtp" // Filter files by extension
        };

        // Show open file dialog box
        bool? result = dialog.ShowDialog();

        // Process open file dialog box results
        if (result != true)
            return;

        // Now we serialize the data. 
        XmlSerializer serial = new XmlSerializer(typeof(ProjectSaveStruct));
        using TextWriter writer = new StreamWriter(dialog.FileName);
        serial.Serialize(writer, saveStruct);
        writer.Close();
    }
}