using MessagePack;
using System;

namespace Seeker
{
    [MessagePackObject]
    [System.Serializable]
    public class Message
    {
        [Key(0)]
        public string Username;
        [Key(1)]
        public int Id;
        [Key(2)]
        public bool Replayed;
        [Key(3)]
        public DateTime LocalDateTime;
        [Key(4)]
        public DateTime UtcDateTime;
        [Key(5)]
        public string MessageText;
        [Key(6)]
        public bool FromMe = false;
        [Key(7)]
        public SentStatus SentMsgStatus = SentStatus.None;
        [Key(8)]
        public SpecialMessageCode SpecialCode = SpecialMessageCode.None;
        [Key(9)]
        public bool SameAsLastUser = false;

        public Message()
        {
        }

        public Message(string username, int id, bool replayed, DateTime localDateTime, DateTime utcDateTime, string messageText, bool fromMe)
        {
            Username = username;
            Id = id;
            Replayed = replayed;
            LocalDateTime = localDateTime;
            UtcDateTime = utcDateTime;
            MessageText = messageText;
            FromMe = fromMe;
        }

        public Message(string username, int id, bool replayed, DateTime localDateTime, DateTime utcDateTime, string messageText, bool fromMe, SentStatus sentStatus)
        {
            Username = username;
            Id = id;
            Replayed = replayed;
            LocalDateTime = localDateTime;
            UtcDateTime = utcDateTime;
            MessageText = messageText;
            FromMe = fromMe;
            SentMsgStatus = sentStatus;
        }

        public Message(DateTime localDateTime, DateTime utcDateTime, SpecialMessageCode connectOrDisconnect, string messageText)
        {
            Username = string.Empty;
            Id = -2;
            Replayed = false;
            LocalDateTime = localDateTime;
            UtcDateTime = utcDateTime;
            MessageText = messageText;
            SentMsgStatus = 0;
            SpecialCode = connectOrDisconnect;
        }
    }

    [System.Serializable]
    public enum SentStatus
    {
        None = 0,
        Pending = 1,
        Failed = 2,
        Success = 3,
    }

    [System.Serializable]
    public enum SpecialMessageCode
    {
        None = 0,
        Reconnect = 1,
        Disconnect = 2,
    }
}
