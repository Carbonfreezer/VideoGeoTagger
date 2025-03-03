using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using Microsoft.Win32;
using VideoGeoTagger.GpxData;
using VideoGeoTagger.TimeSpanSystem;

namespace VideoGeoTagger.SegmentSystem;

/// <summary>
///     A container for all the segments.
/// </summary>
public class SegmentAdministrator
{
    /// <summary>
    ///     The name space we use for the xml encoding.
    /// </summary>
    public const string NameSpace = @"http://www.topografix.com/GPX/1/1";


    /// <summary>
    ///     Needed to generate the final gpx file.
    /// </summary>
    private readonly GpxRepresentation m_gpxAdministration;

    /// <summary>
    ///     The gpx visualization system.
    /// </summary>
    private readonly GpxVisualizer m_gpxVisualizer;


    /// <summary>
    ///     The video segments we have.
    /// </summary>
    private readonly List<VideoSegment> m_listOfSegments = new List<VideoSegment>();

    /// <summary>
    ///     The button for the save routine.
    /// </summary>
    private readonly Button m_saveButton;


    /// <summary>
    ///     The gui element for the segments.
    /// </summary>
    private readonly ListBox m_segmentListBox;


    /// <summary>
    ///     Checkbox if we want synchronization.
    /// </summary>
    private readonly CheckBox m_synchronizeBox;

    /// <summary>
    ///     The video administration system.
    /// </summary>
    private readonly VideoAdministrator m_videoAdmin;

    /// <summary>
    ///     Is used to suppress the callback, if we set our slider itself.
    /// </summary>
    private bool m_suppressCallback;

    /// <summary>
    ///     The total video time we have once it is known.
    /// </summary>
    private TimeSpan m_totalVideoTime;

    /// <summary>
    ///     We administrate all segments.
    /// </summary>
    /// <param name="segmentListBox">The gui element to show the segments.</param>
    /// <param name="synchronize"></param>
    /// <param name="saveButton"></param>
    /// <param name="videoAdmin"></param>
    /// <param name="splitting"></param>
    /// <param name="visualizer"></param>
    /// <param name="gpxRepresentation"></param>
    public SegmentAdministrator(ListBox segmentListBox, CheckBox synchronize, Button saveButton,
        VideoAdministrator videoAdmin,
        SplittingAdministrator splitting, GpxVisualizer visualizer, GpxRepresentation gpxRepresentation)
    {
        m_segmentListBox = segmentListBox;
        m_gpxVisualizer = visualizer;
        m_videoAdmin = videoAdmin;
        m_segmentListBox.SelectionChanged += ItemSelected;
        m_videoAdmin.OnVideoPositionChanged += VideoAdminOnOnVideoPositionChanged;
        m_videoAdmin.OnVideoReadyForTiming += InformNewVideo;
        splitting.OnSplittingPointsChanged += SplittingListChanged;
        m_gpxVisualizer.OnMapTimeSelected += MapTimeSelected;
        m_synchronizeBox = synchronize;
        m_saveButton = saveButton;
        m_saveButton.Click += SaveButtonOnClick;
        m_gpxAdministration = gpxRepresentation;
    }


    /// <summary>
    ///     Gets the save information from the segment administrator.
    /// </summary>
    public List<VideoSegmentInfo> SafeInfo => m_listOfSegments.Select(seg => seg.SaveInfo).ToList();


    /// <summary>
    ///     Sets the list with the segment information from a project load.
    /// </summary>
    /// <param name="infoList">The list with the segment informations.</param>
    public void SetLoadingInfo(List<VideoSegmentInfo> infoList)
    {
        Debug.Assert(infoList.Count == m_listOfSegments.Count, "The set list should be as long as the segment list.");
        for (int i = 0; i < m_listOfSegments.Count; ++i)
            m_listOfSegments[i].SetLoadingInfo(infoList[i]);

        PopulateGui();
    }

