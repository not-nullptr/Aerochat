using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Aerochat.ViewModels
{
    public class NewsViewModel : ViewModelBase
    {
        private ulong _date;
        private string _body;

        public ulong Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        public string Body
        {
            get => _body;
            set => SetProperty(ref _body, value);
        }

        public static NewsViewModel FromNews(JsonElement news)
        {
            var date = news.GetProperty("date").GetUInt64()!;
            var body = news.GetProperty("body").GetString()!;
            return new NewsViewModel
            {
                Date = date,
                Body = body
            };
        }
    }
}
