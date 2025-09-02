!define LANG "English"
!insertmacro LANG_STRING STRING_NOT_X64 "Aerochat does not support 32-bit systems."
!insertmacro LANG_STRING STRING_NOT_WIN7 "Aerochat requires Windows 7 or greater."
!insertmacro LANG_STRING STRING_UPGRADE_TITLE "Upgrade"
!insertmacro LANG_STRING STRING_UPGRADE_DESCRIPTION "Prepare to upgrade Aerochat."
!insertmacro LANG_STRING STRING_UPGRADE_BODY "You are about to upgrade your copy of Aerochat. Press $\"Upgrade$\" to continue."
!insertmacro LANG_STRING STRING_UPGRADE_BUTTON "&Upgrade" # Maintain the accelerator when localising please.
!insertmacro LANG_STRING STRING_FOLDER_MUST_BE_EMPTY "The specified installation target folder must be empty."
!insertmacro LANG_STRING STRING_FOLDER_IS_ILLEGAL_OS_FOLDER "You may not use the specified target folder as a installation destination as it is an important operating system folder."
!insertmacro LANG_STRING STRING_WELCOME_TEXT "Setup will guide you through the installation of Aerochat.$\r$\n$\r$\n\
Aerochat is a custom Discord client that replicates the appearance of Windows Live Messenger 2009.$\r$\n$\r$\n\
Please note that Aerochat is still beta software with many unimplemented features and bugs. You use this software at your own risk.$\r$\n$\r$\n\
Click Next to continue."
!insertmacro LANG_STRING STRING_CREATE_DESKTOP_SHORTCUT "Create desktop shortcut"
!insertmacro LANG_STRING STRING_AUTO_OPEN "Start Aerochat once I close this installer"

!insertmacro LANG_STRING STRING_NOTICE_TITLE "Notice"
!insertmacro LANG_STRING STRING_NEEDSKB2533623_DESCRIPTION "Your system requires updates in order to run Aerochat."
!insertmacro LANG_STRING STRING_NEEDSKB2533623_BODY "Your older Windows operating system is lacking the quality-of-life update $\"KB2533623$\", which supplements the functionality of the operating system with new security features.$\r$\n$\r$\nSince the .NET Runtime assumes these security features are available and does not fall back to older functionality, you will have to install this update in order to use Aerochat."
!insertmacro LANG_STRING STRING_NEEDSKB2533623_GETHELP_BUTTON "Get help installing this update"
!insertmacro LANG_STRING STRING_NEEDSKB2533623_GETHELP_LINK "https://github.com/not-nullptr/Aerochat/wiki/Frequently%E2%80%90asked-questions#q-im-on-windows-7-and-nothing-happens-when-i-try-to-open-aerochat"
!insertmacro LANG_STRING STRING_NEEDSKB2533623_CONTINUEANYWAY_BUTTON "Continue anyway"

!insertmacro LANG_STRING STRING_CONTINUEANYWAY_BODY "Are you sure you want to continue? If you continue, Aerochat may not work correctly."

!insertmacro LANG_STRING STRING_STATUS_MSVCRT_ENSURING "Ensuring that Visual C++ Runtime libraries are installed... This may take a second."
!insertmacro LANG_STRING STRING_STATUS_MSVCRT_ENSURED "Visual C++ Runtime libraries are installed."

!insertmacro LANG_STRING STRING_STATUS_DOTNET_ENSURING "Ensuring that the .NET Desktop Runtime 8.0 is installed... This may take a second."
!insertmacro LANG_STRING STRING_STATUS_DOTNET_ENSURED "The .NET Desktop Runtime 8.0 is installed."