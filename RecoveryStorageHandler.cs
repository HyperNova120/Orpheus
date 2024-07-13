using Newtonsoft.Json;
using Orpheus.JailHandling;

namespace Orpheus
{
    public static class RecoveryStorageHandler
    {
        private static RecoveryStorageJson storageJson = new RecoveryStorageJson();
        private static isSavingToRecovery isSaving = new isSavingToRecovery()
        {
            isSavingToRecoveryTrue = false
        };
        private static string FileName = "recoveryStorage.json";

        private static void updateRecoveryStorage()
        {
            if (!File.Exists(FileName))
            {
                File.CreateText(FileName);
            }
            while (true)
            {
                lock (isSaving)
                {
                    if (!isSaving.isSavingToRecoveryTrue)
                    {
                        isSaving.isSavingToRecoveryTrue = true;
                        break;
                    }
                }
            }
            string jsonString = JsonConvert.SerializeObject(storageJson);
            //Console.Write("STORE TEMPSTORAGE:");
            File.WriteAllText(FileName, jsonString);
            //Console.WriteLine(File.Exists("tempStorage.txt"));
            lock (isSaving)
            {
                if (isSaving.isSavingToRecoveryTrue)
                {
                    isSaving.isSavingToRecoveryTrue = false;
                }
            }
        }

        public static void InitiateRecovery()
        {
                Console.WriteLine("INITIATE RECOVERY");
            if (!File.Exists(FileName))
            {
                File.CreateText(FileName);
                Console.WriteLine("RECOVERY STOP, NO FILE");
                storageJson = new RecoveryStorageJson();
                return;
            }
            while (true)
            {
                lock (isSaving)
                {
                    if (!isSaving.isSavingToRecoveryTrue)
                    {
                        isSaving.isSavingToRecoveryTrue = true;
                        break;
                    }
                }
            }
            RecoveryStorageJson? recoveredStorageJson =
                JsonConvert.DeserializeObject<RecoveryStorageJson>(File.ReadAllText(FileName));
            storageJson = new RecoveryStorageJson();
            recoverVoteMessages(recoveredStorageJson);
            lock (isSaving)
            {
                isSaving.isSavingToRecoveryTrue = false;
            }
        }

        private static void recoverVoteMessages(RecoveryStorageJson? recoveredStorageJson)
        {
            if (recoveredStorageJson == null)
            {
                Console.WriteLine("MSG RECOVERY FAIL: NO MSG TO RECOVER");
                return;
            }
            foreach (StoredVoteMessage storedVoteMessage in recoveredStorageJson.voteMessages)
            {
                if (storedVoteMessage.voteType.Equals("CourtVote"))
                {
                    Console.WriteLine("MSG RECOVERY START:\n\t"+storedVoteMessage.toString());
                    _ = JailCourtHandler.RestartJailCourtMessage(storedVoteMessage);
                }
            }
        }
        public static void StoreVoteMessage(StoredVoteMessage msg)
        {
            storageJson.voteMessages.Add(msg);
            updateRecoveryStorage();
        }

        public static void UpdateVoteMessage(StoredVoteMessage msg)
        {
            RemoveVoteMessage(msg);
            StoreVoteMessage(msg);
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

    internal sealed class isSavingToRecovery
    {
        public bool isSavingToRecoveryTrue { get; set; }
    }

    internal sealed class RecoveryStorageJson
    {
        public List<StoredVoteMessage> voteMessages = new List<StoredVoteMessage>();
        public List<StoredAudioAction> audioActions = new List<StoredAudioAction>();
    }

    public struct StoredVoteMessage
    {
        public ulong serverID;
        public ulong messageID;
        public ulong channelID;
        public ulong userID;
        public string voteType;
        public long storedCountdownTimerSeconds;

        public bool equals(StoredVoteMessage other)
        {
            return other.messageID == messageID;
        }

        public string toString()
        {
            return $"serverID:{serverID}, messageID:{messageID}, channelID:{channelID}, userID:{userID}, voteType:{voteType}, remianingSeconds:{storedCountdownTimerSeconds}";
        }
    }

    public struct StoredAudioAction
    {
        public ulong serverID;
        public ulong channelID;
        public string Url;
        public TimeSpan position;

        public bool equals(StoredAudioAction other)
        {
            if (other.Url == null || other.Url.Length == 0)
            {
                return other.serverID == serverID;
            }
            return other.serverID == serverID && other.Url == Url;
        }
    }
}
