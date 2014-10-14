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
    public class RecordData
    {
        public String FirstName;
        public String LastName;
        public String PhoneNumber;
        public String Email;
        public String DataSourceName;

        public double recordValue;

        public DateTime recordDate;

        public RecordData()
        {
            recordDate = DateTime.Now;
        }
    }
}
