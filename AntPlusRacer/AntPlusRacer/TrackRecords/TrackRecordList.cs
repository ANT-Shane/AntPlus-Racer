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
    public class TrackRecordList
    {
        public racerSportType sportType;
        public double trackDistance;

        public List<RecordData> trackRecords;   //Should be sorted list, but xml serialization doesn't work, so we sort in addRecord()

        //Keep list sorted
        public int addRecord(RecordData record)
        {
            //Add 
            int i;
            for (i = 0; i < trackRecords.Count; ++i)
            {
                if (trackRecords[i].recordValue > record.recordValue)
                {
                    trackRecords.Insert(i, record);
                    break;
                }
            }
            if (i == trackRecords.Count)
                trackRecords.Add(record);

            return i;
        }
    }
}
