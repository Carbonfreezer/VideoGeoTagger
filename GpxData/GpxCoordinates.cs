﻿using System.Globalization;
using System.Windows;
using System.Xml;
using VideoGeoTagger.SegmentSystem;

namespace VideoGeoTagger.GpxData;

/// <summary>
///     Contains a gpx coordinates consisting of latitude, longitude and eventually height.
/// </summary>
public class GpxCoordinates
{
    /// <summary>
    ///     Scaling factor used for tiles.
    /// </summary>
    private static readonly double ScalingFactorTiles = Math.Pow(2.0, GpxVisualizer.ScalingLevel);


    /// <summary>
    ///     The height of the position.
    /// </summary>
    private readonly double m_height;

    /// <summary>
    ///     Constructs the gpx coordinates from the tile coordinates.
    /// </summary>
    /// <param name="tileCoordinates">tile coordinates to generate from.</param>
    public GpxCoordinates(Vector tileCoordinates)
    {
        Longitude = (tileCoordinates.X) / ScalingFactorTiles * 360.0 - 180.0;
        Latitude = Math.Atan(Math.Sinh(Math.PI - ((float)tileCoordinates.Y) / ScalingFactorTiles * 2.0 * Math.PI)) *
                   180.0 /
                   Math.PI;
        m_height = -5000.0;
        TileCoordinates = tileCoordinates;
    }

    /// <summary>
    ///     Constructor with explicit settings of values.
    /// </summary>
    /// <param name="longitude">longitude</param>
    /// <param name="latitude">latitude</param>
    /// <param name="height">optional height</param>
    public GpxCoordinates(double latitude, double longitude, double height = -5000.0)
    {
        Longitude = longitude;
        Latitude = latitude;
        m_height = height;

        double x = (Longitude + 180.0) / 360.0 * ScalingFactorTiles;

        double angleCorrect = Latitude * Math.PI / 180.0;
        double y = (1.0 -
                    (Math.Log(Math.Tan(angleCorrect) + 1.0 / (Math.Cos(angleCorrect))) / Math.PI)) *
                   ScalingFactorTiles * 0.5;

        TileCoordinates = new Vector(x, y);
    }

    /// <summary>
    ///     The latitude of the position.
    /// </summary>
    public double Latitude { get; }

    /// <summary>
    ///     The longitude of the position.
    /// </summary>
    public double Longitude { get; }


    /// <summary>
    ///     Gets the tile coordinates of the position.
    /// </summary>
    public Vector TileCoordinates { get; }


    /// <summary>
    ///     Generates a tracking point xml node of the current data.
    /// </summary>
    /// <param name="doc">The document we can use to generate elements.</param>
    /// <param name="givenTime">The date time that should be inserted into the tracking point.</param>
    /// <returns>The final element with all the data contained.</returns>
    public XmlElement GetTrackingElement(XmlDocument doc, DateTime givenTime)
    {
        XmlElement returnElement = doc.CreateElement("trkpt", SegmentAdministrator.NameSpace);
        returnElement.SetAttribute("lat", Latitude.ToString(CultureInfo.InvariantCulture));
        returnElement.SetAttribute("lon", Longitude.ToString(CultureInfo.InvariantCulture));

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


    /// <summary>
    ///     Gets an interpolated coordinate.
    /// </summary>
    /// <param name="other">Other point to interpolate to.</param>
    /// <param name="alpha">Blend value: 0 = our position 1 = other position.</param>
    /// <returns>Interpolated coordinate.</returns>
    public GpxCoordinates GetInterpolatedValue(GpxCoordinates other, double alpha)
    {
        return new GpxCoordinates(Latitude + alpha * (other.Latitude - Latitude),
            Longitude + alpha * (other.Longitude - Longitude),
            m_height + alpha * (other.m_height - m_height));
    }
}