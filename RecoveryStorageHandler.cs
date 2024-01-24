using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using Orpheus.JailHandling;

namespace Orpheus
{
    public static class RecoveryStorageHandler
    {
        private static RecoveryStorageJson? storageJson = new RecoveryStorageJson();
        private static string FileName = "recoveryStorage.json";

        private static void updateRecoveryStorage()
        {
            string jsonString = JsonConvert.SerializeObject(storageJson);
            //Console.Write("STORE TEMPSTORAGE:");
            File.WriteAllText(FileName, jsonString);
            //Console.WriteLine(File.Exists("tempStorage.txt"));
        }

        public static void InitiateRecovery()
        {
            if (!File.Exists(FileName))
            {
                storageJson = new RecoveryStorageJson();
                return;
            }
            storageJson = JsonConvert.DeserializeObject<RecoveryStorageJson>(
                File.ReadAllText(FileName)
            );
            if (storageJson == null)
            {
                storageJson = new RecoveryStorageJson();
                return;
            }
            recoverVoteMessages();
        }

        private static void recoverVoteMessages()
        {
            foreach (StoredVoteMessage storedVoteMessage in storageJson.voteMessages)
            {
                if (storedVoteMessage.voteType.Equals("CourtVote"))
                {
                    _ = JailCourtHandler.RestartJailCourtMessage(storedVoteMessage);
                }
            }
        }

        public static void StoreVoteMessage(StoredVoteMessage msg)
        {
            storageJson.voteMessages.Add(msg);
            updateRecoveryStorage();
        }

        public static void RemoveVoteMessage(StoredVoteMessage msg)
        {
            for (int i = 0; i < storageJson.voteMessages.Count; i++)
            {
                if (storageJson.voteMessages[i].equals(msg))
                {
                    storageJson.voteMessages.RemoveAt(i);
                    updateRecoveryStorage();
                    return;
                }
            }
        }
    }

    internal sealed class RecoveryStorageJson
    {
        public List<StoredVoteMessage> voteMessages = new List<StoredVoteMessage>();
    }

    public struct StoredVoteMessage
    {
        public ulong serverID;
        public ulong messageID;
        public ulong channelID;
        public ulong userID;
        public string voteType;

        public bool equals(StoredVoteMessage other)
        {
            return other.messageID == messageID;
        }
    }
}
