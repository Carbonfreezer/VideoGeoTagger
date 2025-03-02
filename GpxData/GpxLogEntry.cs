namespace VideoGeoTagger.GpxData;

/// <summary>
///     Some data that is contained in a GPS log entry.
/// </summary>
public class GpxLogEntry
{
    /// <summary>
    ///     The position we are in.
    /// </summary>
    public GpxCoordinates m_coordinates = new GpxCoordinates(0.0, 0.0);

    /// <summary>
    ///     The time stamp, that was included in the log.
    /// </summary>
    public DateTime m_originalTimeStamp;

    /// <summary>
    ///     The time that has passed since the beginning of the log.
    /// </summary>
    public TimeSpan m_timeFromBeginning;
}