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
    public static class TempStorageHandler
    {
        private static TempStorageJson storageJson = new TempStorageJson();

        public static void StoreVoteMessage(StoredVoteMessage msg)
        {
            storageJson.voteMessages.Add(msg);
            updateTempStorage();
        }

        public static void RemoveVoteMessage(StoredVoteMessage msg)
        {
            for (int i = 0; i < storageJson.voteMessages.Count; i++)
            {
                if (storageJson.voteMessages[i].equals(msg))
                {
                    storageJson.voteMessages.RemoveAt(i);
                    updateTempStorage();
                    return;
                }
            }
        }

        private static void updateTempStorage()
        {
            string jsonString = JsonConvert.SerializeObject(storageJson);
            //Console.Write("STORE TEMPSTORAGE:");
            File.WriteAllText("tempStorage.txt", jsonString);
            //Console.WriteLine(File.Exists("tempStorage.txt"));
        }

        public static void RestartFromTempStorage()
        {
            storageJson = JsonConvert.DeserializeObject<TempStorageJson>(
                File.ReadAllText("tempStorage.txt")
            );
            if (storageJson == null)
            {
                storageJson = new TempStorageJson();
                return;
            }
            foreach (StoredVoteMessage storedVoteMessage in storageJson.voteMessages)
            {
                if (storedVoteMessage.voteType.Equals("CourtVote"))
                {
                    _ = JailCourtHandler.RestartJailCourtMessage(storedVoteMessage);
                }
            }
        }
    }

    internal sealed class TempStorageJson
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
