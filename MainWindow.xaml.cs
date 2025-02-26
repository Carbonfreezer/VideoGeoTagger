using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VideoGeoTagger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }


        /// <summary>
        /// Gets called, when the zoom in button has been pressed 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnZoomIn(object sender, RoutedEventArgs e)
        {

        }


        /// <summary>
        /// Gets called when the zoom out button has been pressed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnZoomOut(object sender, RoutedEventArgs e)
        {

        }


        /// <summary>
        /// Gets called when the mouse has been oved up un the gpx window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnGpxMouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        /// <summary>
        /// Gets called when the mouse is released over the GPX window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnGpxMouseUp(object sender, MouseButtonEventArgs e)
        {

        }

        /// <summary>
        /// Gets called when the mouse is moved over the gpx window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnGpxMouseMove(object sender, MouseEventArgs e)
        {

        }


        /// <summary>
        /// Gets called, when the video slider has changed, is range from 0..1
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnVideoSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }
    }
}