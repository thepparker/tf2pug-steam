using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SteamKit2;

namespace SteamBot.PugLib
{
    class Pug
    {
        private long id;

        private int size;

        private EPugState state;

        private long mapvote_start_time;
        private long mapvote_duration = 60;

        private EPugMaps win_map = EPugMaps.None;
        private int win_map_count = 0;

        private Dictionary<SteamID, EPugMaps> player_votes;
        private Dictionary<EPugMaps, int> map_votes;

        private List<SteamID> players;

        private List<SteamID> team_red;
        private List<SteamID> team_blue;
        
        /** Server details */
        public String ip { get; set; }
        public int port { get; set; }
        public String password { get; set; }
        private String admin_password;

        public Pug(int size)
        {
            id = PugManager.GetUnixTimeStamp();

            players = new List<SteamID>();

            team_red = new List<SteamID>();
            team_blue = new List<SteamID>();

            this.size = size;

            state = EPugState.GATHERING_PLAYERS;
        }

        //----------------------------------------------
        // MAP VOTING
        //----------------------------------------------

        public bool VoteInProgress
        {
            get { return this.state == EPugState.MAP_VOTING; }
        }

        public void StartMapVote()
        {
            mapvote_start_time = PugManager.GetUnixTimeStamp();

            player_votes = new Dictionary<SteamID, EPugMaps>();
            map_votes = new Dictionary<EPugMaps, int>();

            state = EPugState.MAP_VOTING;
        }

        public void EndMapVote()
        {
            map_votes.OrderBy(vote => vote.Value);

            KeyValuePair<EPugMaps, int> win_pair = map_votes.ElementAt(0);

            this.win_map = win_pair.Key;
            this.win_map_count = win_pair.Value;
        }

        public bool VotingTimeElapsed(long current_time)
        {
            return (current_time - mapvote_start_time) > mapvote_duration;
        }

        public void Vote(SteamID player, EPugMaps map)
        {
            // if it's the first time this player has voted, add his voted map
            // and increment that map count by 1
            if (!player_votes.ContainsKey(player))
            {
                player_votes.Add(player, map);
                IncrementMapVote(map);
            }
            else
            {
                // else, decrement the map previously voted for, increment the
                // new map and update which map this player is voting for
                DecrementMapVote(player_votes[player]);
                IncrementMapVote(map);
                player_votes[player] = map;
            }
        }

        void IncrementMapVote(EPugMaps map)
        {
            if (map_votes.ContainsKey(map))
                map_votes[map] += 1;
            else
                map_votes.Add(map, 1);
        }

        void DecrementMapVote(EPugMaps map)
        {
            if (map_votes.ContainsKey(map))
            {
                map_votes[map] -= 1;

                if (map_votes[map] < 0)
                {
                    map_votes[map] = 0;
                }
            }
            else
            {
                map_votes[map] = 0;
            }
        }

        //----------------------------------------------
        // PLAYER MANIPULATION & INFORMATION
        //----------------------------------------------

        public SteamID Starter
        {
            get { return this.players[0]; }
        }

        public void Add(SteamID player)
        {
            this.players.Add(player);
        }

        public void Remove(SteamID player)
        {
            this.players.Remove(player);
        }

        public static String GetMapsAsString()
        {
            return String.Join(" - ", GetMapsAsList().ToArray());
        }

        public static List<EPugMaps> GetMapsAsList()
        {
            // convert the EPugMaps enum to a list
            List<EPugMaps> list = Enum.GetValues(typeof(EPugMaps)).Cast<EPugMaps>().ToList();
            list.RemoveAt(0); //remove the None item

            return list;
        }

        public String SlotsRemaining
        {
            get
            {
                return String.Format("{0}/{1} slots remaining", size - players.Count, size);
            }
        }

        //----------------------------------------------
        // TEAMS
        //----------------------------------------------

        public void ShuffleTeams()
        {
            team_red.Clear();
            team_blue.Clear();

            team_red.Add(players[0]);
            team_red.Add(players[1]);
            team_red.Add(players[2]);
            team_red.Add(players[3]);
            team_red.Add(players[4]);
            team_red.Add(players[5]);

            team_blue.Add(players[6]);
            team_blue.Add(players[7]);
            team_blue.Add(players[8]);
            team_blue.Add(players[9]);
            team_blue.Add(players[10]);
            team_blue.Add(players[11]);
        }

        public List<SteamID> TeamRed
        {
            get { return this.team_red; }
        }

        public List<SteamID> TeamBlue
        {
            get { return this.team_blue; }
        }

        //----------------------------------------------
        // HELPERS
        //----------------------------------------------

        public String GetStatusMessage()
        {
            if (state == EPugState.MAP_VOTING)
            {
                return "Map voting currently in progress";
            }
            else if (state == EPugState.GAME_STARTED)
            {
                return "The game has started";
            }
            else if (state == EPugState.GATHERING_PLAYERS)
            {
                return String.Format("Gathering players. ({0})", SlotsRemaining);
            }
            else if (state == EPugState.DETAILS_SENT)
            {
                return "Details have been sent. Waiting for the game to begin";
            }
            else if (state == EPugState.GAME_OVER)
            {
                return "The game is over";
            }
            else
            {
                return "Uknown";
            }
        }

        public long Id
        {
            get { return this.id; }
        }

        public bool Started
        {
            get { return this.state == EPugState.GAME_STARTED; }
        }

        public EPugMaps Map
        {
            get { return this.win_map; }
            set { this.win_map = value; }
        }

        public int MapVoteCount(EPugMaps map)
        {
            return map_votes[map];
        }

        public List<SteamID> Players
        {
            get { return this.players; }
        }

        public int Size
        {
            get { return this.size; }
            set { this.size = value; }
        }

        public bool Full
        {
            get { return this.size == this.players.Count; }
        }

        public EPugState State
        {
            get { return this.state; }
            set { this.state = value; }
        }

        public String AdminPassword
        {
            get { return this.admin_password; }
            set { this.admin_password = value; }
        }

        public String ConnectString
        {
            get
            {
                return String.Format("connect {0}:{1}; password {2}",
                                ip, port, password
                            );
            }
        }
    }

    //----------------------------------------------
    // ENUMS
    //----------------------------------------------
    enum EPugMaps
    {
        None,
        cp_granary,
        cp_well,
        cp_badlands
    }

    enum EPugState
    {
        GATHERING_PLAYERS,
        MAP_VOTING,
        DETAILS_SENT,
        GAME_STARTED,
        GAME_OVER
    }
}
