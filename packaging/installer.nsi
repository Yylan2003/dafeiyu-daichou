Unicode true
!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "FileFunc.nsh"
Name "大肥鱼代抽1.0"
OutFile "${OUTPUT}"
InstallDir "$LOCALAPPDATA\Programs\DaFeiYuDaiChou"
InstallDirRegKey HKCU "Software\DaFeiYuDaiChou" "InstallDir"
RequestExecutionLevel user
SetCompressor /SOLID lzma
ShowInstDetails show
ShowUninstDetails show
VIProductVersion "1.0.0.0"
VIAddVersionKey /LANG=2052 "ProductName" "大肥鱼代抽1.0"
VIAddVersionKey /LANG=2052 "FileDescription" "大肥鱼代抽1.0 安装程序"
VIAddVersionKey /LANG=2052 "FileVersion" "1.0.0.0"
VIAddVersionKey /LANG=2052 "LegalCopyright" "2026 大肥鱼代抽 contributors"
!define MUI_ABORTWARNING
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "${STAGE}\LICENSE"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "SimpChinese"
Var VerifyOnly

Function .onInit
    ${GetParameters} $0
    ClearErrors
    ${GetOptions} $0 "/VERIFY" $1
    ${If} ${Errors}
        StrCpy $VerifyOnly "0"
    ${Else}
        StrCpy $VerifyOnly "1"
    ${EndIf}
    ReadRegDWORD $0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" "Release"
    ${If} $0 < 528040
        MessageBox MB_OK|MB_ICONSTOP ".NET Framework 4.8 或更高版本尚未安装。请先安装 .NET Framework 4.8，再运行本安装程序。" /SD IDOK
        SetErrorLevel 2
        Abort
    ${EndIf}
FunctionEnd

Section "安装"
    SetShellVarContext current
    SetOutPath "$INSTDIR"
    File /r "${STAGE}\*"
    WriteUninstaller "$INSTDIR\卸载大肥鱼代抽.exe"
    ${If} $VerifyOnly == "1"
        FileOpen $0 "$INSTDIR\.verify-install" w
        FileClose $0
    ${Else}
        CreateDirectory "$SMPROGRAMS\大肥鱼代抽1.0"
        CreateShortcut "$SMPROGRAMS\大肥鱼代抽1.0\大肥鱼代抽1.0.lnk" "$INSTDIR\大肥鱼代抽1.0.exe"
        CreateShortcut "$SMPROGRAMS\大肥鱼代抽1.0\卸载.lnk" "$INSTDIR\卸载大肥鱼代抽.exe"
        CreateShortcut "$DESKTOP\大肥鱼代抽1.0.lnk" "$INSTDIR\大肥鱼代抽1.0.exe"
        WriteRegStr HKCU "Software\DaFeiYuDaiChou" "InstallDir" "$INSTDIR"
        WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DaFeiYuDaiChou" "DisplayName" "大肥鱼代抽1.0"
        WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DaFeiYuDaiChou" "UninstallString" '"$INSTDIR\卸载大肥鱼代抽.exe"'
        WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DaFeiYuDaiChou" "QuietUninstallString" '"$INSTDIR\卸载大肥鱼代抽.exe" /S'
        WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DaFeiYuDaiChou" "DisplayVersion" "1.0.0"
        WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DaFeiYuDaiChou" "DisplayIcon" "$INSTDIR\大肥鱼代抽1.0.exe"
        WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DaFeiYuDaiChou" "InstallLocation" "$INSTDIR"
        WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DaFeiYuDaiChou" "NoModify" 1
        WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DaFeiYuDaiChou" "NoRepair" 1
    ${EndIf}
SectionEnd

Section "Uninstall"
    SetShellVarContext current
    IfFileExists "$INSTDIR\.verify-install" skipIntegration
        Delete "$DESKTOP\大肥鱼代抽1.0.lnk"
        Delete "$SMPROGRAMS\大肥鱼代抽1.0\大肥鱼代抽1.0.lnk"
        Delete "$SMPROGRAMS\大肥鱼代抽1.0\卸载.lnk"
        RMDir "$SMPROGRAMS\大肥鱼代抽1.0"
        DeleteRegKey HKCU "Software\DaFeiYuDaiChou"
        DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DaFeiYuDaiChou"
    skipIntegration:
    !include "${UNINSTALL_LIST}"
    Delete "$INSTDIR\.verify-install"
    Delete "$INSTDIR\卸载大肥鱼代抽.exe"
    RMDir "$INSTDIR"
    ; Personal save files in LocalAppData\SmokeSimulator are intentionally retained.
SectionEnd