    /// <summary>
    ///     Callback to save the processed GPX file.
    /// </summary>
    private void SaveButtonOnClick(object sender, RoutedEventArgs e)
    {
        SaveFileDialog dialog = new SaveFileDialog
        {
            FileName = "Processed",
            DefaultExt = ".gpx", // Default file extension
            Filter = "GPS Data (.gpx)|*.gpx" // Filter files by extension
        };

        // Show open file dialog box
        bool? diagRes = dialog.ShowDialog();

        if (diagRes != true)
            return;


        m_listOfSegments[0].m_isFirst = true;
        m_listOfSegments[^1].m_isLast = true;

        TimeSpan oneSecond = TimeSpan.FromSeconds(1.0f);
        TimeSpan scanning = TimeSpan.Zero;

        // Generate the result.
        XmlDocument document = new XmlDocument();
        XmlElement baseElement = document.CreateElement("gpx", NameSpace);
        baseElement.SetAttribute("version", "1.1");
        document.AppendChild(baseElement);
        XmlElement trk = document.CreateElement("trk", NameSpace);
        baseElement.AppendChild(trk);
        XmlElement trkSeg = document.CreateElement("trkseg", NameSpace);
        trk.AppendChild(trkSeg);

        TimeSpan gpxTime;
        XmlElement trkPoint;
        VideoSegment? relevantSegment;
        while (scanning < m_totalVideoTime)
        {
            relevantSegment = m_listOfSegments.Find(seg => seg.CanGetSave(scanning));
            if (relevantSegment != null)
            {
                gpxTime = relevantSegment.GetGpxTime(scanning);
                trkPoint = m_gpxAdministration.GetTrackingPointElement(document, gpxTime, scanning);
                trkSeg.AppendChild(trkPoint);
            }

            scanning += oneSecond;
        }

        // Get the final point.
        relevantSegment = m_listOfSegments.Find(seg => seg.IsResponsibleVideoTime(m_totalVideoTime));

        Debug.Assert(relevantSegment != null, "Should not happen");
        gpxTime = relevantSegment.GetGpxTime(m_totalVideoTime);
        trkPoint = m_gpxAdministration.GetTrackingPointElement(document, gpxTime, m_totalVideoTime);
        trkSeg.AppendChild(trkPoint);

        using (XmlWriter writer = XmlWriter.Create(dialog.FileName))
        {
            document.WriteTo(writer);
        }
    }


    /// <summary>
    ///     Gets called, when the user has picked onto the map to get the closest gpx time.
    /// </summary>
    /// <param name="gpxTime">The time in gpx system selected.</param>
    private void MapTimeSelected(TimeSpan gpxTime)
    {
        if (m_synchronizeBox.IsChecked == true)
        {
            int selectedPosition = m_segmentListBox.SelectedIndex;
            VideoSegment relevantSegment = m_listOfSegments[selectedPosition];
            relevantSegment.SetSynchronization(m_videoAdmin.VideoPosition, gpxTime);
            m_segmentListBox.Items[m_segmentListBox.SelectedIndex] =
                $"Segment {m_segmentListBox.SelectedIndex + 1:D2} - Synced";
            UpdateMarker();
            m_synchronizeBox.IsChecked = false;
            m_segmentListBox.SelectedIndex = selectedPosition;

            m_saveButton.IsEnabled = m_listOfSegments.All(seg => seg.IsSynchronized);
        }
        else
        {
            VideoSegment? candidate = m_listOfSegments.Find(seg => seg.IsResponsibleGpxTime(gpxTime));
            if (candidate != null)
                m_videoAdmin.VideoPosition = candidate.GetVideoTime(gpxTime);
        }
    }


