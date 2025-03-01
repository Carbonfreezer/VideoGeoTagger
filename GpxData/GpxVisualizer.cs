using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VideoGeoTagger.GpxData;

/// <summary>
///     This class does all the visual representation of the gpx files.
/// </summary>
public class GpxVisualizer
{
    /// <summary>
    ///     Delegate for event that gets called, when a map position has been selected.
    /// </summary>
    /// <param name="gpxTime">The time point in gpx time we have been closest to</param>
    public delegate void MapPositionSelected(TimeSpan gpxTime);

    /// <summary>
    ///     The scaling level we use for the geo map tile system.
    /// </summary>
    private const int ScalingLevel = 14; // 15;

    /// <summary>
    ///     The size of a single tile in pixel.
    /// </summary>
    private const int TileSize = 256;


    /// <summary>
    ///     The line size we use for drawing.
    /// </summary>
    private const double LineSize = 2.0;

    /// <summary>
    ///     The size of the cross.
    /// </summary>
    private const double CrossSize = 5.0;

    /// <summary>
    ///     The tile provider we use to query the tiles.
    /// </summary>
    private const string TileProvider = "https://tile.openstreetmap.org";


    /// <summary>
    ///     The base drawing group that contains everything.
    /// </summary>
    private readonly DrawingGroup m_baseGroup;

    /// <summary>
    ///     The size of the display gadget.
    /// </summary>
    private readonly Size m_gadgetSize;

    /// <summary>
    ///     The gpx representation for the track.
    /// </summary>
    private readonly GpxRepresentation m_gpxRepresentation;

    /// <summary>
    ///     The slider we have.
    /// </summary>
    private readonly Slider m_gpxZoomSlider;

    /// <summary>
    ///     The drawing for the marker.
    /// </summary>
    private readonly DrawingGroup m_markerDrawing;


    /// <summary>
    ///     The scaling factor that has on the scaling level for tiles.
    /// </summary>
    private readonly float m_scalingFactorTiles;

    /// <summary>
    ///     The drawing offset point corresponds to the upper left corner of the system. Used to interpret mouse clicks.
    /// </summary>
    private Point m_drawingOffsetPoint;

    /// <summary>
    ///     Indicates, that the gpx data is present.
    /// </summary>
    private bool m_gpxSet;

    /// <summary>
    ///     The old mouse position from previous sample.
    /// </summary>
    private Point m_oldMousePosition;


    /// <summary>
    ///     The center of the represented region in tile coordinates.
    /// </summary>
    private Vector m_originTileSystem;

    /// <summary>
    ///     The render offset point in tile coordinates we use for panning.
    /// </summary>
    private Vector m_renderOffsetPointInTiles;


    /// <summary>
    ///     The scaling factor we have internally for map drawing.
    /// </summary>
    private float m_scalingFactorMapDrawing;

    /// <summary>
    ///     Generates the gpx visualization module, that does all the internal handling.
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
        m_markerDrawing = GenerateMarker();
        controlImage.Source = new DrawingImage(m_baseGroup);
        m_gadgetSize = new Size(controlImage.Width, controlImage.Height);
        m_scalingFactorMapDrawing = 1.0f;
        gpxZoomSlider.Value = 100.0f;
        gpxZoomSlider.ValueChanged += GpxZoomSliderOnValueChanged;
        m_gpxZoomSlider = gpxZoomSlider;

