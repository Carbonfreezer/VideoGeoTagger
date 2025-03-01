namespace VideoGeoTagger;

/// <summary>
///     This struct contains all the data, to load and save the project from.
/// </summary>
[Serializable]
public class ProjectSaveStruct
{
    /// <summary>
    ///     Contains the gpx filename without the path.
    /// </summary>
    public string m_gpxFilename = "";

    /// <summary>
    ///     Contains the list with the splitting points.
    /// </summary>
    public List<TimeSpan> m_splittingPoints = new List<TimeSpan>();

    /// <summary>
    ///     Contains the vide filename without the path.
    /// </summary> 
    public string m_videoFilename = "";


    /// <summary>
    ///     Contains the list per video segment, if the synchronization has been performed.
    /// </summary>
    public List<VideoSegmentInfo> m_videoSegments = new List<VideoSegmentInfo>();
}

/// <summary>
///     Contains the information per video segment, that has to be saved.
/// </summary>
[Serializable]
public class VideoSegmentInfo
{
    /// <summary>
    ///     The correct ion offset we need to get from video to gpx time.
    /// </summary>
    public TimeSpan m_correctionOffset;

    /// <summary>
    ///     Is the segment synchronized.
    /// </summary>
    public bool m_isSynchronized;

    /// <summary>
    ///     Where in time is the synchronization time point.
    /// </summary>
    public TimeSpan m_synchronizationTime;
}