﻿using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OpenMod.API.Plugins;
using OpenMod.API.Users;
using OpenMod.Core.Commands;
using OpenMod.Core.Console;
using OpenMod.Core.Users;
using OpenMod.Unturned.Users;
using Pustalorc.GlobalBan.API.Enums;
using Pustalorc.GlobalBan.API.Services;
using Pustalorc.PlayerInfoLib.Unturned.Database;
using SDG.Unturned;
using Steamworks;
using Command = OpenMod.Core.Commands.Command;

namespace Pustalorc.GlobalBan.Commands
{
    [Command("ban")]
    [CommandSyntax("<player> [duration] [reason]")]
    [CommandDescription("Bans a player globally from the network.")]
    public class CommandBan : Command
    {
        private readonly IConfiguration m_Configuration;
        private readonly IStringLocalizer m_StringLocalizer;
        private readonly IUserManager m_UserManager;
        private readonly IPluginAccessor<GlobalBanPlugin> m_Plugin;
        private readonly IPlayerInfoRepository m_PlayerInfoRepository;
        private readonly IGlobalBanRepository m_GlobalBanRepository;
        private readonly ILogger<CommandBan> m_Logger;

        public CommandBan(IStringLocalizer stringLocalizer, IUserManager userManager,
            IPluginAccessor<GlobalBanPlugin> globalBanPlugin, IPlayerInfoRepository playerInfoRepository,
            IGlobalBanRepository globalBanRepository, IConfiguration configuration, ILogger<CommandBan> logger,
            IServiceProvider serviceProvider) :
            base(serviceProvider)
        {
            m_UserManager = userManager;
            m_StringLocalizer = stringLocalizer;
            m_Configuration = configuration;
            m_Plugin = globalBanPlugin;
            m_PlayerInfoRepository = playerInfoRepository;
            m_GlobalBanRepository = globalBanRepository;
            m_Logger = logger;
        }

        protected override async Task OnExecuteAsync()
        {
            var actor = Context.Actor;

            // Parse arguments
            var target = await Context.Parameters.GetAsync<string>(0);

            if (!Context.Parameters.TryGet<uint>(1, out var duration))
                duration = uint.MaxValue;

            var reason = "N/A";
            if (Context.Parameters.Count >= 3)
                reason = Context.Parameters.GetArgumentLine(2);


            // Get config option
            var shouldIpAndHwidBan = m_Configuration.GetSection("commands:ban:ban_hwid_and_ip").Get<bool>();

            // Try to find user to ban
            var user = await m_UserManager.FindUserAsync(KnownActorTypes.Player, target, UserSearchMode.NameOrId);
            var pData = await m_PlayerInfoRepository.FindPlayerAsync(target, UserSearchMode.NameOrId);
            var isId = ulong.TryParse(target, out var pId) && pId >= 76561197960265728 && pId <= 103582791429521408;

            if ((user == null || user is OfflineUser) && pData == null && !isId)
            {
                await actor.PrintMessageAsync(m_StringLocalizer["commands:global:playernotfound",
                    new {Input = target}]);
                return;
            }

            var adminId = ulong.TryParse(actor.Id, out var id) ? id : 0ul;
            CSteamID steamId;
            string characterName;
            var ip = 0u;
            var hwid = "";

            if (user is UnturnedUser player)
            {
                steamId = player.SteamId;
                characterName = player.DisplayName;
                if (shouldIpAndHwidBan)
                {
                    SteamGameServerNetworking.GetP2PSessionState(steamId, out var sessionState);
                    ip = sessionState.m_nRemoteIP == 0 ? 0 : sessionState.m_nRemoteIP;
                    hwid = string.Join("", player.SteamPlayer.playerID.hwid);
                }
            }
            else if (pData != null)
            {
                steamId = (CSteamID) pData.Id;
                characterName = pData.CharacterName;
                if (shouldIpAndHwidBan)
                {
                    ip = (uint) pData.Ip;
                    hwid = pData.Hwid;
                }
            }
            else
            {
                steamId = (CSteamID) pId;
                characterName = pId.ToString();
            }

            var server = await m_PlayerInfoRepository.GetCurrentServerAsync();
            await m_GlobalBanRepository.BanPlayerAsync(server?.Id ?? 0, steamId.m_SteamID, ip, hwid, duration, adminId,
                reason);

            if (user is UnturnedUser)
            {
                await UniTask.SwitchToMainThread();
                Provider.ban(steamId, reason, duration);
                await UniTask.SwitchToThreadPool();
            }

            var translated = m_StringLocalizer["commands:ban:banned", new {Player = characterName, Reason = reason}];
            await m_UserManager.BroadcastAsync(translated);
            await actor.PrintMessageAsync(translated);
            if (!(actor is ConsoleActor))
                m_Logger.LogInformation(translated);

            if (m_Plugin.Instance != null)
                await m_Plugin.Instance.SendWebhookAsync(WebhookType.Ban, characterName, actor.DisplayName, reason,
                    steamId.ToString(), duration);
        }
    }
}