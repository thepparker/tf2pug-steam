using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

using SteamBot.Handlers;

using SteamKit2;

namespace SteamBot.PugLib
{
    class PugManager
    {
        private SteamFriends steam_friends;

        private List<Pug> pug_list;

        public PugManager(SteamFriends steam_friends)
        {
            this.pug_list = new List<Pug>();
            this.steam_friends = steam_friends;
        }

        /**
         * Checks if there are any current pugs that have space available
         * 
         * @return Pug The pug with space, or null
         */
        public Pug SpaceAvailable()
        {
            Pug pug;
            foreach (var pug_item in pug_list)
            {
                pug = (Pug)pug_item;

                if (pug.Players.Count < pug.Size)
                    return pug;
            }
            
            return null;
        }

        /**
         * Adds the given player to the first pug with room
         *
         * @param SteamID player The player to add
         */
        public void AddPlayer(SteamID player)
        {
            if (PlayerInPug(player))
            {
                ChatHandler.sendMessage(null, player, "You're already in a pug");
                return;
            }

            Pug pug;

            if ((pug = SpaceAvailable()) != null)
            {
                AddPlayer(player, pug);

                if (pug.Full)
                {
                    String msg = String.Format("Pug {0} is now full. Players: {1}", pug.Id, GetPlayerListAsString(pug));
                    StartMapVote(pug);
                }
            }
            else
            {
                CreateNewPug(player);
            }
        }

        /**
         * Adds the given player to the specified pug
         * 
         * @param SteamID player The player to add
         * @param Pug pug The pug to add the player to
         * 
         * @return bool Whether or not the player was added to the pug
         */
        bool AddPlayer(SteamID player, Pug pug)
        {
            if (pug.Players.Count < pug.Size)
            {
                pug.Add(player);

                return true;
            }

            return false;
        }

        /**
         * Starts a new pug and adds the given player
         */ 
        public void CreateNewPug(SteamID player, int size = 12)
        {
            Pug pug = new Pug(size);

            pug_list.Add(pug);

            AddPlayer(player, pug);

            String msg = String.Format("A {0} player pug has been started by {1}. Type !j to join",
                pug.Size, steam_friends.GetFriendPersonaName(pug.Starter));

            ChatHandler.sendMessage(Program.pugChatId, null, msg);

            AdvertisePug(pug);
        }

        /** Starts map voting for the given pug
         * @param Pug pug The pug to start the map vote for
         */
        void StartMapVote(Pug pug)
        {
            pug.VoteInProgress = true;

            String msg = String.Format("Map voting is now in progress for pug {0}. Maps: {1}",
                pug.Id, Pug.GetMapsAsString());
            ChatHandler.sendMessage(Program.pugChatId, null, msg);

            ChatHandler.sendMessage(Program.pugChatId, null,
                "To vote for a map, type !map <map>. eg, !map cp_granary");
        }

        void AdvertisePug(Pug pug)
        {
            
        }

        bool PlayerInPug(SteamID player)
        {
            return GetPlayerPug(player) != null;
        }

        public void VoteMap(SteamID player, String map)
        {
            EPugMaps enum_map = (EPugMaps)Enum.Parse(typeof(EPugMaps), map);
            if (!Pug.GetMapsAsList().Contains(enum_map))
            {
                return;
            }

            Pug pug = GetPlayerPug(player);
           
            if (pug != null)
            {
                pug.Vote(player, enum_map);
            }
        }

        Pug GetPlayerPug(SteamID player)
        {
            Pug pug;
            foreach (var pug_item in pug_list)
            {
                pug = (Pug)pug_item;

                if (pug.Players.Contains(player))
                    return pug;
            }

            return null;
        }

        public String GetPlayerListAsString(Pug pug)
        {
            List<String> names = new List<String>();

            foreach (var player in pug.Players)
            {
                names.Add(steam_friends.GetFriendPersonaName(player));
            }

            return String.Join(", ", names);
        }

        public List<Pug> Pugs
        {
            get { return this.pug_list; }
        }
    }
}
