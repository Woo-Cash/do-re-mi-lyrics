!include "MUI.nsh"

!define MUI_ABORTWARNING # This will warn the user if they exit from the installer.

!insertmacro MUI_PAGE_WELCOME # Welcome to the installer page.
!insertmacro MUI_PAGE_DIRECTORY # In which folder install page.
!insertmacro MUI_PAGE_INSTFILES # Installing page.
!insertmacro MUI_PAGE_FINISH # Finished installation page.

!insertmacro MUI_LANGUAGE "English"

# define name of installer
Name "Do-Re-Mi Lyrics" # Name of the installer (usually the name of the application to install).
OutFile "Do-Re-Mi Lyrics Setup.exe"
 
# define installation directory
InstallDir "$PROGRAMFILES\Do-Re-Mi Lyrics"
 
# For removing Start Menu shortcut in Windows 7
RequestExecutionLevel user
 
# start default section
Section
 
    # set the installation directory as the destination for the following actions
    SetOutPath $INSTDIR
 
    # create the uninstaller
    WriteUninstaller "$INSTDIR\uninstall.exe"
 
    # point the new shortcut at the program uninstaller
    CreateShortcut "$SMPROGRAMS\Do-Re-Mi Lyrics.lnk" "$INSTDIR\Do-Re-Mi Lyrics.exe"
    CreateShortcut "$SMPROGRAMS\Do-Re-Mi Lyrics Uninstall.lnk" "$INSTDIR\uninstall.exe"

    File /r "..\bin\Release\net6.0-windows7.0\*"

SectionEnd
 
# uninstaller section start
Section "uninstall"
 
    # first, delete the uninstaller
    Delete "$INSTDIR\uninstall.exe"
 
    # second, remove the link from the start menu
    Delete "$SMPROGRAMS\Do-Re-Mi Lyrics.lnk"
    Delete "$SMPROGRAMS\Do-Re-Mi Lyrics Uninstall.lnk"
 
    Delete $INSTDIR

# uninstaller section end
SectionEnd