/*
This software is subject to the license described in the license.txt file included with this software distribution.You may not use this file except in compliance with this license. 
Copyright © Dynastream Innovations Inc. 2012
All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AntPlusRacer.TrackRecords
{
    public class RecordDatabase
    {
        public const string DatabaseName = "AntPlusRacerRecords";

        static RecordDatabase instance;

        public List<TrackRecordList> recordBook = new List<TrackRecordList>();

        public static RecordDatabase getInstance()
        {
            if (instance == null)
                instance = XmlDatabaser.loadDatabase<RecordDatabase>(DatabaseName);
            return instance;
        }

        public TrackRecordList getTrackRecordList(racerSportType sportType, double distance)
        {
            foreach (TrackRecordList i in recordBook)
                if (i.sportType == sportType && i.trackDistance == distance)
                    return i;

            //Create a new list if there are no matches
            TrackRecordList newRecordList = new TrackRecordList();
            newRecordList.sportType = sportType;
            newRecordList.trackDistance = distance;
            newRecordList.trackRecords = new List<RecordData>();

            recordBook.Add(newRecordList);

            return newRecordList;
        }

        
    }
}
