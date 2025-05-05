!include MUI2.nsh
!include LogicLib.nsh
!include x64.nsh
!include WinVer.nsh
!include nsProcess.nsh
!include nsDialogs.nsh
!include StrFunc.nsh

Unicode true
Name "Aerochat"
Outfile "../Output/aerochat-setup.exe"
RequestExecutionLevel user
ManifestSupportedOS Win7

!define AEROCHAT_BIN_FOLDER "bin\x64\Release\net8.0-windows7.0"
!define AEROCHAT_VERSION "0.2.0"

# Required for detecting running Aerochat processes and closing them:
!define AEROCHAT_APP_GUID_NOBRACE "8231C4FA-AD94-487A-BDBF-A936306AE009"
!define AEROCHAT_APP_GUID "{${AEROCHAT_APP_GUID_NOBRACE}}"
!define AEROCHAT_MESSAGE_WINDOW_CLASS "Aerochat_MessageWindow_${AEROCHAT_APP_GUID_NOBRACE}"
!define WM_AEROCHAT_CLOSE_FOR_REINSTALLATION 32794 # WM_APP + 26

!define UNINSTALL_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\Aerochat"

VIProductVersion "${AEROCHAT_VERSION}.0"
VIFileVersion "${AEROCHAT_VERSION}.0"
VIAddVersionKey "FileDescription" "Aerochat Setup v${AEROCHAT_VERSION}"
VIAddVersionKey "FileVersion" "${AEROCHAT_VERSION}"
VIAddVersionKey "LegalCopyright" "nullptr"

# Required for detecting the old Inno Setup version and migrating.
!define LEGACY_INNO_SETUP_APP_ID "{A44D14A8-326F-45DF-A991-5A0D0EA526DF}"
!define LEGACY_INNO_SETUP_REGISTRY_PATH "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\${LEGACY_INNO_SETUP_APP_ID}_is1\"
!define LEGACY_INNO_SETUP_INSTALL_PATH "$LOCALAPPDATA\Aerochat"
!define LEGACY_INNO_SETUP_UNINSTALLER "${LEGACY_INNO_SETUP_INSTALL_PATH}\unins000.exe"
!define LEGACY_INNO_SETUP_CONFIG_FILE "Aerochat.json"

${Using:StrFunc} StrCase
${Using:StrFunc} UnStrCase

!define MULTIUSER_EXECUTIONLEVEL Highest
!define MULTIUSER_INSTALLMODE_INSTDIR "$(^Name)"
!define MULTIUSER_MUI
!include MultiUser.nsh

!define MUI_ICON "../Aerochat/Icons/MainWnd.ico"
!define MUI_UNICON "../Aerochat/Icons/MainWnd.ico"
!define MUI_WELCOMEFINISHPAGE_BITMAP "installer_sidebar.bmp"
!define MUI_UNWELCOMEFINISHPAGE_BITMAP "installer_sidebar.bmp"
!define MUI_ABORTWARNING
!define MUI_UNABORTWARNING
!define MUI_FINISHPAGE_NOAUTOCLOSE
!define MUI_UNFINISHPAGE_NOAUTOCLOSE

!define MUI_WELCOMEPAGE_TEXT "$(STRING_WELCOME_TEXT)"
!insertmacro MUI_PAGE_WELCOME

Page custom PageUpgradeConfirmation

# We don't want to show install destination options if we're upgrading.
!define MUI_PAGE_CUSTOMFUNCTION_PRE SkipPageIfAlreadyInstalled
!insertmacro MULTIUSER_PAGE_INSTALLMODE

!define MUI_PAGE_CUSTOMFUNCTION_PRE SkipPageIfAlreadyInstalled
!define MUI_PAGE_CUSTOMFUNCTION_LEAVE ValidateInstallationDirectory
!insertmacro MUI_PAGE_DIRECTORY

!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

!macro LANG_LOAD LANGLOAD
    !insertmacro MUI_LANGUAGE "${LANGLOAD}"
    !include "l10n\${LANGLOAD}.nsh"
    !undef LANG
