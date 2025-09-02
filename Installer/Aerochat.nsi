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
!define AEROCHAT_VERSION "0.2.4"
!define AEROCHAT_RC true
!define AEROCHAT_RC_VERSION "Stability Test Release"

!if AEROCHAT_RC
    !define AEROCHAT_RC_SUFFIX " ${AEROCHAT_RC_VERSION}"
!else
    !define AEROCHAT_RC_SUFFIX ""
!endif

# Wrapped in a macro to make it super easy to turn off for testing purposes (makes build go WAY faster)
# Just search for "${PLACE_FILE}" and comment out such lines, instead of having to search for "File"
# which is a lot harder.
!define PLACE_FILE File

# Required for detecting running Aerochat processes and closing them:
!define AEROCHAT_APP_GUID_NOBRACE "8231C4FA-AD94-487A-BDBF-A936306AE009"
!define AEROCHAT_APP_GUID "{${AEROCHAT_APP_GUID_NOBRACE}}"
!define AEROCHAT_MESSAGE_WINDOW_CLASS "Aerochat_MessageWindow_${AEROCHAT_APP_GUID_NOBRACE}"
!define WM_AEROCHAT_CLOSE_FOR_REINSTALLATION 32794 # WM_APP + 26

!define UNINSTALL_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\Aerochat"

VIProductVersion "${AEROCHAT_VERSION}.0"
VIFileVersion "${AEROCHAT_VERSION}.0"
VIAddVersionKey "FileDescription" "Aerochat Setup v${AEROCHAT_VERSION}${AEROCHAT_RC_SUFFIX}"
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

Function CreateDesktopShortcut
    CreateShortcut "$desktop\Aerochat.lnk" "$instdir\Aerochat.exe"
FunctionEnd

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

Page custom PageCheckLoadLibraryExEligibility # For Windows < 8
Page custom PageUpgradeConfirmation

# We don't want to show install destination options if we're upgrading.
!define MUI_PAGE_CUSTOMFUNCTION_PRE SkipPageIfAlreadyInstalled
!insertmacro MULTIUSER_PAGE_INSTALLMODE

!define MUI_PAGE_CUSTOMFUNCTION_PRE SkipPageIfAlreadyInstalled
!define MUI_PAGE_CUSTOMFUNCTION_LEAVE ValidateInstallationDirectory
!insertmacro MUI_PAGE_DIRECTORY

!insertmacro MUI_PAGE_INSTFILES

!define MUI_FINISHPAGE_RUN "$instdir\Aerochat.exe"
!define MUI_FINISHPAGE_RUN_TEXT "$(STRING_AUTO_OPEN)"
# We overload the "show readme" built-in control on the finish page to create a desktop
# shortcut, since this is a more useful functionality, and we don't even have a readme
# to begin with.
!define MUI_FINISHPAGE_SHOWREADME ""
!define MUI_FINISHPAGE_SHOWREADME_TEXT "$(STRING_CREATE_DESKTOP_SHORTCUT)"
!define MUI_FINISHPAGE_SHOWREADME_FUNCTION CreateDesktopShortcut
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

Function OnClickGetHelpForLoadLibraryExUpdateLink
    ExecShell open "$(STRING_NEEDSKB2533623_GETHELP_LINK)"
FunctionEnd

Function OnClickContinueAnywayButton
    MessageBox MB_YESNO|MB_ICONEXCLAMATION "$(STRING_CONTINUEANYWAY_BODY)" IDYES _IfContinue
    Return
    
_IfContinue:
    # Move to the next page.
    SendMessage $HWNDPARENT 0x408 1 0
FunctionEnd

