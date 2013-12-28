using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SteamKit2;

namespace SteamBot.ClanLib
{
    /**
     * This class handles clan membership information, which includes the
     * SteamIDs of the users in the clan and their rank.
     */
    class ClanMemberManager
    {
        /** The members of the clan */
        private Dictionary<SteamID, EClanRank> members;

        public ClanMemberManager()
        {
            members = new Dictionary<SteamID, EClanRank>();
        }

        public void AddMember(SteamID member, EClanRank rank)
        {
            // if the member is already in members, let's just update the rank
            // even if it's the same
            if (members.ContainsKey(member))
            {
                members[member] = rank;
            }
            else
            {
                members.Add(member, rank);
            }
        }

        public void RemoveMember(SteamID member)
        {
            if (members.ContainsKey(member))
                members.Remove(member);
        }

        public EClanRank GetMemberRank(SteamID member)
        {
            if (members.ContainsKey(member))
            {
                return members[member];
            }
            else
            {
                return EClanRank.None;
            }
        }

        public void PrintMembers()
        {
            Console.WriteLine("@CLANMEMBERMANAGER - Members:");
            foreach (var member in members)
            {
                Console.WriteLine("\tUser: {0}, rank: {1}", member.Key, member.Value);
            }
        }
    }
}
