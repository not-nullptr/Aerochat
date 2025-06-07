using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.Enums
{
    public enum AerochatLoginStatus
    {
        Success,
        UnknownFailure,
        Unauthorized,
        ServerError,
        BadRequest,
    }
}
