﻿using System.Globalization;

namespace TeleBanel
{
    public class UserWrapper
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public int Id { get; set; }
        public LanguageCultures LanguageCulture { get; set; } = LanguageCultures.En;
        public CultureInfo Culture => new CultureInfo(LanguageCulture.ToString());
        public bool IsAuthenticated { get; set; } = false;
        public string Password { get; set; } = "";


        public static UserWrapper Factory(Telegram.Bot.Types.User telUser)
        {
            return new UserWrapper
            {
                Id = telUser.Id,
                FirstName = telUser.FirstName,
                LastName = telUser.LastName,
                UserName = telUser.Username
            };
        }
    }
}