    /// <summary>
    ///     Sets or removes the marker at the relevant position.
    /// </summary>
    private void UpdateMarker()
    {
        TimeSpan videoTime = m_videoAdmin.VideoPosition;
        VideoSegment? relevantSegment = m_listOfSegments.Find(seg => seg.IsResponsibleVideoTime(videoTime));
        Debug.Assert(relevantSegment != null, "Should not happen");
        if (!relevantSegment.IsSynchronized)
        {
            m_gpxVisualizer.DisableMarker();
            return;
        }

        TimeSpan gpxTime = relevantSegment.GetGpxTime(videoTime);
        m_gpxVisualizer.SetMarker(gpxTime);
    }

    /// <summary>
    ///     Gets called when we have new splitting points.
    /// </summary>
    /// <param name="splittingPoints">List of splitting points.</param>
    private void SplittingListChanged(List<TimeSpan> splittingPoints)
    {
        m_listOfSegments.Clear();
        TimeSpan firstPoint = TimeSpan.Zero;
        foreach (TimeSpan split in splittingPoints)
        {
            m_listOfSegments.Add(new VideoSegment(firstPoint, split));
            firstPoint = split;
        }

        m_listOfSegments.Add(new VideoSegment(firstPoint, m_totalVideoTime));
        PopulateGui();
    }


    /// <summary>
    ///     Sets the index, when the video position has reached the desired state.
    /// </summary>
    /// <param name="videoPosition">Position of the video.</param>
    private void VideoAdminOnOnVideoPositionChanged(TimeSpan videoPosition)
    {
        if (m_suppressCallback)
            return;

        m_suppressCallback = true;
        int index = m_listOfSegments.FindIndex(seg => seg.IsResponsibleVideoTime(videoPosition));
        if (index != -1)
            m_segmentListBox.SelectedIndex = index;

        m_suppressCallback = false;
        UpdateMarker();
    }

    /// <summary>
    ///     Sets a new video and generates a new list of segments.
    /// </summary>
    /// <param name="videoDuration">How long does the video take.</param>
    private void InformNewVideo(TimeSpan videoDuration)
    {
        m_totalVideoTime = videoDuration;
        m_listOfSegments.Clear();
        m_listOfSegments.Add(new VideoSegment(TimeSpan.Zero, videoDuration));
        PopulateGui();
    }

    /// <summary>
    ///     We simply generate elements for the GUI list.
    /// </summary>
    private void PopulateGui()
    {
        m_segmentListBox.Items.Clear();
        for (int i = 0; i < m_listOfSegments.Count; ++i)
            m_segmentListBox.Items.Add(m_listOfSegments[i].IsSynchronized
                ? $"Segment {i + 1:D2} - Synced"
                : $"Segment {i + 1:D2}");

        UpdateMarker();
        m_saveButton.IsEnabled = m_listOfSegments.All(seg => seg.IsSynchronized);
        m_suppressCallback = true;
        m_segmentListBox.SelectedIndex = 0;
        m_suppressCallback = false;
    }


    /// <summary>
    ///     When a segment got selected we find it.
    /// </summary>
    private void ItemSelected(object sender, SelectionChangedEventArgs e)
    {
        if (m_suppressCallback)
            return;

        int index = m_segmentListBox.SelectedIndex;
        if (index == -1)
            return;


        m_suppressCallback = true;
        if (m_listOfSegments[index].IsSynchronized)
            m_videoAdmin.VideoPosition = m_listOfSegments[index].SyncVideoTime;
        else
            m_videoAdmin.VideoPosition = index == 0 ? m_listOfSegments[0].StartingPoint : m_listOfSegments[index - 1].EndingPoint - TimeSpan.FromSeconds(0.2);
        UpdateMarker();
        m_suppressCallback = false;
    }


    /// <summary>
    ///     Flushes the segment list gets called when a new gpx is loaded.
    /// </summary>
    public void Flush()
    {
        m_listOfSegments.Clear();
        m_listOfSegments.Add(new VideoSegment(TimeSpan.Zero, m_totalVideoTime));
        PopulateGui();
    }
}