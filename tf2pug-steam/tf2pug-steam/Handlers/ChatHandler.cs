using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SteamBot.PugLib;

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

        public ChatHandler(SteamFriends steam_friends, PugManager pug_manager)
        {
            ChatHandler.steam_friends = steam_friends;
            this.pug_manager = pug_manager;
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

        void parse_chat(SteamID chat_room, SteamID sender, String message, bool admin = false)
        {
            //Program.sendMessage();
            String[] split_message = message.Split(' ');
            if (split_message.Length < 1) return;

            String cmd = split_message[0].ToLower();
            String[] args = split_message.Skip(1).Take(split_message.Length - 1).ToArray();

            print_cmd(cmd, args);
            
            if (cmd == "!test")
            {
                sendMessage(chat_room, sender, "test test test!");
            }
            else if (cmd == "!pug")
            {

            }
            else if (cmd == "!j" || cmd == "!join"
                     || cmd == "!add" || cmd == "!me")
            {
                pug_manager.AddPlayer(sender);
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
                foreach (var pug in pug_manager.Pugs)
                {
                    String players = pug_manager.GetPlayerListAsString(pug);

                    String msg = String.Format("Players in pug {0}: {1}",
                        pug.Id, players);

                    sendMainRoomMessage(msg);
                }
            }
            else if (cmd == "!forcemapvote" && admin)
            {
                pug_manager.ForceMapVote(sender);
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
            sendMessage(Program.pugChatId, null, message);
        }

        void print_cmd(String cmd, String[] args)
        {
            Console.WriteLine("cmd: {0}", cmd);
            Console.WriteLine("args: ");

            for (int i = 0; i < args.Length; i++)
            {
                Console.WriteLine("\t{0}: {1}", i, args[i]);
            }
        }
    }
}
