﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TeleBanel.Helper;
using TeleBanel.Models;
using TeleBanel.Models.Middlewares;
using TeleBanel.Properties;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TeleBanel
{
    public partial class BotManager
    {
        #region Constructors

        public BotManager(string apiKey, string botPassword, IWebsiteMiddleware websiteInfo)
        {
            Accounts = new Dictionary<int, UserWrapper>();
            BotApiKey = apiKey;
            BotApiPassword = botPassword;
            KeyboardCollection = new BotKeyboardCollection(websiteInfo);
            WebsiteManager = websiteInfo;
        }

        #endregion


        public async Task StartListeningAsync()
        {
            Bot = new TelegramBotClient(BotApiKey);
            Bot.StartReceiving();
            Bot.OnMessage += Bot_OnMessage;
            Bot.OnCallbackQuery += Bot_OnCallbackQuery;

            Me = await Bot.GetMeAsync();
            Console.WriteLine($"{Me.Username} Connected.");
        }

        public bool UserAuthenticated(User user, out UserWrapper userWrapper)
        {
            if (!Accounts.ContainsKey(user.Id))
                Accounts[user.Id] = UserWrapper.Factory(user);

            userWrapper = Accounts[user.Id];

            return Accounts[user.Id].IsAuthenticated;
        }


        private async void Bot_OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            var command = e.CallbackQuery.Data.ToLower();

            if (UserAuthenticated(e.CallbackQuery.From, out UserWrapper user)) // user authenticated
            {
                user.LastCallBackQuery = e.CallbackQuery;

                if (command == Localization.Cancel.ToLower())
                {
                    user.WaitingMessageQuery = null;
                    await Bot.DeleteMessageAsync(e.CallbackQuery.Message.Chat.Id, e.CallbackQuery.Message.MessageId);
                }
                else if (command.StartsWith(InlinePrefixKeys.PortfolioKey))
                {
                    GoNextPortfolioStep(user);
                }
                else if (command.StartsWith(InlinePrefixKeys.AboutKey))
                {
                    GoNextAboutStep(user);
                }
                else if (command.StartsWith(InlinePrefixKeys.LogoKey))
                {
                    GoNextLogoStep(user);
                }
                else if (command.StartsWith(InlinePrefixKeys.LinksKey))
                {
                    GoNextLinksStep(user);
                }
                else
                {
                    await Bot.SendTextMessageAsync(
                        e.CallbackQuery.Message.Chat.Id,
                        Localization.PleaseChooseYourOptionDoubleDot,
                        replyMarkup: KeyboardCollection.CommonReplyKeyboard);
                }
            }
            else // Before authenticate
                await GoNextPasswordStep(e);
        }


        private async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            var userId = e.Message.From.Id;
            var command = e.Message.Text?.GetNetMessage();

            if (e.Message.Chat.Type != ChatType.Private)
            {
                await Bot.SendTextMessageAsync(e.Message.Chat.Id, Localization.InvalidRequest);
                return;
            }

            if (command == Localization.GetMyId.ToLower())
            {
                await Bot.SendTextMessageAsync(userId,
                    $"{e.Message.From.FirstName} {e.Message.From.LastName}, ID: {userId}");
            }
            else if (UserAuthenticated(e.Message.From, out UserWrapper user)) // CommonReplyKeyboard
            {
                user.LastMessageQuery = e.Message;

                if (command == Localization.Portfolios.ToLower())
                {
                    await Bot.SendTextMessageAsync(e.Message.Chat.Id,
                        Localization.Portfolios,
                        replyMarkup: KeyboardCollection.PortfolioKeyboardInlineKeyboard);
                }
                else if (command == Localization.About.ToLower())
                {
                    await Bot.SendTextMessageAsync(e.Message.Chat.Id,
                        Localization.About + ": \n\r" + (WebsiteManager.About ?? "---"),
                        replyMarkup: KeyboardCollection.AboutKeyboardInlineKeyboard);

                    await Bot.SendTextMessageAsync(e.Message.Chat.Id,
                        Localization.Title + ": \n\r" + (WebsiteManager.Title ?? "---"),
                        replyMarkup: KeyboardCollection.TitleKeyboardInlineKeyboard);

                    await Bot.SendTextMessageAsync(e.Message.Chat.Id,
                        Localization.ContactEmail + ": \n\r" + (WebsiteManager.ContactEmail ?? "---"),
                        replyMarkup: KeyboardCollection.ContactEmailKeyboardInlineKeyboard);

                    await Bot.SendTextMessageAsync(e.Message.Chat.Id,
                        Localization.FeedbackEmail + ": \n\r" + (WebsiteManager.FeedbackEmail ?? "---"),
                        replyMarkup: KeyboardCollection.FeedbackEmailKeyboardInlineKeyboard);

                    await Bot.SendTextMessageAsync(e.Message.Chat.Id,
                        Localization.ContactPhone + ": \n\r" + (WebsiteManager.ContactPhone ?? "---"),
                        replyMarkup: KeyboardCollection.ContactPhoneKeyboardInlineKeyboard);
                }
                else if (command == Localization.Logo.ToLower())
                {
                    using (var stream = new MemoryStream(WebsiteManager.Logo))
                    {
                        await Bot.SendPhotoAsync(e.Message.Chat.Id,
                            photo: new FileToSend("logo", stream),
                            caption: Localization.Logo,
                            replyMarkup: KeyboardCollection.LogoKeyboardInlineKeyboard);
                    }
                }
                else if (command == Localization.Links.ToLower())
                {
                    await Bot.SendTextMessageAsync(e.Message.Chat.Id,
                        $"{Emoji.Link + Emoji.Link}           L  I  N  K  S           {Emoji.Link + Emoji.Link}",
                        replyMarkup: KeyboardCollection.LinksboardInlineKeyboard);
                }
                else
                {
                    if (user.WaitingMessageQuery != null)
                    {
                        var t = typeof(BotManager);
                        var m = t.GetMethod(user.WaitingMessageQuery);
                        m?.Invoke(this, new object[] { user });
                    }
                    else
                        await Bot.SendTextMessageAsync(
                            e.Message.Chat.Id,
                            Localization.PleaseChooseYourOptionDoubleDot,
                            replyMarkup: KeyboardCollection.CommonReplyKeyboard);
                }
            }
            else // RegisterReplyKeyboard
            {
                if (command == Localization.Start.ToLower())
                {
                    await Bot.SendTextMessageAsync(
                        e.Message.Chat.Id,
                        Localization.PleaseChooseYourOptionDoubleDot,
                        replyMarkup: KeyboardCollection.RegisterReplyKeyboard);
                }
                else if (command == Localization.Register.ToLower())
                {
                    Accounts[userId].Password = "";
                    await Bot.SendTextMessageAsync(e.Message.Chat.Id,
                        $"{Emoji.LightBulb} {Localization.Password}: ",
                        replyMarkup: KeyboardCollection.PasswordKeyboardInlineKeyboard);
                }
                else
                {
                    await Bot.SendTextMessageAsync(
                        e.Message.Chat.Id,
                        Localization.PleaseChooseYourOptionDoubleDot,
                        replyMarkup: KeyboardCollection.RegisterReplyKeyboard);
                }
            }
        }

    }
}