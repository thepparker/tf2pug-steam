using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SteamKit2;

namespace SteamBot.ClanLib
{
    /**
     * This class is used to manage users in a chat room. It contains
     * the chat room ID, and the users currently in the chat room.
     */
    class ClanChatUserManager
    {
        /** The chat room's id */
        private SteamID room_id;

        /** List of users in the room */
        private List<SteamID> userlist;

        public ClanChatUserManager()
        {
            userlist = new List<SteamID>();
        }

        public SteamID RoomId
        {
            get { return this.room_id; }
            set { this.room_id = value; }
        }

        public void AddUser(SteamID user)
        {
            if (userlist.Contains(user))
                return;

            userlist.Add(user);
        }

        public void RemoveUser(SteamID user)
        {
            if (userlist.Contains(user))
                userlist.Remove(user);
        }

        public bool InRoom(SteamID user)
        {
            return userlist.Contains(user);
        }

        public bool InRoom(long userid)
        {
            return FilterUsersByUserId((ulong)userid).Count > 0;
        }

        private List<SteamID> FilterUsersByUserId(ulong userid)
        {
            return userlist.Where(x => x.ConvertToUInt64() == userid).ToList();
        }
    }
}
