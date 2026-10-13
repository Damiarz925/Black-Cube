; Black-Cube - Windows artist preview installer
; Requires a recent Inno Setup release (6.3 or newer).
;
; 1. Save this file in your Unity PROJECT ROOT, next to Assets and Packages.
; 2. Build the Windows x64 game into Builds\ArtistBuild.
; 3. Match MyAppExeName below to the actual executable Unity produced.
; 4. Open this file in Inno Setup and select Compile.
;
; Output: Builds\Installers\BlackCube-Artist-Setup-0.1.0.exe
; This script packages an existing game build; it does not build Unity itself.
; It does not install third-party/native-plugin prerequisites or sign the EXE.

#define MyAppName "Black-Cube (Artist Preview)"
#define MyAppVersion "0.1.0"
#define MyAppExeName "Black Cube Idle.exe"

[Setup]
; Keep AppId unchanged for subsequent artist preview releases.
AppId=DavidArmstrong.BlackCube.ArtistPreview
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={localappdata}\Programs\Black-Cube-ArtistPreview
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=Builds\Installers
OutputBaseFilename=BlackCube-Artist-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Files]
; Explicit executable entry makes compilation fail if the EXE name is wrong.
Source: "Builds\BeeperBuild\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

; Preserve the entire build directory structure. Exclude the EXE already added
; above and Unity's two standard developer-only debug/backup directory types.
; Keep those debug/backup folders on your development machine for diagnostics.
Source: "Builds\BeeperBuild\*"; DestDir: "{app}"; Excludes: "\{#MyAppExeName},\*_BackUpThisFolder_ButDontShipItWithYourGame,\*_BackUpThisFolder_ButDontShipItWithYourGame\*,\*_BurstDebugInformation_DoNotShip,\*_BurstDebugInformation_DoNotShip\*"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{userdesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

; No blanket deletion rules are used. The installer does not deliberately
; modify Unity's persistent-data directory or PlayerPrefs.
; On updates, existing packaged files are replaced, but obsolete files that
; disappeared from newer builds are not automatically deleted. For a changed
; build layout/backend, uninstall the previous preview before reinstalling.
; Test the game and this installer locally before sharing.
