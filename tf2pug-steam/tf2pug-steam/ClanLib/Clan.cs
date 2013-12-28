using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SteamKit2;

namespace SteamBot.ClanLib
{
    class Clan
    {
        /** This clan's ID information */
        private SteamID clan;

        /** Member manager for this clan */
        private ClanMemberManager member_manager;

        /** Clan chat user manager */
        private ClanChatUserManager chat_user_manager;

        public Clan(SteamID clan)
        {
            this.clan = clan;

            this.member_manager = new ClanMemberManager();

            SteamID chatId = clan.ConvertToUInt64();
            chatId.AccountInstance = (uint)SteamID.ChatInstanceFlags.Clan;
            chatId.AccountType = EAccountType.Chat;

            this.chat_user_manager = new ClanChatUserManager(chatId);
        }

        public SteamID ClanId
        {
            get { return this.clan; }
            set { this.clan = value; }
        }

        public ClanMemberManager MemberManager
        {
            get { return this.member_manager; }
        }

        public ClanChatUserManager ChatUserManager
        {
            get { return this.chat_user_manager; }
        }

        public override String ToString()
        {
            return this.clan.ToString();
        }
    }
}
