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
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Video_Trimming
{
    /// <summary>
    /// Interaction logic for CustomSliderTrack.xaml
    /// </summary>
    public partial class CustomSliderTrack : Track
    {
        
        public CustomSliderTrack()
        {
            InitializeComponent();
        }

        //private Thumb thumb;

        public Thumb LeftThumb { get; set; }

        //public Thumb Thumb { get { return thumb; } set { thumb = value; } }


        public Thumb RightThumb { get; set; }



        private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            Debug.WriteLine("Yup this is working");
            //Slider

            //thumb.CancelDrag(); // This stops normal dragging
            //thumb.CaptureMouse(); // This recaptures the mouse without making it go into a drag state.
            //canRun = true;
        }





    }
}
