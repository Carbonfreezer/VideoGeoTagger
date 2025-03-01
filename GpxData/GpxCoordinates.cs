using System.Windows;

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
    public double m_height;

    /// <summary>
    ///     The latitude of the position.
    /// </summary>
    public double m_latitude;

    /// <summary>
    ///     The longitude of the position.
    /// </summary>
    public double m_longitude;

    /// <summary>
    ///     Constructs the gpx coordinates from the tile coordinates.
    /// </summary>
    /// <param name="tileCoordinates">tile coordinates to generate from.</param>
    public GpxCoordinates(Vector tileCoordinates)
    {
        m_longitude = (tileCoordinates.X) / ScalingFactorTiles * 360.0 - 180.0;
        m_latitude = Math.Atan(Math.Sinh(Math.PI - ((float)tileCoordinates.Y) / ScalingFactorTiles * 2.0 * Math.PI)) *
                     180.0 /
                     Math.PI;
        m_height = -5000.0;
    }

    /// <summary>
    /// Constructor with explicit settings of values.
    /// </summary>
    /// <param name="longitude">longitude</param>
    /// <param name="latitude">latitude</param>
    /// <param name="height">optional height</param>
    public GpxCoordinates(double latitude, double longitude, double height = -5000.0)
    {
        m_longitude = longitude;
        m_latitude = latitude;
        m_height = height;
    }


    /// <summary>
    ///     Gets the tile coordinates for the gpx ones.
    /// </summary>
    public Vector TileCoordinates
    {
        get
        {
            double x = (m_longitude + 180.0) / 360.0 * ScalingFactorTiles;

            double angleCorrect = m_latitude * Math.PI / 180.0;
            double y = (1.0 -
                        (Math.Log(Math.Tan(angleCorrect) + 1.0 / (Math.Cos(angleCorrect))) / Math.PI)) *
                       ScalingFactorTiles * 0.5;

            return new Vector(x, y);
        }
    }
}