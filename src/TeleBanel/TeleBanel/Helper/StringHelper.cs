﻿using System.IO;
using System.Linq;

namespace TeleBanel.Helper
{
    public static class StringHelper
    {
        public static char[] EnglishLetters { get; } = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSWXYZ".ToCharArray();
        public static char[] NumericalLetters { get; } = "0123456789".ToCharArray();
        public static string UriPattern { get; } = @"^(?:http(s)?:\/\/)?[\w.-]+(?:\.[\w\.-]+)+[\w\-\._~:/?#[\]@!\$&'\(\)\*\+,;=.]+$";


        public static string GetNetMessage(this string msg)
        {
            if (msg == null) return null;

            msg = msg.Replace("/", "");
            var skipFirstLen = 0;
            foreach (var c in msg)
            {
                if (!char.IsLetterOrDigit(c) || !c.IsEnglishLetter())
                    skipFirstLen++;
                else break;
            }

            return msg.Substring(skipFirstLen);
        }

        public static bool IsEnglishLetter(this char ch)
        {
            return EnglishLetters.Any(c => c.Equals(ch)) || NumericalLetters.Any(c => c.Equals(ch));
        }

        public static byte[] ToByte(this Stream input)
        {
            using (var ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}