!macroend
 
!macro LANG_STRING NAME VALUE
    LangString "${NAME}" "${LANG_${LANG}}" "${VALUE}"
!macroend

!insertmacro LANG_LOAD "English"

Var AlreadyInstalled

Function IsApplicationAlreadyInstalled
    # register: $R0 = return value: Boolean
    
    SetRegView 64
    ReadRegStr $R0 HKLM "${UNINSTALL_KEY}" "UninstallString"
    SetRegView lastused
    
    IfErrors _TryHKCU
    StrCpy $R0 "true"
    return
    
_TryHKCU:
    SetRegView 64
    ReadRegStr $R0 HKCU "${UNINSTALL_KEY}" "UninstallString"
    SetRegView lastused
    
    IfErrors _nexists
    StrCpy $R0 "true"
    return
_nexists:

    StrCpy $R0 "false"
    return
FunctionEnd

Function SkipPageIfAlreadyInstalled
    ${If} $AlreadyInstalled > 0
        Abort
    ${EndIf}
FunctionEnd

Function SkipPageIfNotInstalled
    ${If} $AlreadyInstalled < 1
        Abort
    ${EndIf}
FunctionEnd

# I stole this from some guy on a forum in 2002.
# https://nsis-dev.github.io/NSIS-Forums/html/t-82300.html
Function IsDirEmpty

    push $R1
    push $R2

    ClearErrors
    IfFileExists "$0" 0 not_exists
    FindFirst $R1 $R2 "$0\*.*"
    IfErrors no_files
    FindNext $R1 $R2
    IfErrors no_files
    FindNext $R1 $R2
    IfErrors no_files
    ;nicht leer
    FindClose $R1
    ClearErrors
    goto end

    ;Leer
no_files:
    FindClose $R1
    SetErrors
    goto end

    ;nicht existent
not_exists:
    SetErrors

end:
    pop $R2
    pop $R1

FunctionEnd

Function ValidateInstallationDirectory
    # If the user specified the legacy install path, then always pass.
    StrCmp $InstDir ${LEGACY_INNO_SETUP_INSTALL_PATH} _pass
    
    # Otherwise, if the selected directory is any important system folder,
    # immediately request the request.
    StrCmp $InstDir $WINDIR _FailBecauseOfProtectedOsFolder
    StrCmp $InstDir $PROGRAMFILES _FailBecauseOfProtectedOsFolder
    StrCmp $InstDir $PROGRAMFILES32 _FailBecauseOfProtectedOsFolder
    StrCmp $InstDir $PROGRAMFILES64 _FailBecauseOfProtectedOsFolder
    
    # Otherwise, make sure the specified directory is empty:
    Push $0
    StrCpy $0 $InstDir
    Call IsDirEmpty
    Pop $0
    IfErrors 0 _fail
    
    goto _pass
    
_fail:
    MessageBox MB_OK|MB_ICONEXCLAMATION "$(STRING_FOLDER_MUST_BE_EMPTY)"
    Abort
    return

_FailBecauseOfProtectedOsFolder:
    MessageBox MB_OK|MB_ICONEXCLAMATION "$(STRING_FOLDER_IS_ILLEGAL_OS_FOLDER)"
    Abort
    return
    
_pass:
FunctionEnd

Function .onInit
    # NSIS produces an x86-32 installer. Deny installation if
    # we're not on a x86-64 or ARM64 system running WOW64.
    ${IfNot} ${RunningX64}
        MessageBox MB_OK|MB_ICONSTOP "$(STRING_NOT_X64)"
        Quit
    ${EndIf}
    
    # Need at least Windows 7.
    ${IfNot} ${AtLeastWin7}
        MessageBox MB_OK|MB_ICONSTOP "$(STRING_NOT_WIN7)"
        Quit
    ${EndIf}
    
    !insertmacro MULTIUSER_INIT
    
    Call IsApplicationAlreadyInstalled
    StrCmp $R0 "false" _NotAlreadyInstalled
    
    StrCpy $AlreadyInstalled 1
    
    return
    
