using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace TimeIsLifeRegister
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string path=string.Empty;
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            
            for (int i = 0; i < e.Args.Length; i++)
            {
                path += e.Args[i] + " ";
                //MessageBox.Show(path);
            }            
        }
    }
}
