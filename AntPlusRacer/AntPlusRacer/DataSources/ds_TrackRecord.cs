/*
This software is subject to the license described in the license.txt file included with this software distribution.You may not use this file except in compliance with this license. 
Copyright © Dynastream Innovations Inc. 2012
All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AntPlusRacer.DataSources
{
    class ds_TrackRecord: ds_PaceSimulator
    {
        String recordHolderDisplay;
        TrackRecords.RecordData recordHolder;

        public ds_TrackRecord(racerSportType sportType, double trackDistance)
            :base(sportType)
        {
            TrackRecords.RecordDatabase db = TrackRecords.RecordDatabase.getInstance();
            TrackRecords.TrackRecordList recordList = db.getTrackRecordList(sportType, trackDistance);

            if (recordList == null || recordList.trackRecords.Count <= 0)
            {
                initPaceTimer(2); //Default to super slow if there is no track record
                recordHolderDisplay = "No Track Record Exists";
            }
            else
            {
                recordHolder = recordList.trackRecords[0];
                initPaceTimer(trackDistance / recordHolder.recordValue);
                recordHolderDisplay = recordHolder.FirstName + ": " + trackDistance + "m in " + recordHolder.recordValue + "s";//(speedMs * 3.6).ToString("0.0") + "kph";
            }            
        }

        public override string getDefaultSourceName()
        {
            return recordHolderDisplay;
        }
    }
}
