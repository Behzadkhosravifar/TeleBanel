﻿using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TeleBanel.Helper;
using TeleBanel.Models;
using TeleBanel.Properties;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TeleBanel
{
    public partial class BotManager
    {
        public async void GoNextPortfolioStep(UserWrapper user)
        {
            var query = user.LastCallBackQuery.Data.ToLower().Replace(InlinePrefixKeys.PortfolioKey, "");

            switch (query)
            {
                case "addjob":
                    {
                        break;
                    }
                case "showjob":
                    {
                        var job = JobManager.GetJob(new Random().Next().ToString());
                        await Bot.SendPhotoAsync(user, job.Title, job.Id, job.Image);
                        break;
                    }
                case "editjob":
                    {
                        break;
                    }
                case "deletejob":
                    {
                        break;
                    }
            }
        }

        public async void GoNextAboutStep(UserWrapper user)
        {
            if (user.LastCallBackQuery == null)
                return;

            if (user.WaitingMessageQuery == null || user.WaitingMessageQuery != nameof(GoNextAboutStep))
            {
                user.WaitingMessageQuery = nameof(GoNextAboutStep);
                await Bot.AnswerCallbackQueryAsync(user.LastCallBackQuery.Id, "Please enter new About and press Enter key.", true);
                await Bot.EditMessageReplyMarkupAsync(user.LastCallBackQuery.Message.Chat.Id,
                    user.LastCallBackQuery.Message.MessageId, KeyboardCollection.CancelKeyboardInlineKeyboard);
            }
            else
            {
                WebsiteManager.About = user.LastMessageQuery.Text;
                await Bot.AnswerCallbackQueryAsync(user.LastCallBackQuery.Id, "About successfully updated.", true);
                await Bot.EditMessageTextAsync(user.LastCallBackQuery.Message.Chat.Id, user.LastCallBackQuery.Message.MessageId,
                    Localization.About + ": \n\r" + (WebsiteManager.About ?? "---"),
                    ParseMode.Default, false, KeyboardCollection.AboutKeyboardInlineKeyboard);

                user.WaitingMessageQuery = null; // waiting method called and then clear buffer
            }
        }

        public async void GoNextLogoStep(UserWrapper user)
        {
            if (user.LastCallBackQuery == null)
                return;

            if (user.WaitingMessageQuery == null || user.WaitingMessageQuery != nameof(GoNextLogoStep))
            {
                user.WaitingMessageQuery = nameof(GoNextLogoStep);
                await Bot.AnswerCallbackQueryAsync(user.LastCallBackQuery.Id, "Please send an image to change logo ...", true);
                await Bot.EditMessageReplyMarkupAsync(user.LastCallBackQuery.Message.Chat.Id,
                    user.LastCallBackQuery.Message.MessageId, KeyboardCollection.CancelKeyboardInlineKeyboard);
            }
            else if (user.LastMessageQuery.Photo.Any())
            {
                using (var mem = new MemoryStream())
                {
                    var file = await Bot.GetFileAsync(user.LastMessageQuery.Photo.Last().FileId, mem);
                    WebsiteManager.Logo = mem.ToByte();
                    await Bot.DeleteMessageAsync(user.LastCallBackQuery.Message.Chat.Id, user.LastCallBackQuery.Message.MessageId);
                    await Bot.SendTextMessageAsync(user.LastCallBackQuery.Message.Chat.Id, "The logo changed successfully.");
                }
                user.WaitingMessageQuery = null; // waiting method called and then clear buffer
            }
            else
            {
                await Bot.AnswerCallbackQueryAsync(user.LastCallBackQuery.Id, "Please send an image to change logo ...", true);
            }
        }

        public async void GoNextLinksStep(UserWrapper user)
        {
            if (user.LastCallBackQuery == null)
                return;

            var linkName = user.LastCallBackQuery.Data.Replace(InlinePrefixKeys.LinksKey + "Edit", "");
            if (user.WaitingMessageQuery == null || user.WaitingMessageQuery != nameof(GoNextLinksStep))
            {
                user.WaitingMessageQuery = nameof(GoNextLinksStep);

                await Bot.AnswerCallbackQueryAsync(user.LastCallBackQuery.Id,
                    $"Please enter new {linkName} link and press Enter key.", true);
                user.LastCallBackQuery.Message = await Bot.EditMessageReplyMarkupAsync(user.LastCallBackQuery.Message.Chat.Id,
                    user.LastCallBackQuery.Message.MessageId, KeyboardCollection.CancelKeyboardInlineKeyboard);
            }
            else
            {
                if (Regex.IsMatch(user.LastMessageQuery.Text, StringHelper.UriPattern)
                    && Uri.TryCreate(user.LastMessageQuery.Text, UriKind.RelativeOrAbsolute, out Uri uri)
                    && (uri.Scheme == Uri.UriSchemeHttp
                        || uri.Scheme == Uri.UriSchemeHttps
                        || uri.Scheme == Uri.UriSchemeFtp
                        || uri.Scheme == Uri.UriSchemeMailto))
                {
                    var prop = WebsiteManager.GetType()
                        .GetProperties()
                        .FirstOrDefault(p => p.Name.StartsWith(linkName));

                    prop?.SetValue(WebsiteManager, uri.ToString());

                    await Bot.SendTextMessageAsync(user.LastCallBackQuery.Message.Chat.Id, "The link updated.");
                    user.LastCallBackQuery.Message = await Bot.EditMessageReplyMarkupAsync(user.LastCallBackQuery.Message.Chat.Id,
                        user.LastCallBackQuery.Message.MessageId, KeyboardCollection.LinksboardInlineKeyboard);

                    user.WaitingMessageQuery = null; // waiting method called and then clear buffer
                }
                else
                {
                    await Bot.SendTextMessageAsync(user.LastCallBackQuery.Message.Chat.Id,
                        "Please enter just Uri format like example: http://sampleuri.com");
                }
            }
        }

        public async Task<bool> GoNextPasswordStep(CallbackQueryEventArgs e)
        {
            var userId = e.CallbackQuery.From.Id;

            if (!Accounts.ContainsKey(userId))
            {
                await Bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id,
                    Localization.EntryPasswordIsIncorrect,
                    showAlert: true);
                return false;
            }

            var data = e.CallbackQuery.Data.ToLower();
            if (!data.StartsWith("password_")) return false;

            if (data.StartsWith("password_num."))
            {
                Accounts[userId].Password += data.Replace("password_num.", "");
            }
            else if (data == "password_enter")
            {
                if (Accounts[userId].Password == BotApiPassword)
                {
                    Accounts[userId].IsAuthenticated = true;

                    await Bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id,
                        Localization.PasswordIsOk,
                        showAlert: true);
                    await Bot.DeleteMessageAsync(e.CallbackQuery.Message.Chat.Id, e.CallbackQuery.Message.MessageId);
                    await Bot.SendTextMessageAsync(
                        e.CallbackQuery.Message.Chat.Id,
                        Localization.PleaseChooseYourOptionDoubleDot,
                        replyMarkup: KeyboardCollection.CommonReplyKeyboard);
                    return true;
                }
                else // password is incorrect
                {
                    Accounts[userId].Password = "";
                    await Bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id,
                        Localization.EntryPasswordIsIncorrect,
                        showAlert: true);
                    await Bot.DeleteMessageAsync(e.CallbackQuery.Message.Chat.Id, e.CallbackQuery.Message.MessageId);
                    return true;
                }
            }
            else if (data == "password_backspace")
            {
                if (Accounts[userId].Password.Length > 0)
                {
                    Accounts[userId].Password = Accounts[userId]
                        .Password.Remove(Accounts[userId].Password.Length - 1, 1);
                }
            }
            else
            {
                return false;
            }

            await Bot.EditMessageTextAsync(e.CallbackQuery.Message.Chat.Id, e.CallbackQuery.Message.MessageId,
                $"{Emoji.LightBulb} {Localization.Password}: " + new string(Accounts[userId].Password.Select(x => '*').ToArray()),
                ParseMode.Default, false, KeyboardCollection.PasswordKeyboardInlineKeyboard);

            return true;
        }
    }
}
