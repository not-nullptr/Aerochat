using Aerochat.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.Enums
{
    public enum BasicTitlebarSetting
    {
        [Display("Automatically determine based on my theme")]
        Automatic,

        [Display("Always use native OS titlebar")]
        AlwaysNative,

        [Display("Always use custom basic-theme-fallback titlebar")]
        AlwaysNonNative,
    }
}
