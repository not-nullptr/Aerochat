using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using static Vanara.PInvoke.Ole32.PROPERTYKEY.System;

namespace Aerochat.ViewModels
{
    public enum NoticeType
    {
        Info,
        Warning,
        Error
    }
    public class NoticeViewModel : ViewModelBase
    {
        private string _message;
        private NoticeType _type;
        private ulong _date;
        private bool isTargeted;

        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public NoticeType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public ulong Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        public bool IsTargeted
        {
            get => isTargeted;
            set => SetProperty(ref isTargeted, value);
        }

        public static NoticeViewModel FromNotice(JsonElement notice)
        {
            var type = notice.GetProperty("type").GetString()!;
            var message = notice.GetProperty("message").GetString()!;
            var date = notice.GetProperty("date").GetUInt64()!;
            //var targets = notice.GetProperty("targets").GetString();
            // only get targets if it exists
            var targets = notice.TryGetProperty("targets", out var targetsElement) ? targetsElement.GetString() : null;
            // targets will be a string like "<0.0.0.4", "0.0.0.4", ">0.0.0.4", etc
            // parse it out

            bool isTargeted = true;
            if (targets != null)
            {
                var op = targets.Substring(0, targets.IndexOfAny("0123456789".ToCharArray()));
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                var target = new Version(targets.Substring(targets.IndexOfAny("0123456789".ToCharArray())));

                isTargeted = op switch
                {
                    "<" => version < target,
                    ">" => version > target,
                    "=" => version == target,
                    "!" => version != target,
                    ">=" => version >= target,
                    "<=" => version <= target,
                    _ => throw new InvalidOperationException("Invalid operator")
                };
            }

            return new NoticeViewModel
            {
                Message = message,
                Date = date,
                Type = type switch
                {
                    "info" => NoticeType.Info,
                    "warning" => NoticeType.Warning,
                    "error" => NoticeType.Error,
                    _ => throw new InvalidOperationException("Invalid type")
                },
                IsTargeted = isTargeted
            };
        }
    }
}
