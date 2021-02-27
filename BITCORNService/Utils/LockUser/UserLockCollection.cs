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
            if (user != null)
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
    public static class StaticLockCollection
    {
        static HashSet<string> _Locked = new HashSet<string>();
        public static bool Release(string s)
        {
            lock (_Locked)
            {
                return _Locked.Remove(s);
            }
        }

        public static bool Lock(string s)
        {
            lock (_Locked)
            {
                var locked = _Locked.Contains(s);

                if (locked)
                {
                    return false;
                }

                return _Locked.Add(s);
            }

        }
    }
}