# Checks for LoadLibraryEx eligiblity by attempting to load a module with it and catching
# the failure state.
#
# This will detect if the user doesn't have the update KB2533623 installed on their Windows
# Vista or Windows 7 system, and thus wouldn't be able to run Aerochat.
Function PageCheckLoadLibraryExEligibility
    Push $0
    
    # 0x00000800 = LOAD_LIBRARY_SEARCH_SYSTEM32
    System::Call 'Kernel32::LoadLibraryEx(t "kernel32.dll", i 0, i 0x00000800)p .r0'
    
    StrCmp $0 0 _FailedToLoad
    
    # If we succeeded, then we'll obviously skip this page. It's not necessary.
    # This will be the path on all eligible systems.
    System::Call 'Kernel32::FreeLibrary(pr0)'
    
    Pop $0
    Abort
    Return
    
_FailedToLoad:
    Pop $0
    
    !insertmacro MUI_HEADER_TEXT "$(STRING_NOTICE_TITLE)" "$(STRING_NEEDSKB2533623_DESCRIPTION)"
    
    nsDialogs::Create 1018
    Pop $0
    
    ${If} $0 == error
        Abort
    ${EndIf}
    
    # We need to measure the whole "inner dialog" of NSIS in order to get bounds for measuring
    # the text. Once we have that, we can figure out how to position everything. This is a lot
    # of work, but it does work out in the end.
    #
    Push $0 # HWND of inner dialog
    Push $1 # Pointer scratch
    Push $2
    Push $3
    Push $4
    Push $5
    Push $6
    
    FindWindow $0 "#32770" "" $HWNDPARENT # Get NSIS inner dialog.
    
    System::Alloc 16 # RECT structure is 16 bytes
    Pop $1
    
    # Prepare DC for text measurement:
    System::Call "User32::GetDC(pr0)p .r2"
    SendMessage $HWNDPARENT ${WM_GETFONT} 0 0 $3
    System::Call "Gdi32::SelectObject(pr2, pr3)p .r4"
    
    # Measure text:
    System::Call "User32::GetClientRect(pr0, pr1)"
    System::Call 'User32::DrawText(pr2, t `$(STRING_NEEDSKB2533623_BODY)`, i -1, pr1, i 0x410)' # 0x400 = DT_CALCRECT
                                                                                                #  0x10 = DT_WORDBREAK
    
    # Copy measured text into registers for access
    System::Call "*$1(i .r3, i .r4, i .r5, i .r6)"
    
    ${NSD_CreateLabel} $3 $4 $5 $6 "$(STRING_NEEDSKB2533623_BODY)"
    Pop $0
    
    IntOp $6 $6 + 20
    
    # Measure text:
    FindWindow $0 "#32770" "" $HWNDPARENT # Get NSIS inner dialog.
    System::Call "User32::GetClientRect(pr0, pr1)"
    System::Call 'User32::DrawText(pr2, t `$(STRING_NEEDSKB2533623_GETHELP_BUTTON)`, i -1, pr1, i 0x400)' # 0x400 = DT_CALCRECT
    
    # Copy measured text into registers for access
    System::Call "*$1(i, i, i .r3, i .r4)" # Preserve R6 for vertical offset.
    
    ${NSD_CreateLink} 0 $6 $3 $4 "$(STRING_NEEDSKB2533623_GETHELP_BUTTON)"
    Pop $0
    ${NSD_OnClick} $0 OnClickGetHelpForLoadLibraryExUpdateLink
    
    IntOp $6 $6 + $4
    IntOp $6 $6 + 10
    
    # Measure text:
    FindWindow $0 "#32770" "" $HWNDPARENT # Get NSIS inner dialog.
    System::Call "User32::GetClientRect(pr0, pr1)"
    System::Call 'User32::DrawText(pr2, t `$(STRING_NEEDSKB2533623_CONTINUEANYWAY_BUTTON)`, i -1, pr1, i 0x400)' # 0x400 = DT_CALCRECT
    
    # Copy measured text into registers for access
    System::Call "*$1(i, i, i .r3, i .r4)" # Preserve R6 for vertical offset.
    
    ${NSD_CreateLink} 0 $6 $3 $4 "$(STRING_NEEDSKB2533623_CONTINUEANYWAY_BUTTON)"
    Pop $0
    ${NSD_OnClick} $0 OnClickContinueAnywayButton
    
    # Reset DC now that we're done with measurement:
    System::Call "Gdi32::SelectObject(pr2, pr4)"
    System::Call "User32::ReleaseDC(pr2)"
    
    System::Free $1
    
    Pop $6
    Pop $5
    Pop $4
    Pop $3
    Pop $2
    Pop $1
    Pop $0
    
    Push $R0
    GetDlgItem $R0 $HWNDPARENT 1
    EnableWindow $R0 0
    Pop $R0
    
    nsDialogs::Show