_NotAlreadyInstalled:
    StrCpy $AlreadyInstalled 0
FunctionEnd

Function IsInnoSetupVersionInstalled
    # stack: $0 = return value: Uninstall file path if possible, or "null"
    
    ReadRegStr $0 HKCU "${LEGACY_INNO_SETUP_REGISTRY_PATH}" "UninstallString"
    
    IfErrors _nexists
    return
    
_nexists:
    StrCpy $0 "null"
    return
FunctionEnd

Function InnoSetupMigration
    # stack: $0 = local
    #        $1 = local
    
    Push $0
    Call IsInnoSetupVersionInstalled # returns to $0
    StrCmp $0 "null" _exit
    
    DetailPrint "Uninstalling old version of Aerochat..."
    
    # Old Inno Setup versions of Aerochat don't open a mutex to notify if they're running, so we just kill
    # any running process named "aerochat.exe"
    Push $1
    ${nsProcess::KillProcess} "Aerochat.exe" $1
    Pop $1
    # Above command pops $1 internally
    
    DetailPrint "Ensured that Aerochat.exe is not running."
    
    # Because the Inno Setup version exists, we're going to make two transformations.
    #    1. Aerochat v0.1 (versions using Inno Setup) were always installed to local appdata
    #       and stored their configuration alongside the program in Aerochat.json. We want
    #       to move existing user configuration to roaming appdata.
    #    2. Completely uninstall the Inno Setup version. The user has the option to change
    #       the installation directory starting now.
    SetShellVarContext current # In order to get the right appdata folder, we must use current user shell context.
    IfFileExists "${LEGACY_INNO_SETUP_INSTALL_PATH}\${LEGACY_INNO_SETUP_CONFIG_FILE}" 0 _SkipMoveLogic
    CreateDirectory "$APPDATA\Aerochat" # Ensure that the application data directory exists.
    IfErrors _EnsureAppDataDirFailure _EnsureAppDataDirSuccess
_EnsureAppDataDirFailure:
    DetailPrint "Failed to create roaming application data folder '$APPDATA\Aerochat'."
_EnsureAppDataDirSuccess:
    Rename "${LEGACY_INNO_SETUP_INSTALL_PATH}\${LEGACY_INNO_SETUP_CONFIG_FILE}" "$APPDATA\Aerochat\config.json"
    IfErrors _MoveConfigError
    DetailPrint "Moved user configuration file to '$APPDATA\Aerochat\config.json'."
    goto _MoveConfigSuccess
_MoveConfigError:
    DetailPrint "Failed to move user configuration file to '$APPDATA\Aerochat\config.json'."
    goto _MoveConfigSuccess # haha
_SkipMoveLogic:
    DetailPrint "User did not have a configuration file; skipping."
    # [fallthrough]
_MoveConfigSuccess:
    SetShellVarContext lastused # Restore the previous shell variable context.
    
    DetailPrint "Executing old version uninstaller..."
    ExecWait '$0 /VERYSILENT /NORESTART' # /VERYSILENT prevents a new window from popping up on screen.
    DetailPrint "Done running old version uninstaller."
    
    # The Inno Setup uninstaller has no problem with leaving behind files. To ensure a clean erasure,
    # we'll just delete the whole directory it was formerly installed in.
    DetailPrint "Ensuring complete cleanup..."
    RMDir /r "${LEGACY_INNO_SETUP_INSTALL_PATH}"
    DeleteRegKey HKCU "${LEGACY_INNO_SETUP_REGISTRY_PATH}"
    DetailPrint "Done ensuring complete cleanup."
    
    DetailPrint "Successfully uninstalled old version of Aerochat."
    
_exit:
    Pop $0
FunctionEnd

