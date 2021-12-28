using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    class MyApp
    {
        [STAThread]
        static void Main(String[] args)
        {
            Application app = new Application();
            app.StartupUri = new Uri("./MainWindow.xaml",UriKind.Relative);
            app.Run();
        }
    }
}
