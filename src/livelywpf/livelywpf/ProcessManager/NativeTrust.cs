using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
namespace livelywpf.ProcessManager
{
    internal static class NativeTrust
    {
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] struct FileInfo { public uint Size; [MarshalAs(UnmanagedType.LPWStr)] public string Path; public IntPtr Handle; public IntPtr Subject; }
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] struct TrustData { public uint Size; public IntPtr Policy, SIP; public uint UI, Revocation, Choice; public IntPtr File; public uint StateAction; public IntPtr State, URL; public uint Flags, Context; }
        [DllImport("wintrust.dll", ExactSpelling=true, PreserveSig=true)] static extern int WinVerifyTrust(IntPtr hwnd, ref Guid action, ref TrustData data);
        internal static bool Verify(string path, out string signer)
        {
            signer = "Unknown / inaccessible";
            IntPtr ptr = IntPtr.Zero;
            try {
                var file = new FileInfo { Size=(uint)Marshal.SizeOf(typeof(FileInfo)), Path=path };
                ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(FileInfo))); Marshal.StructureToPtr(file, ptr, false);
                var data = new TrustData { Size=(uint)Marshal.SizeOf(typeof(TrustData)), UI=2, Choice=1, File=ptr, Flags=0x1000 };
                var action = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");
                if (WinVerifyTrust(new IntPtr(-1), ref action, ref data) != 0) return false;
                using(var cert = new X509Certificate2(X509Certificate.CreateFromSignedFile(path)))
                    signer=cert.GetNameInfo(X509NameType.SimpleName, false);
                return true;
            } catch { return false; }
            finally { if(ptr != IntPtr.Zero) { Marshal.DestroyStructure(ptr, typeof(FileInfo)); Marshal.FreeHGlobal(ptr); } }
        }
    }
}
