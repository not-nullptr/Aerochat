using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat
{
    class EmojiDictionary
    {
        public static readonly Dictionary<string, string> Map = new Dictionary<string, string>();

        static EmojiDictionary()
        {
            void AddAliases(string[] aliases, string file)
            {
                foreach (var alias in aliases)
                    Map[alias] = file;
            }

            AddAliases(new[] { "grinning", "smiley", "smile", "slight_smile" }, "Smile.png");
            AddAliases(new[] { "grin", "laughing", "sweat_smile", "joy", "rofl" }, "Grin.png");
            AddAliases(new[] { "wink" }, "Wink.png");
            AddAliases(new[] { "hushed", "hushed_face" }, "Surprise.png");
            AddAliases(new[] { "stuck_out_tongue_closed_eyes", "stuck_out_tongue", "stuck_out_tongue_winking_eye", "tongue" }, "Tongue.png");
            AddAliases(new[] { "sunglasses" }, "Sunglasses.png");
            AddAliases(new[] { "rage" }, "Rage.png");
            AddAliases(new[] { "sob", "cry" }, "Sob.png");
            AddAliases(new[] { "confused" }, "Confused.png");
            AddAliases(new[] { "rolling_eyes" }, "RollingEyes.png");
            AddAliases(new[] { "nauseated_face", "sick", "face_vomiting" }, "Sick.png");
            AddAliases(new[] { "people_holding_hands", "two_men_holding_hands", "two_women_holding_hands" }, "Conversation.png");
            AddAliases(new[] { "pray", "folded_hands" }, "HighFive.png");
            AddAliases(new[] { "thinking" }, "Thinking.png");
            AddAliases(new[] { "astonished" }, "Surprised.png");
            AddAliases(new[] { "woman", "woman_beard" }, "Woman.png");
            AddAliases(new[] { "imp", "smiling_imp" }, "Demon.png");
            AddAliases(new[] { "yawning_face" }, "Yawn.png");
            AddAliases(new[] { "angry" }, "Anger.png");
            AddAliases(new[] { "zipper_mouth" }, "ZipMouth.png");
            AddAliases(new[] { "flushed" }, "Flushed.png");
            AddAliases(new[] { "slight_frown" }, "Discontent.png");
            AddAliases(new[] { "frowning", "frowning2", "pensive" }, "Frown.png");
            AddAliases(new[] { "thumbsup" }, "ThumbsUp.png");
            AddAliases(new[] { "thumbsdown" }, "ThumbsDown.png");
            AddAliases(new[] { "nerd" }, "Nerd.png");
            AddAliases(new[] { "video_game", "xbox" }, "Xbox.png");
            AddAliases(new[] { "rose" }, "Rose.png");
            AddAliases(new[] { "wilted_rose" }, "Rose_Wilter.png");
            AddAliases(new[] { "biting_lip" }, "LipBite.png");
            AddAliases(new[] { "partying_face" }, "Party.png");
            AddAliases(new[] { "airplane" }, "Plane.png");
            AddAliases(new[] { "heart", "hearts", "heart_decoration", "black_heart", "green_heart", "blue_heart", "brown_heart",
                       "grey_heart", "light_blue_heart", "orange_heart", "pink_heart", "purple_heart", "yellow_heart", "white_heart" }, "Heart.png");
            AddAliases(new[] { "rainbow" }, "Rainbow.png");
            AddAliases(new[] { "pizza" }, "Pizza.png");
            AddAliases(new[] { "man", "man_beard" }, "Man.png");
            AddAliases(new[] { "angel", "innocent" }, "Angel.png");
            AddAliases(new[] { "bat" }, "Bat.png");
            AddAliases(new[] { "mobile_phone", "calling" }, "CellPhone.png");
            AddAliases(new[] { "smoking" }, "Cigarette.png");
            AddAliases(new[] { "beach", "island" }, "Beach.png");
            AddAliases(new[] { "beer", "beers" }, "Beer.png");
            AddAliases(new[] { "broken_heart" }, "BrokenHeart.png");
            AddAliases(new[] { "cake", "birthday_cake", "moon_cake" }, "Cake.png");
            AddAliases(new[] { "camera", "camera_with_flash", "movie_camera", "video_camera" }, "Camera.png");
            AddAliases(new[] { "red_car", "blue_car", "race_car" }, "Car.png");
            AddAliases(new[] { "black_cat", "cat", "cat2" }, "Cat.png");
            AddAliases(new[] { "person_walking", "reach_left" }, "ReachLeft.png");
            AddAliases(new[] { "person_walking_facing_right", "reach_right" }, "ReachRight.png");
            AddAliases(new[] { "clock", "alarm_clock", "timer_clock" }, "Clock.png");
            AddAliases(new[] { "coffee" }, "Coffee.png");
            AddAliases(new[] { "computer", "desktop_computer" }, "Computer.png");
            AddAliases(new[] { "fingers_crossed" }, "CrossedFingers.png");
            AddAliases(new[] { "handcuffs", "cuffs" }, "Cuffs.png");
            AddAliases(new[] { "coin", "moneybag", "dollar", "euro", "pound", "heavy_dollar_sign", "yen" }, "Currency.png");
            AddAliases(new[] { "dog", "guide_dog", "service_dog", "dog2", "poodle" }, "Dog.png");
            AddAliases(new[] { "film_frames", "projector" }, "Film.png");
            AddAliases(new[] { "soccer", "soccer_ball", "actual_football" }, "SoccerBall.png");
            AddAliases(new[] { "goat" }, "Goat.png");
            AddAliases(new[] { "jump" }, "Jump.png");
            AddAliases(new[] { "bulb", "light_bulb" }, "LightBulb.png");
            AddAliases(new[] { "mailbox_with_mail", "envelope", "postbox", "incoming_envelope", "e_mail", "email", "envelope_with_arrow" }, "Mail.png");
            AddAliases(new[] { "crescent_moon", "full_moon", "full_moon_with_face" }, "Moon.png");
            AddAliases(new[] { "musical_note", "musical_notes" }, "Music.png");
            AddAliases(new[] { "telephone", "telephone_reciever" }, "Phone.png");
            AddAliases(new[] { "fork_knife_plate", "fork_and_knife_with_plate" }, "Plate.png");
            AddAliases(new[] { "gift", "wrapped_gift" }, "Present.png");
            AddAliases(new[] { "rabbit", "rabbit2" }, "Rabbit.png");
            AddAliases(new[] { "cloud_rain" }, "Rain.png");
            AddAliases(new[] { "sheep", "ewe", "ram" }, "Sheep.png");
            AddAliases(new[] { "snail" }, "Snail.png");
            AddAliases(new[] { "bowl_with_spoon", "tea" }, "Soup.png");
            AddAliases(new[] { "thunder_cloud_rain" }, "Thunder.png");
            AddAliases(new[] { "turtle" }, "Tortoise.png");
            AddAliases(new[] { "closed_umbrella", "umbrella", "umbrella2" }, "Umbrella.png");
            AddAliases(new[] { "wine_glass" }, "Wine.png");
            AddAliases(new[] { "computer", "wlm" }, "WLM.png");
        }
    }
}
