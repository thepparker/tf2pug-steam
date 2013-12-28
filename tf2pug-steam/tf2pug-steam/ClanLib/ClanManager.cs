using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SteamKit2;

namespace SteamBot.ClanLib
{
    class ClanManager
    {
        /** List of clans */
        private List<Clan> clans;

        public ClanManager()
        {
            this.clans = new List<Clan>();
        }

        public void AddClan(SteamID id)
        {
            SteamID clanid = CopyClanId(id);

            if (!clanid.IsClanAccount)
            {
                Console.WriteLine("ERROR: Attempted to add SteamID ({0}) which is not a clan",
                        clanid
                    );

                return;
            }

            Clan clan = new Clan(clanid);

            this.clans.Add(clan);
        }

        public void RemoveClan(Clan clan)
        {
            this.clans.Remove(clan);
        }

        public void RemoveClan(SteamID clanid)
        {
            Clan clan = GetClanById(clanid);

            if (clan != null)
            {
                RemoveClan(clan);
            }
        }

        public void AddClanMember(SteamID tmpclanid, SteamID member, EClanRank rank)
        {
            if (!member.IsIndividualAccount)
                return;

            SteamID clanid = CopyClanId(tmpclanid);

            Clan clan = GetClanById(clanid);

            if (clan != null)
            {
                clan.MemberManager.AddMember(member, rank);
            }
        }

        public void AddClanChatMember(SteamID tmpclanid, SteamID member)
        {
            if (!member.IsIndividualAccount)
                return;

            SteamID clanid = CopyClanId(tmpclanid);

            Clan clan = GetClanById(clanid);

            if (clan != null)
            {
                clan.ChatUserManager.AddUser(member);
            }
        }

        Clan GetClanById(SteamID id)
        {
            // copy the id
            SteamID clanid = CopyClanId(id);

            return clans.Find(x => x.ClanId.AccountID == clanid.AccountID);
        }

        /**
         * Takes a SteamID that is expected to be either a chat ID or a clan ID
         * and copies it to a new ID so the original is not modified. If a chat
         * ID is given, it is automatically converted to a clan ID
         * 
         * @param SteamID id The ID to be copied
         * 
         * @return SteamID Copied (and possibly converted) steamid
         */
        SteamID CopyClanId(SteamID id)
        {
            SteamID clanid = id.ConvertToUInt64();

            if (!clanid.IsClanAccount)
            {
                clanid.AccountInstance = (uint)SteamID.ChatInstanceFlags.Clan;
                clanid.AccountType = EAccountType.Clan;
            }

            return clanid;
        }
    }
}
