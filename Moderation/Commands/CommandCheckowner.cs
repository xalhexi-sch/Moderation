using System;
using System.Collections.Generic;
using Moderation.Models;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;

namespace Moderation.Commands
{
    public class CommandCheckowner : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "checkowner";
        public string Help => "Check's the owner of barricade/structure/locked vehicle.";
        public string Syntax => "";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "checkowner" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var plugin = ModerationPlugin.Instance;
            UnturnedPlayer player = (UnturnedPlayer)caller;

            int mask = RayMasks.BARRICADE | RayMasks.STRUCTURE | RayMasks.VEHICLE;
            if (!Physics.Raycast(player.Player.look.aim.position, player.Player.look.aim.forward, out RaycastHit hit, 50f, mask))
            {
                UnturnedChat.Say(caller, plugin.Translate("CheckownerFail"), plugin.MessageColor);
                return;
            }

            ulong ownerId = 0;
            string objectType = null;

            // Check Barricade
            BarricadeDrop barricade = BarricadeManager.FindBarricadeByRootTransform(hit.transform);
            if (barricade != null)
            {
                var data = barricade.GetServersideData();
                if (data != null)
                {
                    ownerId = data.owner;
                    objectType = "barricade";
                }
            }

            // Check Structure
            if (ownerId == 0)
            {
                StructureDrop structure = StructureManager.FindStructureByRootTransform(hit.transform);
                if (structure != null)
                {
                    var data = structure.GetServersideData();
                    if (data != null)
                    {
                        ownerId = data.owner;
                        objectType = "structure";
                    }
                }
            }

            // Check Vehicle
            if (ownerId == 0)
            {
                InteractableVehicle vehicle = DamageTool.getVehicle(hit.transform);
                if (vehicle != null && vehicle.isLocked)
                {
                    ownerId = vehicle.lockedOwner.m_SteamID;
                    objectType = "locked vehicle";
                }
            }

            if (ownerId == 0 || string.IsNullOrEmpty(objectType))
            {
                UnturnedChat.Say(caller, plugin.Translate("CheckownerFail"), plugin.MessageColor);
                return;
            }

            var activeBan = plugin.DatabaseService.GetActiveBan(ownerId);
            bool isBanned = activeBan != null && !activeBan.IsExpired;

            PlayerRecord playerRec = plugin.DatabaseService.GetPlayer(ownerId);
            string ownerName = playerRec != null && !string.IsNullOrEmpty(playerRec.SteamName) ? playerRec.SteamName : ownerId.ToString();

            UnturnedChat.Say(caller, plugin.Translate("CheckownerOwnerName", objectType, ownerName, isBanned), plugin.MessageColor);
            UnturnedChat.Say(caller, plugin.Translate("CheckownerOwnerSteamId", ownerId), plugin.MessageColor);

            if (playerRec != null && playerRec.LastOnline != default(DateTime))
            {
                UnturnedChat.Say(caller, plugin.Translate("CheckownerLastActive", ownerName, playerRec.LastOnline.ToString("yyyy-MM-dd HH:mm:ss")), plugin.MessageColor);
            }
        }
    }
}
