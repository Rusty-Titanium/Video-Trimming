using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
        //public Styles() { }


        /**
         * 
         * BUGS:
         * - when selectionEnd or Start gets changed in code behind, the rectangle doesn't reflect it, so I need to find a way to make sure this doesn't break.
         *      I can fix this by either making a CustomSlider with a couple added methods to help update that information, or just like, be sneaky about it and fix up the rectangle when updating
         *      the value as well, which honestly I dont want to do since that is 1. lazy and 2. can be hard to keep track of down the line.
         * - Not sure if really bug but oversight. because I have to use margin to make the one thumb higher on the slider, it makes the entire slider massive, making the click anywhere
         *      to move thumb to it thing a massive area. Need to do testing but this could be bad.
         * - not fixing now but please at some point: If the slider is Visibility.Collapsed, my code seems to have a heart attack. problem is unless I do some proper fix, some of the code
         *      will never be ran and set up properly. aka I want to make an on visibility changed event to check for visibility when loading in and what not so that doesn't happen again.
         * 
         */



        private void Slider_Loaded(object sender, RoutedEventArgs e)
        {
            // I'm not entirely sure why, but for some reason both the left and right tracks are also getting their minimums and maximum values set without me doing anything, and I don't know why

            Slider slider = (Slider)sender;
            Track mainTrack = (Track)slider.Template.FindName("PART_Track", slider);

            // Main Track
            slider.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(Slider_PreviewMouseLeftButtonDown), true);
            slider.AddHandler(UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(Slider_PreviewMouseLeftButtonUp), true);

            // still thinking that I can get away with not using mainWindow here but thats just a pipe dream for now. just trying to get everything to work.
            MainWindow main = (MainWindow)Application.Current.MainWindow;
            main.AddHandler(UIElement.MouseMoveEvent, new MouseEventHandler(MainWindow_MouseMove), true);

            // Testing purposes
            //Debug.WriteLine(slider.Name + "   Main: " + mainTrack.Minimum + "   " + mainTrack.Value + "   " + mainTrack.Maximum + "  |  " + slider.SelectionStart + "  " + slider.SelectionEnd);

            
            // Left Track
            Track leftTrack = (Track)slider.Template.FindName("PART_LeftTrack", slider);

            Binding b = new Binding();
            b.Mode = BindingMode.TwoWay;
            b.ElementName = ((Slider)leftTrack.TemplatedParent).Name;
            b.Path = new PropertyPath("SelectionStart");
            leftTrack.SetBinding(Track.ValueProperty, b);
            
            
            // Right Track
            Track rightTrack = (Track)slider.Template.FindName("PART_RightTrack", slider);

            b = new Binding();
            b.Mode = BindingMode.TwoWay;
            b.ElementName = ((Slider)rightTrack.TemplatedParent).Name;
            b.Path = new PropertyPath("SelectionEnd");
            rightTrack.SetBinding(Track.ValueProperty, b);
            
            // This sets the SelectionRange Rectangle to its proper visual state.
            SetSelectionRangeRectangle(slider, true);
            
        }


        private void Slider_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetSelectionRangeRectangle((Slider)sender, true);
        }


        private void SetSelectionRangeRectangle(Slider slider, bool setLeftMargin)
        {
            Rectangle rect = (Rectangle)slider.Template.FindName("PART_CustomSelectionRange", slider);
            Border border = (Border)slider.Template.FindName("TrackBackground", slider);

            // Calculates selectionrange rect width. This SHOULD ALWAYS BE CORRECT
            double rectWidthPercentage = (slider.SelectionEnd - slider.SelectionStart) / slider.Maximum;
            rect.Width = border.ActualWidth * rectWidthPercentage;

            if (setLeftMargin)
            {
                // Calculates Canvas left margin Value. Since this is based off the selection start position, this SHOULD ALWAYS BE CORRECT.
                double leftMarginPercentage = slider.SelectionStart / slider.Maximum;
                double leftMargin = border.ActualWidth * leftMarginPercentage;
                Canvas.SetLeft(rect, leftMargin);
            }
        }





        /// <summary>
        /// This method runs once when a drag is started. It pauses all media, mutes it (so scrubbing doesn't make any weird noises), and calls another method to start a timer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            Thumb thumb = (Thumb)sender;
            
            thumb.CancelDrag(); // This stops normal dragging
            thumb.CaptureMouse(); // This recaptures the mouse without making it go into a drag state.
            canRun = true;
        }





        /// <summary>
        /// This method runs when the slider is clicked, so tha the media can be seeked to the timestamp of the mouses location. Event also had to be added during apps initial setup as 
        ///     originally this method is eaten by the slider, so it had to be added in code behind.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Slider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            movingSlider = (Slider)sender;
            
            Thumb leftThumb = ((Track)movingSlider.Template.FindName("PART_LeftTrack", movingSlider)).Thumb;
            Thumb rightThumb = ((Track)movingSlider.Template.FindName("PART_RightTrack", movingSlider)).Thumb;
            Thumb mainThumb = ((Track)movingSlider.Template.FindName("PART_Track", movingSlider)).Thumb;

            if (leftThumb.IsMouseOver)
            {
                movingThumb = leftThumb;
                mouseThumbPointDifference = Mouse.GetPosition(movingThumb).X;
            }
            else if (rightThumb.IsMouseOver)
            {
                movingThumb = rightThumb;
                mouseThumbPointDifference = Mouse.GetPosition(movingThumb).X;
            }
            else if (mainThumb.IsMouseOver)
            {
                movingThumb = mainThumb;
                mouseThumbPointDifference = Mouse.GetPosition(movingThumb).X;
            }
            else // if it gets to here, it means we are trying to select a point on the slider to jump too via main thumb
            {
                mouseThumbPointDifference = -1;
                canRun = true; // This is here because it will allow the mousemove area to start working immediately.
                movingThumb = mainThumb;
                movingThumb.CaptureMouse();
            }

        }



        /// <summary>
        /// This method runs when the slider unclicked, so that the capture of the control can be released.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Slider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            canRun = false;
            movingThumb.ReleaseMouseCapture();
        }
        


        private bool canRun = false;
        double mouseThumbPointDifference = 0;
        private Thumb movingThumb;
        private Slider movingSlider;


        
        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {

            if (canRun && movingThumb.IsMouseCaptured) // true if mouse is captured on the drag
            {
                Border border = (Border)movingSlider.Template.FindName("TrackBackground", movingSlider);

                double borderWidth = border.ActualWidth;
                double newMousePointX = Mouse.GetPosition(border).X;
                double finalPercentage;

                // Since left and rightThumb use Margins in their xaml, it would throw off the calculations if I were to house everything
                //      under one algorithm, so in this code I split the finalPercentage calculations so everything would work nice.

                if (movingThumb.Name.Equals("LeftThumb"))
                {
                    // Setting finalPercentage
                    finalPercentage = CheckPercentageRange((newMousePointX + (movingThumb.ActualWidth - mouseThumbPointDifference)) / borderWidth);
                    /////////////////////////////////////////////////////////////////////////

                    double selectionStart = movingSlider.Maximum * finalPercentage;

                    if (selectionStart > movingSlider.SelectionEnd) // Makes sure selectionStart is not past selectionEnd
                        selectionStart = movingSlider.SelectionEnd;

                    if (selectionStart > movingSlider.Value) // Makes sure selectionStart is not past Value
                        movingSlider.Value = selectionStart;

                    movingSlider.SelectionStart = selectionStart;
                    SetSelectionRangeRectangle(movingSlider, true);
                }
                else if (movingThumb.Name.Equals("RightThumb"))
                {
                    // Setting finalPercentage
                    finalPercentage = CheckPercentageRange((newMousePointX - mouseThumbPointDifference) / borderWidth);
                    /////////////////////////////////////////////////////////////////////////

                    double selectionEnd = movingSlider.Maximum * finalPercentage;

                    if (selectionEnd < movingSlider.SelectionStart) // Makes sure selectionEnd is not before selectionStart
                        selectionEnd = movingSlider.SelectionStart;

                    if (selectionEnd < movingSlider.Value) // Makes sure selectionEnd is not before Value
                        movingSlider.Value = selectionEnd;

                    movingSlider.SelectionEnd = selectionEnd;
                    SetSelectionRangeRectangle(movingSlider, false);
                }
                else // if true, this means it is the main Thumb
                {
                    double halfOfThumbWidth = movingThumb.ActualWidth / 2.0;

                    if (mouseThumbPointDifference >= 0 && mouseThumbPointDifference < halfOfThumbWidth)
                        finalPercentage = CheckPercentageRange((newMousePointX + (halfOfThumbWidth - mouseThumbPointDifference)) / borderWidth);
                    else if (mouseThumbPointDifference > halfOfThumbWidth) // doesn't need a check on upper limit since the mouseThumbPointDifference can only go as high as the thumb's ActualWidth value.
                        finalPercentage = CheckPercentageRange((newMousePointX - (mouseThumbPointDifference - halfOfThumbWidth)) / borderWidth);
                    else // Runs if mouseThumbPointDifference is not necessary
                        finalPercentage = CheckPercentageRange(newMousePointX / borderWidth);
                    /////////////////////////////////////////////////////////////////////////

                    double newValue = movingSlider.Maximum * finalPercentage;

                    // if / else if / else statement here to stop mainThumb from passing the other 2 values.
                    if (newValue < movingSlider.SelectionStart)
                        movingSlider.Value = movingSlider.SelectionStart;
                    else if (newValue > movingSlider.SelectionEnd)
                        movingSlider.Value = movingSlider.SelectionEnd;
                    else
                        movingSlider.Value = newValue;
                }

            }

        }

        
        /// <summary>
        /// This method makes sure that the percentage is not under 0 and not above 1.
        /// </summary>
        /// <param name="percentage"></param>
        /// <returns></returns>
        private double CheckPercentageRange(double percentage)
        {
            if (percentage > 1.0)
                percentage = 1;
            else if (percentage < 0.0)
                percentage = 0;

            return percentage;
        }

        
    }

}
