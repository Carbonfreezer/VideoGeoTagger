using System.Diagnostics;

namespace VideoGeoTagger.TimeSpanSystem;

/// <summary>
///     Represents a video segment, with begin and ending and eventually a time span offset to be added to the video time
///     to get the gps time.
/// </summary>
public class VideoSegment
{
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
    ///     Creates a video segment from the end and the start point.
    /// </summary>
    /// <param name="startPoint">The start point of the video segment.</param>
    /// <param name="endPoint">The end point of the video segment.</param>
    public VideoSegment(TimeSpan startPoint, TimeSpan endPoint)
    {
        m_startPoint = startPoint;
        m_endPoint = endPoint;
    }

    /// <summary>
    ///     Indicates
    /// </summary>
    public bool IsSynchronized { get; private set; }


    /// <summary>
    ///     Gets the time midpoint of the video segment.
    /// </summary>
    public TimeSpan MidPoint => (m_startPoint + m_endPoint) * 0.5;


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
    /// Checks if we are responsible for a gpx time.
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
    /// Asks for the video time. and a given gpx time fails, if we are not responsible.
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