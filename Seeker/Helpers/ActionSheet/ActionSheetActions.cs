using System;
using Android.Content;
using Android.Views;
using Seeker.Chatroom;
using Seeker.Managers;
using Seeker.Services;

namespace Seeker.Helpers.ActionSheet
{
    /// <summary>
    /// Builds reusable per-user action sections for the bottom-sheet menu.
    /// Standard actions route through <see cref="UiHelpers.HandleCommonContextMenuActions"/>
    /// so behavior stays identical to the legacy context menu. Refresh callbacks supplied
    /// via <see cref="UserActionsOptions"/> are forwarded to that helper so rows can update
    /// after toggles (add/remove friend, ignore/unignore, note add/edit, online alert).
    /// </summary>
    public static class ActionSheetActions
    {
        public static ActionSheetSection BuildUserActionsSection(string username, Context ctx, View snackView = null, UserActionsOptions options = null)
        {
            options = options ?? new UserActionsOptions();
            var section = new ActionSheetSection
            {
                HeaderText = username
            };

            // Private-room admin rows go first when applicable.
            if (options.RoomAdmin != null)
            {
                AppendRoomAdminRows(section, username, options.RoomAdmin);
            }

            bool added = UserListService.Instance.ContainsUser(username);
            string friendLabel = ctx.GetString(added ? Resource.String.remove_user : Resource.String.add_user);
            section.Rows.Add(new ActionSheetRow
            {
                IconResId = added ? Resource.Drawable.user_remove : Resource.Drawable.user_add,
                Label = friendLabel,
                OnClick = () =>
                {
                    if (added && options.OverrideRemoveFromFriends != null)
                    {
                        options.OverrideRemoveFromFriends();
                        return;
                    }
                    UiHelpers.HandleCommonContextMenuActions(friendLabel, username, ctx, snackView,
                        options.OnNoteChanged, options.OnAddRemoved, options.OnIgnoreChanged, options.OnOnlineAlertChanged);
                }
            });

            section.Rows.Add(MakeCommonRow(Resource.Drawable.message_user, Resource.String.msg_user, username, ctx, snackView, options));
            section.Rows.Add(MakeCommonRow(Resource.Drawable.folder_shared_browse_outline_30dp, Resource.String.browse_user, username, ctx, snackView, options));
            section.Rows.Add(MakeCommonRow(Resource.Drawable.search_users_files, Resource.String.search_user_files, username, ctx, snackView, options));
            section.Rows.Add(MakeCommonRow(Resource.Drawable.user_info, Resource.String.get_user_info, username, ctx, snackView, options));

            bool hasNote = SeekerState.UserNotes.ContainsKey(username);
            int noteStringId = hasNote ? Resource.String.edit_note : Resource.String.add_note;
            section.Rows.Add(MakeCommonRow(Resource.Drawable.user_note, noteStringId, username, ctx, snackView, options));

            if (options.IncludeOnlineAlert)
            {
                bool alertSet = SeekerState.UserOnlineAlerts.ContainsKey(username);
                int alertStringId = alertSet ? Resource.String.remove_online_alert : Resource.String.set_online_alert;
                section.Rows.Add(MakeCommonRow(Resource.Drawable.notifications_outline_30dp, alertStringId, username, ctx, snackView, options));
            }

            if (options.IncludeGivePrivileges && PrivilegesManager.Instance.GetRemainingDays() >= 1)
            {
                section.Rows.Add(MakeCommonRow(Resource.Drawable.star_wishlist, Resource.String.give_privileges, username, ctx, snackView, options));
            }

            bool ignored = SeekerApplication.IsUserInIgnoreList(username);
            string ignoreLabel = ctx.GetString(ignored ? Resource.String.remove_from_ignored : Resource.String.ignore_user);
            section.Rows.Add(new ActionSheetRow
            {
                Destructive = !ignored,
                IconResId = Resource.Drawable.account_cancel_outline,
                Label = ignoreLabel,
                OnClick = () =>
                {
                    if (ignored && options.OverrideRemoveFromIgnored != null)
                    {
                        options.OverrideRemoveFromIgnored();
                        return;
                    }
                    UiHelpers.HandleCommonContextMenuActions(ignoreLabel, username, ctx, snackView,
                        options.OnNoteChanged, options.OnAddRemoved, options.OnIgnoreChanged, options.OnOnlineAlertChanged);
                }
            });

            return section;
        }

