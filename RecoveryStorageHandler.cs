using Newtonsoft.Json;
using Orpheus.Audio_System;
using Orpheus.JailHandling;

namespace Orpheus
{
    public static class RecoveryStorageHandler
    {
        private static RecoveryStorageJson? storageJson = new RecoveryStorageJson();
        private static isSavingToRecovery isSaving = new isSavingToRecovery()
        {
            isSavingToRecoveryTrue = false
        };
        private static string FileName = "recoveryStorage.json";

        private static void updateRecoveryStorage()
        {
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
            if (!File.Exists(FileName))
            {
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
            recoverAudioActions(recoveredStorageJson);
            lock (isSaving)
            {
                isSaving.isSavingToRecoveryTrue = false;
            }
        }

        private static void recoverVoteMessages(RecoveryStorageJson? recoveredStorageJson)
        {
            foreach (StoredVoteMessage storedVoteMessage in recoveredStorageJson.voteMessages)
            {
                if (storedVoteMessage.voteType.Equals("CourtVote"))
                {
                    _ = JailCourtHandler.RestartJailCourtMessage(storedVoteMessage);
                }
            }
        }

        private static void recoverAudioActions(RecoveryStorageJson? recoveredStorageJson)
        {
            foreach (StoredAudioAction storedAudioAction in recoveredStorageJson.audioActions)
            {
                Uri uri = new Uri(storedAudioAction.Url);
                _ = AudioHandler.PlayMusic(
                    storedAudioAction.serverID,
                    storedAudioAction.channelID,
                    uri,
                    storedAudioAction.position
                );
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

        public static void StoreAudioAction(StoredAudioAction audioAction)
        {
            //Console.WriteLine($"ADD AUDIO {audioAction.Url}");
            RemoveAudioAction(audioAction);
            storageJson.audioActions.Add(audioAction);
            updateRecoveryStorage();
        }

        public static void RemoveAudioAction(StoredAudioAction audioAction)
        {
            //Console.WriteLine($"REMOVE AUDIO {audioAction.Url}");
            for (int i = 0; i < storageJson.audioActions.Count; i++)
            {
                if (storageJson.audioActions[i].equals(audioAction))
                {
                    storageJson.audioActions.RemoveAt(i);
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

        public bool equals(StoredVoteMessage other)
        {
            return other.messageID == messageID;
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
