﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using fr34kyn01535.GlobalBan.API;
using JetBrains.Annotations;
using PlayerInfoLibrary;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;

namespace fr34kyn01535.GlobalBan.Commands
{
    public class CommandSlay : IRocketCommand
    {
        [NotNull] public string Help => "Banns a player for a year";

        [NotNull] public string Name => "slay";

        [NotNull] public string Syntax => "<player> [reason]";

        [NotNull] public List<string> Aliases => new List<string>();

        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        [NotNull] public List<string> Permissions => new List<string> {"globalban.slay"};

        public void Execute(IRocketPlayer caller, [NotNull] params string[] command)
        {
            if (command.Length == 0)
            {
                UnturnedChat.Say(caller, GlobalBan.Instance.Translate("command_generic_invalid_parameter"));
                return;
            }

            var args = command.ToList();
            var target = args.GetIRocketPlayer(out var index);
            if (index > -1)
                args.RemoveAt(index);

            if (target == null)
            {
                UnturnedChat.Say(caller, GlobalBan.Instance.Translate("command_generic_player_not_found"));
                return;
            }

            var reason = string.Join(" ", args);
            var adminName = caller.DisplayName;
            var pData = PlayerInfoLib.Instance.database.QueryById(new CSteamID(ulong.Parse(target.Id)));
            var characterName = pData?.CharacterName ?? target.DisplayName;
            var steamId = pData?.SteamId ?? new CSteamID(ulong.Parse(target.Id));
            var ip = pData?.Ip ?? steamId.GetIp();
            var hwid = pData != null
                ? string.Join("", pData.Hwid)
                : string.Join("", target is UnturnedPlayer tPlayer ? tPlayer.SteamPlayer().playerID.hwid : new byte[0]);

            GlobalBan.Instance.database.BanPlayer(characterName, steamId.ToString(), ip, hwid, adminName, reason,
                uint.MaxValue);
            UnturnedChat.Say(GlobalBan.Instance.Translate("command_ban_public_reason", characterName, reason));
            if (target is UnturnedPlayer)
                Provider.ban(steamId, reason, uint.MaxValue);

            Discord.SendWebhookPost(GlobalBan.Instance.Configuration.Instance.DiscordBanWebhook,
                Discord.BuildDiscordEmbed("A player was banned from the server.",
                    $"{characterName} was banned from the server for {reason}!",
                    "Global Ban",
                    "https://imperialproduction.blob.core.windows.net/shopcoreproducts/productlogos/194/13260ab6-c9b2-d350-64f3-39f360c60fe6/thumbnail.png",
                    GlobalBan.Instance.Configuration.Instance.DiscordBanWebhookColor,
                    new[]
                    {
                        Discord.BuildDiscordField("Steam64ID", steamId.ToString(), true),
                        Discord.BuildDiscordField("Banned By", adminName, true),
                        Discord.BuildDiscordField("Time of Ban",
                            DateTime.Now.ToString(CultureInfo.InvariantCulture), false),
                        Discord.BuildDiscordField("Reason of Ban", reason, true),
                        Discord.BuildDiscordField("Ban duration", uint.MaxValue.ToString(), true)
                    }));
        }
    }
}