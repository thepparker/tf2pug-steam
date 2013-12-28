using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
         * Adds the given player to the first pug with room. The player's ID
         * object is copied to prevent modifying the cache
         *
         * @param SteamID id The unaltered ID of the player to add
         */
        public void AddPlayer(SteamID player)
        {
            // copy the user
            //SteamID player = id.ConvertToUInt64();

            if (PlayerInPug(player))
            {
                ChatHandler.sendMessage(null, player, "You're already in a pug");
                return;
            }

            Pug pug;

            if ((pug = SpaceAvailable()) != null)
            {
                if (AddPlayer(player, pug))
                {
                    String msg = String.Format("{0} has joined pug {1} ({2})",
                            steam_friends.GetFriendPersonaName(player), pug.Id, pug.SlotsRemaining
                        );

                    ChatHandler.sendMainRoomMessage(msg);
                }
                else
                {
                    String msg = String.Format("Unable to add {0} to pug {1}",
                            player, pug.Id
                        );

                    ChatHandler.sendMainRoomMessage(msg);
                }

                if (pug.Full)
                {
                    String msg = String.Format("Pug {0} is now full. Players: {1}", pug.Id, GetPugPlayerListAsString(pug));
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

                String msg = String.Format("{0} has left pug {1}. ({2})",
                        steam_friends.GetFriendPersonaName(player), pug.Id, pug.SlotsRemaining
                    );
                
                ChatHandler.sendMainRoomMessage(msg);
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
            if (PlayerInPug(player))
            {
                ChatHandler.sendMessage(null, player, "You're already in a pug");
                return;
            }

            if (size != 12 && size != 18)
            {
                ChatHandler.sendMessage(null, player, "Invalid pug size specified. Must be 12 or 18");
                return;
            }

            Pug pug = new Pug(size);

            pug_list.Add(pug);

            AddPlayer(player, pug);

            String msg = String.Format("A {0} player pug has been started by {1}. Pug ID: {2}. Type !j to join",
                pug.Size, steam_friends.GetFriendPersonaName(pug.Starter), pug.Id);

            ChatHandler.sendMainRoomMessage(msg);

            AdvertisePug(pug);
        }

        public void EndPug(Pug pug)
        {
            if (pug == null)
                return;

            ResetServer(pug);

            pug_list.Remove(pug);

            String msg = String.Format("Pug {0} has been manually ended", pug.Id);
            ChatHandler.sendMainRoomMessage(msg);
        }

        public void EndPug(SteamID player, bool admin = false)
        {
            Pug pug;

            if ((pug = GetPlayerPug(player)) != null && (pug.Starter == player || admin))
            {
                EndPug(pug);
            }
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
            if (pug.VoteInProgress || pug.Started)
                return;

            pug.StartMapVote();

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
            pug.EndMapVote();

            if (pug.Map == EPugMaps.None)
            {
                // no one voted, woops! need to pick a random map
                pug.Map = EPugMaps.cp_granary;
            }

            String msg = String.Format("Map voting is complete. {0} won the vote with {1} vote(s)",
                    pug.Map, pug.MapVoteCount(pug.Map)
                );

            ChatHandler.sendMainRoomMessage(msg);

            msg = String.Format(
                "Pug {0} is now in-game. Admin: {1}. Details are being sent, please join the server PROMPTLY",
                pug.Id, steam_friends.GetFriendPersonaName(pug.Starter)
            );

            ChatHandler.sendMainRoomMessage(msg);

            // now we do some shit to send details, setup server, etc
            pug.ShuffleTeams();
            ShowTeams(pug);

            PrepareServer(pug);

            SendMassDetails(pug);
        }

        public void ForceMapVote(SteamID player)
        {
            Pug pug;
            if ((pug = GetPlayerPug(player)) != null)
            {
                StartMapVote(pug);
            }
        }

        public void ForceMapVote(Pug pug)
        {
            if (pug != null)
                StartMapVote(pug);
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
                return;

            Pug pug = GetPlayerPug(player);
           
            if (pug != null && pug.VoteInProgress)
            {
                pug.Vote(player, enum_map);
                
                String msg = String.Format("{0} voted for {1} ({2})",
                        steam_friends.GetFriendPersonaName(player), enum_map, pug.MapVoteCount(enum_map)    
                    );
                
                ChatHandler.sendMainRoomMessage(msg);
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
        // SERVER INTERACTION AND DETAILS
        //----------------------------------------------

        void PrepareServer(Pug pug)
        {
            // selects a server and sets up the details and shit
            pug.ip = "1.1.1.1";
            pug.port = 11111;
            pug.password = "123abc";
            pug.AdminPassword = "asdf";
        }

        void ResetServer(Pug pug)
        {

        }

        public void SendMassDetails(Pug pug)
        {
            foreach (var player in pug.Players)
                Details(player, pug);

            pug.State = EPugState.DETAILS_SENT;
        }

        public void Details(SteamID player, bool admin = false)
        {
            Pug pug;
            if ((pug = GetPlayerPug(player)) != null)
            {
                if (pug.State == EPugState.DETAILS_SENT || admin)
                    Details(player, pug, admin);
            }
        }

        public void Details(SteamID player, Pug pug, bool admin = false)
        {
            String msg;

            if (player == pug.Starter || admin)
            {
                msg = String.Format("Server details for pug {0}: {1}. Admin pass: {2}",
                        pug.Id, pug.ConnectString, pug.AdminPassword
                    );
            }
            else
            {
                msg = String.Format("Server details for pug {0}: {1}",
                        pug.Id, pug.ConnectString
                    );
            }

            ChatHandler.sendMessage(null, player, msg);
        }

        public void ShowTeams(Pug pug)
        {
            String msg = String.Format("Teams for pug {0}:", pug.Id);
            ChatHandler.sendMainRoomMessage(msg);

            List<String> team_list = GetNamedPlayerList(pug.TeamRed);

            msg = String.Format("RED: {0}", String.Join(", ", team_list));
            ChatHandler.sendMainRoomMessage(msg);

            team_list = GetNamedPlayerList(pug.TeamBlue);
            msg = String.Format("BLUE: {0}", String.Join(", ", team_list));
            ChatHandler.sendMainRoomMessage(msg);
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
            return pug_list.Find(pug => pug.Players.Contains(player));

            foreach (var pug in pug_list)
            {
                if (pug.Players.Contains(player))
                    return pug;
            }

            return null;
        }

        /**
         * Gets the pug corresponding to the given ID
         */
        public Pug GetPugById(long id)
        {
            return pug_list.Find(pug => pug.Id == id);
        }

        /**
         * Takes a list of SteamIDs and converts it to a list of names
         * 
         * @param List<SteamID> players The player list
         * 
         * @return List<String> A list of player names
         */
        public List<String> GetNamedPlayerList(List<SteamID> players)
        {
            List<String> names = new List<String>();

            foreach (var player in players)
            {
                names.Add(steam_friends.GetFriendPersonaName(player));
            }

            return names;
        }

        /** 
         * Gets a list of players in the given pug as a string so it can be
         * easily printed
         * 
         * @param Pug pug The pug to get the string for
         * 
         * @return String The string of players
         */
        public String GetPugPlayerListAsString(Pug pug)
        {
            return String.Join(", ", GetNamedPlayerList(pug.Players));
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