        /// <summary>
        /// The compact section shown for an ignored user in the user list:
        /// Remove from Ignored (silent, removes the row) + Edit/Add Note.
        /// </summary>
        public static ActionSheetSection BuildIgnoredUserActionsSection(string username, Context ctx, View snackView, Action onRemovedFromIgnored, Action onNoteChanged)
        {
            var section = new ActionSheetSection
            {
                HeaderText = username
            };

            section.Rows.Add(new ActionSheetRow
            {
                IconResId = Resource.Drawable.account_cancel,
                Label = ctx.GetString(Resource.String.remove_from_ignored),
                OnClick = onRemovedFromIgnored
            });

            bool hasNote = SeekerState.UserNotes.ContainsKey(username);
            int noteStringId = hasNote ? Resource.String.edit_note : Resource.String.add_note;
            string noteLabel = ctx.GetString(noteStringId);
            section.Rows.Add(new ActionSheetRow
            {
                IconResId = Resource.Drawable.user_note,
                Label = noteLabel,
                OnClick = () => UiHelpers.HandleCommonContextMenuActions(noteLabel, username, ctx, snackView, onNoteChanged)
            });

            return section;
        }

        private static ActionSheetRow MakeCommonRow(int iconResId, int labelStringId, string username, Context ctx, View snackView, UserActionsOptions options)
        {
            string label = ctx.GetString(labelStringId);
            return new ActionSheetRow
            {
                IconResId = iconResId,
                Label = label,
                OnClick = () => UiHelpers.HandleCommonContextMenuActions(label, username, ctx, snackView,
                    options.OnNoteChanged, options.OnAddRemoved, options.OnIgnoreChanged, options.OnOnlineAlertChanged)
            };
        }

        private static void AppendRoomAdminRows(ActionSheetSection section, string username, RoomAdminContext admin)
        {
            if (admin.CanRemoveUser)
            {
                section.Rows.Add(new ActionSheetRow
                {
                    IconResId = Resource.Drawable.logout_material,
                    Label = SeekerApplication.GetString(Resource.String.remove_user),
                    OnClick = () =>
                    {
                        ChatroomController.AddRemoveUserToPrivateRoomAPI(admin.RoomName, username, true, false, true);
                    }
                });
            }
            if (admin.CanRemoveMod)
            {
                section.Rows.Add(new ActionSheetRow
                {
                    IconResId = Resource.Drawable.account_star,
                    Label = SeekerApplication.GetString(Resource.String.remove_mod_priv),
                    OnClick = () =>
                    {
                        ChatroomController.AddRemoveUserToPrivateRoomAPI(admin.RoomName, username, true, true, true);
                        if (admin.OnAdminChanged != null)
                        {
                            SeekerState.ActiveActivityRef.RunOnUiThread(admin.OnAdminChanged);
                        }
                    }
                });
            }
            if (admin.CanAddMod)
            {
                section.Rows.Add(new ActionSheetRow
                {
                    IconResId = Resource.Drawable.account_star,
                    Label = SeekerApplication.GetString(Resource.String.add_mod_priv),
                    OnClick = () =>
                    {
                        ChatroomController.AddRemoveUserToPrivateRoomAPI(admin.RoomName, username, true, true, false);
                        if (admin.OnAdminChanged != null)
                        {
                            SeekerState.ActiveActivityRef.RunOnUiThread(admin.OnAdminChanged);
                        }
                    }
                });
            }
        }
    }

    public class UserActionsOptions
    {
        public bool IncludeOnlineAlert;
        public bool IncludeGivePrivileges;
        public Action OnAddRemoved;
        public Action OnIgnoreChanged;
        public Action OnNoteChanged;
        public Action OnOnlineAlertChanged;
        public Action OverrideRemoveFromFriends;
        public Action OverrideRemoveFromIgnored;
        public RoomAdminContext RoomAdmin;
    }

    public class RoomAdminContext
    {
        public string RoomName;
        public bool CanRemoveUser;
        public bool CanAddMod;
        public bool CanRemoveMod;
        public Action OnAdminChanged;
    }
}
