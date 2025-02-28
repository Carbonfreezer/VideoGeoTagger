using System.Diagnostics;
using System.Globalization;
using System.Xml;

namespace VideoGeoTagger.GpxData;

/// <summary>
///     Gets a representation of the Gpc data as loaded from file.
/// </summary>
public class GpxRepresentation
{
    /// <summary>
    ///     The logging points we have.
    /// </summary>
    private readonly List<GpxLogEntry> m_originalNodes = new List<GpxLogEntry>();

    /// <summary>
    ///     Generates a virtual start time for the video.
    /// </summary>
    private DateTime m_virtualStartTime;

    /// <summary>
    ///     Asks for the total length of the gpx track.
    /// </summary>
    public TimeSpan TotalLength => m_originalNodes[^1].m_timeFromBeginning;

    /// <summary>
    ///     Asks for the bounding rectangle of the course.
    /// </summary>
    public (float minLatitude, float maxLatitude, float minLongitude, float maxLongitude) BoundingRectangle
    {
        get
        {
            float minLatitude = m_originalNodes.Min(node => node.m_latitude);
            float maxLatitude = m_originalNodes.Max(node => node.m_latitude);
            float minLongitude = m_originalNodes.Min(node => node.m_longitude);
            float maxLongitude = m_originalNodes.Max(node => node.m_longitude);

            return (minLatitude, maxLatitude, minLongitude, maxLongitude);
        }
    }


    /// <summary>
    /// Gets an enumerable for latitude an longitude points for painting.
    /// </summary>
    public IEnumerable<(float latitude, float longitude)> CoordinatePoints
    {
        get { return m_originalNodes.Select(entry => (entry.m_latitude, entry.m_longitude)); }
    }

    /// <summary>
    ///     Loads the data from the file and builds the internal list.
    /// </summary>
    /// <param name="fileName">The file to read the GPX data from.</param>
    public void LoadFromFile(string fileName)
    {
        m_originalNodes.Clear();

        XmlDocument inDoc = new XmlDocument();
        using (XmlReader reader = XmlReader.Create(fileName))
        {
            inDoc.Load(reader);
        }

        foreach (XmlElement point in inDoc.GetElementsByTagName("trkpt"))
        {
            XmlAttribute? latAttribute = point.Attributes["lat"];
            XmlAttribute? lonAttribute = point.Attributes["lon"];

            Debug.Assert((latAttribute != null) && (lonAttribute != null), "Attributes for position missing.");


            float latitude = float.Parse(latAttribute.InnerText, CultureInfo.InvariantCulture);
            float longitude = float.Parse(lonAttribute.InnerText, CultureInfo.InvariantCulture);

            XmlNode? timeNode = point.GetElementsByTagName("time")[0];
            Debug.Assert(timeNode != null, "No time found");
            string timeText = timeNode.InnerText;

            XmlNodeList nodeList  = point.GetElementsByTagName("ele");
            float height = -10000.0f;
            if (nodeList.Count == 1)
            {
                XmlNode? heightNode = nodeList[0];
                if (heightNode != null)
                    height = float.Parse(heightNode.InnerText, CultureInfo.InvariantCulture);
            }

            DateTime timeStamp = DateTime.Parse(timeText, CultureInfo.InvariantCulture);

            GpxLogEntry newLog = new GpxLogEntry
            {
                m_latitude = latitude,
                m_longitude = longitude,
                m_height = height,
                m_originalTimeStamp = timeStamp
            };

            m_originalNodes.Add(newLog);
        }

        // Add the start time.
        DateTime startTime = m_originalNodes[0].m_originalTimeStamp;
        foreach (GpxLogEntry entry in m_originalNodes)
            entry.m_timeFromBeginning = entry.m_originalTimeStamp - startTime;

        m_virtualStartTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, 6, 0, 0);
    }


    /// <summary>
    /// Gets an element as a tracking point.
    /// </summary>
    /// <param name="doc">The document we use to create xml elements.</param>
    /// <param name="gpxTime">The time we search for in the original gpx data.</param>
    /// <param name="videoTime">The time we have on the video track.</param>
    /// <returns>Readily computed Xml element.</returns>
    public XmlElement GetTrackingPointElement(XmlDocument doc, TimeSpan gpxTime, TimeSpan videoTime)
    {
        // First we need to get the entry.
        GpxLogEntry? bestEntry = m_originalNodes.MinBy(logEntry => (logEntry.m_timeFromBeginning - gpxTime).Seconds);
        Debug.Assert(bestEntry != null, "No Entry found.");

        DateTime finalTime = m_virtualStartTime + videoTime;
        return bestEntry.GetTrackingElement(doc, finalTime);
    }

    /// <summary>
    ///     Asks for a given latitude and longitude the closest point we have selected.
    /// </summary>
    /// <param name="latitude">Probing latitude.</param>
    /// <param name="longitude">Probing longitude.</param>
    /// <returns>Best time, when the position has been reached.</returns>
    public TimeSpan GetClosestTime(float latitude, float longitude)
    {
        GpxLogEntry? bestEntry = m_originalNodes.MinBy(logEntry =>
            MathF.Pow(latitude - logEntry.m_latitude, 2.0f) + MathF.Pow(longitude - logEntry.m_longitude, 2.0f));

        Debug.Assert(bestEntry != null, "No Entry found.");
        return bestEntry.m_timeFromBeginning;
    }

    /// <summary>
    ///     Samples the log for a given time and returns the position.
    /// </summary>
    /// <param name="fromStart">Time from start on where we want to sample.</param>
    /// <returns>Position, that was obtained from the time stamp.</returns>
    public (float latitude, float longitude) GetPositionForTimeStamp(TimeSpan fromStart)
    {
        GpxLogEntry? bestEntry = m_originalNodes.MinBy(logEntry => (logEntry.m_timeFromBeginning - fromStart).Seconds);
        Debug.Assert(bestEntry != null, "No Entry found.");

        return (bestEntry.m_latitude, bestEntry.m_longitude);
    }
}