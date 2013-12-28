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

            Console.WriteLine("@CLAN Added clan to clan manager. ID: {0}, chat id: {1}",
                    clan.ClanId, clan.ChatUserManager.Id
                );
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

        public void AddClanMember(SteamID clanid, SteamID member, EClanRank rank)
        {
            if (!member.IsIndividualAccount)
                return;

            Clan clan = GetClanById(clanid);

            if (clan != null)
            {
                clan.MemberManager.AddMember(member, rank);

                Console.WriteLine("@CLAN Added user {0} to clan {1} with rank {2}",
                        member, clan.ClanId, rank);
            }
        }

        public void AddClanChatMember(SteamID clanid, SteamID member)
        {
            if (!member.IsIndividualAccount)
                return;

            Clan clan = GetClanById(clanid);

            if (clan != null)
            {
                clan.ChatUserManager.AddUser(member);

                Console.WriteLine("@CLAN Added user {0} to clan chat for clan {1}", member, clan.ClanId);
            }
        }

        public Clan GetClanById(SteamID id)
        {
            // copy the id to ensure it's a clan id
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
                clanid.AccountInstance = SteamID.AllInstances;
                //clanid.AccountInstance = (uint)SteamID.ChatInstanceFlags.Clan;
                clanid.AccountType = EAccountType.Clan;
            }

            return clanid;
        }
    }
}
