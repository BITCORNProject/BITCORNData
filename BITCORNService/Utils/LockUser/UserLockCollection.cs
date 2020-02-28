using System.Collections.Generic;
using BITCORNService.Models;

namespace BITCORNService.Utils.LockUser
{
    public static class UserLockCollection
    {
        static HashSet<int> _LockedUsers = new HashSet<int>();
        public static int UserLockedReturnCode => 420;
        public static bool Lock(User user)
        {
            if (user != null)
                return Lock(user.UserId);
            return false;
        }

        public static bool Lock(int userId)
        {
            lock (_LockedUsers)
            {
                var userLocked = _LockedUsers.Contains(userId);

                if (userLocked)
                {
                    return false;
                }

                return _LockedUsers.Add(userId);
            }

        }

        public static bool Release(User user)
        {
            if (user!=null)
                return Release(user.UserId);
            return false;
        }

        public static bool Release(int userId)
        {
            lock (_LockedUsers)
            {
                return _LockedUsers.Remove(userId);
            }
        }
    }
}
