using Aerochat.Enums;
using DSharpPlus.Entities;
using MimeTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Aerochat.ViewModels
{
    public class AttachmentViewModel : ViewModelBase
    {
        private string _url;
        private int _width;
        private int _height;
        private string _size;
        private string _name;
        private MediaType _mediaType;
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
        public MediaType MediaType
        {
            get => _mediaType;
            set => SetProperty(ref _mediaType, value);
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
            MediaType mediaType;

            if (attachment.Width <= 0 || attachment.Height <= 0)
                mediaType = MediaType.Unknown;
            else
            {
                var fileNameSects = attachment.FileName.Split('.');
                string mimeType = MimeTypeMap.GetMimeType(fileNameSects[fileNameSects.Length - 1]);
                if (mimeType.Contains("image"))
                    mediaType = attachment.FileName.Contains(".gif") ? MediaType.Gif : MediaType.Image;
                else if (mimeType.Contains("video"))
                    mediaType = MediaType.Video;
                else if (mimeType.Contains("audio"))
                    mediaType = MediaType.Audio;
                else mediaType = MediaType.Unknown;
            }


            return new AttachmentViewModel()
            {
                Url = attachment.ProxyUrl ?? attachment.Url,
                Width = attachment.Width ?? 0,
                Height = attachment.Height ?? 0,
                Name = attachment.FileName,
                Size = FormatSize(attachment.FileSize),
                MediaType = mediaType,
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