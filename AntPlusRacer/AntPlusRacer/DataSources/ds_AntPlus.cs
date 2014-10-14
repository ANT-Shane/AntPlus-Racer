/*
This software is subject to the license described in the license.txt file included with this software distribution.You may not use this file except in compliance with this license. 
Copyright © Dynastream Innovations Inc. 2012
All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ANT_Managed_Library;

namespace AntPlusRacer.DataSources
{
    public abstract class ds_AntPlus: DataSourceBase
    {
        String sourceName;
        
        public AntChannelProfile searchProfile;
        public bool isInitialized = false;


        public class AntChannelProfile
        {
            public byte rfOffset;
            public byte transType;
            public byte deviceType;
            public ushort deviceNumber;
            public ushort messagePeriod;
            public bool pairingEnabled;
        }

        public ds_AntPlus(String defaultSourceName, racerSportType sportType)
            :base(sportType, true)
        {
            this.sourceName = defaultSourceName;
            this.searchProfile = getDefaultSearchProfile();
        }

        protected abstract AntChannelProfile getDefaultSearchProfile();

        public abstract void handleChannelResponse(ANT_Response response);

        public override string getDefaultSourceName()
        {
            //return sourceName;
            return String.Format("{0,-14}", sourceName);    //HACK qc to try and even out some of the dynamic sizing issues
        }

        public override void reset()
        {
            base.reset();
        }
    }
}