FunctionEnd

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
    ${PLACE_FILE} /r /x *.pdb /x *.xml /x Aerochat.json "..\Aerochat\${AEROCHAT_BIN_FOLDER}\*"
    
!if AEROCHAT_RC # We include PDBs for RC builds.
    ${PLACE_FILE} /r "..\Aerochat\${AEROCHAT_BIN_FOLDER}\*.pdb"
!else
    Delete "$InstDir\*.pdb" # Delete all PDBs from a previous RC build, if present.
!endif
    
    # The .NET runtime requires the Visual C++ Runtime, which is not guaranteed to be installed
    # on the user's system. We will just try to install it no matter what, and nothing should
    # happen on a regular user's system.
    Push $0
    DetailPrint "$(STRING_STATUS_MSVCRT_ENSURING)"
    
    ReadRegDword $0 HKLM "SOFTWARE\Wow6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\X64" "Bld"
    IntCmp $0 23026 0 0 _VcrtAlreadyInstalled # 23026 = 2015 runtime build number
    
    SetDetailsPrint listonly # Prefer displaying the user-friendly message since this is a long action.
    ${PLACE_FILE} ".\VC_redist.x64.exe"
    ExecWait '"$INSTDIR\VC_redist.x64.exe" /quiet /norestart'
    SetDetailsPrint both
    Delete "$INSTDIR\VC_redist.x64.exe"
    
_VcrtAlreadyInstalled:
    DetailPrint "$(STRING_STATUS_MSVCRT_ENSURED)"
    Pop $0
    
    # Aerochat requires the .NET runtime, of course.
    DetailPrint "$(STRING_STATUS_DOTNET_ENSURING)"
    SetDetailsPrint listonly # Prefer displaying the user-friendly message since this is a long action.
    ${PLACE_FILE} ".\windowsdesktop-runtime-8.0.19-win-x64.exe"
    ExecWait '"$INSTDIR\windowsdesktop-runtime-8.0.19-win-x64.exe" /quiet /norestart'
    SetDetailsPrint both
    Delete "$INSTDIR\windowsdesktop-runtime-8.0.19-win-x64.exe"
    DetailPrint "$(STRING_STATUS_DOTNET_ENSURED)"

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
                 "DisplayVersion" "${AEROCHAT_VERSION}${AEROCHAT_RC_SUFFIX}"
    WriteRegDWORD SHCTX "${UNINSTALL_KEY}" \
                 "NoModify" 1
    WriteRegDWORD SHCTX "${UNINSTALL_KEY}" \
                 "NoRepair" 1
SectionEnd

Function un.onInit
    !insertmacro MULTIUSER_UNINIT
FunctionEnd

!define CLSID_ShellLinkW {00021401-0000-0000-C000-000000000046}
!define IID_IShellLinkW  {000214F9-0000-0000-C000-000000000046}
!define IID_IPersistFile {0000010B-0000-0000-C000-000000000046}
!define CLSCTX_INPROC_SERVER 0x1

# COM interface method offsets
!define IUnknown_QueryInterface 0
!define IUnknown_AddRef 1
!define IUnknown_Release 2

!define IShellLinkW_GetPath 3
!define IShellLinkW_GetIDList 4 # We only need this

!define IPersistFile_GetClassID 3
!define IPersistFile_IsDirty 4
!define IPersistFile_Load 5
!define IPersistFile_Save 6
!define IPersistFile_SaveCompleted 7
!define IPersistFile_GetCurFile 8

!define STGM_READ 0x0

!define MAX_PATH 260

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

