using System;
using System.Collections.Generic;
using Gtk;
using Gdk;
using tester;

public class ProgBox : Box
{
    float prevAngle;
    Label angle;
    Label delta;
    Label frame;
    Label average;
    List<int> points;
    public ProgBox(Orientation orientation, int spacing) : base(orientation, spacing)
    {
        points = new List<int>();
        angle = new Label();
        delta = new Label();
        frame = new Label();
        average = new Label();
        prevAngle = 0;
        this.Add(frame);
        this.Add(delta);
        this.Add(angle);
        this.Add(average);
        this.ShowAll();
    }

    public int changeValues(float angle, int delta, int frame)
    {
        double angVelocity;
        if (angle > prevAngle)
            angVelocity = (angle - prevAngle) / delta * 1000;
        else
            angVelocity = (angle + Math.PI - prevAngle) / delta * 1000;
        if (points.Count > 20)
        {
            int median, avg, sum = 0;
            List<int> temp = new List<int>(points);
            temp.Sort();
            median = temp[temp.Count/2];
            foreach (int item in temp)
            {
                sum += item;
            }
            avg = sum / temp.Count;
            points.RemoveAt(0);
            this.average.Text = String.Format("Median: {0} Average: {1}", median, avg);
        }
        points.Add(delta);
        this.angle.Text = String.Format("Angle: {0:0.0000} Angular velocity: {1:0.0000}", angle, angVelocity);
        this.delta.Text = String.Format("Delta: {0}", delta);
        this.frame.Text = String.Format("Frame: {0}", frame);
        return 0;
    }
}

public partial class MainWindow : Gtk.Window
{
#if _WIN32
    string path = @"/tester/testing/build_win/test";
#else
    string path = @"/testing/build/test";
#endif
    LinkedList<SampleProgram> samplePrograms;
    Entry txtTimeout;
    Box progStatus = new Box(Orientation.Vertical, 5);

    void clickOnRunButton(object obj, EventArgs args)
    {
        int timeout = -1;
        if(!Int32.TryParse(txtTimeout.Text, out timeout))
        {
            timeout = -1;
        }
        ProgBox pb = new ProgBox(Orientation.Horizontal, 5);
        this.progStatus.Add(pb);
        pb.Show();
        SampleProgram sp = new SampleProgram(this.path, timeout, pb.changeValues);
        
        this.samplePrograms.AddLast(sp);
    }

    public MainWindow() : base(Gtk.WindowType.Toplevel)
    {
        this.samplePrograms = new LinkedList<SampleProgram>();
        txtTimeout = new Entry();
        txtTimeout.WidthRequest = 50;
        txtTimeout.Text = "-1";
        this.Resize(640, 480);
        Button btn_run = new Button("Run");
        btn_run.Clicked += clickOnRunButton;
        Label lbl1 = new Label("Timeout (-1 for unlimited)");
        Layout main_layout = new Layout(new Adjustment(0, 0, 640, 1, 640, 640),
            new Adjustment(0, 0, 480, 1, 480, 480));
        this.Add(main_layout);
        main_layout.Put(lbl1, 10, 20);
        main_layout.Put(btn_run, 10, 80);
        main_layout.Put(txtTimeout, 10, 40);
        progStatus.WidthRequest = 640;
        main_layout.Put(progStatus, 5, 130);
        this.Destroyed += new EventHandler(OnDestroy);
        this.ShowAll();
    }

    private static void OnDestroy(object o, EventArgs args)
    {
        Console.WriteLine("Destroyed");
        foreach (var item in ((MainWindow)o).samplePrograms)
        {
            item.Stop();
        }
        Application.Quit();
    }
}
