using System;
using Gtk;

namespace tester
{
    class Program
    {
        static void Main(string[] args)
        {
            Application.Init();
            MainWindow win = new MainWindow();
            win.Show();
            Application.Run();
        }
    }
}
