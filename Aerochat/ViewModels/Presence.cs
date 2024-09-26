﻿using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.ViewModels
{
    public class PresenceViewModel : ViewModelBase
    {
        private string _presenceString;
        private string _status;
        private string _type;

        public required string Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }
        public required string Presence
        {
            get => _presenceString;
            set => SetProperty(ref _presenceString, value);
        }

        public static PresenceViewModel FromPresence(DiscordPresence presence)
        {
            if (presence is null) return new()
            {
                Presence = "",
                Status = "Offline",
                Type = ""
            };
            var activity = presence.Activities?.OrderByDescending(x => x?.ActivityType switch
            {
                ActivityType.Custom => 6,
                ActivityType.Playing => 5,
                ActivityType.Streaming => 4,
                ActivityType.ListeningTo => 3,
                ActivityType.Watching => 2,
                ActivityType.Competing => 1,
                _ => 0
            })?.FirstOrDefault();

            // if its custom but activity.CustomStatus?.Name is null or empty, search for the next one in the priority list
            if (activity?.ActivityType == ActivityType.Custom && string.IsNullOrEmpty(activity?.CustomStatus?.Name))
                activity = presence.Activities?.OrderByDescending(x => x.ActivityType switch
                {
                    ActivityType.Playing => 5,
                    ActivityType.Streaming => 4,
                    ActivityType.ListeningTo => 3,
                    ActivityType.Watching => 2,
                    ActivityType.Competing => 1,
                    _ => 0
                }).FirstOrDefault();

            var presenceString = activity switch
            {
                { ActivityType: ActivityType.Custom } => activity.CustomStatus?.Name ?? "",
                { ActivityType: ActivityType.Playing } => activity.Name,
                { ActivityType: ActivityType.Streaming } => activity.Name,
                { ActivityType: ActivityType.ListeningTo } => $"{activity.RichPresence?.State} - {activity.RichPresence?.Details}",
                { ActivityType: ActivityType.Watching } => activity.Name,
                { ActivityType: ActivityType.Competing } => activity.Name,
                _ => ""
            };

            return new PresenceViewModel
            {
                Presence = presenceString.Trim(),
                Status = presence.Status.ToString(),
                Type = activity?.ActivityType.ToString() ?? ""
            };
        }
    }
}
