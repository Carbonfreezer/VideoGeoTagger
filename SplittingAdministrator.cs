using System.Windows;
using System.Windows.Controls;

namespace VideoGeoTagger;

/// <summary>
///     Administrates the splitting points that generate the segments.
/// </summary>
public class SplittingAdministrator
{
    /// <summary>
    ///     Delegate to inform another instance, that the splitting points have been changed.
    /// </summary>
    /// <param name="splittingPoints">List with splitting points.</param>
    public delegate void SetNewSplittingPoints(List<TimeSpan> splittingPoints);

    /// <summary>
    ///     The button to create a splitting point.
    /// </summary>
    private readonly Button m_createButton;


    /// <summary>
    ///     The button to delete a splitting point.
    /// </summary>
    private readonly Button m_deleteButton;

    /// <summary>
    ///     The list box with the splitting points.
    /// </summary>
    private readonly ListBox m_listBox;


    /// <summary>
    ///     The video administrator.
    /// </summary>
    private readonly VideoAdministrator m_videoAdmin;

    /// <summary>
    ///     The time span with the splitting points contained.
    /// </summary>
    private List<TimeSpan> m_splittingPoints = new List<TimeSpan>();


    /// <summary>
    ///     Constructor takes all gui elements.
    /// </summary>
    /// <param name="listBox">List box with the splitting points.</param>
    /// <param name="createButton">The button to create a splitting point.</param>
    /// <param name="deleteButton">The button to delete a splitting point.</param>
    /// <param name="videoAdmin">The video player to get / set position.</param>
    public SplittingAdministrator(ListBox listBox, Button createButton, Button deleteButton,
        VideoAdministrator videoAdmin)
    {
        m_listBox = listBox;
        m_listBox.SelectionChanged += ItemSelected;
        m_createButton = createButton;
        m_createButton.IsEnabled = false;
        m_createButton.Click += CreateClicked;
        m_deleteButton = deleteButton;
        m_deleteButton.IsEnabled = false;
        m_deleteButton.Click += DeleteClicked;
        m_videoAdmin = videoAdmin;
    }


    /// <summary>
    ///     Gets the splitting points for project save purposes.
    /// </summary>
    public List<TimeSpan> SplittingPoints => m_splittingPoints;


    /// <summary>
    ///     Gets invoked when the splitting points have been changed,
    /// </summary>
    public event SetNewSplittingPoints? OnSplittingPointsChanged;


    /// <summary>
    ///     Sets the splitting points from the outside, used for saving procedures.
    /// </summary>
    /// <param name="savedSplittingPoints">The saved splitting points.</param>
    public void SetSplittingPoints(List<TimeSpan> savedSplittingPoints)
    {
        m_splittingPoints = savedSplittingPoints;
        UpdateList();
    }

    /// <summary>
    ///     Gets called from the outside when new data is loaded.
    /// </summary>
    public void ResetData()
    {
        m_listBox.Items.Clear();
        m_createButton.IsEnabled = true;
        m_splittingPoints.Clear();
    }

    /// <summary>
    ///     The delete command has been clicked for a splitting point.
    /// </summary>
    private void DeleteClicked(object sender, RoutedEventArgs e)
    {
        m_deleteButton.IsEnabled = false;
        m_splittingPoints.RemoveAt(m_listBox.SelectedIndex);
        UpdateList();
    }


    /// <summary>
    ///     The create command has been clicked for the splitting point.
    /// </summary>
    private void CreateClicked(object sender, RoutedEventArgs e)
    {
        TimeSpan currentSplittingPoint = m_videoAdmin.VideoPosition;
        m_splittingPoints.Add(currentSplittingPoint);
        UpdateList();
    }


    /// <summary>
    ///     The list with the splitting point needs updating.
    /// </summary>
    private void UpdateList()
    {
        m_splittingPoints.Sort();
        OnSplittingPointsChanged?.Invoke(m_splittingPoints);
        m_listBox.Items.Clear();
        foreach (TimeSpan point in m_splittingPoints)
            m_listBox.Items.Add(point.ToString());
    }


    /// <summary>
    ///     An item on the splitting point list has been added.
    /// </summary>
    private void ItemSelected(object sender, SelectionChangedEventArgs e)
    {
        int index = m_listBox.SelectedIndex;
        if (index == -1)
            return;

        m_videoAdmin.VideoPosition = m_splittingPoints[index];
        m_deleteButton.IsEnabled = true;
    }
}