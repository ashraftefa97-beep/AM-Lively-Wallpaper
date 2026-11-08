using System;
using System.Diagnostics;
using System.Linq;
using livelywpf.ProcessManager;
class SnapshotSmoke
{
 static int Main() {
  try {
   var engine=new ProcessEngine(AppDomain.CurrentDomain.BaseDirectory,AppDomain.CurrentDomain.BaseDirectory);
   var first=engine.Snapshot();
   System.Threading.Thread.Sleep(1200);
   var rows=engine.Snapshot();
   if(rows.Count==0)throw new Exception("No process/service data");
   var self=rows.Single(x=>x.ServiceName==null&&x.Pid==Process.GetCurrentProcess().Id);
   if(self.Allowed||!self.RamMB.HasValue||!self.Cpu.HasValue)throw new Exception("Self protection or real metrics failed");
   if(rows.Any(x=>x.ServiceName!=null&&x.ServiceName!="AdobeARMservice"&&x.Allowed))throw new Exception("An unreviewed service is stoppable");
   Console.WriteLine("PASS: read-only Windows snapshot, "+rows.Count+" rows, self RAM "+self.RamMB+" MB, CPU "+self.Cpu+"%. No stop or restore invoked.");return 0;
  }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
 }
}
