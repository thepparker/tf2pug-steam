using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

using SteamKit2;

namespace SteamBot.PugLib
{
    class Pug
    {
        private long id;

        private int size;
        private bool started = false;
        private bool vote_in_progress = false;
        private EPugMaps map = EPugMaps.None;

        private List<SteamID> players;

        public Pug(int size)
        {
            id = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

            players = new List<SteamID>();

            this.size = size;
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

        public long Id
        {
            get { return this.id; }
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

        public bool VoteInProgress
        {
            get { return this.vote_in_progress; }
            set { this.vote_in_progress = value; }
        }

        public SteamID Starter
        {
            get { return this.players[0]; }
        }

        public void Add(SteamID player)
        {
            this.players.Add(player);
        }

        public void Vote(SteamID player, EPugMaps map)
        {

        }
    }

    enum EPugMaps
    {
        None,
        cp_granary,
        cp_well,
        cp_badlands
    }
}
