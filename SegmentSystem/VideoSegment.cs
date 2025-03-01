using System.Diagnostics;

namespace VideoGeoTagger.TimeSpanSystem;

/// <summary>
///     Represents a video segment, with begin and ending and eventually a time span offset to be added to the video time
///     to get the gps time.
/// </summary>
public class VideoSegment
{
    /// <summary>
    ///     The time we want to stay away from an inner border.
    /// </summary>
    private const float BlackoutTime = 2.5f;


    /// <summary>
    ///     The endpoint in video time.
    /// </summary>
    private readonly TimeSpan m_endPoint;

    /// <summary>
    ///     The start point in video time.
    /// </summary>
    private readonly TimeSpan m_startPoint;

    /// <summary>
    ///     The time we have to add to the film time to get the gpx time.
    /// </summary>
    private TimeSpan m_filmToGpxAdder;

    /// <summary>
    ///     Marks the segment as first.
    /// </summary>
    public bool m_isFirst;

    /// <summary>
    ///     Marks the segment as last.
    /// </summary>
    public bool m_isLast;

    /// <summary>
    /// The blackout time we want to have around cuts. 
    /// </summary>
    private readonly TimeSpan m_blackoutTimeSegment;


    /// <summary>
    ///     Creates a video segment from the end and the start point.
    /// </summary>
    /// <param name="startPoint">The start point of the video segment.</param>
    /// <param name="endPoint">The end point of the video segment.</param>
    public VideoSegment(TimeSpan startPoint, TimeSpan endPoint)
    {
        m_startPoint = startPoint;
        m_endPoint = endPoint;
        m_blackoutTimeSegment = TimeSpan.FromSeconds(BlackoutTime);
    }

    /// <summary>
    ///     Indicates
    /// </summary>
    public bool IsSynchronized { get; private set; }


    /// <summary>
    /// Contains the synchronization point of the segment in video time.
    /// </summary>
    public TimeSpan SyncVideoTime { get; private set; }


    /// <summary>
    ///     Sets or resets the synchronization time.
    /// </summary>
    /// <param name="videoTime">The time we have on the video.</param>
    /// <param name="gpxTime">The time we use to query the gpx.</param>
    public void SetSynchronization(TimeSpan videoTime, TimeSpan gpxTime)
    {
        Debug.Assert((videoTime >= m_startPoint) && (videoTime <= m_endPoint), "Not responsible for that video time.");
        m_filmToGpxAdder = gpxTime - videoTime;
        IsSynchronized = true;
        SyncVideoTime = videoTime;
    }


    /// <summary>
    ///     Checks if we are responsible for the video time.
    /// </summary>
    /// <param name="videoTime">The time we want to check.</param>
    /// <returns>If we are responsible for.</returns>
    public bool IsResponsibleVideoTime(TimeSpan videoTime)
    {
        return (videoTime >= m_startPoint) && (videoTime <= m_endPoint);
    }

    /// <summary>
    ///     Simply checks if we are not too close to an inner border.
    /// </summary>
    /// <param name="videoTime"></param>
    /// <returns>Indicates that we are not too close to a border and if we administrate the time.</returns>
    public bool CanGetSave(TimeSpan videoTime)
    {
        bool leftOk = (m_isFirst && (videoTime >= m_startPoint)) || (videoTime >= m_startPoint + m_blackoutTimeSegment);
        bool rightOk = (m_isLast && (videoTime <= m_endPoint)) || (videoTime <= m_endPoint - m_blackoutTimeSegment);
        return leftOk && rightOk;
    }

    /// <summary>
    ///     Gets the corresponding time in the gpx time line.
    /// </summary>
    /// <param name="videoTime">The video time line.</param>
    /// <returns>Corresponding gpx time.</returns>
    public TimeSpan GetGpxTime(TimeSpan videoTime)
    {
        Debug.Assert(IsSynchronized, "We can to check if we are not synchronized");
        Debug.Assert((videoTime >= m_startPoint) && (videoTime <= m_endPoint), "Not responsible for that video time.");

        return videoTime + m_filmToGpxAdder;
    }

    /// <summary>
    ///     Checks if we are responsible for a gpx time.
    /// </summary>
    /// <param name="gpxTime">The gpx time we have.</param>
    /// <returns>True if we are responsible.</returns>
    public bool IsResponsibleGpxTime(TimeSpan gpxTime)
    {
        if (!IsSynchronized)
            return false;

        TimeSpan videoTime = gpxTime - m_filmToGpxAdder;
        return (videoTime >= m_startPoint) && (videoTime <= m_endPoint);
    }

    /// <summary>
    ///     Asks for the video time. and a given gpx time fails, if we are not responsible.
    /// </summary>
    /// <param name="gpxTime">gpx time</param>
    /// <returns>video time</returns>
    public TimeSpan GetVideoTime(TimeSpan gpxTime)
    {
        Debug.Assert(IsSynchronized, "We can to check if we are not synchronized");
        TimeSpan videoTime = gpxTime - m_filmToGpxAdder;
        Debug.Assert((videoTime >= m_startPoint) && (videoTime <= m_endPoint), "Not responsible for that video time.");
        return videoTime;
    }
}