﻿namespace TeleBanel.Models
{
    public interface IBot
    {
        string Username { get; set; }
        string DisplayName { get; set; }
        int Password { get; set; }
        string Token { get; set; }
    }
}