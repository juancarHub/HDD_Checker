#define AppName "ST_Holmes"
#define AppVersion "1.0.0"
#define Publisher "Juancar and Luna"
#define ProjectUrl "https://github.com/juancarHub/ST_Holmes"

[Setup]
AppId={{7E409DBB-A063-46A8-B629-7D4791AB90B3}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#Publisher}
AppPublisherURL={#ProjectUrl}
AppSupportURL={#ProjectUrl}
AppUpdatesURL={#ProjectUrl}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir=..\artifacts\installer
OutputBaseFilename=ST_Holmes-Setup-{#AppVersion}
SetupIconFile=..\assets\ST_Holmes.ico
UninstallDisplayIcon={app}\ST_Holmes.exe
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern dynamic
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=no
RestartApplications=no
ChangesAssociations=no
VersionInfoVersion={#AppVersion}.0
VersionInfoCompany={#Publisher}
VersionInfoDescription=Instalador de supervision preventiva ST_Holmes
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\artifacts\package-win-x64\*"; DestDir: "{app}"; \
    Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\scripts\prepare-install.ps1"; Flags: dontcopy

[Icons]
Name: "{autoprograms}\ST_Holmes - Manual de usuario"; \
    Filename: "{app}\ST_Holmes-Manual-de-usuario.pdf"

[Run]
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; \
    Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\install.ps1"" -InstallRoot ""{app}"" -DataRoot ""{commonappdata}\ST_Holmes"" -SkipFileCopy"; \
    StatusMsg: "Configurando el servicio de ST_Holmes..."; Flags: runhidden waituntilterminated

[UninstallRun]
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; \
    Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\uninstall.ps1"" -InstallRoot ""{app}"" -DataRoot ""{commonappdata}\ST_Holmes"" -SkipFileRemoval"; \
    Flags: runhidden waituntilterminated; RunOnceId: "STHolmesCleanup"

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  ExtractTemporaryFile('prepare-install.ps1');
  if not Exec(
    ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
    '-NoProfile -ExecutionPolicy Bypass -File "' +
      ExpandConstant('{tmp}\prepare-install.ps1') + '"',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    Result := 'No se pudo preparar la actualización de ST_Holmes.';
    exit;
  end;
  if ResultCode <> 0 then
  begin
    Result := 'No se pudieron detener los componentes activos de ST_Holmes.';
    exit;
  end;
  Result := '';
end;
