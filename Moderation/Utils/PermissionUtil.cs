using Rocket.API;
using Rocket.Core;

namespace Moderation.Utils
{
    public static class PermissionUtil
    {
        public static bool HasPermission(IRocketPlayer player, string permission)
        {
            if (player == null) return false;
            if (player is ConsolePlayer) return true;
            return R.Permissions.HasPermission(player, permission);
        }
    }
}
