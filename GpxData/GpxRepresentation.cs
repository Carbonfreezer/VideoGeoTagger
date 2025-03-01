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
    ///     Asks for the bounding rectangle of the course.
    /// </summary>
    public (double minLatitude, double maxLatitude, double minLongitude, double maxLongitude) BoundingRectangle
    {
        get
        {
            double minLatitude = m_originalNodes.Min(node => node.m_coordinates.m_latitude);
            double maxLatitude = m_originalNodes.Max(node => node.m_coordinates.m_latitude);
            double minLongitude = m_originalNodes.Min(node => node.m_coordinates.m_longitude);
            double maxLongitude = m_originalNodes.Max(node => node.m_coordinates.m_longitude);

            return (minLatitude, maxLatitude, minLongitude, maxLongitude);
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
        // First we need to get the entry.
        GpxLogEntry? bestEntry = m_originalNodes.MinBy(logEntry =>
            Math.Abs(logEntry.m_timeFromBeginning.TotalSeconds - gpxTime.TotalSeconds));
        Debug.Assert(bestEntry != null, "No Entry found.");

        DateTime finalTime = m_virtualStartTime + videoTime;
        return bestEntry.GetTrackingElement(doc, finalTime);
    }


    /// <summary>
    ///     Gets the angular distance on longitude. Takes care of the wrap around.
    /// </summary>
    /// <param name="angleA">First angle.</param>
    /// <param name="angleB">Second angle.</param>
    /// <returns>Angular distance.</returns>
    private double GetLongitudeDistance(double angleA, double angleB)
    {
        double diff = angleA - angleB;

        if (diff > 180.0)
            diff = 360.0 - diff;

        if (diff < -180.0)
            diff = 380.0 + diff;

        return diff;
    }



    // TODO: Has to change in further processing.

    /// <summary>
    ///     Asks for a given latitude and longitude the closest point we have selected. Returns gpx time.
    /// </summary>
    /// <returns>Best time, when the position has been reached.</returns>
    public TimeSpan GetClosestTime(GpxCoordinates coordinates)
    {
        

        GpxLogEntry? bestEntry = m_originalNodes.MinBy(logEntry =>
            Math.Pow(coordinates.m_latitude - logEntry.m_coordinates.m_latitude, 2.0) +
            Math.Pow(GetLongitudeDistance(coordinates.m_longitude, logEntry.m_coordinates.m_longitude), 2.0));

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