!if 0 # Currently does not work, so it's disabled for the Stability Test Release.
    # Since we want to be nice, we will only delete the desktop shortcut if we know it is Aerochat.
    IfFileExists "$desktop\Aerochat.lnk" 0 _NoDesktopShortcut
    
    Push $0 # Pointer to IShelLLink object
    Push $1 # HRESULT
    Push $2 # Pointer to ITEMIDLIST structure
    Push $3 # Pointer to IPersistFile object
    Push $4 # Pointer to path string
    
    ; System::StrAlloc ${MAX_PATH} + 1
    ; Pop $4
    
    System::Call "ole32::CoCreateInstance(g '${CLSID_ShellLinkW}', p 0, i ${CLSCTX_INPROC_SERVER}, g '${IID_IShellLinkW}', *p .r0)i .r1"
    
    # Check if HR failed
    IntCmp $1 0 0 _FailedCreateShellLink 0
    
    # Cast to IPersistFile:
    System::Call "$0->${IUnknown_QueryInterface}(g '${IID_IPersistFile}', *p .r3)i .r1"
    IntCmp $1 0 0 _FailedCreatePersistFile 0
    
    System::Call "$3->${IPersistFile_Load}(t '$desktop\Aerochat.lnk', i ${STGM_READ})i .r1"
    IntCmp $1 0 0 _FailedLoadLinkFile 0
    
    System::Call "$0->${IShellLinkW_GetIDList}(*p .r2)i .r1"
    IntCmp $1 0 0 _FailedGetIdList 0
    
    MessageBox MB_OK "$2 $1"
    
    System::Call "shell32:SHGetPathFromIDListW(pr2, w .r4)i .r1" # R1 actually holds a BOOL in this case
    StrCmp $1 0 _FailedGetPath
    
    # XXX: See if this is necessary
    #Push $5
    #System::Call "*$4(&t${MAX_PATH} .r5)" # Read the pointer into 
    MessageBox MB_OK "Hello $4"
    #Pop $5
    
    Goto _CleanUpDesktopShortcutDeletion

_FailedCreateShellLink:
    DetailPrint "Failed to create IShellLink object to delete desktop shortcut. $1"
    Goto _CleanUpDesktopShortcutDeletion

_FailedCreatePersistFile:
    DetailPrint "Failed to create IPersistFile. $1"
    Goto _CleanUpDesktopShortcutDeletion

_FailedLoadLinkFile:
    DetailPrint "Failed IPersistFile::Load. $1"
    Goto _CleanUpDesktopShortcutDeletion

_FailedGetIdList:
    DetailPrint "Failed IShellLink::GetIDList. $1"
    Goto _CleanUpDesktopShortcutDeletion

_FailedGetPath:
    DetailPrint "Failed SHGetPathFromIDListW. $1"
    Goto _CleanUpDesktopShortcutDeletion
    
_CleanUpDesktopShortcutDeletion: # We'll deem this "CUDSD" for sub-labels.
    ; System::Free $4

    StrCmp $2 0 _CUDSD_NoItemIdList
    System::Call "ole32:CoTaskMemFree(pr2)"

_CUDSD_NoItemIdList:
    StrCmp $0 0 _CUDSD_NoInstancePersistFile # This is a basically a C pattern `if (pUnk) pUnk->Release();`
                                             # Obviously, we need the compare because we can't call a method on a null pointer.
    System::Call "$3->${IUnknown_Release}()"

_CUDSD_NoInstancePersistFile:
    StrCmp $0 0 _CUDSD_NoInstanceShellLink
    System::Call "$0->${IUnknown_Release}()"

_CUDSD_NoInstanceShellLink:

    Pop $4
    Pop $3
    Pop $2
    Pop $1
    Pop $0
    
_NoDesktopShortcut:

!endif

    # Delete uninstall entry
    SetRegView 64
    DeleteRegKey HKCU "${UNINSTALL_KEY}" # SHCTX doesn't seem to work properly, so we'll also just
                                         # remove indiscriminately under HKCU
    DeleteRegKey SHCTX "${UNINSTALL_KEY}"
SectionEnd