using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using HarmonyLib;
using NAudio.MediaFoundation;

namespace Aerochat.Helpers
{
    /// <summary>
    /// Installs our various hooks to fix Microsoft's bugs in WPF with the power of monkey patching.
    /// </summary>
    public class FixMicrosoftBadCodeMakingShitCrash
    {
        public static void InstallHooks()
        {
            Harmony harmony = new("live.aerochat.fixmicrosoftbadcodemakingshitcrash");
            harmony.PatchAll();
        }
    }

    /// <summary>
    /// Monkey patches WPF to prevent unhandled OverflowException objects from crashing the program when
    /// the user's display colour profile does not match whatever bitmap image is being attempted to be
    /// displayed.
    /// </summary>
    /// <remarks>
    /// As I understand it, this is a WPF bug. Since it can literally occur from the bitmap image converter
    /// in XAML, it can be completely opaque and impossible to catch from within C# code without creating
    /// a huge mess. This has been reported to the WPF maintainers before <see href="https://github.com/dotnet/wpf/issues/3884"/>
    /// but has not been truly fixed as I can tell.
    /// </remarks>
    /// <remarks>
    /// Because we never want the program to crash under such silly circumstances, because we are writing
    /// a chatting client where the majority of images that we display are arbitrary, this monkey patch
    /// works better than any "proper" or "safe" way to catch these exceptions in .NET.
    /// </remarks>
    [HarmonyPatch(typeof(BitmapSource))]
    [HarmonyPatch("CreateCachedBitmap")]
    class BitmapSource_FinalizeCreation_HookClass
    {
        // Protection to ensure we don't run the hook multiple times between instances.
        // This prevents us from ever causing infinite recursion.
        static List<uint> _bypassThreadIds = new();

        [HarmonyFinalizer]
        public static Exception Finalizer(object[] __args, Exception __exception, MethodInfo __originalMethod, ref object __result)
        {
            uint currentThreadId = Vanara.PInvoke.Kernel32.GetCurrentThreadId();

            // Bad colour profile. Bypass.
            if (__exception is OverflowException && !_bypassThreadIds.Contains(currentThreadId))
            {
                // Modify the creation parameters to disable the colour profile:
                BitmapCreateOptions createOptions = (BitmapCreateOptions)__args[2];
                createOptions |= BitmapCreateOptions.IgnoreColorProfile;
                __args[2] = createOptions;

                _bypassThreadIds.Add(currentThreadId);
                // "Original method" is actually a misnomer. It refers to the current method, with all the
                // applied hooks. That's not a problem for us because we're augmenting the functionality
                // of the original function rather than overriding it, but it does mean that we need to be
                // careful to place a guard to prevent infinite recursion.
                object result = __originalMethod.Invoke(null, __args);
                _bypassThreadIds.Remove(currentThreadId);
                __result = result;

                return null;
            }

            return __exception;
        }
    }
}
