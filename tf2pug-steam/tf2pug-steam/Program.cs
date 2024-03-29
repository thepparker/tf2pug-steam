﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;

using SteamKit2;

using SteamBot.PugLib;
using SteamBot.Handlers;
using SteamBot.ClanLib;

namespace SteamBot
{
    class Program
    {
        /** Declare our static vars for client connection management */
        static SteamClient client;
        static CallbackManager cb_manager;

        static SteamUser user;
        static SteamFriends steam_friends;

        static bool running;

        static string username, password;

        /** SteamGuard shiz */
        static string auth_code;
        static string sentry_filename = "default_sentry.bin";

        /** Pug management interfaces */
        static PugManager pug_manager;

        /** Chat parser */
        static ChatHandler cparser;

        /** SteamID object for the pug group, needed to send messages to group chat */
        public static SteamID pugClanId = new SteamID(103582791434957782);

        /** A management object for clans, their members and their chat rooms */
        static ClanManager clan_manager;

        /** 
         * Entry point. Establish client and user, setup callbacks, and 
         * run the bot
         * 
         * @param string[] args Command line arguments
         */
        static void Main(string[] args)
        {
            // if we don't have 2 args, print error and usage message
            if (args.Length < 2)
            {
                Console.WriteLine("ERROR: No username or password specified");
                print_usage();

                return;
            }

            Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e)
            {
                Console.WriteLine("KeyboardInterrupt, cleaning up");
                e.Cancel = false;
                cleanup();

                running = false;

                Console.WriteLine("Finished cleanup");
            };

            // get uname/pass
            username = args[0];
            password = args[1];

            // establish client and user
            client = new SteamClient();

            user = client.GetHandler<SteamUser>();
            steam_friends = client.GetHandler<SteamFriends>();

            // establish callback manager
            cb_manager = new CallbackManager(client);
            
            // register callbacks with the manager
            new Callback<SteamClient.ConnectedCallback>(onConnected, cb_manager);
            new Callback<SteamClient.DisconnectedCallback>(onDisconnected, cb_manager);
            
            new Callback<SteamUser.LoggedOnCallback>(onUserLoggedOn, cb_manager);
            new Callback<SteamUser.LoggedOffCallback>(onUserLoggedOff, cb_manager);

            // friends list callbacks
            new Callback<SteamUser.AccountInfoCallback>(onAccountInfo, cb_manager);

            new Callback<SteamFriends.FriendsListCallback>(onFriendsList, cb_manager);
            new Callback<SteamFriends.PersonaStateCallback>(onPersonaState, cb_manager);
            new Callback<SteamFriends.FriendAddedCallback>(onFriendAdded, cb_manager);

            // chat callbacks
            new Callback<SteamFriends.FriendMsgCallback>(onFriendChatMessage, cb_manager);
            new Callback<SteamFriends.ChatMsgCallback>(onChatRoomMessage, cb_manager);
            new Callback<SteamFriends.ChatMemberInfoCallback>(onChatMemberInfo, cb_manager);
            new Callback<SteamFriends.ChatEnterCallback>(onChatEntered, cb_manager);
            new Callback<SteamFriends.ChatActionResultCallback>(onChatAction, cb_manager);
            
            // clan callback
            new Callback<SteamFriends.ClanStateCallback>(onClanState, cb_manager);

            // this is a job callback, and triggers when steam wants us to do
            // something. in this case, it is to store the steamguard sentry
            new JobCallback<SteamUser.UpdateMachineAuthCallback>(onSteamGuardAuth, cb_manager);

            // pug manager
            pug_manager = new PugManager(steam_friends);

            // get the clan manager
            clan_manager = new ClanManager();

            // establish chat parser
            cparser = new ChatHandler(steam_friends, pug_manager, clan_manager);

            // now enter the main loop and connect
            Console.WriteLine("Connecting to steam...");

            running = true;
            client.Connect();

