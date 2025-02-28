using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VideoGeoTagger.GpxData;

/// <summary>
///     This class does all the visual representation of the gpx files.
/// </summary>
public class GpxVisualizer
{
    /// <summary>
    ///     The scaling level we use for the geo map tile system.
    /// </summary>
    private const int ScalingLevel = 15;

    /// <summary>
    ///     The size of a single tile in pixel.
    /// </summary>
    private const int TileSize = 256;

    /// <summary>
    ///     The tile provider we use to query the tiles.
    /// </summary>
    private const string TileProvider = "https://tile.openstreetmap.org";


    /// <summary>
    ///     The base drawing group that contains everything.
    /// </summary>
    private readonly DrawingGroup m_baseGroup;

    /// <summary>
    ///     The gpx representation for the track.
    /// </summary>
    private readonly GpxRepresentation m_gpxRepresentation;


    /// <summary>
    ///     The scaling factor that hans on the scaling level for tiles.
    /// </summary>
    private readonly float m_scalingFactorTiles;

    /// <summary>
    ///     Indicates, that the gpx data is present.
    /// </summary>
    private bool m_gpxSet;

    /// <summary>
    /// The size of the display gadget.
    /// </summary>
    private readonly Size m_gadgetSize;


    /// <summary>
    /// The center of the represented region in tile coordinates.
    /// </summary>
    private Point m_originTileSystem;

   

    /// <summary>
    /// The scaling factor we have internally for map drawing.
    /// </summary>
    private float m_scalingFactorMapDrawing;


    /// <summary>
    /// Generates the gpx visualization module, that does all the internal handling.
    /// </summary>
    /// <param name="controlImage">The image control we draw the map, path and marker in.</param>
    /// <param name="gpxZoomSlider">The slider we use for zooming.</param>
    /// <param name="representation">The gpx representation to extract the information from.</param>
    public GpxVisualizer(Image controlImage, Slider gpxZoomSlider, GpxRepresentation representation)
    {
        m_scalingFactorTiles = MathF.Pow(2.0f, ScalingLevel);
        m_gpxSet = false;
        m_gpxRepresentation = representation;
        m_baseGroup = new DrawingGroup();
        controlImage.Source = new DrawingImage(m_baseGroup);
        m_gadgetSize = new Size(controlImage.Width, controlImage.Height);
        m_scalingFactorMapDrawing = 1.0f;
        gpxZoomSlider.Value = 100.0f;
        gpxZoomSlider.ValueChanged += GpxZoomSliderOnValueChanged;
    }


