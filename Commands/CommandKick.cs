﻿using System;
using System.Collections.Generic;
using System.Globalization;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;

namespace fr34kyn01535.GlobalBan.Commands
{
    public class CommandKick : IRocketCommand
    {
        public string Help => "Kicks a player";

        public string Name => "kick";

        public string Syntax => "<player> [reason]";

        public List<string> Aliases => new List<string>();

        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public List<string> Permissions => new List<string> {"globalban.kick"};

        public void Execute(IRocketPlayer caller, params string[] command)
        {
            if (command.Length == 0 || command.Length > 2)
            {
                UnturnedChat.Say(caller, GlobalBan.Instance.Translate("command_generic_invalid_parameter"));
                return;
            }

            var playerToKick = UnturnedPlayer.FromName(command[0]);
            if (playerToKick == null)
            {
                UnturnedChat.Say(caller, GlobalBan.Instance.Translate("command_generic_player_not_found"));
                return;
            }

            if (command.Length >= 2)
            {
                UnturnedChat.Say(GlobalBan.Instance.Translate("command_kick_public_reason", playerToKick.CharacterName,
                    command[1]));
                Provider.kick(playerToKick.CSteamID, command[1]);

                Discord.SendWebhookPost(GlobalBan.Instance.Configuration.Instance.DiscordKickWebhook,
                    Discord.BuildDiscordEmbed("A player was kicked from the server.",
                        $"{playerToKick.CharacterName} was kicked from the server for {command[1]}!", "Global Ban",
                        "https://imperialproduction.blob.core.windows.net/shopcoreproducts/productlogos/194/13260ab6-c9b2-d350-64f3-39f360c60fe6/thumbnail.png",
                        GlobalBan.Instance.Configuration.Instance.DiscordKickWebhookColor,
                        new[]
                        {
                            Discord.BuildDiscordField("Steam64ID", playerToKick.CSteamID.ToString(), true),
                            Discord.BuildDiscordField("Kicked By", caller.DisplayName, true),
                            Discord.BuildDiscordField("Time of Kick",
                                DateTime.Now.ToString(CultureInfo.InvariantCulture), false),
                            Discord.BuildDiscordField("Reason of Kick", command[1], true)
                        }));
            }
            else
            {
                UnturnedChat.Say(GlobalBan.Instance.Translate("command_kick_public", playerToKick.CharacterName));
                Provider.kick(playerToKick.CSteamID,
                    GlobalBan.Instance.Translate("command_kick_private_default_reason"));

                Discord.SendWebhookPost(GlobalBan.Instance.Configuration.Instance.DiscordKickWebhook,
                    Discord.BuildDiscordEmbed("A player was kicked from the server.",
                        $"{playerToKick.CharacterName} was kicked from the server for {GlobalBan.Instance.Translate("command_kick_private_default_reason")}!",
                        "Global Ban",
                        "https://imperialproduction.blob.core.windows.net/shopcoreproducts/productlogos/194/13260ab6-c9b2-d350-64f3-39f360c60fe6/thumbnail.png",
                        GlobalBan.Instance.Configuration.Instance.DiscordKickWebhookColor,
                        new[]
                        {
                            Discord.BuildDiscordField("Steam64ID", playerToKick.CSteamID.ToString(), true),
                            Discord.BuildDiscordField("Kicked By", caller.DisplayName, true),
                            Discord.BuildDiscordField("Time of Kick",
                                DateTime.Now.ToString(CultureInfo.InvariantCulture), false),
                            Discord.BuildDiscordField("Reason of Kick",
                                GlobalBan.Instance.Translate("command_kick_private_default_reason"), true)
                        }));
            }
        }
    }
}