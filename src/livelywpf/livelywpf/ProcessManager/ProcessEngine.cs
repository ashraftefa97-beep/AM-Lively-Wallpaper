using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.ServiceProcess;
namespace livelywpf.ProcessManager
{
    public sealed class ProcessRow
    {
        public int Pid {get;set;} public long Started {get;set;} public string Name {get;set;} public string Path {get;set;}
        public string Publisher {get;set;} public double? RamMB {get;set;} public double? Cpu {get;set;} public string Uptime {get;set;}
        public string Category {get;set;} public string Reason {get;set;} public bool Allowed {get;set;} public bool HasWindow {get;set;}
        public string ServiceName {get;set;} public string Status {get;set;} public string Key {get {return ServiceName == null ? Pid+":"+Started : "service:"+ServiceName;}}
    }
    [DataContract] public sealed class RestoreEntry
    {
        [DataMember] public int Pid; [DataMember] public long Started; [DataMember] public string Name;
        [DataMember] public string Path; [DataMember] public string Hash; [DataMember] public string Service;
        [DataMember] public string State; [DataMember] public string Time; [DataMember] public string Error;
    }
    [DataContract] public sealed class ModeSettings
    {
        [DataMember] public bool RestoreOnExit = true;
        [DataMember] public Dictionary<string,string> Modes = new Dictionary<string,string> {
            {"Gaming Mode", "Spotify, Discord"}, {"Focus Mode", "Discord, steam, EpicGamesLauncher"},
            {"Battery Saver", "Spotify, Discord, steam, EpicGamesLauncher, service:AdobeARMservice"}
        };
    }
    public sealed class ProcessEngine
    {
        public static ProcessEngine Current;
        readonly object gate = new object(); readonly string directory, app, data;
        readonly Dictionary<string,Tuple<double,long>> samples = new Dictionary<string,Tuple<double,long>>();
        readonly Dictionary<string,Tuple<string,bool>> trust = new Dictionary<string,Tuple<string,bool>>(StringComparer.OrdinalIgnoreCase);
        bool shuttingDown; List<RestoreEntry> journal; public ModeSettings Settings; public string StorageError {get; private set;}
        public ProcessEngine(string appDirectory, string dataDirectory)
        {
            app=appDirectory; data=dataDirectory;
            directory=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AM Lively Wallpaper", "ProcessManager");
            Directory.CreateDirectory(directory);
            try { journal=Load<List<RestoreEntry>>("restore.json") ?? new List<RestoreEntry>(); Settings=Load<ModeSettings>("modes.json") ?? new ModeSettings(); }
            catch(Exception ex) { StorageError="Recovery file could not be read. Stopping is disabled: "+ex.Message; journal=new List<RestoreEntry>(); Settings=new ModeSettings(); }
        }
        T Load<T>(string name) where T:class { var file=System.IO.Path.Combine(directory,name); if(!File.Exists(file))return null; using(var s=File.OpenRead(file)) return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(s); }
        void Save<T>(string name,T value)
        {
            var file=System.IO.Path.Combine(directory,name); var temp=file+".tmp";
            using(var s=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)) { new DataContractJsonSerializer(typeof(T)).WriteObject(s,value); s.Flush(true); }
            if(File.Exists(file)) File.Replace(temp,file,file+".bak"); else File.Move(temp,file);
        }
        public void SaveSettings() { lock(gate)Save("modes.json",Settings); }
        void Persist() { Save("restore.json",journal); }
        public string Log { get { lock(gate) return StorageError + "\n" + String.Join("\n",journal.Select(x=>x.Time+" | "+x.Name+" | "+x.State+" "+x.Error)); } }
        public int Pending {get {lock(gate)return journal.Count(x=>x.State=="Stopped" || x.State=="Intent" || x.State=="NeedsForce");}}
        static string Hash(string file) {using(var sha=SHA256.Create())using(var s=File.OpenRead(file))return BitConverter.ToString(sha.ComputeHash(s));}
        Tuple<string,bool> Identity(string path,bool fresh=false)
        {
            if(String.IsNullOrEmpty(path)) return Tuple.Create("Unknown / inaccessible",false);
            var key=path+"|"+File.GetLastWriteTimeUtc(path).Ticks;
            Tuple<string,bool> result;
            if(!fresh && trust.TryGetValue(key,out result))return result;
            string signer; bool verified=NativeTrust.Verify(path,out signer); result=Tuple.Create(signer,verified); trust[key]=result;return result;
        }
        sealed class Metadata { public int Parent; public string Path; }
        public List<ProcessRow> Snapshot()
        {
            lock(gate) {
                var meta=new Dictionary<int,Metadata>(); var hosts=new HashSet<int>(); var serviceRows=new List<ProcessRow>();
                // WMI failure aborts the snapshot: without service ownership it is unsafe to classify a process as stoppable.
                using(var q=new ManagementObjectSearcher("SELECT ProcessId,ParentProcessId,ExecutablePath FROM Win32_Process"))
                using(var results=q.Get()) foreach(ManagementObject m in results) using(m) meta[Convert.ToInt32(m["ProcessId"])]=new Metadata {Parent=Convert.ToInt32(m["ParentProcessId"]),Path=m["ExecutablePath"] as string};
                using(var q=new ManagementObjectSearcher("SELECT Name,ProcessId,PathName,State FROM Win32_Service"))
                using(var results=q.Get()) foreach(ManagementObject m in results) using(m) {
                    int pid=Convert.ToInt32(m["ProcessId"]); if(pid>0)hosts.Add(pid);
                    var name=(string)m["Name"]; var row=new ProcessRow {Name=name,ServiceName=name,Pid=pid,Status=(string)m["State"],Publisher="Unknown / inaccessible",Uptime="—"};
                    var verdict=SafetyPolicy.Service(name,"",false,false,true);
                    if(name.Equals("AdobeARMservice",StringComparison.OrdinalIgnoreCase)) {
                        try { using(var sc=new ServiceController(name)) {
                            row.Path=ServiceExecutable((string)m["PathName"]); var id=Identity(row.Path); row.Publisher=id.Item1;
                            verdict=SafetyPolicy.Service(name,id.Item1,id.Item2,sc.CanStop,HasDependents(sc));
                        }} catch { /* protected */ }
                    }
                    Apply(row,verdict);serviceRows.Add(row);
                }
                var own=new HashSet<int>{Process.GetCurrentProcess().Id}; bool changed;
                do {changed=false;foreach(var kv in meta)if(own.Contains(kv.Value.Parent)&&own.Add(kv.Key))changed=true;}while(changed);
                int session=Process.GetCurrentProcess().SessionId; var list=new List<ProcessRow>(); var live=new HashSet<string>();
                foreach(var p in Process.GetProcesses()) using(p) {
                    var row=new ProcessRow {Pid=p.Id,Name="Unavailable",Publisher="Unknown / inaccessible",Uptime="—",Category="Critical (do not touch)",Reason="Access denied or process exited.",Status="Running"};
                    try {
                        row.Name=p.ProcessName; row.Started=p.StartTime.ToUniversalTime().Ticks; row.RamMB=p.WorkingSet64/1048576.0; row.HasWindow=p.MainWindowHandle!=IntPtr.Zero;
                        row.Uptime=(DateTime.UtcNow-p.StartTime.ToUniversalTime()).ToString(@"dd\.hh\:mm\:ss");
                        Metadata m; if(meta.TryGetValue(p.Id,out m))row.Path=m.Path;
                        var cpu=p.TotalProcessorTime.TotalMilliseconds; var now=Stopwatch.GetTimestamp(); Tuple<double,long> old;
                        if(samples.TryGetValue(row.Key,out old))row.Cpu=Math.Max(0,Math.Min(100,(cpu-old.Item1)/((now-old.Item2)*1000.0/Stopwatch.Frequency)/Environment.ProcessorCount*100));
                        samples[row.Key]=Tuple.Create(cpu,now); live.Add(row.Key);
                        var id=Identity(row.Path); row.Publisher=id.Item1;
                        Apply(row,SafetyPolicy.Classify(row.Name,row.Path,id.Item1,id.Item2,p.SessionId==session,hosts.Contains(p.Id),own.Contains(p.Id),Environment.GetFolderPath(Environment.SpecialFolder.Windows),app,data));
                    } catch {row.Allowed=false;row.Category="Critical (do not touch)";row.Reason="Cannot establish full process identity.";}
                    list.Add(row);
                }
                foreach(var key in samples.Keys.Where(x=>!live.Contains(x)).ToArray())samples.Remove(key);
                foreach(var row in serviceRows) { var host=list.FirstOrDefault(x=>x.Pid==row.Pid); if(host!=null) {if(row.Publisher=="Unknown / inaccessible")row.Publisher=host.Publisher;row.RamMB=host.RamMB;row.Cpu=host.Cpu;row.Uptime=host.Uptime;row.Reason+=" Metrics belong to host PID (may be shared).";} }
                return list.Concat(serviceRows).OrderByDescending(x=>x.Cpu??-1).ThenByDescending(x=>x.RamMB??-1).ToList();
            }
        }
        static void Apply(ProcessRow row,Verdict v) {row.Allowed=v.Allowed;row.Category=v.Category;row.Reason=v.Reason;}
        static bool HasDependents(ServiceController sc) {var deps=sc.DependentServices;try{return deps.Length>0;}finally{foreach(var d in deps)d.Dispose();}}
        static string ServiceExecutable(string command)
        {
            if(String.IsNullOrWhiteSpace(command))return null;command=Environment.ExpandEnvironmentVariables(command.Trim());
            if(command.StartsWith("\"",StringComparison.Ordinal)) {int end=command.IndexOf('"',1);return end>1?command.Substring(1,end-1):null;}
            // Ambiguous unquoted paths are deliberately refused.
            return command.IndexOf(' ')<0 && command.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)?command:null;
        }
        public string Stop(ProcessRow selected, bool forceApproved)
        {
            lock(gate) {
                if(shuttingDown)throw new InvalidOperationException("Application is shutting down.");
                if(StorageError!=null)throw new InvalidOperationException(StorageError);
                var current=Snapshot().FirstOrDefault(x=>x.Key==selected.Key);
                if(current==null || !current.Allowed)throw new InvalidOperationException("The process has exited, changed identity, or is protected.");
                var identity=Identity(current.Path,true);if(!identity.Item2 || identity.Item1!=current.Publisher)throw new InvalidOperationException("Publisher verification failed.");
                var entry=new RestoreEntry {Pid=current.Pid,Started=current.Started,Name=current.Name,Path=current.Path,Hash=Hash(current.Path),Service=current.ServiceName,State="Intent",Time=DateTime.UtcNow.ToString("o")};
                if(current.ServiceName!=null) {
                    if(forceApproved)throw new InvalidOperationException("Force-killing services is prohibited.");
                    using(var sc=new ServiceController(current.ServiceName)) {
                        if(sc.Status!=ServiceControllerStatus.Running)return "Service is not running.";
                        if(!SafetyPolicy.Service(sc.ServiceName,identity.Item1,identity.Item2,sc.CanStop,HasDependents(sc)).Allowed)throw new InvalidOperationException("Service no longer eligible.");
                        journal.Add(entry);Persist();
                        try {sc.Stop();sc.WaitForStatus(ServiceControllerStatus.Stopped,TimeSpan.FromSeconds(12));entry.State="Stopped";}
                        catch(Exception ex) {entry.Error=ex.Message;Persist();throw;}
                    }
                } else using(var p=Process.GetProcessById(current.Pid)) {
                    if(p.StartTime.ToUniversalTime().Ticks!=current.Started)throw new InvalidOperationException("PID was reused.");
                    // Force is accepted only after a recorded graceful attempt for this exact process instance.
                    if(forceApproved && !journal.Any(x=>x.Pid==current.Pid && x.Started==current.Started && x.State=="NeedsForce"))throw new InvalidOperationException("Graceful close must be attempted first.");
                    journal.Add(entry);Persist();
                    try {
                        if(forceApproved)p.Kill(); else p.CloseMainWindow();
                        if(p.WaitForExit(forceApproved?5000:8000))entry.State="Stopped";
                        else {entry.State="NeedsForce";entry.Error="Graceful close did not exit. No force action taken.";}
                    } catch(Exception ex) {entry.Error=ex.Message;Persist();throw;}
                }
                Persist();return entry.State;
            }
        }
        public void Shutdown()
        {
            lock(gate) { shuttingDown=true; if(Settings.RestoreOnExit)RestoreAll(); }
        }
        public string RestoreAll()
        {
            lock(gate) {
                if(StorageError!=null)return StorageError;
                foreach(var e in journal.Where(x=>x.State=="Stopped" || x.State=="Intent" || x.State=="NeedsForce").ToArray()) {
                    try {
                        if(e.Service!=null) {
                            if(!String.Equals(e.Service,"AdobeARMservice",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Service recovery is not allowed by policy.");
                            using(var q=new ManagementObjectSearcher("SELECT PathName FROM Win32_Service WHERE Name='AdobeARMservice'"))
                            using(var results=q.Get()) {
                                string actual=null;foreach(ManagementObject m in results)using(m)actual=ServiceExecutable((string)m["PathName"]);
                                if(actual==null || !String.Equals(actual,e.Path,StringComparison.OrdinalIgnoreCase) || Hash(actual)!=e.Hash)throw new InvalidOperationException("Service executable identity changed.");
                                var id=Identity(actual,true);if(!SafetyPolicy.Service(e.Service,id.Item1,id.Item2,true,false).Allowed)throw new InvalidOperationException("Service publisher verification failed.");
                            }
                            using(var sc=new ServiceController(e.Service)) {
                                if(sc.Status==ServiceControllerStatus.StopPending)sc.WaitForStatus(ServiceControllerStatus.Stopped,TimeSpan.FromSeconds(12));
                                if(sc.Status==ServiceControllerStatus.Stopped) {sc.Start();sc.WaitForStatus(ServiceControllerStatus.Running,TimeSpan.FromSeconds(12));}
                                if(sc.Status!=ServiceControllerStatus.Running)throw new InvalidOperationException("Service is not running yet.");
                            }
                        } else {
                            if(IsOriginalAlive(e)) {e.Error="Still running; no duplicate launch. Retry if graceful close completes later.";Persist();continue;}
                            if(IsPathRunning(e.Path)) {e.State="Already running";Persist();continue;}
                            if(!File.Exists(e.Path)||Hash(e.Path)!=e.Hash)throw new InvalidOperationException("Executable changed or missing; automatic restart refused.");
                            var id=Identity(e.Path,true);
                            if(!SafetyPolicy.Classify(e.Name,e.Path,id.Item1,id.Item2,true,false,false,Environment.GetFolderPath(Environment.SpecialFolder.Windows),app,data).Allowed)throw new InvalidOperationException("Restore safety verification failed.");
                            // Clean launch, no stale document/installer arguments, no elevation, no shell interpretation.
                            using(var started=Process.Start(new ProcessStartInfo(e.Path) {UseShellExecute=false,WorkingDirectory=System.IO.Path.GetDirectoryName(e.Path)})) {if(started==null)throw new InvalidOperationException("Restart returned no process.");}
                        }
                        e.State="Restored";e.Error=null;
                    }catch(Exception ex){e.Error="Restore pending: "+ex.Message;}
                    Persist();
                }
                return Log;
            }
        }
        static bool IsOriginalAlive(RestoreEntry e) {try{using(var p=Process.GetProcessById(e.Pid))return p.StartTime.ToUniversalTime().Ticks==e.Started&&!p.HasExited;}catch(ArgumentException){return false;}}
        static bool IsPathRunning(string path) {
            using(var q=new ManagementObjectSearcher("SELECT ExecutablePath FROM Win32_Process"))using(var results=q.Get())foreach(ManagementObject m in results)using(m)
                if(String.Equals(m["ExecutablePath"] as string,path,StringComparison.OrdinalIgnoreCase))return true;return false;
        }
    }
}
