using System.Diagnostics;
using System.Windows.Controls;
using VideoGeoTagger.GpxData;
using VideoGeoTagger.TimeSpanSystem;

namespace VideoGeoTagger.SegmentSystem;

/// <summary>
///     A container for all the segments.
/// </summary>
public class SegmentAdministrator
{


    /// <summary>
    ///     The video segments we have.
    /// </summary>
    private readonly List<VideoSegment> m_listOfSegments = new List<VideoSegment>();


    /// <summary>
    ///     The gui element for the segments.
    /// </summary>
    private readonly ListBox m_segmentListBox;

    /// <summary>
    ///     Is used to suppress the callback, if we set our slider itself.
    /// </summary>
    private bool m_suppressCallback;

    /// <summary>
    ///     The total video time we have once it is known.
    /// </summary>
    private TimeSpan m_totalVideoTime;

    /// <summary>
    /// The video administration system.
    /// </summary>
    private readonly VideoAdministrator m_videoAdmin;

    /// <summary>
    /// The gpx visualization system.
    /// </summary>
    private readonly GpxVisualizer m_gpxVisualizer;


    /// <summary>
    /// Checkbox if we want synchronization.
    /// </summary>
    private readonly CheckBox m_synchronizeBox;

    /// <summary>
    ///     We administrate all segments.
    /// </summary>
    /// <param name="segmentListBox">The gui element to show the segments.</param>
    /// <param name="synchronize"></param>
    /// <param name="videoAdmin"></param>
    /// <param name="splitting"></param>
    /// <param name="visualizer"></param>
    public SegmentAdministrator(ListBox segmentListBox, CheckBox synchronize, VideoAdministrator videoAdmin,
        SplittingAdministrator splitting, GpxVisualizer visualizer)
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
    }

    /// <summary>
    /// Gets called, when the user has picked onto the map to get the closest gpx time.
    /// </summary>
    /// <param name="gpxTime">The time in gpx system selected.</param>
    private void MapTimeSelected(TimeSpan gpxTime)
    {
        if (m_synchronizeBox.IsChecked == true)
        {
            VideoSegment relevantSegment = m_listOfSegments[m_segmentListBox.SelectedIndex];
            relevantSegment.SetSynchronization(m_videoAdmin.VideoPosition, gpxTime);
            m_segmentListBox.Items[m_segmentListBox.SelectedIndex] =
                $"Segment {m_segmentListBox.SelectedIndex + 1:D2} - Synced";
            UpdateMarker();
            m_synchronizeBox.IsChecked = false;
        }
        else
        {
            VideoSegment? candidate = m_listOfSegments.Find(seg => seg.IsResponsibleGpxTime(gpxTime));
            if (candidate != null)
                m_videoAdmin.VideoPosition = candidate.GetVideoTime(gpxTime);
        }
    }



    /// <summary>
    /// Sets or removes the marker at the relevant position.
    /// </summary>
    private void UpdateMarker()
    {
        VideoSegment? relevantSegment = m_listOfSegments.Find(seg => seg.IsResponsibleVideoTime(m_videoAdmin.VideoPosition));
        Debug.Assert(relevantSegment != null, "Should not happen");
        if (!relevantSegment.IsSynchronized)
        {
            m_gpxVisualizer.DisableMarker();
            return;
        }

        TimeSpan gpxTime = relevantSegment.GetGpxTime(m_videoAdmin.VideoPosition);
        m_gpxVisualizer.SetMarker(gpxTime);
    }


    /// <summary>
    /// Checks if all segments are tagged.
    /// </summary>
    public bool AllTagged => m_listOfSegments.All(seg => seg.IsSynchronized);

    /// <summary>
    /// Gets called when we have new splitting points.
    /// </summary>
    /// <param name="splittingPoints">List of splitting points.</param>
    private void SplittingListChanged(List<TimeSpan> splittingPoints)
    {
        m_listOfSegments.Clear();
        TimeSpan firstPoint = TimeSpan.Zero;
        ;
        foreach (TimeSpan split in splittingPoints)
        {
            m_listOfSegments.Add(new VideoSegment(firstPoint, split));
            firstPoint = split;
        }

        m_listOfSegments.Add(new VideoSegment(firstPoint, m_totalVideoTime));

        PopulateGui();
    }


    /// <summary>
    /// Sets the index, when the video position has reached the desired state.
    /// </summary>
    /// <param name="videoPosition">Position of the video.</param>
    private void VideoAdminOnOnVideoPositionChanged(TimeSpan videoPosition)
    {
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
            m_segmentListBox.Items.Add($"Segment {i + 1:D2}");

        UpdateMarker();
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


        m_videoAdmin.VideoPosition = m_listOfSegments[index].MidPoint;
    }
}