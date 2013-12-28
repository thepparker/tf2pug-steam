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
        private bool started = false;

        private long mapvote_start_time;
        private long mapvote_duration = 60;
        private bool vote_in_progress = false;

        private EPugMaps win_map = EPugMaps.None;
        private int win_map_count = 0;

        private Dictionary<SteamID, EPugMaps> player_votes;
        private Dictionary<EPugMaps, int> map_votes;

        private List<SteamID> players;

        /** Server details */
        private String ip;
        private int port;
        private String password;

        public Pug(int size)
        {
            id = PugManager.GetUnixTimeStamp();

            players = new List<SteamID>();

            this.size = size;
        }

        //----------------------------------------------
        // MAP VOTING
        //----------------------------------------------

        public bool VoteInProgress
        {
            get { return this.vote_in_progress; }
            
            set 
            {
                if (value == true)
                {
                    mapvote_start_time = PugManager.GetUnixTimeStamp();

                    player_votes = new Dictionary<SteamID, EPugMaps>();
                    map_votes = new Dictionary<EPugMaps, int>();
                }

                this.vote_in_progress = value; 
            }
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

        /**
         * Sets the winning map
         */
        public void DetermineWinningMap()
        {
            map_votes.OrderBy(vote => vote.Value);

            KeyValuePair<EPugMaps, int> win_pair = map_votes.ElementAt(0);

            this.win_map = win_pair.Key;
            this.win_map_count = win_pair.Value;
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

        //----------------------------------------------
        // HELPERS
        //----------------------------------------------

        public long Id
        {
            get { return this.id; }
        }

        public bool Started
        {
            get { return this.started; }
            set { this.started = value; }
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
    }

    //----------------------------------------------
    // MAP ENUM
    //----------------------------------------------
    enum EPugMaps
    {
        None,
        cp_granary,
        cp_well,
        cp_badlands
    }
}
