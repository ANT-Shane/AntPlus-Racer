/*
This software is subject to the license described in the license.txt file included with this software distribution.You may not use this file except in compliance with this license. 
Copyright © Dynastream Innovations Inc. 2012
All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Media.Animation;

namespace AntPlusRacer
{
    /// <summary>
    /// Interaction logic for RacerInfoPanel.xaml
    /// </summary>
    public partial class RacerInfoPanel : UserControl
    {
        static Object listLock = new object();

        static List<BitmapImage> cyclingPics = null;
        static List<BitmapImage> rowingPics = null;
        static List<BitmapImage> runningPics = null;
        static List<BitmapImage> skiingPics = null;

        List<BitmapImage> curAnimPics;
        Image animationImage;
        Storyboard cadenceAnimation;
        double cadenceMovingAverage;
        int curPicIndex;

        public RacerInfoPanel(racerSportType raceType, double targetValue)
        {
            InitializeComponent();

            loadAnimationPics();

            progressBar1.Maximum = targetValue;
            updateProgressDisplay(0, 0);

            switch (raceType)
            {
                case racerSportType.Biking:
                    curAnimPics = cyclingPics;
                    Label_Cadence.Content = "0 rpm";
                    Border_PowerLabelHolder.Visibility = System.Windows.Visibility.Hidden;
                    Grid.SetColumnSpan(Border_ProgressLabelHolder, 3);
                    break;
                case racerSportType.Running:
                    curAnimPics = runningPics;
                    Label_Cadence.Content = "0 spm";
                    Border_PowerLabelHolder.Visibility = System.Windows.Visibility.Hidden;
                    Grid.SetColumnSpan(Border_ProgressLabelHolder, 3);
                    break;
                case racerSportType.Rowing:
                    curAnimPics = rowingPics;
                    Label_Cadence.Content = "0 spm";
                    break;
                case racerSportType.Skiing:
                    curAnimPics = skiingPics;
                    Label_Cadence.Content = "0 spm";
                    break;
            }

            initAnimation();
        }

        public static BitmapImage getRaceDisplayBitmap(racerSportType sportType)
        {
            //TODO: qc Should share all the bitmap images as static resources for animations and these displays

            BitmapImage bmp = new BitmapImage();
            bmp.BeginInit();

            switch (sportType)
            {
                case racerSportType.Biking:
                    bmp.UriSource = new Uri("./Animations/Cycling/cycling10.gif", UriKind.Relative);
                    break;
                case racerSportType.Running:
                    bmp.StreamSource = Application.GetResourceStream(new Uri("pack://application:,,,/Images/Race Icons/runner_icon.png")).Stream;
                    break;
                case racerSportType.Rowing:
                    bmp.UriSource = new Uri("./Animations/Rowing/rowing_agif15.gif", UriKind.Relative);
                    break;
                case racerSportType.Skiing:
                    bmp.StreamSource = Application.GetResourceStream(new Uri("pack://application:,,,/Images/Race Icons/skier_icon.png")).Stream;
                    break;
                case racerSportType.Unknown:
                    bmp.StreamSource = Application.GetResourceStream(new Uri("pack://application:,,,/Images/ANT+ logo w_TM.png")).Stream;
                    break;
                default:
                    return null;
            }
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();

            return bmp;
        }

        void initAnimation()
        {
            //Set up the graphic
            curPicIndex = 0;
            animationImage = new Image();
            animationImage.Source = curAnimPics[curPicIndex];
            animationImage.Stretch = Stretch.Uniform;
            animationImage.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            animationImage.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            Grid.SetRow(animationImage, 1);
            Grid.SetColumn(animationImage, 1);

            //Create the Animation
            ObjectAnimationUsingKeyFrames cadenceObjAnimation = new ObjectAnimationUsingKeyFrames();
            cadenceObjAnimation.Duration = new Duration(new TimeSpan(0, 0, 1));
            cadenceObjAnimation.FillBehavior = FillBehavior.Stop;
            cadenceObjAnimation.RepeatBehavior = RepeatBehavior.Forever;
            Storyboard.SetTarget(cadenceObjAnimation, animationImage);
            Storyboard.SetTargetProperty(cadenceObjAnimation, new PropertyPath(Image.SourceProperty));

            //Keyframes, first is next frame, ending with beginning frame
            for(int i=1; i<curAnimPics.Count; ++i)
                cadenceObjAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(curAnimPics[i]));
            cadenceObjAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(curAnimPics[0]));

            //Create the storyboard and begin the animation            
            cadenceAnimation = new Storyboard();
            cadenceAnimation.Children.Add(cadenceObjAnimation);
            cadenceAnimation.Begin(this, true);
            updateCadence(0);
            cadenceMovingAverage = 0;

            Grid_AnimationDisplay.Children.Add(animationImage);
        }

        public void makeAnnouncement(string announcement)
        {
            Dispatcher.Invoke(new Action(() =>
                {
                    if (String.IsNullOrWhiteSpace(announcement))
                    {
                        Border_AnnouncementFade.Visibility = System.Windows.Visibility.Hidden;
                    }
                    else
                    {
                        Border_AnnouncementFade.Visibility = System.Windows.Visibility.Visible;
                    }
                    Label_LoudAnnouncement.Content = announcement;
                }));
        }

        public void updateProgressDisplay(double curValue, int msToFinish)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (msToFinish != 0)
                {
                    DoubleAnimation da = new DoubleAnimation(progressBar1.Maximum, new Duration(new TimeSpan(0, 0, 0, 0, msToFinish)));
                    //if(Math.Abs( (progressBar1.Value - curValue) / curValue) < 0.15)
                    //    da = new DoubleAnimation(progressBar1.Value, progressBar1.Maximum, new Duration(new TimeSpan(0, 0, 0, 0, msToFinish)));
                    //else //If the difference between progressBar and actual distance becomes too seperated, explicitly jump to the actual value
                    //    da = new DoubleAnimation(curValue, progressBar1.Maximum, new Duration(new TimeSpan(0, 0, 0, 0, msToFinish)));
                    progressBar1.BeginAnimation(ProgressBar.ValueProperty, da);
                }
                else
                {
                    progressBar1.BeginAnimation(ProgressBar.ValueProperty, null);
                    progressBar1.Value = curValue;
                }
                Label_Progress.Content = String.Format("{0:0} / {1:0.#}m", curValue, progressBar1.Maximum);
            }));
        }

        public void moveDistance(double screenFraction, double scaleFraction)
        {
            //System.Console.Out.WriteLine("{0:0.00}, {1:0.00}", screenFraction, scaleFraction);
            //Racers maintain good image visibility at a range of 4-24
            //double scaleLength = 4;
            //if (scaleFraction != 1)
            //    scaleLength = 24 - (20 * scaleFraction);

            double distance = 4 * screenFraction;

            //System.Console.Out.WriteLine(distance + ", " + scaleLength);
            Dispatcher.Invoke(new Action(()=>
                {
                    if (Grid_AnimationDisplay.ActualHeight != 0)    //Don't try and set values if the screen hasn't been drawn
                    {
                        if (Double.IsInfinity(animationImage.MaxHeight))    //Initialize the values if they aren't set
                        {
                            animationImage.MaxHeight = Grid_AnimationDisplay.ActualHeight * scaleFraction;
                            animationImage.MaxWidth = (Grid_AnimationDisplay.ActualWidth / 5) * scaleFraction;
                        }

                        if (animationImage.MaxHeight != Grid_AnimationDisplay.ActualHeight * scaleFraction) //If the scale is already set, don't launch useless animations
                        {
                            DoubleAnimation daH = new DoubleAnimation(Grid_AnimationDisplay.ActualHeight * scaleFraction, new TimeSpan(0, 0, 0, 0, 3000));
                            animationImage.BeginAnimation(Image.MaxHeightProperty, daH);

                            DoubleAnimation daW = new DoubleAnimation((Grid_AnimationDisplay.ActualWidth / 5) * scaleFraction, new TimeSpan(0, 0, 0, 0, 3000));
                            animationImage.BeginAnimation(Image.MaxWidthProperty, daW);
                        }
                    }

                    GridLengthAnimation glaL = new GridLengthAnimation();
                    glaL.From = Col_LeftPush.Width.Value;
                    glaL.To = distance;
                    glaL.Duration = new TimeSpan(0, 0, 0, 0, 1000);
                    Col_LeftPush.BeginAnimation(ColumnDefinition.WidthProperty, glaL);

                    GridLengthAnimation glaR = new GridLengthAnimation();
                    glaR.From = Col_RightFill.Width.Value;
                    glaR.To = 4 - distance;
                    glaR.Duration = new TimeSpan(0, 0, 0, 0, 1000);
                    Col_RightFill.BeginAnimation(ColumnDefinition.WidthProperty, glaR);
                }));
        }

        void updateCadence(double cadence)
        {
            if((Label_Cadence.Content as String).Contains("spm"))
                Label_Cadence.Content = String.Format("{0:0}spm",cadence);
            else
                Label_Cadence.Content = String.Format("{0:0}rpm", cadence);
            if (cadence == 0)
                cadenceMovingAverage = 0;
            else
                cadenceMovingAverage = (cadenceMovingAverage + cadence) / 2;
            cadenceAnimation.SetSpeedRatio(this, cadenceMovingAverage / 60);
        }

        internal void updateStats(DataSources.DataSourcePacket data)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (data.speed_ms < ushort.MaxValue)
                {
                    if(AntPlusRacerConfig.getInstance().displayMph)
                        Label_Speed.Content = String.Format("{0:0.0}mph", data.speed_ms * 2.237);
                    else   //else use real measurement units
                        Label_Speed.Content = String.Format("{0:0.0}kph", data.speed_ms * 3.6);
                }

                if (data.power != ushort.MaxValue)
                    Label_Power.Content = String.Format("{0:0}W", data.power);

                if (data.cadence != -1)
                    updateCadence(data.cadence);
            }));
        }

        void loadAnimationPics()
        {
            lock (listLock)
            {
                if (cyclingPics != null)
                    return;
                    
                cyclingPics = new List<BitmapImage>();
                rowingPics = new List<BitmapImage>();
                runningPics = new List<BitmapImage>();
                skiingPics = new List<BitmapImage>();

                string[] cyclingPicPaths = System.IO.Directory.GetFiles("./Animations/Cycling", "*.gif");
                string[] rowingPicPaths = System.IO.Directory.GetFiles("./Animations/Rowing", "*.gif");
                string[] runningPicPaths = System.IO.Directory.GetFiles("./Animations/Running", "*.png");
                string[] skiingPicPaths = System.IO.Directory.GetFiles("./Animations/Skiing", "*.png");

                foreach (string i in cyclingPicPaths)
                {
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(i, UriKind.Relative);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    cyclingPics.Add(bmp);
                }

                foreach (string i in rowingPicPaths)
                {
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(i, UriKind.Relative);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    rowingPics.Add(bmp);
                }

                foreach (string i in runningPicPaths)
                {
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(i, UriKind.Relative);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    runningPics.Add(bmp);
                }

                foreach (string i in skiingPicPaths)
                {
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(i, UriKind.Relative);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    skiingPics.Add(bmp);
                }
            }
        }

        internal void finishRace()
        {
            cadenceAnimation.Stop(this);
            cadenceAnimation.Remove(this);
            animationImage.Source = null;

            updateProgressDisplay(progressBar1.Maximum, 0);
        }

        public void resetDisplay()
        {
            if (animationImage.Source == null)
                initAnimation();

            makeAnnouncement("");
            moveDistance(0, 1);
            updateProgressDisplay(0, 0);
            updateStats(new DataSources.DataSourcePacket(0, 0, 0, 0, 0));
        }
    }
}
