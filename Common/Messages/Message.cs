using System;

namespace Seeker
{
    [System.Serializable]
    public class Message
    {
        public readonly string Username;
        public readonly int Id;
        public readonly bool Replayed;
        public readonly DateTime UtcDateTime;
        public readonly string MessageText;
        public readonly bool FromMe = false;
        public SentStatus SentMsgStatus = SentStatus.None;
        public readonly SpecialMessageCode SpecialCode = SpecialMessageCode.None;
        public bool SameAsLastUser = false;

        private DateTime _localDateTime;
        private bool _localComputed;

        public DateTime LocalDateTime
        {
            get
            {
                if (!_localComputed)
                {
                    var utc = DateTime.SpecifyKind(UtcDateTime, DateTimeKind.Utc);
                    try
                    {
                        _localDateTime = utc.ToLocalTime();
                    }
                    catch
                    {
                        _localDateTime = utc;
                    }
                    _localComputed = true;
                }
                return _localDateTime;
            }
            set
            {
                _localDateTime = value;
                _localComputed = true;
            }
        }

        public Message(string username, int id, bool replayed, DateTime localDateTime, DateTime utcDateTime, string messageText, bool fromMe)
            : this(username, id, replayed, utcDateTime, messageText, fromMe, SentStatus.None, SpecialMessageCode.None, false)
        {
            LocalDateTime = localDateTime;
        }

        public Message(string username, int id, bool replayed, DateTime localDateTime, DateTime utcDateTime, string messageText, bool fromMe, SentStatus sentStatus)
            : this(username, id, replayed, utcDateTime, messageText, fromMe, sentStatus, SpecialMessageCode.None, false)
        {
            LocalDateTime = localDateTime;
        }

        public Message(DateTime localDateTime, DateTime utcDateTime, SpecialMessageCode connectOrDisconnect, string messageText)
            : this(string.Empty, -2, false, utcDateTime, messageText, false, SentStatus.None, connectOrDisconnect, false)
        {
            LocalDateTime = localDateTime;
        }

        public Message(
            string username,
            int id,
            bool replayed,
            DateTime utcDateTime,
            string messageText,
            bool fromMe,
            SentStatus sentMsgStatus,
            SpecialMessageCode specialCode,
            bool sameAsLastUser)
        {
            Username = username;
            Id = id;
            Replayed = replayed;
            UtcDateTime = utcDateTime;
            MessageText = messageText;
            FromMe = fromMe;
            SentMsgStatus = sentMsgStatus;
            SpecialCode = specialCode;
            SameAsLastUser = sameAsLastUser;
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
