/*
This software is subject to the license described in the license.txt file included with this software distribution.You may not use this file except in compliance with this license. 
Copyright © Dynastream Innovations Inc. 2012
All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AntPlusRacer
{
    public static class XmlDatabaser
    {
        public static String saveDatabase(string databaseName, Object dbObj)
        {
            System.Xml.Serialization.XmlSerializer xs;

            try
            {
                //Backup the database
                xs = new System.Xml.Serialization.XmlSerializer(dbObj.GetType());

                //Save a backup copy if database exists to a temp file
                if (System.IO.File.Exists(databaseName+"db.xml"))
                {
                    System.IO.File.Copy(databaseName + "db.xml", databaseName + "backup.db.xml", true);
                }
            }
            catch (Exception ex)
            {
                return "Save Backup Failed, aborting after error: " + ex.Message;
            }

            try
            {
                using (System.IO.FileStream dbFile = new System.IO.FileStream(databaseName + "db.xml", System.IO.FileMode.Create))
                {
                    xs.Serialize(dbFile, dbObj);
                    dbFile.Close();
                }
                return null; //success
            }
            catch (Exception ex)  //Save failed, restore the db backup
            {
                //Ensure we don't keep a corrupt db
                try
                {
                    System.IO.File.Copy(databaseName + "backup.db.xml", databaseName + "db.xml", true);
                    return "Restored backup after database save failure. Database Failure: " + ex.Message;
                }
                catch (Exception ex2)
                {
                    return "Database Save Failed and Restoring Backup Failed. Database Failure: " + ex.Message + ", Restore Failure: " + ex2.Message;
                }
            }
        }

        public static T loadDatabase<T>(string databaseName, bool createNewIfNotExist=true) where T: new()
        {
            T dbObj;

            if (System.IO.File.Exists(databaseName + "db.xml"))
            {
                System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(T));
                using (System.IO.FileStream dbFile = new System.IO.FileStream(databaseName + "db.xml", System.IO.FileMode.OpenOrCreate))
                {
                    dbObj = (T)xs.Deserialize(dbFile);
                    dbFile.Close();
                }
            }
            else if (createNewIfNotExist)
            {
                dbObj = new T();
            }
            else
            {
                throw new System.IO.FileNotFoundException("Load Database Failed, file does not exist");
            }

            return dbObj;
        }
    }
}
