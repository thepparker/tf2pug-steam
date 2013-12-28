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
        private EPugMaps map = EPugMaps.None;

        private List<SteamID> players;

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
                    mapvote_start_time = PugManager.GetUnixTimeStamp();

                this.vote_in_progress = value; 
            }
        }

        public int MapVoteCount
        {
            get { return 0; }
        }

        public bool VotingTimeElapsed(long current_time)
        {
            return (current_time - mapvote_start_time) > mapvote_duration;
        }

        public void Vote(SteamID player, EPugMaps map)
        {

        }

        public void TallyVotes()
        {

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

        public EPugMaps Map
        {
            get { return this.map; }
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
