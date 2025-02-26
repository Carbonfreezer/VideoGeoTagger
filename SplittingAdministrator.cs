using System.Windows;
using System.Windows.Controls;

namespace VideoGeoTagger;

public class SplittingAdministrator
{
    private readonly Button m_createButton;
    private readonly Button m_deleteButton;
    private readonly ListBox m_listBox;
    private readonly List<TimeSpan> m_splittingPoints = new List<TimeSpan>();
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

    public void ResetData()
    {
        m_listBox.Items.Clear();
        m_createButton.IsEnabled = true;
        m_splittingPoints.Clear();
    }

    private void DeleteClicked(object sender, RoutedEventArgs e)
    {
        m_deleteButton.IsEnabled = false;
        m_splittingPoints.RemoveAt(m_listBox.SelectedIndex);
        UpdateList();
    }

    private void CreateClicked(object sender, RoutedEventArgs e)
    {
        TimeSpan currentSplittingPoint = m_videoAdmin.VideoPosition;
        m_splittingPoints.Add(currentSplittingPoint);
        UpdateList();
    }

    private void UpdateList()
    {
        m_splittingPoints.Sort();
        m_listBox.Items.Clear();
        foreach (TimeSpan point in m_splittingPoints)
            m_listBox.Items.Add(point.ToString());
    }

    private void ItemSelected(object sender, SelectionChangedEventArgs e)
    {
        int index = m_listBox.SelectedIndex;
        if (index == -1)
            return;

        m_videoAdmin.VideoPosition = m_splittingPoints[index];
        m_deleteButton.IsEnabled = true;
    }
}