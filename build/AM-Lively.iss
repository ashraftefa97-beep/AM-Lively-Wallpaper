; Build with Inno Setup 6.2.2. Product runtime remains .NET Framework 4.7.2.
[Setup]
AppId={{537ED746-1E97-4DCC-9768-9EA9E12EC539}}
AppName=AM Lively Wallpaper
AppVersion=0.9.6.1
AppPublisher=AM (based on Lively Wallpaper by rocksdanister)
DefaultDirName={localappdata}\Programs\AM Lively Wallpaper
DefaultGroupName=AM Lively Wallpaper
PrivilegesRequired=lowest
MinVersion=6.3
OutputDir=..\dist
OutputBaseFilename=AM-Lively-Wallpaper-Setup-x86
SetupIconFile=..\src\livelywpf\livelywpf\brand\AM-Lively.ico
UninstallDisplayIcon={app}\AM-Lively-Wallpaper.exe
LicenseFile=..\License.txt
Compression=lzma
SolidCompression=yes
WizardStyle=modern
AppMutex=AM:DESKTOPWALLPAPERSYSTEM
CloseApplications=no
[Files]
Source: "..\dist\portable\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Tasks]
Name: desktopicon; Description: "Create a desktop shortcut"; Flags: unchecked
[Icons]
Name: "{autoprograms}\AM Lively Wallpaper"; Filename: "{app}\AM-Lively-Wallpaper.exe"
Name: "{autodesktop}\AM Lively Wallpaper"; Filename: "{app}\AM-Lively-Wallpaper.exe"; Tasks: desktopicon
[Run]
Filename: "{app}\AM-Lively-Wallpaper.exe"; Description: "Launch AM Lively Wallpaper"; Flags: nowait postinstall skipifsilent
[Code]
function InitializeSetup(): Boolean;
var Release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 461808);
  if not Result then MsgBox('Install .NET Framework 4.7.2 before installing AM Lively Wallpaper.', mbError, MB_OK);
end;
