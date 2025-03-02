using System.Diagnostics;
using System.Globalization;
using System.Windows;
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
    ///     Asks for the bounding rectangle of the course in tile coordinates.
    /// </summary>
    public (Vector minPoint, Vector maxPoint) BoundingRectangle
    {
        get
        {
            Vector minPoint = new Vector(
                m_originalNodes.Min(node => node.m_coordinates.TileCoordinates.X),
                m_originalNodes.Min(node => node.m_coordinates.TileCoordinates.Y)
            );

            Vector maxPoint = new Vector(
                m_originalNodes.Max(node => node.m_coordinates.TileCoordinates.X),
                m_originalNodes.Max(node => node.m_coordinates.TileCoordinates.Y)
            );


            return (minPoint, maxPoint);
        }
    }


    /// <summary>
    ///     Gets an enumerable for latitude an longitude points for painting.
    /// </summary>
    public IEnumerable<GpxCoordinates> CoordinatePoints => m_originalNodes.Select(entry => entry.m_coordinates);

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

            XmlNodeList nodeList = point.GetElementsByTagName("ele");
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
                m_coordinates = new GpxCoordinates(latitude, longitude, height),
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
    ///     Gets an element as a tracking point.
    /// </summary>
    /// <param name="doc">The document we use to create xml elements.</param>
    /// <param name="gpxTime">The time we search for in the original gpx data.</param>
    /// <param name="videoTime">The time we have on the video track.</param>
    /// <returns>Readily computed Xml element.</returns>
    public XmlElement GetTrackingPointElement(XmlDocument doc, TimeSpan gpxTime, TimeSpan videoTime)
    {
        // We get the first entry where we are in front of.
        int foundIndex = m_originalNodes.FindIndex(log => log.m_timeFromBeginning > gpxTime);
        if (foundIndex == -1)
            // In this case we take the last element.
            return m_originalNodes[^1].m_coordinates.GetTrackingElement(doc, m_virtualStartTime + videoTime);

        if (foundIndex == 0)
            // In this case we are already slightly ahead.
            return m_originalNodes[0].m_coordinates.GetTrackingElement(doc, m_virtualStartTime + videoTime);

        // In this case we have to interpolate.
        TimeSpan baseTime = m_originalNodes[foundIndex - 1].m_timeFromBeginning;
        TimeSpan delta = m_originalNodes[foundIndex].m_timeFromBeginning - baseTime;

        double alpha = (gpxTime - baseTime) / delta;
        GpxCoordinates interpolatedCoords = m_originalNodes[foundIndex - 1].m_coordinates
            .GetInterpolatedValue(m_originalNodes[foundIndex].m_coordinates, alpha);

        return interpolatedCoords.GetTrackingElement(doc, m_virtualStartTime + videoTime);
    }


    /// <summary>
    ///     Asks for a given latitude and longitude the closest point we have selected. Returns gpx time.
    /// </summary>
    /// <returns>Best time, when the position has been reached.</returns>
    public TimeSpan GetClosestTime(GpxCoordinates coordinates)
    {
        Vector tileCoordProbing = coordinates.TileCoordinates;
        GpxLogEntry? bestEntry = m_originalNodes.MinBy(logEntry =>
            (tileCoordProbing - logEntry.m_coordinates.TileCoordinates).LengthSquared);

        Debug.Assert(bestEntry != null, "No Entry found.");
        return bestEntry.m_timeFromBeginning;
    }

    /// <summary>
    ///     Samples the log for a given time and returns the position closest  time.
    /// </summary>
    /// <param name="fromStart">Time from start on where we want to sample.</param>
    /// <returns>Position, that was obtained from the time stamp.</returns>
    public GpxCoordinates GetPositionForTimeStamp(TimeSpan fromStart)
    {
        GpxLogEntry? bestEntry = m_originalNodes.MinBy(logEntry =>
            Math.Abs(logEntry.m_timeFromBeginning.TotalSeconds - fromStart.TotalSeconds));
        Debug.Assert(bestEntry != null, "No Entry found.");

        return bestEntry.m_coordinates;
    }
}