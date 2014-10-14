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

namespace AntPlusRacer.TrackRecords
{
    /// <summary>
    /// Interaction logic for PostRacePanel.xaml
    /// </summary>
    public partial class PostRacePanel : UserControl
    {
        TrackRecordList recordList;

        RacerDetails racer;
        Action<TrackRecordList, int> finished;

        public PostRacePanel(Action<TrackRecordList, int> finishedDisplay, RacerDetails racerDetails)
        {
            InitializeComponent();
            finished = finishedDisplay;
            racer = racerDetails;
            recordList = RecordDatabase.getInstance().getTrackRecordList(racer.dataSource.getSportType(), racer.targetValue);
            finished = finishedDisplay;            
            
            Label_RacerTag.Content = racer.dataSource.getSourceName() + " - " + racer.finishResult.ToString("0.0");

            int i;
            for(i=0; i<recordList.trackRecords.Count; ++i)
            {
                if (recordList.trackRecords[i].recordValue > racer.finishResult)
                    break;
            }
            Label_ListResultTag.Content = "You placed " + (i + 1) + " out of " + (recordList.trackRecords.Count + 1) + " on the leaderboard";
        }

        private void button_Finished_Click(object sender, RoutedEventArgs e)
        {
            RecordData record = new RecordData();

            String fullName = textBox_FullName.Text.Trim();
            int lastNameSpace = fullName.LastIndexOf(' ');

            if (lastNameSpace > 0)
            {
                record.FirstName = fullName.Remove(lastNameSpace);
                record.LastName = fullName.Substring(lastNameSpace + 1);
            }
            else
            {
                record.FirstName = fullName;
            }
            
            record.DataSourceName = racer.dataSource.getSourceName();
            record.Email = textBox_email.Text.Trim();
            record.PhoneNumber = textBox_phone.Text.Trim();
            record.recordValue = racer.finishResult;

            racer.racerRecordInfo = record;

            if (String.IsNullOrEmpty(racer.racerRecordInfo.FirstName))
                racer.racerRecordInfo.FirstName = "Anonymous";

            int i = saveRacerRecord(recordList, racer.racerRecordInfo);

            finished(recordList, i);
        }

        public static int saveRacerRecord(TrackRecordList list, RecordData racer)
        {
            int ret = list.addRecord(racer);

            RecordDatabase db = RecordDatabase.getInstance();
            String saveError = XmlDatabaser.saveDatabase(RecordDatabase.DatabaseName, db);
            if (saveError != null) //Save succeeded
            {
                MessageBox.Show(saveError, "Ant+ Racer Database Error");
            }

            return ret;
        }
    }
}