        controlImage.MouseDown += ControlImageOnMouseDown;
        controlImage.MouseMove += ControlImageOnMouseMove;
        controlImage.MouseWheel += ControlImageOnMouseWheel;
    }

    /// <summary>
    ///     Gets invoked, when a map position has been selected.
    /// </summary>
    public event MapPositionSelected? OnMapTimeSelected;


    /// <summary>
    ///     Generates the marker for drawing.
    /// </summary>
    /// <returns>Drawing Group for the marker.</returns>
    private DrawingGroup GenerateMarker()
    {
        DrawingGroup result = new DrawingGroup();

        Pen crossPenn = new Pen(new SolidColorBrush(Color.FromRgb(80, 80, 255)), LineSize);
        for (int yDir = -1; yDir <= 1; yDir += 2)
        {
            LineGeometry geo = new LineGeometry(new Point(-CrossSize, -CrossSize * yDir),
                new Point(CrossSize, CrossSize * yDir));
            GeometryDrawing geoDrawing = new GeometryDrawing
            {
                Geometry = geo,
                Pen = crossPenn
            };
            result.Children.Add(geoDrawing);
        }

        return result;
    }


    /// <summary>
    ///     Sets the marker at an indicated position.
    /// </summary>
    /// <param name="gpxTime">The time in gpx time we want to set the marker for.</param>
    public void SetMarker(TimeSpan gpxTime)
    {
        var coord = m_gpxRepresentation.GetPositionForTimeStamp(gpxTime);
        Vector tileCoords = GetInTileCoordinates(coord.latitude, coord.longitude);
        tileCoords *= TileSize;

        m_markerDrawing.Transform = new TranslateTransform(tileCoords.X, tileCoords.Y);
    }


    /// <summary>
    ///     Disables the marker.
    /// </summary>
    public void DisableMarker()
    {
        m_markerDrawing.Transform = new ScaleTransform(0.0, 0.0);
    }


    /// <summary>
    ///     The mouse wheel controls the zoom.
    /// </summary>
    private void ControlImageOnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Delta > 0)
            m_scalingFactorMapDrawing += 0.03f;
        else
            m_scalingFactorMapDrawing -= 0.03f;

        if (m_scalingFactorMapDrawing > 3.0f)
            m_scalingFactorMapDrawing = 3.0f;
        if (m_scalingFactorMapDrawing < 0.01f)
            m_scalingFactorMapDrawing = 0.01f;

        m_gpxZoomSlider.Value = 100.0f * m_scalingFactorMapDrawing;
        UpdateTransformation();
    }


    /// <summary>
    ///     On right mouse button we pan.
    /// </summary>
    private void ControlImageOnMouseMove(object sender, MouseEventArgs e)
    {
        Point position = e.GetPosition((Image)sender);
        Vector delta = position - m_oldMousePosition;
        m_oldMousePosition = position;
        if (e.MiddleButton != MouseButtonState.Pressed)
            return;

        delta /= TileSize;
        delta /= m_scalingFactorMapDrawing;
        m_renderOffsetPointInTiles += delta;
        UpdateTransformation();
    }

    /// <summary>
    ///     Mouse down generates latitude and longitude positions.
    /// </summary>
    private void ControlImageOnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        Vector mousePosition = (Vector)e.GetPosition((Image)sender);
        mousePosition /= m_scalingFactorMapDrawing;
        mousePosition += (Vector)m_drawingOffsetPoint;
        Vector scaledMousePosition = mousePosition / TileSize;

        var coords = GetGpxCoords(scaledMousePosition);
        OnMapTimeSelected?.Invoke(m_gpxRepresentation.GetClosestTime(coords.latitude, coords.longitude));
    }


    /// <summary>
    ///     Gets called, when the zoom slider has been moved.
    /// </summary>
    private void GpxZoomSliderOnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!m_gpxSet)
            return;

        m_scalingFactorMapDrawing = (float)(m_gpxZoomSlider.Value / 100.0f);
        if (m_scalingFactorMapDrawing < 0.01f)
            m_scalingFactorMapDrawing = 0.01f;
        UpdateTransformation();
    }


    /// <summary>
    ///     Updates the transformation to display the correct section of the image.
    /// </summary>
    private void UpdateTransformation()
    {
        Size scaledSize = new Size(m_gadgetSize.Width / m_scalingFactorMapDrawing,
            m_gadgetSize.Height / m_scalingFactorMapDrawing);

        Vector transformedOrigin = (m_originTileSystem - m_renderOffsetPointInTiles) * TileSize;

        m_drawingOffsetPoint = new Point(transformedOrigin.X - scaledSize.Width * 0.5,
            transformedOrigin.Y - scaledSize.Height * 0.5);
        Rect clippingArea = new Rect(m_drawingOffsetPoint, scaledSize);
        m_baseGroup.ClipGeometry = new RectangleGeometry(clippingArea);
        m_baseGroup.Transform = new ScaleTransform(m_scalingFactorMapDrawing, m_scalingFactorMapDrawing,
            transformedOrigin.X, transformedOrigin.Y);
    }

    /// <summary>
    ///     Sets the gpx representation we need and prepares the visualization for the map system.
    ///     This is called, when a new gpx file has been set.
    /// </summary>
    public void UpdateRepresentation()
    {
        m_gpxSet = true;

        // Now we build the different tile sets.
        DrawingGroup mapWithPath = BuildMapWithPath();
        m_baseGroup.Children.Clear();
        m_baseGroup.Children.Add(mapWithPath);
        m_baseGroup.Children.Add(m_markerDrawing);
        UpdateTransformation();
        DisableMarker();
    }


    /// <summary>
    ///     Generates a drawing group that contains the complete map and the geometric representation of the gps path.
    /// </summary>
    /// <returns>Drawing group with visual representation.</returns>
    private DrawingGroup BuildMapWithPath()
    {
        var boundary = m_gpxRepresentation.BoundingRectangle;

        Vector minPosition = GetInTileCoordinates(boundary.maxLatitude, boundary.minLongitude);
        Vector maxPosition = GetInTileCoordinates(boundary.minLatitude, boundary.maxLongitude);


        m_originTileSystem = (minPosition + maxPosition) * 0.5;

        // Now we create a drawing group for all the tiles.
        DrawingGroup drawingGroup = new DrawingGroup();

        // First we a big grey area to make sure that the drawing is always centred after clipping.
        const double bigValue = 1e+10;
        Rect bigRect = new Rect(new Point(-bigValue, -bigValue), new Point(bigValue, bigValue));
        GeometryDrawing bgRect = new GeometryDrawing
        {
            Brush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
            Geometry = new RectangleGeometry(bigRect)
        };
        drawingGroup.Children.Add(bgRect);

        // Now the tiles.
        for (int x = (int)minPosition.X; x <= (int)maxPosition.X; ++x)
        for (int y = (int)minPosition.Y; y <= (int)maxPosition.Y; ++y)
        {
            ImageDrawing subImage = new ImageDrawing(new BitmapImage(GetImageUri(x, y)),
                new Rect(new Point(x * TileSize, y * TileSize), new Size(TileSize, TileSize)));
            drawingGroup.Children.Add(subImage);
        }


        // Build the path with the lines from gps.
        GeometryDrawing geoDrawing = new GeometryDrawing();
        geoDrawing.Pen = new Pen(new SolidColorBrush(Color.FromRgb(255, 80, 80)), LineSize);
        PathGeometry pathGeo = new PathGeometry();
        PathFigure pathFigure = new PathFigure();

        bool firstElement = true;
        foreach ((float latitude, float longitude) point in m_gpxRepresentation.CoordinatePoints)
        {
            Point drawingPoint = (Point)(GetInTileCoordinates(point.latitude, point.longitude) * TileSize);
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
    private Vector GetInTileCoordinates(float latitude, float longitude)
    {
        float x = (longitude + 180.0f) / 360.0f * m_scalingFactorTiles;

        float angleCorrect = latitude * MathF.PI / 180.0f;
        float y = (1.0f -
                   (MathF.Log(MathF.Tan(angleCorrect) + 1.0f / (MathF.Cos(angleCorrect))) / MathF.PI)) *
                  m_scalingFactorTiles * 0.5f;

        return new Vector(x, y);
    }


    /// <summary>
    ///     Gets the latitude and longitude for a given tile coordinate.
    /// </summary>
    /// <param name="tileCoords">The coordinates in tiles.</param>
    /// <returns>latitude longitude pair.</returns>
    /// <seealso cref="GetInTileCoordinates" />
    private (float latitude, float longitude) GetGpxCoords(Vector tileCoords)
    {
        float longitude = ((float)tileCoords.X) / m_scalingFactorTiles * 360.0f - 180.0f;
        float latitude =
            MathF.Atan(MathF.Sinh(MathF.PI - ((float)tileCoords.Y) / m_scalingFactorTiles * 2.0f * MathF.PI)) * 180.0f /
            MathF.PI;

        return (latitude, longitude);
    }

    #endregion
}