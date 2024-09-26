using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.ViewModels
{
    public class FieldViewModel: ViewModelBase
    {
        private string _name;
        private string _value;
        private bool _inline;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
        public bool Inline
        {
            get => _inline;
            set => SetProperty(ref _inline, value);
        }

        public static FieldViewModel FromField(DiscordEmbedField field)
        {
            return new FieldViewModel
            {
                Name = field.Name,
                Value = field.Value,
                Inline = field.Inline
            };
        }
    }
    public class EmbedViewModel : ViewModelBase
    {
        private EmbedAuthorViewModel _author;
        private string _color;
        private string _description;
        private List<FieldViewModel> _fields;
        private string _footer;
        private EmbedImageViewModel _image;
        private EmbedProviderViewModel _provider;
        private EmbedImageViewModel _thumbnail;
        private DateTime? _timestamp;
        private string _title;
        private string _type;
        private string _url;
        private string _video;

        public EmbedAuthorViewModel Author
        {
            get => _author;
            set => SetProperty(ref _author, value);
        }
        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }
        public List<FieldViewModel> Fields
        {
            get => _fields;
            set => SetProperty(ref _fields, value);
        }
        public string Footer
        {
            get => _footer;
            set => SetProperty(ref _footer, value);
        }
        public EmbedImageViewModel Image
        {
            get => _image;
            set => SetProperty(ref _image, value);
        }
        public EmbedProviderViewModel Provider
        {
            get => _provider;
            set => SetProperty(ref _provider, value);
        }
        public EmbedImageViewModel Thumbnail
        {
            get => _thumbnail;
            set => SetProperty(ref _thumbnail, value);
        }
        public DateTime? Timestamp
        {
            get => _timestamp;
            set => SetProperty(ref _timestamp, value);
        }
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
        public string Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }
        public string Url
        {
            get => _url;
            set => SetProperty(ref _url, value);
        }
        public string Video
        {
            get => _video;
            set => SetProperty(ref _video, value);
        }
        public static EmbedViewModel FromEmbed(DiscordEmbed embed)
        {
            List<FieldViewModel> fields = new();
            foreach (var field in embed.Fields)
            {
                fields.Add(FieldViewModel.FromField(field));
            }

            return new EmbedViewModel
            {
                Author = embed.Author is null ? null : EmbedAuthorViewModel.FromAuthor(embed.Author),
                Color = embed.Color.ToString(),
                Description = embed.Description,
                Fields = fields,
                Footer = embed.Footer?.Text,
                Image = embed.Image is null ? null : EmbedImageViewModel.FromImage(embed.Image),
                Provider = embed.Provider is null ? null : EmbedProviderViewModel.FromProvider(embed.Provider),
                Thumbnail = embed.Thumbnail is null ? null : EmbedImageViewModel.FromThumbnail(embed.Thumbnail),
                Timestamp = embed.Timestamp is null ? null : (DateTime)embed.Timestamp?.DateTime,
                Title = embed.Title,
                Type = embed.Type,
                Url = embed.Url?.ToString(),
                Video = embed.Video?.Url.ToString()
            };
        }
    }

    public class EmbedAuthorViewModel : ViewModelBase
    {
        // Name, Url, IconUrl
        private string name;
        private string url;
        private string iconUrl;

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public string Url
        {
            get => url;
            set => SetProperty(ref url, value);
        }

        public string IconUrl
        {
            get => iconUrl;
            set => SetProperty(ref iconUrl, value);
        }

        public static EmbedAuthorViewModel FromAuthor(DiscordEmbedAuthor author)
        {
            return new EmbedAuthorViewModel
            {
                Name = author.Name,
                Url = author.Url?.ToString(),
                IconUrl = author.IconUrl?.ToString()
            };
        }
    }

    public class EmbedProviderViewModel : ViewModelBase
    {
        private string name;
        private string url;

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }
        public string Url
        {
            get => url;
            set => SetProperty(ref url, value);
        }

        public static EmbedProviderViewModel FromProvider(DiscordEmbedProvider provider)
        {
            return new EmbedProviderViewModel
            {
                Name = provider?.Name,
                Url = provider?.Url?.ToString()
            };
        }
    }

    public class EmbedImageViewModel : ViewModelBase
    {
        private int width;
        private int height;
        private string url;

        public int Width
        {
            get => width;
            set => SetProperty(ref width, value);
        }
        public int Height
        {
            get => height;
            set => SetProperty(ref height, value);
        }
        public string Url
        {
            get => url;
            set => SetProperty(ref url, value);
        }

        public static EmbedImageViewModel FromImage(DiscordEmbedImage image)
        {
            return new EmbedImageViewModel
            {
                Width = image.Width,
                Height = image.Height,
                Url = image.ProxyUrl.ToString()
            };
        }

        public static EmbedImageViewModel FromThumbnail(DiscordEmbedThumbnail thumbnail)
        {
            return new EmbedImageViewModel
            {
                Width = thumbnail.Width,
                Height = thumbnail.Height,
                Url = thumbnail.ProxyUrl.ToString()
            };
        }
    }
}
