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
            foreach (var pug in pug_list)
            {
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
                    ChatHandler.sendMainRoomMessage(msg);

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
         * Removes the given player from the pug they are in (if any)
         * 
         * @param SteamID player The player to remove
         */
        public void RemovePlayer(SteamID player)
        {
            Pug pug;

            if ((pug = GetPlayerPug(player)) != null)
            {
                pug.Remove(player);
            }
        }

        /**
         * Starts a new pug and adds the given player
         * 
         * @param SteamID player The player who is starting this pug
         * @param int size (Optional) The number of players that can join the pug
         */ 
        public void CreateNewPug(SteamID player, int size = 12)
        {
            Pug pug = new Pug(size);

            pug_list.Add(pug);

            AddPlayer(player, pug);

            String msg = String.Format("A {0} player pug has been started by {1}. Type !j to join",
                pug.Size, steam_friends.GetFriendPersonaName(pug.Starter));

            ChatHandler.sendMainRoomMessage(msg);

            AdvertisePug(pug);
        }

        void AdvertisePug(Pug pug)
        {
            
        }

        //----------------------------------------------
        // MAP VOTING
        //----------------------------------------------

        /** 
         * Starts map voting for the given pug
         * 
         * @param Pug pug The pug to start the map vote for
         */
        void StartMapVote(Pug pug)
        {
            pug.VoteInProgress = true;

            String msg = String.Format("Map voting is now in progress for pug {0}. Maps: {1}",
                pug.Id, Pug.GetMapsAsString());

            ChatHandler.sendMainRoomMessage(msg);

            ChatHandler.sendMainRoomMessage(
                    "To vote for a map, type !map <map>. eg, !map cp_granary"
                );
        }

        /** 
         * Ends map voting for the given pug, forces vote tally, etc
         * 
         * @param Pug pug The pug to end map voting for
         */
        void EndMapVote(Pug pug)
        {
            pug.VoteInProgress = false;

            pug.TallyVotes();

            if (pug.Map == EPugMaps.None)
            {
                // no one voted, woops! need to pick a random map

            }

            String msg = String.Format("Map voting is complete. {0} won the vote with {1} votes",
                    pug.Map, pug.MapVoteCount
                );

            ChatHandler.sendMainRoomMessage(msg);
        }

        public void ForceMapVote(SteamID player)
        {
            Pug pug;

            if ((pug = GetPlayerPug(player)) != null)
            {
                StartMapVote(pug);
            }
        }

        /** 
         * Adds a player's vote for the given map to the pug they're in
         * 
         * @param SteamID player The player who is voting
         * @param String map The map being voted for
         */
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

        /** 
         * Periodically called to check pug vote periods. Ends map voting
         * once the appropriate duration has passed
         */
        public void CheckMapVotes()
        {
            long current_time = GetUnixTimeStamp();

            foreach (var pug in pug_list)
            {
                if (pug.VoteInProgress && pug.VotingTimeElapsed(current_time))
                {
                    EndMapVote(pug);
                }
            }
        }

        //----------------------------------------------
        // MISC HELPER METHODS
        //----------------------------------------------

        /** 
         * Returns whether or not a player is in a pug
         * 
         * @param SteamID player The player to check for
         * 
         * @return bool True if in a pug
         */
        bool PlayerInPug(SteamID player)
        {
            return GetPlayerPug(player) != null;
        }

        /** 
         * Gets the pug the given player is in
         * 
         * @param SteamID player The player to check for
         * 
         * @return Pug The pug, or null if not in a pug
         */
        Pug GetPlayerPug(SteamID player)
        {
            foreach (var pug in pug_list)
            {
                if (pug.Players.Contains(player))
                    return pug;
            }

            return null;
        }

        /** 
         * Gets a list of players in the given pug as a string so it can be
         * easily printed
         * 
         * @param Pug pug The pug to get the string for
         * 
         * @return String The string of players
         */
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

        /** 
         * Gets the current unix timestamp with respect to UTC time
         * 
         * @return long Unix timestamp (in seconds)
         */
        public static long GetUnixTimeStamp()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }
    }
}
