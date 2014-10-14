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

namespace AntPlusRacer.TrackRecords
{
    /// <summary>
    /// Interaction logic for RecordDisplayPanel.xaml
    /// </summary>
    public partial class RecordDisplayPanel : UserControl
    {
        System.Timers.Timer pageCycleTimer = new System.Timers.Timer(7000);
        int pageNum;
        List<TrackRecordList> pages;
        List<BitmapImage> icons;

        DoubleAnimation fadeOutAnim = new DoubleAnimation(0.1, new Duration(new TimeSpan(0, 0, 1)));
        DoubleAnimation fadeInAnim = new DoubleAnimation(1.0, new Duration(new TimeSpan(0, 0, 1)));

        public RecordDisplayPanel()
        {
            InitializeComponent();

            //Only display races in use
            pages = new List<TrackRecordList>();
            icons = new List<BitmapImage>();
            
            AntPlusRacerConfig cfg = AntPlusRacerConfig.getInstance();
            RecordDatabase db = RecordDatabase.getInstance();
            
            if (cfg.enabledRaceTracks.Count == 0)
            {
                Viewbox vb = new Viewbox();
                vb.Child = new Label() { Content = "No races selected for use. Exit and add races to 'UsingRaceTracks' in " + AntPlusRacerConfig.DatabaseName + ".db.xml" };
                this.Content = vb;
                return;
            }

            //Get all tracks with records
            foreach (AntPlusRacerConfig.RaceTrack i in cfg.enabledRaceTracks)
            {
                TrackRecordList recordList = db.getTrackRecordList(i.sportType, i.distance);
                if(recordList.trackRecords.Count > 0)
                    pages.Add(recordList);
            }  

            if (pages.Count == 0)
            {
                Viewbox vb = new Viewbox();
                vb.Child = new Label() { Content = "No records exist in database. After you complete races and save some records, then reload this page to view records." };
                this.Content = vb;
                return;
            }

            foreach (TrackRecordList i in pages)
            {
                icons.Add(RacerInfoPanel.getRaceDisplayBitmap(i.sportType));
            }

            pageNum = -1;
            nextPage();

            if (pages.Count > 1)
            {
                pageCycleTimer.Elapsed += new System.Timers.ElapsedEventHandler(pageCycleTimer_Elapsed);
                pageCycleTimer.Start();
            }
        }

        public RecordDisplayPanel(TrackRecordList recordsToShow, List<int> indicesOfNew)
        {
            InitializeComponent();

            Label_RecordTitle.Content = String.Format("{0:0}m {1}", recordsToShow.trackDistance, recordsToShow.sportType);

            listBox_Records.Items.Clear();

            listBox_Records.Items.Add("       Name                 Time (s) ");
            listBox_Records.Items.Add("-------------------------------------");

            listBox_Records.SelectedIndex = 0;


            int max = 5;

            if (indicesOfNew != null)
            {
                foreach (int i in indicesOfNew)
                {
                    if (i > 4)
                        --max;
                }
            }

            for (int i = 0; i < recordsToShow.trackRecords.Count && i < max; ++i)
                listBox_Records.Items.Add(formatRecord(recordsToShow.trackRecords, i));

            if (indicesOfNew != null)
            {
                foreach (int i in indicesOfNew)
                    if (i > 4)
                        listBox_Records.Items.Add(formatRecord(recordsToShow.trackRecords, i));
            }
        }

        void pageCycleTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.Invoke((Action)fadeOut);
            System.Threading.Thread.Sleep(1000);
            Dispatcher.Invoke((Action)nextPage);
        }

        void fadeOut()
        {
            Label_RecordTitle.BeginAnimation(Label.OpacityProperty, fadeOutAnim);
            Image_IconDisplayLeft.BeginAnimation(Image.OpacityProperty, fadeOutAnim);
            Image_IconDisplayRight.BeginAnimation(Image.OpacityProperty, fadeOutAnim);
            listBox_Records.BeginAnimation(ListBox.OpacityProperty, fadeOutAnim);
        }

        void fadeIn()
        {
            Label_RecordTitle.BeginAnimation(Label.OpacityProperty, fadeInAnim);
            Image_IconDisplayLeft.BeginAnimation(Image.OpacityProperty, fadeInAnim);
            Image_IconDisplayRight.BeginAnimation(Image.OpacityProperty, fadeInAnim);
            listBox_Records.BeginAnimation(ListBox.OpacityProperty, fadeInAnim);
        }

        void nextPage()
        {
            ++pageNum;
            if (pageNum >= pages.Count)
                pageNum = 0;

            Label_RecordTitle.Content = String.Format("{0:0}m {1} - Top 5", pages[pageNum].trackDistance, pages[pageNum].sportType);
            Image_IconDisplayLeft.Source = icons[pageNum];
            Image_IconDisplayRight.Source = icons[pageNum];

            listBox_Records.Items.Clear();

            listBox_Records.Items.Add("       Name                 Time (s) ");
            listBox_Records.Items.Add("-------------------------------------");

            listBox_Records.SelectedIndex = 0;

            List<RecordData> recordsToShow = pages[pageNum].trackRecords;
            for (int i = 0; i < recordsToShow.Count && i < 5; ++i)
                listBox_Records.Items.Add(formatRecord(recordsToShow, i));

            fadeIn();
        }

        private string formatRecord(List<RecordData> recordsToShow, int recordIndex)    //TODO should fix the formatting here to use a real table or something, and print better details
        {
            String name = recordsToShow[recordIndex].FirstName + " " + recordsToShow[recordIndex].LastName;
            if (name.Length > 20)
            {
                //Check if we can abbreviate a first name
                int indexOfSpace = name.IndexOf(' ');
                if (indexOfSpace >= 0)
                    name = name[0] + ". " + name.Substring(indexOfSpace+1);

                //Just use elipsis if still too large
                if (name.Length > 20)
                    name = name.Remove(17) + "...";
            }
            return String.Format("{0,-3}-{1,-20} - {2,10:0.0}", recordIndex+1, name, recordsToShow[recordIndex].recordValue);
        }

        private void listBox_Records_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //Primitive attempt to not allow user to change selection
            e.Handled = true;
        }
    }
}
