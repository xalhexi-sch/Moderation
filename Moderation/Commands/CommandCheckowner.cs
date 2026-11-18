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

            if (player?.Player?.look?.aim == null) return;

            int mask = RayMasks.BARRICADE | RayMasks.STRUCTURE | RayMasks.VEHICLE;
            if (!Physics.Raycast(player.Player.look.aim.position, player.Player.look.aim.forward, out RaycastHit hit, 50f, mask))
            {
                UnturnedChat.Say(caller, plugin.Translate("CheckownerFail"), plugin.MessageColor);
                return;
            }

            ulong ownerId = 0;
            string objectType = null;
            Transform hitTransform = hit.transform;

            // 1. Check Barricade using DamageTool.getBarricadeRootTransform to resolve child sub-colliders
            try
            {
                Transform barricadeRoot = DamageTool.getBarricadeRootTransform(hitTransform);
                BarricadeDrop barricade = BarricadeManager.FindBarricadeByRootTransform(barricadeRoot);
                if (barricade != null)
                {
                    var data = barricade.GetServersideData();
                    if (data != null && data.owner != 0)
                    {
                        ownerId = data.owner;
                        objectType = "barricade";
                    }
                }
            }
            catch { }

            // 2. Check Structure using DamageTool.getStructureRootTransform to resolve child sub-colliders
            if (ownerId == 0)
            {
                try
                {
                    Transform structureRoot = DamageTool.getStructureRootTransform(hitTransform);
                    StructureDrop structure = StructureManager.FindStructureByRootTransform(structureRoot);
                    if (structure != null)
                    {
                        var data = structure.GetServersideData();
                        if (data != null && data.owner != 0)
                        {
                            ownerId = data.owner;
                            objectType = "structure";
                        }
                    }
                }
                catch { }
            }

            // 3. Check Vehicle
            if (ownerId == 0)
            {
                try
                {
                    InteractableVehicle vehicle = DamageTool.getVehicle(hitTransform);
                    if (vehicle != null && vehicle.isLocked && vehicle.lockedOwner.m_SteamID != 0)
                    {
                        ownerId = vehicle.lockedOwner.m_SteamID;
                        objectType = "locked vehicle";
                    }
                }
                catch { }
            }

            // 4. Fallback using Unturned's DamageTool.TryFindOwnership
            if (ownerId == 0)
            {
                try
                {
                    if (DamageTool.TryFindOwnership(hitTransform, out ulong fallbackOwner, out _) && fallbackOwner != 0)
                    {
                        ownerId = fallbackOwner;
                        objectType = "object";
                    }
                }
                catch { }
            }

            if (ownerId == 0 || string.IsNullOrEmpty(objectType))
            {
                UnturnedChat.Say(caller, plugin.Translate("CheckownerFail"), plugin.MessageColor);
                return;
            }

            var activeBan = plugin?.DatabaseService?.GetActiveBan(ownerId);
            bool isBanned = activeBan != null && activeBan.IsCurrentlyActive;

            PlayerRecord playerRec = plugin?.DatabaseService?.GetPlayer(ownerId);
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
