Unicode true
!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "x64.nsh"
!ifndef PAYLOAD
  !define PAYLOAD "..\dist\VOLT-0.6.6"
!endif
!ifndef OUTPUT
  !define OUTPUT "..\..\VOLT-0.6.6-Setup.exe"
!endif
Var RuntimeVersion
Var InstallMessage
Var ExistingLocation
Var Updating
Name "VOLT"
OutFile "${OUTPUT}"
InstallDir "$LOCALAPPDATA\Programs\VOLT"
InstallDirRegKey HKCU "Software\VOLT\Installer" "Location"
RequestExecutionLevel user
SetCompressor /SOLID lzma
SetCompressorDictSize 32
Icon "..\Assets\Volt.ico"
UninstallIcon "..\Assets\Volt.ico"
VIProductVersion "0.6.6.0"
VIAddVersionKey /LANG=1046 "ProductName" "VOLT"
VIAddVersionKey /LANG=1046 "FileDescription" "Instalador do VOLT 0.6.6"
VIAddVersionKey /LANG=1046 "FileVersion" "0.6.6"
VIAddVersionKey /LANG=1046 "LegalCopyright" "VOLT"
!define MUI_ABORTWARNING
!define MUI_WELCOMEPAGE_TEXT "$InstallMessage"
!define MUI_FINISHPAGE_RUN "$INSTDIR\app\VOLT.exe"
!define MUI_FINISHPAGE_RUN_TEXT "Abrir o VOLT"
!insertmacro MUI_PAGE_WELCOME
!define MUI_PAGE_CUSTOMFUNCTION_PRE DirectoryPagePre
!define MUI_DIRECTORYPAGE_TEXT_TOP "Esta é a pasta padrão do VOLT. Mantenha este local para facilitar as próximas atualizações."
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "PortugueseBR"
Function .onInit
  ${IfNot} ${RunningX64}
    MessageBox MB_ICONSTOP "Esta versão do VOLT requer Windows de 64 bits."
    Abort
  ${EndIf}
  SetShellVarContext current
  SetRegView 32
  StrCpy $Updating 0
  ReadRegStr $ExistingLocation HKCU "Software\VOLT\Installer" "Location"
  ${If} $ExistingLocation == ""
    ReadRegStr $ExistingLocation HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\VOLT" "InstallLocation"
  ${EndIf}
  ${If} $ExistingLocation != ""
    IfFileExists "$ExistingLocation\app\VOLT.exe" existing_found default_check
  ${EndIf}
  default_check:
    StrCpy $ExistingLocation "$LOCALAPPDATA\Programs\VOLT"
    IfFileExists "$ExistingLocation\app\VOLT.exe" existing_found new_install
  existing_found:
    StrCpy $INSTDIR $ExistingLocation
    StrCpy $Updating 1
    StrCpy $InstallMessage "O VOLT já está instalado. Este assistente vai atualizar o programa no mesmo local, sem criar outra cópia.$\r$\n$\r$\nLocal: $INSTDIR$\r$\n$\r$\nFeche o VOLT antes de continuar. Seus favoritos, configurações e perfil serão preservados."
    Goto init_done
  new_install:
    StrCpy $INSTDIR "$LOCALAPPDATA\Programs\VOLT"
    StrCpy $InstallMessage "O VOLT será instalado na pasta padrão do seu usuário:$\r$\n$\r$\n$INSTDIR$\r$\n$\r$\nSerão criados atalhos no menu Iniciar e na área de trabalho. Feche o VOLT antes de continuar. Se o WebView2 estiver ausente, será necessária internet."
  init_done:
FunctionEnd
Function DirectoryPagePre
  ; Updates reuse the registered installation and never offer a second location.
  ${If} $Updating == 1
    Abort
  ${EndIf}
FunctionEnd
Function EnsureWebView
  SetRegView 32
  ReadRegStr $RuntimeVersion HKLM "Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" "pv"
  ${If} $RuntimeVersion != ""
  ${AndIf} $RuntimeVersion != "0.0.0.0"
    Return
  ${EndIf}
  ReadRegStr $RuntimeVersion HKCU "Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" "pv"
  ${If} $RuntimeVersion != ""
  ${AndIf} $RuntimeVersion != "0.0.0.0"
    Return
  ${EndIf}
  DetailPrint "Instalando Microsoft Edge WebView2 Runtime…"
  InitPluginsDir
  SetOutPath "$PLUGINSDIR"
  File "MicrosoftEdgeWebview2Setup.exe"
  ExecWait '"$PLUGINSDIR\MicrosoftEdgeWebview2Setup.exe" /silent /install' $0
  ReadRegStr $RuntimeVersion HKCU "Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" "pv"
  ${If} $RuntimeVersion == ""
  ${OrIf} $RuntimeVersion == "0.0.0.0"
    ReadRegStr $RuntimeVersion HKLM "Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" "pv"
  ${EndIf}
  ${If} $RuntimeVersion == ""
  ${OrIf} $RuntimeVersion == "0.0.0.0"
    MessageBox MB_ICONSTOP "Não foi possível preparar o WebView2 (código $0). Verifique a internet, instale o WebView2 Runtime da Microsoft e execute este instalador novamente."
    Abort
  ${EndIf}
FunctionEnd
Section "VOLT" Main
  Call EnsureWebView
  SetOutPath "$INSTDIR\app"
  SetOverwrite on
  File /r /x *.pdb "${PAYLOAD}\*.*"
  CreateDirectory "$SMPROGRAMS\VOLT"
  CreateShortcut "$SMPROGRAMS\VOLT\VOLT.lnk" "$INSTDIR\app\VOLT.exe" "" "$INSTDIR\app\VOLT.exe" 0
  CreateShortcut "$DESKTOP\VOLT.lnk" "$INSTDIR\app\VOLT.exe" "" "$INSTDIR\app\VOLT.exe" 0
  WriteUninstaller "$INSTDIR\Desinstalar-VOLT.exe"
  CreateShortcut "$SMPROGRAMS\VOLT\Desinstalar VOLT.lnk" "$INSTDIR\Desinstalar-VOLT.exe"
  WriteRegStr HKCU "Software\VOLT\Installer" "Location" "$INSTDIR"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\VOLT" "DisplayName" "VOLT"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\VOLT" "DisplayVersion" "0.6.6"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\VOLT" "DisplayIcon" "$INSTDIR\app\VOLT.exe,0"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\VOLT" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\VOLT" "UninstallString" '$\"$INSTDIR\Desinstalar-VOLT.exe$\"'
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\VOLT" "NoModify" 1
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\VOLT" "NoRepair" 1
SectionEnd
Section "Uninstall"
  SetShellVarContext current
  SetRegView 32
  Delete "$DESKTOP\VOLT.lnk"
  Delete "$SMPROGRAMS\VOLT\VOLT.lnk"
  Delete "$SMPROGRAMS\VOLT\Desinstalar VOLT.lnk"
  RMDir "$SMPROGRAMS\VOLT"
  RMDir /r "$INSTDIR\app"
  Delete "$INSTDIR\Desinstalar-VOLT.exe"
  RMDir "$INSTDIR"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\VOLT"
  DeleteRegKey HKCU "Software\VOLT\Installer"
  ; Keep %LocalAppData%\VOLT: bookmarks, settings, profile and login are user data.
SectionEnd
