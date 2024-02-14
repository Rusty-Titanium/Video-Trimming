using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup.Localizer;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace Video_Trimming
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /**
         * Additions:
         * - (Super Optional) Add a way to delete after making a clip (only if its deletion shows up in recycle folder afterwards)
         * - (Optional) Add ability to choose from pre-determined resolutions the video should be re-rendered at (aka keeping aspect ratio).
         * - (Super Optional): Add in stepping through frame by frame. Won't be hard, but it would mean I either need a key input for it "< and >" or something on screen. Can use ffmpeg commands for calculating timestamps for each frame or a rough idea.
         *                          I believe I remember reading somewhere you could get like the timestamps of each frame I think? If I can do that I can just figure out which frame I'm on based on that info and go from there.
         * - (Optional) Add a way to choose where new files end up, instead of just the desktop.
         * - (Should Probably Do) Maybe add in a way to check if ffmpeg is even on the users computer and if not, show them how to get it installed and in their SYSTEM PATH properly.
         * - (Maybe some day) I want to do a bit of refactoring cause honestly when something small changes in this code, it breaks quite easily and its getting annoying trying to debug
         *      the issue I caused that I would think is unrelated. I dont need to refactor everything, but at least the media and Slider stuffs so that if an issue were to go wrong, it can
         *      only break in one spot, and make sure that no code can overlap each other in terms of function, i.e. 2 or 3 areas needing to check if a side thumb is past the main thumb etc.
         * 
         * Unresolvable Issue:
         * - if you see a note with an arbitrary -100 (or -0.1 seconds), that is to prevent the program from getting to the end of a video. The reason for this is because it seems like when 
         *      a MediaElement ends its video on its own, it seems to set its position to MediaElement.NaturalDuration. The reason this is a problem is because with my workaround, my program 
         *      allows videoplayback that is usually greater than NaturalDuration (since NaturalDuration doesn't take milliseconds into account). this means that when the video ended, the thumb 
         *      of my program would snap back to the last full second of the video, and would lock up the sliders until the video is changed. Since most of my issues stem from the MediaElement 
         *      Control, I could of went and look for 3rd party alternatives that natively gives me the ACCURATE duration of a video, but after spending so much time on this program as it is, 
         *      isn't worth my time anymore. I would also have to overcome the hurdle of if the video finishes playing and I can't completely take control of the endpoint of the video anyway, then 
         *      I would still have this issue regardless. tl;dr I don't want to spend days trying to do that at this point when this project should of been completed in 3-4 days, not a week.
         * 
         * Stuff to fix:
         * - Bug - Noticed that if you click just left or right of the stem of the mainSlider thumb, it doesn't move it or have anyway to interact. I guess the way I built it doesn't allow it to
         *              click past whatever invisible thingy that is there. If you find some time please fix this.
         * - Bug - seems like we are getting jumping at the end of the video. I presume this is because the video is running a small portion of the last 100 ms that I conveniently leave out, causing
         *              what looks like a bad stutter. I think this is due to setting the mainSlider.Value to an int and depending on the value it may make it move a pixel which causes the funkiness.
         * - Bug not really but - there is no message for when creating a video without re-encoding. I might want to add SOMETHING just to tell the user that something is happening, instead of nothing.
         * 
         * 
         * Notes:
         * - After selecting a folder, if a file gets renamed before showing up in the program, it will no longer function, but it doesn't seem to crash it so I don't really care that much.
         * - Giving this program a clip smaller than a second or bigger than 99 minutes might cause issues but haven't tested myself.
         * - It seems when checking or unchecking the ToggleButton in CodeBehind, it will still run the Checked/Unchecked events, so that is basically my play or pause method now.
         * - when it comes to output (talking about RunCommand Method), the print prints out the entire string again with the appended shit at the bottom, might be useful for 
         *      stuff like getting max frames to calculate fps so stepping frame by frame is possible.
         * 
         * 
         * other stuff:
         * (This is ffmpeg code to check for packets (or frames in a video) to get an accurate measurement of frames in a video. Problem is that if the video lags a bunch it might be possible for fps to be wrong.)
         * 
         *
         * (counts number of packets which is always the same as frames, and is much faster.)
         * ffprobe -v error -select_streams v:0 -count_packets -show_entries stream=nb_read_packets -of csv=p=0 "C:\Users\Richi\Desktop\Desktop 2023.12.18 - 8 sec rec.mp4"
         * 
         * Duration: seems really nice
         * ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 "C:\Users\Richi\Videos\Raw Recordings\Elden Ring\Elden Ring 2023.01.24 - 21.58.14.06.DVR.mp4"
         * 
         */

        private List<String> listOfPaths = new List<String>(); // list of file Paths
        private String originalPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) + "\\"; // This should really just be the environment path
        private Thumb thumb, rightThumb; // currently dont need leftThumb cause doesn't seem like it needs it atm.
        private bool isPlaying = true;

        public MainWindow()
        {
            InitializeComponent();

            singleFilePathBox.Text = originalPath;
            folderPathBox.Text = originalPath;
            settingsControl.Visibility = Visibility.Visible;

            // Needed because otherwise the leftbuttondown event would get eaten and wouldn't allow the main slider thumb to move.
            mainSlider.AddHandler(PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(MainSlider_PreviewMouseLeftButtonDown), true);
            mainSlider.AddHandler(PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(MainSlider_PreviewMouseLeftButtonUp), true);

            this.MouseMove += MainWindow_MouseMove;

            // This is the timer for when video is playing which updates the mainSlider thumb location.
            timerVideoTime = new DispatcherTimer();
            timerVideoTime.Interval = TimeSpan.FromMilliseconds(10); // This says how often the timer will run. The lower the value, the smoother the main Thumb will look while moving.
            timerVideoTime.Tick += new EventHandler(Timer_Video_Tick);

            // This is the timer for Scrubbing getting initialized.
            timerScrubbing = new DispatcherTimer();
            timerScrubbing.Interval = TimeSpan.FromMilliseconds(100); // original is 100
            timerScrubbing.Tick += new EventHandler(Timer_Scrubbing_Tick);




            //thumb.
            //testSlider.Value
            //trackF.mi
            //mainSlider.mi

        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            thumb = (mainSlider.Template.FindName("PART_Track", mainSlider) as Track).Thumb;
            rightThumb = (rightSlider.Template.FindName("PART_Track", rightSlider) as Track).Thumb;
        }


        private String RunCommand(string fileName, string args)  // original  -->  private String RunCommand(string fileName, string args)
        {
            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var sb = new StringBuilder();
            Process process;

            using (process = new Process())
            {
                process.StartInfo = start;
                process.EnableRaisingEvents = true;
                process.OutputDataReceived += (sender, eventArgs) =>
                {
                    sb.AppendLine(eventArgs.Data); //allow other stuff as well
                };
                process.ErrorDataReceived += (sender, eventArgs) =>
                {
                    sb.AppendLine(eventArgs.Data); //For some reason some ffmpeg commands prints in the ErrorOutput section, not entirely sure why.
                };

                if (process.Start())
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // Keep looping until we have text in the stringbuilder (this text HAS to show up before it can continue as it's the duration of the video.)
                    while (sb.ToString().Trim().Length == 0)
                        Task.Delay(20).Wait();

                    process.Kill(); // This is here because otherwise a bunch of cmd processes would just be open in task manager.
                }
            }

            return sb.ToString();
        }



        private void Single_File_Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = originalPath;
            openFileDialog.Filter = "Video Files (*.mp4)|*.mp4";
            
            if ((bool)openFileDialog.ShowDialog())
            {
                String path = openFileDialog.FileName;

                listOfPaths = new List<String>();
                listOfPaths.Add(path);
                singleFilePathBox.Text = path;

                Next_File(); // Retrieves the next file.

                createButton.IsEnabled = true;
                gameNameTBox.Text = ""; // Since it is single file grabbing, user might not want to name it under the games title, so its set to blank in the event they want to add one themselves.
            }
        }






        /// <summary>
        /// This method opens a dialog option and allows the user to choose a folder for the program to search. If valid, will create a list of video paths from the given folder, and 
        ///     calls a method to start the first video.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            openFolderDialog.InitialDirectory = originalPath;
            
            if ((bool)openFolderDialog.ShowDialog())
            {
                Predicate<String> nonMP4Remover = (String s) => { return !s.EndsWith(".mp4"); }; // Predicate to return true if the file is a .mp4

                String path = openFolderDialog.FolderName;
                listOfPaths = Directory.GetFiles(path).ToList();
                listOfPaths.RemoveAll(nonMP4Remover); // Removes all files that don't end in .mp4

                if (listOfPaths.Count > 0)
                {
                    folderPathBox.Text = path;

                    Next_File(); // Retrieves the next file.

                    gameNameTBox.Text = openFolderDialog.SafeFolderName;
                    createButton.IsEnabled = true;

                    if (listOfPaths.Count > 1)
                        nextButton.IsEnabled = true;
                }
                else
                    MessageBox.Show("Directory was either empty or invalid. Please try again.", "Directory Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private String currentFilePath;

        /// <summary>
        /// This method will get the next file path, delete it from the list, then use it to then start a new video.
        /// </summary>
        private async void Next_File()
        {
            currentFilePath = listOfPaths[0];
            listOfPaths.RemoveAt(0);

            if (listOfPaths.Count == 0)
                nextButton.IsEnabled = false;

            // Sets up string for getting the duration from ffmpeg
            String durationInfo = "ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"" + currentFilePath + "\"";
            String result = RunCommand("cmd.exe", @"/K " + durationInfo); // Method call to get the ACTUAL duration of the video.

            Debug.WriteLine("Total Time in Seconds." + result + "    KEEP THIS RUNNING UNTIL IT CRASHES AGAIN. I WANT TO SEE IF THE RESULT IS WHAT CAUSES THE CRASH");
            TotalTime = new TimeSpan(0, 0, 0, 0, (int)(double.Parse(result) * 1000) - 100); // Arbitrary -100 to stop the program from having the media prematurely end and causing a crap ton of issues. More about this issue at the top of the project.
            // TOTALTIME crashed here for some reason like once, but then not again, not really sure what happened.

            String fileName = currentFilePath.Substring(currentFilePath.LastIndexOf('\\') + 1);
            this.Title = "Video Trimmer | " + fileName;
            fileNameTBox.Text = fileName;
            newFileNameTBox.Text = "";

            dateTBox.Text = String.Format("{0:yyyy.MM.dd}", File.GetCreationTime(currentFilePath)); // formats a DateTime object to format I want. and yess, the y M d capitalization matters.
            remainingLabel.Content = "Remaining Files: " + listOfPaths.Count;
            mediaElement.Source = new Uri(currentFilePath);
            settingsControl.Visibility = Visibility.Collapsed;
            playPauseToggle.IsChecked = true;


            // I'm not entirely sure if this code is necessary anymore since I have a wait in the Runcommand area which I believe basically counts for this area now as well, so I don't think I'll
            //      run into anymore issues, but I will keep it here for now, and if at any point the program crashes around this area while I'm clipping, I will reinstate this code.
            /**
             // This waits .25 of a second to allow everything to load in. Not doing this sometimes broke the auto start playing. I should probably take a 
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            timer.Start();
            timer.Tick += (sender, args) =>
            {
                timer.Stop();
                playPauseToggle.IsChecked = true; // This will cause a Play.
            };
             */
        }





        private int totalFrames;

        /// <summary>
        /// The method will take current parameters and create a new file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Create_Click(object sender, RoutedEventArgs e)
        {
            String newVideo = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + gameNameTBox.Text + " " + dateTBox.Text + " - " + newFileNameTBox.Text.Trim() + ".mp4";

            if (!File.Exists(newVideo))
            {
                if ((bool)cBox.IsChecked)
                {
                    encodingProgress.Value = 0;
                    frameCountBlock.Text = "Frames: 0 / 0";
                    encodingPercentage.Text = "0%";
                    encodingProgressGrid.Visibility = Visibility.Visible;
                }

                
                
                // This is the start of a Process to calculate TotalFrames in a video.
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var sb = new StringBuilder();
                Process process;

                using (process = new Process())
                {
                    startInfo.Arguments = "/C ffprobe -v error -select_streams v:0 -show_entries stream=avg_frame_rate -of csv=p=0 \"" + currentFilePath + "\"";

                    process.StartInfo = startInfo;
                    process.EnableRaisingEvents = true;
                    process.OutputDataReceived += (sender, eventArgs) => { sb.AppendLine(eventArgs.Data); };
                    process.ErrorDataReceived += (sender, eventArgs) => { sb.AppendLine(eventArgs.Data); };
                    process.Exited += async (sender, eventArgs) =>
                    {
                        String finalString = sb.ToString().Trim();
                        int divisionIndex = finalString.IndexOf("/");
                        double frameRate = double.Parse(finalString.Substring(0, divisionIndex)) / double.Parse(finalString.Substring(divisionIndex + 1));

                        await Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            double minutes = double.Parse(clipDurationBlock.Text.Substring(0, 2)); // Hardcoded minute and second values because they both can only go up to 59 on a timer like this one.
                            double seconds = double.Parse(clipDurationBlock.Text.Substring(3));
                            totalFrames = (int)((minutes * 60.0 + seconds) * frameRate);

                            encodingProgress.Minimum = 0;
                            encodingProgress.Maximum = totalFrames;
                        }));
                    };

                    if (process.Start())
                    {
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        await process.WaitForExitAsync();
                    }
                }


                // This is the start of a Process to create a new video file.
                startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                sb = new StringBuilder();

                using (process = new Process())
                {
                    if ((bool)cBox.IsChecked) // true if we are reencoding the video
                    {
                        //original
                        startInfo.Arguments = "/C ffmpeg -i \"" + currentFilePath + "\" -ss 00:" + GetTimerText(leftSlider.Value) + " -to 00:" + GetTimerText(rightSlider.Value) + " -fps_mode vfr -c:v libx264 -c:a copy \"" + newVideo + "\"";
                        process.StartInfo = startInfo;
                        process.EnableRaisingEvents = true;
                        process.OutputDataReceived += (sender, eventArgs) => { sb.AppendLine(eventArgs.Data); };
                        process.ErrorDataReceived += async (sender, eventArgs) =>
                        {
                            // When re-encoding video using ffmpeg, it sends the information to Error.

                            sb.AppendLine(eventArgs.Data); //For some reason some ffmpeg commands prints in the ErrorOutput section, not entirely sure why.

                            if (eventArgs.Data != null && eventArgs.Data.StartsWith("frame="))
                            {
                                String framesText = eventArgs.Data.Substring(0, eventArgs.Data.IndexOf("fps") - 1);
                                double numOfCompleteFrames = double.Parse(framesText.Substring(6).Trim()); // this isolates the frames to just a number

                                await Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                                {
                                    if (numOfCompleteFrames > totalFrames)
                                    {
                                        encodingProgress.Value = totalFrames;
                                        frameCountBlock.Text = "Frames: " + totalFrames + " / " + totalFrames;
                                        encodingPercentage.Text = "100%";
                                    }
                                    else
                                    {
                                        encodingProgress.Value = numOfCompleteFrames;
                                        frameCountBlock.Text = "Frames: " + numOfCompleteFrames + " / " + totalFrames;
                                        encodingPercentage.Text = (numOfCompleteFrames / totalFrames * 100).ToString("0.0") + "%";
                                    }
                                }));
                            }

                        };

                        process.Exited += async (sender, eventArgs) =>
                        {
                            Task.Delay(1000).Wait(); // Leaving a delay here so user can see that the task made it to 100% before disappearing.

                            await Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                            {
                                encodingProgressGrid.Visibility = Visibility.Collapsed;
                            }));
                        };
                        
                        if (process.Start())
                        {
                            process.BeginOutputReadLine();
                            process.BeginErrorReadLine();
                            await process.WaitForExitAsync(); // tried bunch of fixes to stop deadlock from happening. happened regardless, so now I just loop code until we get a value returned.
                        }

                    }
                    else // Will probably end up changing how this one works as well, but not sure yet. need to make it more user friendly yay
                    {
                        startInfo.Arguments = "/C ffmpeg -i \"" + currentFilePath + "\" -ss 00:" + GetTimerText(leftSlider.Value) + " -to 00:" + GetTimerText(rightSlider.Value) + " -c:v copy -c:a copy \"" + newVideo + "\"";
                        process.StartInfo = startInfo;
                        process.Start();
                    }
                }

            }
            else
                MessageBox.Show("File Already Exists. Please Change the name or delete the original.", "Name Conflict", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// This method goes to the next file in the list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Next_Click(object sender, RoutedEventArgs e)
        {
            Next_File();
        }


        private static readonly Regex _regex = new Regex("[^0-9]+"); // regex that disallows anything that isn't a numeral
        /// <summary>
        /// This Event handler checks and denies any character that isn't a number
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = _regex.IsMatch(e.Text);
        }


        static string regexSearch = new string(System.IO.Path.GetInvalidFileNameChars()) + new string(System.IO.Path.GetInvalidPathChars()); // Text that includes all chars for invalid file/folder names
        private static readonly Regex _regexInvalidFileChar = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch))); //regex that disallows invalid file and folder name chars
        /// <summary>
        /// This Event handler checks and denies invalid characters for file and folder names.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewFileNameTBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = _regexInvalidFileChar.IsMatch(e.Text);
        }

        /// <summary>
        /// This Event handler checks and denies cut, copy, and paste commands so invalid text can not be entered
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PreviewCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Cut || e.Command == ApplicationCommands.Copy || e.Command == ApplicationCommands.Paste)
                e.Handled = true;
        }





        // EVERYTHING PAST THIS POINT WILL BE MEDIAELEMENT CONTROLS RELATED. SPACE IS HERE TO MORE EASILY SEE WHEN SPEED SCROLLING.






        /// <summary>
        /// This method runs when the ToggleButton is CHECKED and will start playing the video.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine(mainSlider.Value + " - " + (int)mainSlider.SelectionEnd + " <= 1");

            // original. I am fixing this up a bit with the code below but not sure if it will always work so I want to run it for a bit.
            //if (Math.Abs(mainSlider.Value - (int)mainSlider.SelectionEnd) <= 0) // if the thumb is basically on top of the right thumb, it needs to start from the beginning again.
            //    mediaElement.Position = new TimeSpan(0, 0, 0, 0, (int)mainSlider.SelectionStart);

            if ((int)mainSlider.SelectionEnd - mainSlider.Value == 0) // if the thumb is basically on top of the right thumb, it needs to start from the beginning again.
                mediaElement.Position = new TimeSpan(0, 0, 0, 0, (int)mainSlider.SelectionStart);

            if ((bool)volumeToggleButton.IsChecked)
                mediaElement.IsMuted = true;
            else
                mediaElement.IsMuted = false;

            mediaElement.Play();
            timerVideoTime.Start();
            isPlaying = true;
        }

        /// <summary>
        /// This method runs when the ToggleButton is UNCHECKED and will pause the video.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine("Pause Click");

            if (timerVideoTime != null)
                timerVideoTime.Stop();

            mediaElement.IsMuted = true; // new code.
            mediaElement.Pause();
            isPlaying = false;
        }





        /// <summary>
        /// The method will run once mediaElement opens. This sets a couple of important variables that get used throughout the program.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Element_MediaOpened(object sender, EventArgs e)
        {
            String fullDuration = GetTimerText(TotalTime.TotalMilliseconds);

            totalTimeBlock.Text = fullDuration;
            clipDurationBlock.Text = fullDuration;

            // This is resetting the thumbs positions after a file change as well as setting new max position values in the event the video has a different length
            leftSlider.Maximum = TotalTime.TotalMilliseconds;
            leftSlider.Value = 0;
            rightSlider.Maximum = TotalTime.TotalMilliseconds;
            rightSlider.Value = TotalTime.TotalMilliseconds;
            mainSlider.Maximum = TotalTime.TotalMilliseconds;
            mainSlider.Value = 0;
        }


        private TimeSpan TotalTime; // Duration of the current video
        private DispatcherTimer timerVideoTime; // A timer specifically for when the video is playing.
        private DispatcherTimer timerScrubbing; // A timer that runs when user is "dragging" the mainSlider's thumb.

        void Timer_Video_Tick(object sender, EventArgs e)
        {
            //Debug.WriteLine("Timer_Video_Tick");

            // I might want to take a look at how this if statement is running again because I feel like I might of changed how
            //      timervideo tick is used enough that the if statement may or may not be necessary anymore.

            if (mediaElement.NaturalDuration.HasTimeSpan && TotalTime.TotalSeconds > 0)
                mainSlider.Value = mediaElement.Position.TotalMilliseconds;
        }

        void Timer_Scrubbing_Tick(object sender, EventArgs e)
        {
            //Debug.WriteLine("Scrubbing_Tick");

            Set_Media_Position((int)mainSlider.Value);
        }


        


        /// <summary>
        /// This method takes in a value that will become the new mainSlider.Value value.
        /// </summary>
        /// <param name="newMainSliderValue"></param>
        public void Set_Media_Position(int newMainSliderValue)
        {
            //Debug.WriteLine("Set_Media_Position");

            mediaElement.Position = new TimeSpan(0, 0, 0, 0, newMainSliderValue);
            mainSlider.Value = newMainSliderValue; // placed here to ensure that the thumb doesn't move back and forth visually even if values are off by .0001
        }


        /// <summary>
        /// This method runs once when a drag is started. It pauses all media, mutes it (so scrubbing doesn't make any weird noises), and calls another method to start a timer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainSlider_DragStarted(object sender, DragStartedEventArgs e)
        {
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
        private void MainSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            timerVideoTime.Stop();
            mediaElement.Pause();
            mediaElement.IsMuted = true;

            if (thumb.IsMouseOver) // True if thumb drag is going to happen
            {
                mouseThumbPointDifference = Mouse.GetPosition(thumb).X;
                // no capture mouse or timer scrubbing start here as that is handled in the drag start event.
            }
            else // this is the new dragging system as originally ran when scrubbing.
            {
                mouseThumbPointDifference = -1;
                canRun = true; // This is here because it will allow the mousemove area to start working immediately.

                thumb.CaptureMouse();
            }
            
        }


        /// <summary>
        /// This method runs when the slider unclicked, so that the capture of the control can be released.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            canRun = false;

            timerScrubbing.Stop(); // got an error here saying that it was null? not sure how that is possible.
            thumb.ReleaseMouseCapture();

            Set_Media_Position((int)mainSlider.Value); // This is here as to immediately set the position before the video starts again so the video starts in the correct position.

            if (isPlaying)
            {
                if ((bool)volumeToggleButton.IsChecked)
                    mediaElement.IsMuted = true;
                else
                    mediaElement.IsMuted = false;

                timerVideoTime.Start();
                mediaElement.Play();
            }
        }


        
        private bool canRun = false;
        private double mouseThumbPointDifference = 0;
        private System.Windows.Point lastMousePoint = new System.Windows.Point(0, 0);


        // currently for testing, but might need
        private async void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            // the mouse pointer check is for when you are holding the thumb, but not moving the mouse. for some reason this event fires when hovering over the slider with the mouse, so this is to stop unnecessary values being set.
            if (thumb.IsMouseCaptured && canRun && lastMousePoint.X != e.GetPosition(this).X) // true if mouse is captured on the drag
            {
                lastMousePoint.X = e.GetPosition(this).X;

                if (timerScrubbing.IsEnabled)
                    timerScrubbing.Stop();
                timerScrubbing.Start();

                double sliderWidth = mainSlider.ActualWidth, halfOfThumbWidth = thumb.ActualWidth / 2.0, newMousePointX;

                if (mouseThumbPointDifference > 0 && mouseThumbPointDifference < halfOfThumbWidth)
                    newMousePointX = Mouse.GetPosition(mainSlider).X + (halfOfThumbWidth - mouseThumbPointDifference);
                else if (mouseThumbPointDifference > halfOfThumbWidth) // doesn't need a check on upper limit since the mouseThumbPointDifference can only go as high as the thumb's ActualWidth value.
                    newMousePointX = Mouse.GetPosition(mainSlider).X - (mouseThumbPointDifference - halfOfThumbWidth);
                else // Runs if mouseThumbPointDifference is not necessary
                    newMousePointX = Mouse.GetPosition(mainSlider).X;

                double percentageOfThumbWidth = halfOfThumbWidth / sliderWidth;

                // In short terms, without skewed, the cursor would slowly drift from left to right when draging mainThumb left to right because the width of the thumb is not calculated properly as part of the 
                // slider, causing the drift. This variable calculates the width of the thumb into the equation and offsets the main value so that the mouse will stay in the same spot on the thumb the entire drag.
                double skewedWidthPercentage = (1 - (newMousePointX / (sliderWidth / 2))) * percentageOfThumbWidth;
                double initialPercentage = newMousePointX / sliderWidth;
                double finalPercentage = initialPercentage - skewedWidthPercentage; // this is the final percentage with the skew to make sure the mouse stays in the same place.

                if (finalPercentage > 1.0) // this if statement makes sure the value isn't being set over the minimum and maximums
                    finalPercentage = 1;
                else if (finalPercentage < 0.0)
                    finalPercentage = 0;

                mainSlider.Value = mainSlider.Maximum * finalPercentage;

            }

        }





        private void LeftRightSlider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            timerScrubbing.Stop();

            if (!isPlaying)
                Set_Media_Position((int)mainSlider.Value); // wasn't here originally, but realized if I let go quick enough before timer scrubbing ran, the media position wouldn't get updated.

            //Set_Media_Position((int)mainSlider.Value); // wasn't here originally, but realized if I let go quick enough before timer scrubbing ran, the media position wouldn't get updated.
        }


        /// <summary>
        /// This method stops the left and right thumbs from getting within 1 second of each other and also checking if the mainSlider is being passed or not, and will make sure
        ///     the mainSlider thumb will always be ahead of the left thumb.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LeftSlider_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (rightSlider.Value - leftSlider.Value < 1000) // true if the sliders are too close to each other.
                leftSlider.Value = rightSlider.Value - 1000;
            
            if (mainSlider.Value < mainSlider.SelectionStart) // true if mainThumb is to the left of the selectionStart.
            {
                if (timerScrubbing.IsEnabled)
                    timerScrubbing.Stop();
                timerScrubbing.Start();

                playPauseToggle.IsChecked = false;
                //mainSlider.Value = (int)(mainSlider.SelectionStart + 0); // +1 to stop wack stuff from happening. trying to see if I can fix this somehow so I can remove that.
                mainSlider.Value = (int)mainSlider.SelectionStart; // removed the random +1 as I don't think I need it thanks to the way I've coded this now.
                currentTimeBlock.Text = GetTimerText(mainSlider.Value);
            }

            CalculateTrimRange();
        }

        /// <summary>
        /// This method makes sure that the left and right thumbs don't get within 1 second of each other.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RightSlider_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (rightSlider.Value - leftSlider.Value < 1000)
                rightSlider.Value = leftSlider.Value + 1000;

            if (mainSlider.Value > mainSlider.SelectionEnd) // true if mainThumb is more to the right than the selection
            {
                if (timerScrubbing.IsEnabled)
                    timerScrubbing.Stop();
                timerScrubbing.Start();


                playPauseToggle.IsChecked = false;

                mainValueChanged_AllowedToRun = false;
                //mainSlider.Value = (int)mainSlider.SelectionEnd - 10; // was originally a - 10, but trying to see if I can just remove it entirely.
                mainSlider.Value = (int)mainSlider.SelectionEnd; // trying to see if I can remove this random minus now.
                mainValueChanged_AllowedToRun = true;

                currentTimeBlock.Text = GetTimerText(mainSlider.Value);
            }

            CalculateTrimRange();
        }

        /// <summary>
        /// This method calculates the range of the Trim and displays that for a textblock.
        /// </summary>
        private void CalculateTrimRange()
        {
            clipDurationBlock.Text = GetTimerText(rightSlider.Value - leftSlider.Value);
        }


        private bool mainValueChanged_AllowedToRun = true;

        private void MainSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            
            if (!mainValueChanged_AllowedToRun)
            {
                //Debug.WriteLine("Not Allowed to Run");
                return;
            }
            

            //Debug.WriteLine("MainSlider Value Changed Ran" + mainSlider.Value + "  " + mainSlider.SelectionEnd);

            // Either statement will only run if the value was changed via video playback, any other mainSlider.Value set will not get this code to run, as it is handled prior.
            
            if (mainSlider.Value > mainSlider.SelectionEnd) // This stops main thumb from exceeding right Thumb during video playback
            {
                mainSlider.Value = mainSlider.SelectionEnd;
                return; // Stops current iteration from running since the value was set in here, it will run again anyway. This is to stop unnecessary calculations.
            }
            else if (mainSlider.Value < mainSlider.SelectionStart) // This stops main thumb from exceeding left Thumb during video playback
            {
                //mainSlider.Value = mainSlider.SelectionStart + 1; // +1 here to avoid issues of a double pause when attempting to start again.
                mainSlider.Value = mainSlider.SelectionStart; // Trying to see if removing +1 is fine.
                return; // Stops current iteration from running since the value was set in here, it will run again anyway. This is to stop unnecessary calculations.
            }
            
            int mediaINT = (int)mediaElement.Position.TotalMilliseconds; // based off the media itself rather than the thumb location as in this instance, it needs to be more accurate.
            int selectionEndINT = (int)mainSlider.SelectionEnd;

            // this if statement never runs on the times when the media ends itself.

            // true if the video hits the selectionEnd area.
            if (mediaINT > selectionEndINT) // && !rightThumb.IsDragging) // originally here, but I might not need it now.
            {
                //Debug.WriteLine("Ran in MainSlider_ValueChanged " + (mediaINT > mainSlider.Maximum) + "   " + mediaINT + " > " + mainSlider.Maximum);

                //Debug.WriteLine(mediaINT + " > " + selectionEndINT + " | " + mainSlider.SelectionEnd);

                playPauseToggle.IsChecked = false;
                Set_Media_Position(selectionEndINT);
            }

            currentTimeBlock.Text = GetTimerText(mainSlider.Value);
        }

        /// <summary>
        /// This method takes a time in milliseconds, and it will return a formatted time in the shape of '00:00.00' for TextBlocks
        /// </summary>
        /// <param name="initialTime"></param>
        /// <returns></returns>
        private String GetTimerText(double initialTime)
        {
            int minutes = 0, seconds = 0, milliseconds = 0;

            while (initialTime >= 60000) // 1,000 = 1 second so 60,000 = 60 seconds = 1 minute
            {
                minutes++;
                initialTime -= 60000;
            }

            while (initialTime >= 1000) // 1,000 = 1 second
            {
                seconds++;
                initialTime -= 1000;
            }

            while (initialTime >= 10)
            {
                milliseconds++;
                initialTime -= 10;
            }

            return minutes.ToString("00") + ":" + seconds.ToString("00") + "." + milliseconds.ToString("00");
        }

        
        private void ChangeMediaVolume(object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            mediaElement.Volume = (double)volumeSlider.Value;
        }
        

        /// <summary>
        /// This method will play an animation if the video was paused or played by clicking on the video itself.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MediaElement_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            SetAndStartAnimation();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (settingsControl.Visibility != Visibility.Visible && e.Key == Key.Space)
                SetAndStartAnimation();
        }

        private void SetAndStartAnimation()
        {
            playPauseGrid.Visibility = Visibility.Visible; // though only needed once, this should be fine.

            if (isPlaying)
            {
                playPauseToggle.IsChecked = false;
                playBox.Visibility = Visibility.Hidden;
                pauseBox.Visibility = Visibility.Visible;
            }
            else
            {
                playPauseToggle.IsChecked = true;
                playBox.Visibility = Visibility.Visible;
                pauseBox.Visibility = Visibility.Hidden;
            }

            // Animation for fade out
            var fade = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                FillBehavior = FillBehavior.HoldEnd,
                Duration = new Duration(TimeSpan.FromSeconds(0.5))
            };

            //Animation for growing
            var grow = new DoubleAnimation
            {
                From = 100,
                To = 150,
                FillBehavior = FillBehavior.Stop,
                Duration = new Duration(TimeSpan.FromSeconds(0.5))
            };

            playPauseGrid.BeginAnimation(OpacityProperty, fade, HandoffBehavior.SnapshotAndReplace);
            playPauseGrid.BeginAnimation(HeightProperty, grow, HandoffBehavior.SnapshotAndReplace);
            playPauseGrid.BeginAnimation(WidthProperty, grow, HandoffBehavior.SnapshotAndReplace);
        }





        /// <summary>
        /// This method when ran reveals the settings grid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            settingsControl.Visibility = Visibility.Visible;
            playPauseToggle.IsChecked = false;
        }

        /// <summary>
        /// This method will collapse the settings grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Settings_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (mediaElement.Source != null)
                settingsControl.Visibility = Visibility.Collapsed;
        }


        private void Volume_Checked(object sender, RoutedEventArgs e)
        {
            mediaElement.IsMuted = true;
        }

        private void Volume_Unchecked(object sender, RoutedEventArgs e)
        {
            mediaElement.IsMuted = false;
        }

        
        // This code is kind of for testing, kind of not. Was trying to see if I could remove the arbitrary -100 from the timeline and have shit not break on me. Unfortunately, my code is too spaghetti
        //  to get everything running consistently enough cause for the most part everything works, but sometimes stuff breaks HARD. So I'm leaving this in for now in the event I might try my luck again,
        //  but for now, I'm giving it a rest.
        private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            // I'm going to guess when a stop occurs, it sets the mediaElement.Position to its natural duration, which is wrong.
            
            
            Debug.WriteLine("Media did infact, end. Here is Position of Mediaelement: " + mediaElement.Position.TotalMilliseconds + "   " + mediaElement.NaturalDuration.TimeSpan.TotalMilliseconds);

            playPauseToggle.IsChecked = false;
            mediaElement.Stop();

            // from here, I want to set position to the maximum value
            Set_Media_Position((int)mainSlider.SelectionEnd);
            
        }








        // everything past this point is AI crap that I'm testing.

        /**
        private Thumb startThumb;
        private Thumb endThumb;
        private Track track;




        private void CustomSliderControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Get the thumbs after the control has loaded
            startThumb = CustomSlider.Template.FindName("PART_StartThumb", CustomSlider) as Thumb;
            endThumb = CustomSlider.Template.FindName("PART_EndThumb", CustomSlider) as Thumb;
            track = CustomSlider.Template.FindName("PART_Track", CustomSlider) as Track;
        }

        // Event handler for the start thumb's drag operation
        private void StartThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newSelectionStart = CustomSlider.SelectionStart + e.HorizontalChange;
            newSelectionStart = ClampValue(newSelectionStart, CustomSlider.Minimum, CustomSlider.SelectionEnd);
            CustomSlider.SelectionStart = newSelectionStart;

            // Update the position of the start thumb
            UpdateThumbPosition(startThumb, newSelectionStart);
        }

        // Event handler for the end thumb's drag operation
        private void EndThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newSelectionEnd = CustomSlider.SelectionEnd + e.HorizontalChange;
            newSelectionEnd = ClampValue(newSelectionEnd, CustomSlider.SelectionStart, CustomSlider.Maximum);
            CustomSlider.SelectionEnd = newSelectionEnd;

            // Update the position of the end thumb
            UpdateThumbPosition(endThumb, newSelectionEnd);
        }

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