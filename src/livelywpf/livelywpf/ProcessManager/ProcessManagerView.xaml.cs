using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
namespace livelywpf.ProcessManager
{
    public partial class ProcessManagerView : UserControl
    {
        ProcessEngine engine; readonly DispatcherTimer timer=new DispatcherTimer {Interval=TimeSpan.FromSeconds(3)};
        bool refreshing,busy; List<ProcessRow> latest=new List<ProcessRow>(); string baseline;
        public ProcessManagerView()
        {
            InitializeComponent();
            Loaded+=async (s,e)=> {
                try {
                    if(engine==null) {
                        engine=ProcessEngine.Current ?? (ProcessEngine.Current=new ProcessEngine(AppDomain.CurrentDomain.BaseDirectory,App.PathData));
                        Modes.ItemsSource=engine.Settings.Modes.Keys.ToArray();Modes.SelectedIndex=0;AutoRestore.IsChecked=engine.Settings.RestoreOnExit;
                    }
                    timer.Start();await Refresh();
                }catch(Exception ex){Journal.Text=ex.Message;}
            };
            Unloaded+=(s,e)=>timer.Stop();timer.Tick+=async(s,e)=>await Refresh();
        }
        static string Totals(IEnumerable<ProcessRow> rows)
        {
            var p=rows.Where(x=>x.ServiceName==null).ToList();
            var cpu=p.Any(x=>x.Cpu.HasValue)?p.Sum(x=>x.Cpu??0).ToString("F1")+"%":"sampling…";
            return "Measured processes: CPU "+cpu+", working sets "+p.Sum(x=>x.RamMB??0).ToString("F1")+" MB";
        }
        async Task Refresh()
        {
            if(engine==null||refreshing||busy)return;refreshing=true;
            try {
                var selected=Rows.SelectedItems.Cast<ProcessRow>().Select(x=>x.Key).ToArray();
                latest=await Task.Run(()=>engine.Snapshot());Rows.ItemsSource=latest;
                foreach(var row in latest.Where(x=>selected.Contains(x.Key)))Rows.SelectedItems.Add(row);
                Metrics.Text=(baseline==null?"": "Before: "+baseline+"\nAfter / live: ")+Totals(latest)+". Shared RAM may be counted more than once; excludes inaccessible processes. Changes are not proof of savings.";
                Journal.Text=engine.Log+"\nPending restore: "+engine.Pending;
            }catch(Exception ex){Journal.Text="Live view unavailable; no action taken: "+ex.Message;latest.Clear();Rows.ItemsSource=null;}
            finally{refreshing=false;}
        }
        void ModeChanged(object sender,SelectionChangedEventArgs e) {if(engine!=null&&Modes.SelectedItem!=null)ModeList.Text=engine.Settings.Modes[(string)Modes.SelectedItem];}
        void SaveMode(object sender,RoutedEventArgs e) {try{engine.Settings.Modes[(string)Modes.SelectedItem]=ModeList.Text;engine.SaveSettings();Journal.Text="Mode saved. Protection cannot be overridden.";}catch(Exception ex){Journal.Text=ex.Message;}}
        void AutoChanged(object sender,RoutedEventArgs e) {if(engine==null)return;try{engine.Settings.RestoreOnExit=AutoRestore.IsChecked==true;engine.SaveSettings();}catch(Exception ex){Journal.Text=ex.Message;}}
        async void ApplyMode(object sender,RoutedEventArgs e)
        {
            if(busy||refreshing||engine==null)return;
            var names=ModeList.Text.Split(',').Select(x=>x.Trim()).Where(x=>x.Length>0).ToArray();
            var candidates=latest.Where(x=>names.Contains(x.ServiceName==null?x.Name:"service:"+x.ServiceName,StringComparer.OrdinalIgnoreCase)).ToList();
            await StopRows(candidates);
        }
        async void StopSelected(object sender,RoutedEventArgs e) {if(busy||refreshing||engine==null)return;await StopRows(Rows.SelectedItems.Cast<ProcessRow>().ToList());}
        async Task StopRows(List<ProcessRow> candidates)
        {
            var eligible=candidates.Where(x=>x.Allowed && x.Status=="Running").ToList();
            if(eligible.Count==0){Journal.Text="No eligible running targets. Unknown processes and protected services cannot be stopped.";return;}
            busy=true;Actions.IsEnabled=false;
            try {
                var plan=String.Join("\n",eligible.Select(x=>x.Name+" (PID "+x.Pid+")"+(x.HasWindow?" — OPEN WINDOW":"")+(x.ServiceName!=null?" — SERVICE":"")));
                if(MessageBox.Show("Temporarily close these targets?\n\n"+plan+"\n\nPrograms may contain unsaved work even if no window is visible. Save your work first. Downloads, calls or games may stop. Restore launches apps again but cannot recover unsaved work. Service startup settings will not change.\n\nProtected/skipped: "+(candidates.Count-eligible.Count),"AM Lively Wallpaper — approval",MessageBoxButton.YesNo,MessageBoxImage.Warning,MessageBoxResult.No)!=MessageBoxResult.Yes)return;
                baseline=Totals(latest);
                foreach(var row in eligible) {
                    try {
                        var result=await Task.Run(()=>engine.Stop(row,false));
                        if(result=="NeedsForce" && MessageBox.Show(row.Name+" did not close normally. Force-kill this exact process?\nUnsaved work can be lost. No other processes will be killed.","Last resort — force warning",MessageBoxButton.YesNo,MessageBoxImage.Warning,MessageBoxResult.No)==MessageBoxResult.Yes)
                            await Task.Run(()=>engine.Stop(row,true));
                    }catch(Exception ex){MessageBox.Show(row.Name+": "+ex.Message,"AM Lively Wallpaper",MessageBoxButton.OK,MessageBoxImage.Information);}
                }
            }finally {busy=false;Actions.IsEnabled=true;await Refresh();}
        }
        async void Restore(object sender,RoutedEventArgs e)
        {
            if(busy||refreshing||engine==null)return;busy=true;Actions.IsEnabled=false;
            try{Journal.Text=await Task.Run(()=>engine.RestoreAll());}catch(Exception ex){Journal.Text=ex.Message;}
            finally{busy=false;Actions.IsEnabled=true;await Refresh();}
        }
    }
}
