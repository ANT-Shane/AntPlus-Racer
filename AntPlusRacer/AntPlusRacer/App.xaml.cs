/*
This software is subject to the license described in the license.txt file included with this software distribution.You may not use this file except in compliance with this license. 
Copyright © Dynastream Innovations Inc. 2012
All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Text;

namespace AntPlusRacer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            #if !DEBUG
                this.DispatcherUnhandledException += Application_DispatcherUnhandledException;
            #endif
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            StringBuilder sb = new StringBuilder("Sorry the track is temporarily closed.\n");
            
            Exception curExcp = e.Exception;

            while(curExcp != null)
            {
                sb.AppendLine(curExcp.Message);
                sb.AppendLine(" ");
                curExcp = curExcp.InnerException;
            }

            sb.AppendLine(" => ");
            sb.AppendLine(e.Exception.StackTrace);

            System.Windows.MessageBox.Show(sb.ToString(), "Rain Delay");
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow = AntPlusRacer.MainWindow.getInstance();
            MainWindow.Show();
        }
    }
}