!macro DeclareIsApplicationMutexOpen prefix
Function ${prefix}IsApplicationMutexOpen
    # register: $R0 = return value: Boolean
    
    # The GUID name used by the mutex is stored in lowercase and without braces.
    !if `${prefix}` == "un."
        ${UnStrCase} $R0 ${AEROCHAT_APP_GUID_NOBRACE} "L"
    !else
        ${StrCase} $R0 ${AEROCHAT_APP_GUID_NOBRACE} "L"
    !endif
    
    System::Call 'kernel32::OpenMutex(i 0x100000, i 0, t "$R0")p.R0'
    IntPtrCmp $R0 0 _NotOpen
    System::Call 'kernel32::CloseHandle(p $R0)'
    StrCpy $R0 "true"
    return

_NotOpen:
    StrCpy $R0 "false"
FunctionEnd
!macroend

!insertmacro DeclareIsApplicationMutexOpen "" # Installer
!insertmacro DeclareIsApplicationMutexOpen "un." # Uninstaller

!macro DeclareEnsureApplicationClosed prefix
Function ${prefix}EnsureApplicationClosed
    # register: $R0 = work register; dirtied.
    # register: $R1 = work register.
    
    DetailPrint "Making sure that there isn't any Aerochat process running..."
    
    # Initially check if the application mutex is open.
    Call ${prefix}IsApplicationMutexOpen
    StrCmp $R0 "true" 0 _NotRunning
    
    # If the mutex is open, then we will try to find the message window.
    # This will allow us to exit rather gracefully.
    FindWindow $R0 "${AEROCHAT_MESSAGE_WINDOW_CLASS}"
    IntPtrCmp $R0 0 _LogFindMessageFailureAndForcefullyClose
    
    # If we found the message window, then we'll post a quit message and wait
    # for the process to close.
    DetailPrint "Sending quit message to Aerochat message window..."
    SendMessage $R0 ${WM_AEROCHAT_CLOSE_FOR_REINSTALLATION} 0 0
    
    # Set up for the sleep loop:
    Push $R1
    StrCpy $R1 10 # Wait for 1 seconds ten times = 10 seconds total
    
_SleepLoop:
    Sleep 1000 # Ensure that the operating system has had enough time to clean up resources
               # used by this process.
    
    # Just in case, we're going to requery if the mutex is open. If it's still
    # open, then we didn't really exit, and there's more work to do.
    Call ${prefix}IsApplicationMutexOpen
    StrCmp $R0 "true" 0 _MutexClosedNicely
    
    # Otherwise, we're going to loop to give the program enough time to finish closing:
    IntOp $R1 $R1 - 1
    StrCmp $R1 0 _ExitSleepLoop
    
    goto _SleepLoop
    
_ExitSleepLoop:
    Pop $R1
    DetailPrint "Failed to safely close the Aerochat process despite our best efforts."
    goto _ForcefullyClose
    
_MutexClosedNicely:
    # Otherwise, we successfully ensured that the process is closed and we can continue on
    # with our lives:
    return
    
_LogFindMessageFailureAndForcefullyClose:
    DetailPrint "Failed to find the Aerochat message window, \
however we know for a fact that the process is running."
    goto _ForcefullyClose
    
_ForcefullyClose:
    # If we ended up here, then we failed at any sort of graceful exit.
    # We will just kill any process named "Aerochat.exe" and hope for the
    # best...
    DetailPrint "Forcefully closing any process named Aerochat.exe."
    ${nsProcess::KillProcess} "Aerochat.exe" $R0
    Sleep 5000 # Ensure that the operating system has had enough time to clean up resources
               # used by this process.
    
    return
    
_NotRunning:
    DetailPrint "No process was running. Nice :3"
FunctionEnd
!macroend

!insertmacro DeclareEnsureApplicationClosed "" # Installer
!insertmacro DeclareEnsureApplicationClosed "un." # Uninstaller

Function PageUpgradeConfirmation
    Call SkipPageIfNotInstalled
    
    # Find the installation directory:
    SetRegView 64
    ReadRegStr $R0 HKLM "${UNINSTALL_KEY}" "UninstallString"
    SetRegView lastused
    
    IfErrors _TryHKCU
    
    goto _GetInstallDir
    
