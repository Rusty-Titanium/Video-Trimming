using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Video_Trimming
{
    /// <summary>
    /// Interaction logic for Styles.xaml
    /// </summary>
    public partial class Styles : ResourceDictionary
    {
        public Styles()
        {
            
        }

        
        public void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            Debug.WriteLine("running in Styles code behind.");


            //thumb.CancelDrag(); // This stops normal dragging
            //thumb.CaptureMouse(); // This recaptures the mouse without making it go into a drag state.
            //canRun = true;
        }

        private void PART_LeftTrack_Loaded(object sender, RoutedEventArgs e)
        {
            Track track = (Track)sender;


            //base.OnApplyTemplate();
            Binding b = new Binding();
            b.Mode = BindingMode.TwoWay;
            b.ElementName = "testSlider"; // original
            //b.ElementName = window.testSlider.Name;

            b.Path = new PropertyPath("SelectionStart");
            track.SetBinding(Track.ValueProperty, b);
        }

        private void PART_RightTrack_Loaded(object sender, RoutedEventArgs e)
        {

            MainWindow window = (MainWindow)Application.Current.MainWindow;

            Track track = (Track)sender;


            //base.OnApplyTemplate();
            Binding b = new Binding();
            b.Mode = BindingMode.TwoWay;
            b.ElementName = "testSlider"; // original
            //b.ElementName = window.testSlider.Name;

            b.Path = new PropertyPath("SelectionEnd");
            track.SetBinding(Track.ValueProperty, b);


           


            Debug.WriteLine(track.Value);
        }


        private void LeftThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Thumb thumb = (Thumb)sender;
            Slider slider = (Slider)thumb.TemplatedParent;

            
            /**
             * Notes for when I come back:
             * 
             * - I think I'm at a point where I think I can do the thing that I want, but its going to involve lots more math to figure everything out.
             * - I may be able to borrow mouse location code from the image cropper stuffs to expidite this and with that I think setting the values will be quite easy.
             * - If I can get this into a single control I think it will make everything a lot smoother.
             * 
             * 
             */



            

            //Debug.WriteLine(e.HorizontalChange + " out of " + slider.ActualWidth);

            //Mouse.GetPosition(slider).X = e.HorizontalChange;


            Debug.WriteLine("Mouse Position X: " + Mouse.GetPosition(slider).X);

            double mousePositionX = Mouse.GetPosition(slider).X;
            double percentage = mousePositionX / slider.ActualWidth; // gives us a percentage of what the seleciton thing needs to be.
            double finalAnswer = percentage * slider.Maximum;
            slider.SelectionStart = finalAnswer;




            /**
            double newSelectionStart = CustomSlider.SelectionStart + e.HorizontalChange;
            newSelectionStart = ClampValue(newSelectionStart, CustomSlider.Minimum, CustomSlider.SelectionEnd);
            CustomSlider.SelectionStart = newSelectionStart;

            // Update the position of the start thumb
            UpdateThumbPosition(startThumb, newSelectionStart);
             */
        }

        
        // Event handler for the end thumb's drag operation
        private void RightThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {


            /**
            double newSelectionEnd = CustomSlider.SelectionEnd + e.HorizontalChange;
            newSelectionEnd = ClampValue(newSelectionEnd, CustomSlider.SelectionStart, CustomSlider.Maximum);
            CustomSlider.SelectionEnd = newSelectionEnd;

            // Update the position of the end thumb
            UpdateThumbPosition(endThumb, newSelectionEnd);
             */
        }

        /**
        // Helper method to update the position of a thumb
        private void UpdateThumbPosition(Thumb thumb, double value)
        {
            double trackWidth = track.ActualWidth;

            Debug.WriteLine(trackWidth);

            double thumbPosition = (value - CustomSlider.Minimum) / (CustomSlider.Maximum - CustomSlider.Minimum) * trackWidth;

            // Set the Canvas.Left property of the thumb

            Canvas.SetLeft(thumb, thumbPosition);
        }

        // Helper method to clamp a value within a specified range
        private double ClampValue(double value, double minValue, double maxValue)
        {
            return Math.Max(minValue, Math.Min(maxValue, value));
        }
         */











    }

}
