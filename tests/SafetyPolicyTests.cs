using System;
using System.IO;
using livelywpf.ProcessManager;
class SafetyPolicyTests
{
 static int count;
 static void Check(bool condition,string message) {if(!condition)throw new Exception(message);count++;}
 static Verdict App(string name="Spotify",string signer="Spotify AB",bool trusted=true,bool session=true,bool host=false,bool own=false,string path=null) {
  string root=Path.GetPathRoot(Path.GetFullPath("."));
  return SafetyPolicy.Classify(name,path??Path.Combine(root,"apps","Spotify.exe"),signer,trusted,session,host,own,Path.Combine(root,"Windows"),Path.Combine(root,"AM"),Path.Combine(root,"AM-data"));
 }
 static int Main() {
  try {
   Check(App().Allowed,"Known signed optional app should be eligible");
   foreach(string name in new[]{"explorer","csrss","winlogon","lsass","svchost","audiodg","MsMpEng","NisSrv","AM-Lively-Wallpaper","AM-Lively-Helper","LivelyCefSharp","mpv","UnknownAntivirus","DriverControl","notepad","winword","OneDrive"})Check(!App(name).Allowed,"Protected: "+name);
   Check(!App(trusted:false).Allowed,"Unsigned executable cannot impersonate Spotify");
   Check(!App(signer:"Spotify AB Evil").Allowed,"Publisher substring must not match");
   Check(!App(session:false).Allowed,"Other sessions protected");
   Check(!App(host:true).Allowed,"Service hosts protected");
   Check(!App(own:true).Allowed,"Wallpaper descendants protected");
   var root=Path.GetPathRoot(Path.GetFullPath("."));
   foreach(var dir in new[]{"Windows","AM","AM-data"})Check(!App(path:Path.Combine(root,dir,"Spotify.exe")).Allowed,"Protected path: "+dir);
   Check(!SafetyPolicy.IsUnder(Path.Combine(root,"AM-other","app.exe"),Path.Combine(root,"AM")),"Path boundary must not overmatch");
   Check(!App(path:Path.Combine(root,"apps","..","Windows","Spotify.exe")).Allowed,"Path traversal normalized");
   foreach(var service in new[]{"WinDefend","Audiosrv","Dhcp","Dnscache","WlanSvc","BFE","wuauserv","UnknownService"})Check(!SafetyPolicy.Service(service,"Adobe Inc.",true,true,false).Allowed,"Service protected: "+service);
   Check(SafetyPolicy.Service("AdobeARMservice","Adobe Inc.",true,true,false).Allowed,"Reviewed updater");
   Check(!SafetyPolicy.Service("AdobeARMservice","Adobe Inc.",true,true,true).Allowed,"Service dependencies protected");
   Check(!SafetyPolicy.Service("AdobeARMservice","Adobe Inc.",false,true,false).Allowed,"Untrusted service blocked");
   Check(!SafetyPolicy.Service("AdobeARMservice","Adobe Inc.",true,false,false).Allowed,"Nonstoppable service blocked");
   Check(!SafetyPolicy.Service("AdobeARMservice","Other",true,true,false).Allowed,"Wrong publisher blocked");
   Console.WriteLine("PASS: "+count+" safety policy assertions. No processes or services were stopped.");return 0;
  }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
 }
}
