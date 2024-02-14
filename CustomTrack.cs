using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Video_Trimming
{
    public partial class CustomTrack : Track
    {
        //private Thumb leftThumb, rightThumb;

        // does actually run
        public CustomTrack()
        {
            // thumbs are infact null when we get here.
            //Debug.WriteLine("Does run? " + (this.LeftThumb == null) + " " + (this.Thumb == null) + " " + (this.RightThumb == null) + " ");

            //Debug.WriteLine("Does run? " + this.LeftThumb.Name);

            base.OnApplyTemplate();
            Binding b = new Binding();
            b.ElementName = "SliderHorizontal";
            b.Path = new PropertyPath("SelectionEnd");

            this.SetBinding(ValueProperty, b);
            //SetBinding(Slider.SelectionStartProperty, b);
            
        }


        public CustomTrack(Thumb LeftThumb, Thumb RightThumb, Thumb Thumb)
        {
            Debug.WriteLine("Does run?22222");
        }



        /**
        
        public CustomTrack(Thumb leftThumb, Thumb rightThumb, bool contentLoaded)
        {
            this.leftThumb = leftThumb;
            this.rightThumb = rightThumb;
            //_contentLoaded = contentLoaded;
        }
         */

        // doing this so I can easily comment it out
        //private Thumb thumb;
        //public Thumb Thumb { get { return thumb; } set { thumb = value; } }

        //public Thumb Thumb { get; set; }


        public Thumb LeftThumb { get; set; }
        public Thumb RightThumb { get; set; }


        private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            Debug.WriteLine("Yup this is working");


            //thumb.CancelDrag(); // This stops normal dragging
            //thumb.CaptureMouse(); // This recaptures the mouse without making it go into a drag state.
            //canRun = true;
        }

        public static void Set_SelectionStart(CustomTrack track)
        {
            MainWindow window = (MainWindow)Application.Current.MainWindow;

            //base.OnApplyTemplate();
            Binding b = new Binding();
            b.ElementName = "testSlider"; // original
            //b.ElementName = window.testSlider.Name;

            b.Path = new PropertyPath("SelectionStart");
            track.SetBinding(ValueProperty, b);


            // finally got this working yay. I'm pretty sure I'm out of the woods at this point but keep vigilant.
            // When I get back I dont want to use a hardcoded name, but instead do like the parent shit until I find a slider control name that would be nice.



            Debug.WriteLine(track.Value);
        }

        public static void Set_SelectionEnd(Track track)
        {
            MainWindow window = (MainWindow)Application.Current.MainWindow;

            //base.OnApplyTemplate();
            Binding b = new Binding();
            b.ElementName = "testSlider"; // original
            //b.ElementName = window.testSlider.Name;

            b.Path = new PropertyPath("SelectionEnd");
            track.SetBinding(ValueProperty, b);


            // finally got this working yay. I'm pretty sure I'm out of the woods at this point but keep vigilant.
            // When I get back I dont want to use a hardcoded name, but instead do like the parent shit until I find a slider control name that would be nice.



            Debug.WriteLine(track.Value);
        }



    }
}