    /// <summary>
    /// Gets called, when the zoom slider has been moved.
    /// </summary>
    private void GpxZoomSliderOnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!m_gpxSet)
            return;

        m_scalingFactorMapDrawing =  (float)((Slider)sender).Value / 100.0f;
        UpdateTransformation();
    }


 
    /// <summary>
    /// Updates the transformation.
    /// </summary>
    private void UpdateTransformation()
    {
        m_baseGroup.Transform = new ScaleTransform(m_scalingFactorMapDrawing, m_scalingFactorMapDrawing);
    }

    /// <summary>
    ///     Sets the gpx representation we need and prepares the visualization for the map system.
    /// </summary>
    public void UpdateRepresentation()
    {
        m_gpxSet = true;

        // Now we build the different tile sets.
        DrawingGroup mapWithPath = BuildMapWithPath();
        m_baseGroup.Children.Clear();
        m_baseGroup.Children.Add(mapWithPath);
        UpdateTransformation();
    }


    /// <summary>
    ///     Generates a drawing group that contains the complete map and the geometric representation of the gps path.
    /// </summary>
    /// <returns>Drawing group with visual representation.</returns>
    private DrawingGroup BuildMapWithPath()
    {
        var boundary = m_gpxRepresentation.BoundingRectangle;

        var minPosition = GetInTileCoordinates(boundary.maxLatitude, boundary.minLongitude);
        var maxPosition = GetInTileCoordinates(boundary.minLatitude, boundary.maxLongitude);

       
        m_originTileSystem = new Point(0.5f * (minPosition.x + maxPosition.x) * TileSize, 0.5f * (minPosition.y + maxPosition.y) * TileSize);

        // Now we create a drawing group for all the tiles.
        DrawingGroup drawingGroup = new DrawingGroup();
        for (int x = (int)minPosition.x; x <= (int)maxPosition.x; ++x)
        for (int y = (int)minPosition.y; y <= (int)maxPosition.y; ++y)
        {
            ImageDrawing subImage = new ImageDrawing(new BitmapImage(GetImageUri(x, y)),
                new Rect(new Point(x * TileSize, y * TileSize), new Size(TileSize, TileSize)));
            drawingGroup.Children.Add(subImage);
        }


        // Build the path with the lines from gps.
        GeometryDrawing geoDrawing = new GeometryDrawing();
        geoDrawing.Pen = new Pen(new SolidColorBrush(Color.FromRgb(255, 80, 80)), 20.0);
        PathGeometry pathGeo = new PathGeometry();
        PathFigure pathFigure = new PathFigure();

        bool firstElement = true;
        foreach ((float latitude, float longitude) point in m_gpxRepresentation.CoordinatePoints)
        {
            var tilePosition = GetInTileCoordinates(point.latitude, point.longitude);
            Point drawingPoint = new Point(tilePosition.x * TileSize, tilePosition.y * TileSize);
            if (firstElement)
                pathFigure.StartPoint = drawingPoint;
            else
                pathFigure.Segments.Add(new LineSegment(drawingPoint, true));

            firstElement = false;
        }

        pathGeo.Figures.Add(pathFigure);
        geoDrawing.Geometry = pathGeo;
        drawingGroup.Children.Add(geoDrawing);

        return drawingGroup;
    }

    /// <summary>
    ///     Gets the image URI.
    /// </summary>
    /// <param name="xTile">xTile we want to load.</param>
    /// <param name="yTile">yTile we want to load.</param>
    /// <returns>Uri for the tile image.</returns>
    private Uri GetImageUri(int xTile, int yTile)
    {
        // HACK HACK HACK
        return new Uri(@"D:\HoloLensTest\VideoGeoTagger\Dummy.png");

        return new Uri($"{TileProvider}/{ScalingLevel}/{xTile}/{yTile}.png");
    }

    #region Coordinate Conversion

    /// <summary>
    ///     Gets the tile coordinates for a given latitude and longitude. The integer part is the tile and the fraction part
    ///     the part of the tile.
    ///     x coordinate is from left to right and y coordinate from top to bottom.
    /// </summary>
    /// <param name="latitude">The latitude of the position to get.</param>
    /// <param name="longitude">The longitude of the position to get.</param>
    /// <returns>Tile coordinates in Mercator system.</returns>
    private (float x, float y) GetInTileCoordinates(float latitude, float longitude)
    {
        float x = (longitude + 180.0f) / 360.0f * m_scalingFactorTiles;

        float angleCorrect = latitude * MathF.PI / 180.0f;
        float y = (1.0f -
                   (MathF.Log(MathF.Tan(angleCorrect) + 1.0f / (MathF.Cos(angleCorrect))) / MathF.PI)) *
                  m_scalingFactorTiles * 0.5f;

        return (x, y);
    }


    /// <summary>
    ///     Gets the latitude and longitude for a given tile coordinate.
    /// </summary>
    /// <param name="xTile">x Tile coordinate</param>
    /// <param name="yTile">y Tile coordinate</param>
    /// <returns>latitude longitude pair.</returns>
    /// <seealso cref="GetInTileCoordinates" />
    private (float latitude, float longitude) GetGpxCoords(float xTile, float yTile)
    {
        float longitude = xTile / m_scalingFactorTiles * 360.0f - 180.0f;
        float latitude = MathF.Atan(MathF.Sinh(MathF.PI - yTile / m_scalingFactorTiles * 2.0f * MathF.PI)) * 180.0f /
                         MathF.PI;

        return (latitude, longitude);
    }

    #endregion
}