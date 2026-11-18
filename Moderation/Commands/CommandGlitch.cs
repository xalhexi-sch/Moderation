using System;
using System.Collections.Generic;
using Moderation.Models;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using UnityEngine;

namespace Moderation.Commands
{
    public class CommandGlitch : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "glitch";
        public string Help => "Teleports you to suspected glitch barricade or structure.";
        public string Syntax => "[index|rescan]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "glitch" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var plugin = ModerationPlugin.Instance;
            UnturnedPlayer player = (UnturnedPlayer)caller;

            if (command.Length > 0 && string.Equals(command[0], "rescan", StringComparison.OrdinalIgnoreCase))
            {
                int count = plugin.GlitchService.Rescan();
                UnturnedChat.Say(caller, plugin.Translate("GlitchRescan", count), plugin.MessageColor);
                return;
            }

            if (plugin.GlitchService.Count == 0)
            {
                plugin.GlitchService.Rescan();
            }

            int total = plugin.GlitchService.Count;
            if (total == 0)
            {
                UnturnedChat.Say(caller, plugin.Translate("NoGlitchesFound"), plugin.MessageColor);
                return;
            }

            int targetIndex;
            if (command.Length > 0)
            {
                if (!int.TryParse(command[0], out targetIndex) || targetIndex < 1 || targetIndex > total)
                {
                    UnturnedChat.Say(caller, plugin.Translate("GlitchInvalidIndex", command[0], total), plugin.MessageColor);
                    return;
                }
            }
            else
            {
                targetIndex = plugin.GlitchService.GetNextIndex(player.CSteamID.m_SteamID);
            }

            GlitchObject glitch = plugin.GlitchService.GetGlitch(targetIndex);
            if (glitch == null || glitch.IsDestroyed)
            {
                UnturnedChat.Say(caller, plugin.Translate("GlitchedDestroyed", targetIndex, total), plugin.MessageColor);
                return;
            }

            Vector3 tpPos = glitch.Position + new Vector3(0, 1.5f, 0);
            player.Teleport(tpPos, player.Rotation);
            UnturnedChat.Say(caller, plugin.Translate("GlitchTeleported", targetIndex, total), plugin.MessageColor);
        }
    }
}
