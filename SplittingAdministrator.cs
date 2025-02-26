using System.Windows;
using System.Windows.Controls;

namespace VideoGeoTagger;

public class SplittingAdministrator
{
 
    /// <summary>
    /// The button to create a splitting point.
    /// </summary>
    private readonly Button m_createButton;


    /// <summary>
    /// The button to delete a splitting point.
    /// </summary>
    private readonly Button m_deleteButton;

    /// <summary>
    /// The list box with the splitting points.
    /// </summary>
    private readonly ListBox m_listBox;

    /// <summary>
    /// The time span with the splitting points contained.
    /// </summary>
    private readonly List<TimeSpan> m_splittingPoints = new List<TimeSpan>();


    /// <summary>
    /// The video administrator.
    /// </summary>
    private readonly VideoAdministrator m_videoAdmin;

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
    /// Gets called from the outside when new data is loaded.
    /// </summary>
    public void ResetData()
    {
        m_listBox.Items.Clear();
        m_createButton.IsEnabled = true;
        m_splittingPoints.Clear();
    }

    /// <summary>
    /// The delete command has been clicked for a splitting point.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void DeleteClicked(object sender, RoutedEventArgs e)
    {
        m_deleteButton.IsEnabled = false;
        m_splittingPoints.RemoveAt(m_listBox.SelectedIndex);
        UpdateList();
    }


    /// <summary>
    /// The create command has been clicked for the splitting point.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CreateClicked(object sender, RoutedEventArgs e)
    {
        TimeSpan currentSplittingPoint = m_videoAdmin.VideoPosition;
        m_splittingPoints.Add(currentSplittingPoint);
        UpdateList();
    }


    /// <summary>
    /// The list with the splitting point needs updating.
    /// </summary>
    private void UpdateList()
    {
        m_splittingPoints.Sort();
        m_listBox.Items.Clear();
        foreach (TimeSpan point in m_splittingPoints)
            m_listBox.Items.Add(point.ToString());
    }


    /// <summary>
    /// An item on the splitting point list has been added.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ItemSelected(object sender, SelectionChangedEventArgs e)
    {
        int index = m_listBox.SelectedIndex;
        if (index == -1)
            return;

        m_videoAdmin.VideoPosition = m_splittingPoints[index];
        m_deleteButton.IsEnabled = true;
    }
}