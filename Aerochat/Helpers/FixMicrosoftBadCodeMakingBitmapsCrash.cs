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
    public class FixMicrosoftBadCodeMakingBitmapsCrash
    {
        public static void InstallHooks()
        {
            Harmony harmony = new("live.aerochat.fixmicrosoftbadcodemakingbitmapscrash");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(BitmapSource))]
    [HarmonyPatch("CreateCachedBitmap")]
    class BitmapSource_FinalizeCreation_HookClass
    {
        // Protection to ensure we don't run the hook multiple times between instances.
        static bool _isRunningBypass = false;

        [HarmonyFinalizer]
        public static Exception Finalizer(object[] __args, Exception __exception, MethodInfo __originalMethod, ref object __result)
        {
            // Bad colour profile. Bypass.
            if (__exception is OverflowException && !_isRunningBypass)
            {
                // Modify the creation parameters to disable the colour profile:
                BitmapCreateOptions createOptions = (BitmapCreateOptions)__args[2];
                createOptions |= BitmapCreateOptions.IgnoreColorProfile;
                __args[2] = createOptions;

                _isRunningBypass = true;
                object result = __originalMethod.Invoke(null, __args); // Original method is the same method.
                _isRunningBypass = false;
                __result = result;

                return null;
            }

            return __exception;
        }
    }
}
