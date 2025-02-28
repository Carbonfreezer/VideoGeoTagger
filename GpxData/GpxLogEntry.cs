using System.Globalization;
using System.Windows;
using System.Xml;
using VideoGeoTagger.SegmentSystem;

namespace VideoGeoTagger.GpxData;

/// <summary>
///     Some data that is contained in a GPS log entry.
/// </summary>
public class GpxLogEntry
{
    /// <summary>
    ///     The latitude of the log.
    /// </summary>
    public float m_latitude;

    /// <summary>
    ///     The longitude of the log.
    /// </summary>
    public float m_longitude;

    /// <summary>
    /// The measurement height if available.
    /// </summary>
    public float m_height;

    /// <summary>
    ///     The time stamp, that was included in the log.
    /// </summary>
    public DateTime m_originalTimeStamp;

    /// <summary>
    ///     The time that has passed since the beginning of the log.
    /// </summary>
    public TimeSpan m_timeFromBeginning;


    /// <summary>
    /// Generates a tracking point xml node of the current data.
    /// </summary>
    /// <param name="doc">The document we can use to generate elements.</param>
    /// <param name="givenTime">The date time that should be inserted into the tracking point.</param>
    /// <returns>The final element with all the data contained.</returns>
    public XmlElement GetTrackingElement(XmlDocument doc, DateTime givenTime)
    {
       
        XmlElement returnElement = doc.CreateElement("trkpt", SegmentAdministrator.NameSpace);
        returnElement.SetAttribute("lat",  m_latitude.ToString(CultureInfo.InvariantCulture));
        returnElement.SetAttribute("lon", m_longitude.ToString(CultureInfo.InvariantCulture));

        XmlElement timeNode = doc.CreateElement("time", SegmentAdministrator.NameSpace);
        timeNode.InnerText = givenTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        returnElement.AppendChild(timeNode);

        if (m_height > -5000.0f)
        {
            XmlElement elevationNode = doc.CreateElement("ele", SegmentAdministrator.NameSpace);
            elevationNode.InnerText = m_height.ToString(CultureInfo.InvariantCulture);
            returnElement.AppendChild(elevationNode);
        }

        return returnElement;
    }
}