            while (running)
            {
                // wait 1 second for new callbacks. loop indefinitely
                cb_manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));

                // check map vote timers
                pug_manager.CheckMapVotes();
            }

            cleanup();
        }

        static void onConnected(SteamClient.ConnectedCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                Console.WriteLine("Unable to connect to steam: {0}", callback.Result);

                running = false;
                return;
            }

            Console.WriteLine("Connected. Attempting to login with user '{0}'", username);

            // for steamguard auth
            byte[] sentryhash = null;
            // if we already have a sentry file, we use the existing hash
            // so that steam knows its us
            if (File.Exists(sentry_filename))
            {
                // we have a stored and saved sentry file, so read it and 
                // sha-1 hash it so we can send this hash in logon
                byte[] sentryfile = File.ReadAllBytes(sentry_filename);
                sentryhash = CryptoHelper.SHAHash(sentryfile);
            }

            user.LogOn(
                new SteamUser.LogOnDetails
                {
                    Username = username,
                    Password = password,

                    // steamguard auth code, null on first attempt
                    AuthCode = auth_code,

                    // and we use the sentry hash for proof, null on first attempt
                    SentryFileHash = sentryhash
                }
            );            
        }

        static void onDisconnected(SteamClient.DisconnectedCallback callback)
        {
            // if we get an AccountLogonDenied message, we'll be disconnected
            // from steam. we then need to read an authcode from the console
            // and attempt to reconnect

            Console.WriteLine("Disconnected from Steam. Reconnecting in 5 seconds...");

            // sleep!
            Thread.Sleep(TimeSpan.FromSeconds(5));

            // and re-initiate the logon process
            client.Connect();
            Console.WriteLine("Attempting to reconnect...");
        }

        static void onUserLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result == EResult.AccountLogonDenied)
            {
                Console.WriteLine("ERROR: This machine is not authenticated with SteamGuard");
                Console.Write("Please enter the code sent to the email at {0}: ", callback.EmailDomain);

                auth_code = Console.ReadLine();
            }
            else if (callback.Result != EResult.OK)
            {
                Console.WriteLine("Unable to logon to Steam: {0} / {1}", callback.Result, callback.ExtendedResult);

                // end the infinite loop
                running = false;
            }
            else
            {
                // else, we're successfully logged in and we can do shit on steam
                Console.WriteLine("Successfully logged in!");
            }
        }

        static void onUserLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            Console.WriteLine("Logged off Steam: {0}", callback.Result);
        }

        static void onSteamGuardAuth(SteamUser.UpdateMachineAuthCallback callback, JobID jobid)
        {
            // we need to update the sentry file
            Console.WriteLine("Updating sentry file...");

            byte[] sentryhash = CryptoHelper.SHAHash(callback.Data);

            // write to the given filename
            sentry_filename = callback.FileName;
            Console.WriteLine("Writing sentry to {0}", sentry_filename);
            File.WriteAllBytes(sentry_filename, callback.Data);

            // let steam know we're accepting the sentry data
            user.SendMachineAuthResponse(
                new SteamUser.MachineAuthDetails
                {
                    JobID = jobid,
                    FileName = callback.FileName,

                    BytesWritten = callback.BytesToWrite,
                    FileSize = callback.Data.Length,
                    Offset = callback.Offset,
                    
                    Result = EResult.OK,
                    LastError = 0,

                    OneTimePassword = callback.OneTimePassword,

                    SentryFileHash = sentryhash
                }
            );

            Console.WriteLine("Sentry file updated, and machine auth response sent");
        }

        static void onAccountInfo(SteamUser.AccountInfoCallback callback)
        {
            // we need to wait for this callback before we can interact with
            // friends. once we hit this, we can set our persona state to
            // online and begin to perform friends actions. this is called
            // shortly after a successful logon

            steam_friends.SetPersonaState(EPersonaState.Online);
            steam_friends.SetPersonaName("iPGN PUG");

            Console.WriteLine("Account info received. Clan count: {0}, friend count: {1}",
                steam_friends.GetClanCount(), steam_friends.GetFriendCount());

            // join the pug channel
            steam_friends.JoinChat(pugClanId);
        }

        static void onFriendsList(SteamFriends.FriendsListCallback callback)
        {
            // this is called when we receive our friends list. the friends
            // list is sent upon connect, and whenever a change in the list
            // occurs. i.e if someone deletes us, the list is sent. likewise,
            // if someone adds us, the list is sent again.

            int num_friends = steam_friends.GetFriendCount();

            Console.WriteLine("Friends list received. Number of friends: {0}", num_friends);

            // iterate over the new list and accept requests that we may have
            // just received
            foreach (var friend in callback.FriendList)
            {
                // if we are a recipient of their request, add them
                if (friend.Relationship == EFriendRelationship.RequestRecipient)
                    steam_friends.AddFriend(friend.SteamID);
            }

            Console.WriteLine("Friends:");

            for (int i = 0; i < num_friends; i++)
            {
                SteamID friendId = steam_friends.GetFriendByIndex(i);

                Console.WriteLine("\tidx {0}: {1} ({2}) - {3}", i, friendId,
                    steam_friends.GetFriendPersonaName(friendId), friendId.AccountType);
            }
        }

        static void onFriendAdded(SteamFriends.FriendAddedCallback callback)
        {
            // this triggers for a variety of events.
            // 1. when someone adds us
            // 2. when we add someone
            // 3. when someone invites us to something
            // and probably more i haven't discovered yet

            if (callback.SteamID.IsClanAccount)
            {
                // someone invited us to a clan. what do?
                Console.WriteLine("Invited to a clan - {0} ({1})",
                        steam_friends.GetClanName(callback.SteamID),
                        callback.SteamID
                    );

                // we just ignore clan invites
                steam_friends.IgnoreFriend(callback.SteamID);
            }
            else if (callback.SteamID.AccountType == EAccountType.Individual)
                Console.WriteLine("{0} ({1}) is now a friend", callback.PersonaName, callback.SteamID.Render());
            else
                Console.Write("Unknown onFriendAdded event. {0}: {1}", callback.SteamID, callback.Result);
        }

        static void onPersonaState(SteamFriends.PersonaStateCallback callback)
        {
            // called when a friend's persona state changes, ie someone goes
            // from offline to online

            if (callback.FriendID.IsClanAccount)
            {
                Console.WriteLine("Clan info - name: {0}, id: {1}, rank: {2}",
                        callback.Name, callback.FriendID, callback.ClanRank
                    );

                clan_manager.AddClan(callback.FriendID);
            }
            else if (callback.SourceSteamID.IsClanAccount)
            {
                Console.WriteLine("CLAN MEMBERSHIP INFO - {0} ({1}) is rank {2} in clan {3}",
                        callback.Name, callback.FriendID, (EClanRank)callback.ClanRank, 
                        callback.SourceSteamID
                    );

                EClanRank rank = (EClanRank)callback.ClanRank;

                //add member to this clan
                clan_manager.AddClanMember(callback.SourceSteamID, callback.FriendID, rank);
            }
            else if (callback.SourceSteamID.IsChatAccount)
            {
                Console.WriteLine("GROUP CHAT INFO - user: {0} ({1}), chat: {2}, rank: {3}, tag: {4}, stateflag: {5}, statusflag: {6}",
                        callback.Name, callback.FriendID, callback.SourceSteamID, (EClanRank)callback.ClanRank,
                        callback.ClanTag, callback.StateFlags, callback.StatusFlags
                    );

                clan_manager.AddClanChatMember(callback.SourceSteamID, callback.FriendID);
            }
            else
            {
                Console.WriteLine("State change: {0} ({1}) changed to {2}. Type: {3}, rank: {4}, source: {5}",
                        callback.Name, callback.FriendID, callback.State,
                        callback.FriendID.AccountType, callback.ClanRank, callback.SourceSteamID
                    );
            }
        }

        static void onClanState(SteamFriends.ClanStateCallback callback)
        {
            return;

            Console.WriteLine("Clan state change: {0} ({1})",
                callback.ClanName, callback.ClanID);

            Console.WriteLine("\tMembers: {0}", callback.MemberTotalCount);
            Console.WriteLine("\tOnline: {0}, in-game: {1}, chatting: {2}",
                callback.MemberOnlineCount, callback.MemberInGameCount,
                callback.MemberChattingCount);
        }

        static void onFriendChatMessage(SteamFriends.FriendMsgCallback callback)
        {
            // called when a friend message is received
            // ignore "user is typing..." shit
            if (callback.EntryType == EChatEntryType.Typing)
                return;

            Console.WriteLine("@PRIVCHAT <- {0} ({1}): {2}",
                steam_friends.GetFriendPersonaName(callback.Sender),
                callback.Sender.Render(), callback.Message);

            cparser.parse(callback);
        }

        static void onChatRoomMessage(SteamFriends.ChatMsgCallback callback)
        {
            // called when a chat room message is received
            if (callback.ChatMsgType == EChatEntryType.Typing)
                return;
            
            Console.WriteLine("@PUBCHAT <- {0} ({1}): {2}",
                callback.ChatRoomID.Render(),
                steam_friends.GetFriendPersonaName(callback.ChatterID),
                callback.Message);

            cparser.parse(callback);
        }

        static void onChatMemberInfo(SteamFriends.ChatMemberInfoCallback callback)
        {
            if (callback.Type == EChatInfoType.StateChange)
            {
                Console.WriteLine("CHAT STATE CHANGE - id: {0}, state: {1}, acted on: {2}, acted by: {3}",
                        callback.ChatRoomID, callback.StateChangeInfo.StateChange,
                        callback.StateChangeInfo.ChatterActedOn, callback.StateChangeInfo.ChatterActedBy);
            }
            else if (callback.Type == EChatInfoType.InfoUpdate)
            {
                Console.WriteLine("CHAT INFO UPDATE - id: {0}",
                        callback.ChatRoomID
                    );
            }
            else
            {
                Console.WriteLine("CHAT MEMBER INFO - id: {0}, state: {1}, type: {2}, accounttype: {3}, acted on: {4}",
                        callback.ChatRoomID, callback.StateChangeInfo, callback.Type,
                        callback.ChatRoomID.AccountType, callback.StateChangeInfo
                    );
            }
        }

        static void onChatEntered(SteamFriends.ChatEnterCallback callback)
        {
            Console.WriteLine("Entered chat {0} - {1}. Response: {2}, clan: {3}, owner: {4}, friend: {5}, flags: {6}",
                    callback.ChatID, callback.ChatRoomType, callback.EnterResponse,
                    callback.ClanID, callback.OwnerID, callback.FriendID,
                    callback.ChatFlags
                );
            
        }

        static void onChatAction(SteamFriends.ChatActionResultCallback callback)
        {
            Console.WriteLine("CHAT ACTION - action: {0}, room: {1}, chatter: {2}, result: {3}",
                    callback.Action, callback.ChatRoomID, callback.ChatterID, callback.Result);
        }

        /** Print simple usage message */
        static void print_usage()
        {
            Console.WriteLine("Usage: tf2pug-steam [user] [pass]");
        }

        static void cleanup()
        {
            Console.WriteLine("Beginning clean up...");

            if (File.Exists(sentry_filename) && sentry_filename != "default_sentry.bin")
            {
                Console.WriteLine("Moving most recent sentry file to default_sentry.bin");
                File.Move(sentry_filename, "default_sentry.bin");
            }

            client.Disconnect();
        }
    }
}
