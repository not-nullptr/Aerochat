using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aerochat.Attributes;

namespace Aerochat.Enums
{
    public enum TimeFormat
    {
        [Display("24-Hour Clock")]
        TwentyFourHour,

        [Display("12-Hour Clock (AM/PM)")]
        TwelveHour
    }
}
