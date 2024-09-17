using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.ViewModels
{
    public class AttachmentViewModel : ViewModelBase
    {
        private string _url;
        private int _width;
        private int _height;
        private string _size;
        private string _name;
        private bool _isImage;
        private ulong _id;

        public string Url
        {
            get => _url;
            set => SetProperty(ref _url, value);
        }
        public int Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }
        public int Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }
        public string Size
        {
            get => _size;
            set => SetProperty(ref _size, value);
        }
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
        public bool IsImage
        {
            get => _isImage;
            set => SetProperty(ref _isImage, value);
        }
        public ulong Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private static string FormatSize(long size)
        {
            return size switch
            {
                < 1024 => $"{size} B",
                < 1024 * 1024 => $"{size / 1024} KB",
                < 1024 * 1024 * 1024 => $"{size / 1024 / 1024} MB",
                _ => $"{size / 1024 / 1024 / 1024} GB"
            };
        }

        public static AttachmentViewModel FromAttachment(DiscordAttachment attachment)
        {

            return new AttachmentViewModel()
            {
                Url = attachment.ProxyUrl ?? attachment.Url,
                Width = attachment.Width ?? 0,
                Height = attachment.Height ?? 0,
                Name = attachment.FileName,
                Size = FormatSize(attachment.FileSize),
                IsImage = attachment.Width > 0 && attachment.Height > 0,
                Id = attachment.Id,
            };
        }

        public static List<AttachmentViewModel> FromAttachments(List<DiscordAttachment> attachments)
        {
            List<AttachmentViewModel> result = new();
            foreach (var attachment in attachments)
            {
                result.Add(FromAttachment(attachment));
            }
            return result;
        }
    }
}