_TryHKCU:
    SetRegView 64
    ReadRegStr $R0 HKCU "${UNINSTALL_KEY}" "UninstallString"
    SetRegView lastused
    
_GetInstallDir:
    Push $R1
    StrLen $R1 $R0
    IntOp $R1 $R1 - 16 # Length of "\uninstall.exe" minus the first quote and the final quote
    StrCpy $InstDir $R0 $R1 1
    Pop $R1
    
    !insertmacro MUI_HEADER_TEXT "$(STRING_UPGRADE_TITLE)" "$(STRING_UPGRADE_DESCRIPTION)"
    
    nsDialogs::Create 1018
    Pop $0
    
    ${If} $0 == error
        Abort
    ${EndIf}
    
    ${NSD_CreateLabel} 0 0 100% 12u "$(STRING_UPGRADE_BODY)"
    Pop $0
    
    Push $R0
    GetDlgItem $R0 $HWNDPARENT 1
    SendMessage $R0 ${WM_SETTEXT} 0 "STR:$(STRING_UPGRADE_BUTTON)"
    Pop $R0
    
    nsDialogs::Show
FunctionEnd

Section "Aerochat" Aerochat
    # Required
    SectionIn RO
    
    # Migrate configuration from Inno Setup version and uninstall existing version (if it exists)
    Call InnoSetupMigration
    
    # Otherwise, ensure that the application is closed so that we can upgrade it.
    # Note that this function only handles NSIS-installer versions of Aerochat. Legacy Inno Setup
    # versions are forcefully closed through their process name in InnoSetupMigration.
    Call EnsureApplicationClosed

    # Install files
    SetOutPath "$InstDir"
    WriteUninstaller "$InstDir\uninstall.exe"
    File /r /x *.pdb /x *.xml /x Aerochat.json "..\Aerochat\${AEROCHAT_BIN_FOLDER}\*"

    # Create shortcut
    SetShellVarContext all
    CreateShortCut "$SMPROGRAMS\Aerochat.lnk" \
        "$InstDir\Aerochat.exe"
    SetShellVarContext lastused
    
    # Create Uninstall entry
    SetRegView 64
    WriteRegStr SHCTX "${UNINSTALL_KEY}" \
                 "DisplayName" "Aerochat"
    WriteRegStr SHCTX "${UNINSTALL_KEY}" \
                 "DisplayIcon" "$InstDir\Aerochat.exe,0"
    WriteRegStr SHCTX "${UNINSTALL_KEY}" \
                 "UninstallString" "$\"$InstDir\uninstall.exe$\""
    WriteRegStr SHCTX "${UNINSTALL_KEY}" \
                 "Publisher" "nullptr"
    WriteRegStr SHCTX "${UNINSTALL_KEY}" \
                 "DisplayVersion" "${AEROCHAT_VERSION}"
    WriteRegDWORD SHCTX "${UNINSTALL_KEY}" \
                 "NoModify" 1
    WriteRegDWORD SHCTX "${UNINSTALL_KEY}" \
                 "NoRepair" 1
SectionEnd

Function un.onInit
    !insertmacro MULTIUSER_UNINIT
FunctionEnd

Section "Uninstall"
    # Otherwise, ensure that the application is closed so that we can upgrade it.
    # Note that this function only handles NSIS-installer versions of Aerochat. Legacy Inno Setup
    # versions are forcefully closed through their process name in InnoSetupMigration.
    Call un.EnsureApplicationClosed

    # Delete files
    RMDir /r "$InstDir"

    # Delete shortcut
    SetShellVarContext all
    Delete "$SMPROGRAMS\Aerochat.lnk"
    SetShellVarContext lastused

    # Delete uninstall entry
    SetRegView 64
    DeleteRegKey HKCU "${UNINSTALL_KEY}" # SHCTX doesn't seem to work properly, so we'll also just
                                         # remove indiscriminately under HKCU
    DeleteRegKey SHCTX "${UNINSTALL_KEY}"
SectionEnd