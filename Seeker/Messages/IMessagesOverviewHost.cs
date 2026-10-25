using System;

namespace Seeker.Messages
{
    /// <summary>
    /// Narrow seam between the messages overview UI and the screen hosting it.
    /// It lets the exact same fragment/adapter/action-mode run inside
    /// MainActivity's shell page and inside the original MessagesActivity
    /// without either duplicating message logic or referencing the other.
    /// </summary>
    public interface IMessagesOverviewHost
    {
        /// <summary>Open the "message a user" composer.</summary>
        void ShowNewMessageDialog();

        /// <summary>Open the conversation thread for a user.</summary>
        void OpenMessagesConversation(string username);
    }
}
