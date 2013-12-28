using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SteamBot.PugLib;
using SteamBot.ClanLib;

using SteamKit2;

namespace SteamBot.Handlers
{
    /**
     * The ChatHandler class parses all chat messages received from both
     * private messages and the public chat room. It uses method overloading
     * to initially split the different message types, and then uses a single
     * method to parse the message
     */
    class ChatHandler
    {
        private static SteamFriends steam_friends;
        private PugManager pug_manager;
        private ClanManager clan_manager;

        public ChatHandler(SteamFriends steam_friends, PugManager pug_manager,
            ClanManager clan_manager)
        {
            ChatHandler.steam_friends = steam_friends;
            this.pug_manager = pug_manager;
            this.clan_manager = clan_manager;
        }

        public void parse(SteamFriends.ChatMsgCallback group_chat)
        {
            // this is a group chat message

            parse_chat(group_chat.ChatRoomID, group_chat.ChatterID, group_chat.Message);
        }
        public void parse(SteamFriends.FriendMsgCallback private_chat)
        {
            // this is a friend (private) chat message
            if (private_chat.FromLimitedAccount)
            {
                sendMessage(null, private_chat.Sender,
                    "You cannot use this service with a limited account");

                return;
            }

            parse_chat(null, private_chat.Sender, private_chat.Message);
        }

        void parse_chat(SteamID chat_room, SteamID sender, String message)
        {
            //Program.sendMessage();
            String[] split_message = message.Split(' ');
            if (split_message.Length < 1) return;

            // check if the user is an admin by looking at the clan list
            Clan pug_clan = clan_manager.GetClanById(Program.pugClanId);
            EClanRank rank = pug_clan.MemberManager.GetMemberRank(sender);
            
            bool admin = (rank == EClanRank.Officer || rank == EClanRank.Owner);

            Console.WriteLine("@CHATHANDLER - User: {0}, Clan: {1}, rank: {2}, admin: {3}",
                    sender, pug_clan, rank, admin
                );

            // split the string into cmd and arguments
            String cmd = split_message[0].ToLower();
            String[] args = split_message.Skip(1).Take(split_message.Length - 1).ToArray();

            print_cmd(cmd, args, admin);

            if (cmd == "!test")
            {
                sendMessage(chat_room, sender, "test test test!");
            }
            else if (cmd == "!pug")
            {
                if (args.Length > 0)
                {
                    pug_manager.CreateNewPug(sender, int.Parse(args[0]));
                }
                else
                {
                    pug_manager.CreateNewPug(sender);
                }
            }
            else if (cmd == "!j" || cmd == "!join" || cmd == "!add")
            {
                pug_manager.AddPlayer(sender);
            }
            else if (cmd == "!l" || cmd == "!leave" || cmd == "!del")
            {
                pug_manager.RemovePlayer(sender);
            }
            else if (cmd == "!maps")
            {
                sendMessage(chat_room, sender, Pug.GetMapsAsString());
            }
            else if (cmd == "!map")
            {
                if (args.Length == 1)
                    pug_manager.VoteMap(sender, args[0]);
            }
            else if (cmd == "!players")
            {
                PrintPlayersAll();
            }
            else if (cmd == "!status")
            {
                if (args.Length > 0)
                {
                    return;
                }
                else
                {
                    PrintStatusAll();
                }
            }
            else if (cmd == "!forcemapvote" && admin)
            {
                if (args.Length > 0)
                {
                    pug_manager.ForceMapVote(pug_manager.GetPugById(long.Parse(args[0])));
                }
                else
                {
                    pug_manager.ForceMapVote(sender);
                }
            }
            else if (cmd == "!clanmembers" && rank == EClanRank.Owner)
            {
                pug_clan.MemberManager.PrintMembers();
            }
            else if (cmd == "!fakeuserjoin" && rank == EClanRank.Owner)
            {
                if (args.Length == 0)
                    return;

                int num_fakes = int.Parse(args[0]);
                SteamID fake;

                for (int i = 0; i < num_fakes; i++)
                {
                    fake = new SteamID((76561197960265728UL + (ulong)i));

                    pug_manager.AddPlayer(fake);
                }
            }
        }

        public static void sendMessage(SteamID chat_room, SteamID target = null, String message = "")
        {
            EChatEntryType type = EChatEntryType.ChatMsg;

            if (chat_room != null)
            {
                Console.WriteLine("@PUBCHAT -> {0}: {1}", chat_room, message);
                steam_friends.SendChatRoomMessage(chat_room, type, message);
            }
            else
            {
                Console.WriteLine("@PRIVCHAT -> {0} ({1}): {2}",
                    steam_friends.GetFriendPersonaName(target),
                    target, message);

                steam_friends.SendChatMessage(target, type, message);
            }
        }

        public static void sendMainRoomMessage(String message)
        {
            sendMessage(Program.pugClanId, null, message);
        }

        void print_cmd(String cmd, String[] args, bool admin)
        {
            Console.WriteLine("cmd: {0} (user is admin: {1})", cmd, admin);
            Console.WriteLine("args: ");

            for (int i = 0; i < args.Length; i++)
            {
                Console.WriteLine("\t{0}: {1}", i, args[i]);
            }
        }

        void PrintPlayersAll()
        {
            foreach (var pug in pug_manager.Pugs)
                PrintPlayers(pug);
        }

        void PrintPlayers(Pug pug)
        {
            String players = pug_manager.GetPlayerListAsString(pug);

            String msg = String.Format("Players in pug {0}: {1}",
                pug.Id, players);

            sendMainRoomMessage(msg);
        }

        void PrintStatusAll()
        {
            foreach (var pug in pug_manager.Pugs)
                PrintStatus(pug);
        }

        void PrintStatus(Pug pug)
        {
            String pug_status = pug.GetStatusMessage();

            String msg = String.Format("Status for pug {0}: {1}", pug.Id, pug_status);
            sendMainRoomMessage(msg);
        }
    }
}
