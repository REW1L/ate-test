using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;


namespace tester
{

    public class SampleProgram
    {
        private Process pr;
        private int timeout;
        private Func<float,int,int,int> status;
        private DebuggerInterface dbg;

        void printline(object sender, DataReceivedEventArgs args)
        {
            Console.WriteLine(args.ToString());
        }

        void onExit(object sender, EventArgs args)
        {
            Console.WriteLine(args.ToString());
            this.Stop();
        }

        public SampleProgram(string rawPath, int timeout, Func<float,int,int, int> status)
        {
            this.timeout = timeout;
            this.status = status;
            string path = Path.GetFullPath(rawPath);
            Console.WriteLine("Sample program is starting...");
            pr = new Process();
            pr.StartInfo.UseShellExecute = false;
            pr.StartInfo.FileName = path;
            pr.StartInfo.CreateNoWindow = true;
            pr.Exited += onExit;
            pr.OutputDataReceived += printline;
            pr.Start();
            while (pr.MainModule == null && !pr.HasExited) ;
            Thread thread = new Thread(new ThreadStart(this.programStart));
            thread.Start();
        }
        private void programStart()
        {
#if _WIN32
            dbg = new DebuggerWin(this.timeout);
            dbg.setHook(this.status);
            dbg.attachProcess(this.pr);
#endif
        }

        public int Stop()
        {
            this.dbg.Stop();
            if (pr != null)
            {
                pr = null;
            }
            return 0;
        }
    }
}
