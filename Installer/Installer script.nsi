!include "MUI.nsh"

Name "Do-Re-Mi Lyrics"
OutFile "Do-Re-Mi Lyrics Setup.exe"
Unicode True

InstallDir "$PROGRAMFILES\Do-Re-Mi Lyrics"
 
RequestExecutionLevel user
 
!define MUI_ABORTWARNING

!insertmacro MUI_PAGE_LICENSE "..\License"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

Section
 
    SetOutPath $INSTDIR
 
    WriteUninstaller "$INSTDIR\uninstall.exe"
 
    CreateShortcut "$SMPROGRAMS\Do-Re-Mi Lyrics.lnk" "$INSTDIR\Do-Re-Mi Lyrics.exe"
    CreateShortcut "$SMPROGRAMS\Do-Re-Mi Lyrics Uninstall.lnk" "$INSTDIR\uninstall.exe"

    File /r "..\bin\Release\net6.0-windows7.0\*"

SectionEnd
 
Section "uninstall"
 
    Delete "$INSTDIR\uninstall.exe"
    Delete "$SMPROGRAMS\Do-Re-Mi Lyrics.lnk"
    Delete "$SMPROGRAMS\Do-Re-Mi Lyrics Uninstall.lnk"
    Delete "$INSTDIR\*"
    Delete "$INSTDIR\ref\*"
    RMDir "$INSTDIR\ref"
    RMDir $INSTDIR

# uninstaller section end
SectionEnd