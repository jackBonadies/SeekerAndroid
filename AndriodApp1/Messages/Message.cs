using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AndriodApp1
{
    [System.Serializable] 
    public class Message
    {
        public string Username;
        public int Id;
        public bool Replayed;
        public DateTime LocalDateTime;
        public DateTime UtcDateTime;
        public string MessageText;
        public bool FromMe = false;
        public SentStatus SentMsgStatus = SentStatus.None;
        public SpecialMessageCode SpecialCode = SpecialMessageCode.None;
        public bool SameAsLastUser = false;
        public Message()
        {
            //i think this is necessary for serialization...
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

        public Message(DateTime localDateTime, DateTime utcDateTime, SpecialMessageCode connectOrDisconnect)
        {
            Username = string.Empty;
            Id = -2;
            Replayed = false;
            LocalDateTime = localDateTime;
            UtcDateTime = utcDateTime;
            SetConnectDisconnectText(localDateTime, connectOrDisconnect);
            SentMsgStatus = 0;
            SpecialCode = connectOrDisconnect;
        }

        private void SetConnectDisconnectText(DateTime localDateTime, SpecialMessageCode connectOrDisconnect)
        {
            if (connectOrDisconnect == SpecialMessageCode.Disconnect)
            {
                MessageText = string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.chatroom_disconnected_at), CommonHelpers.GetNiceDateTime(localDateTime));
            }
            else
            {
                MessageText = string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.chatroom_reconnected_at), CommonHelpers.GetNiceDateTime(localDateTime));
            }
        }
    }

}