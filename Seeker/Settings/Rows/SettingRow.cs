using Android.Content;
using System;
using System.Collections.Generic;

namespace Seeker.Settings.Rows
{
    public enum SettingRowType
    {
        Header = 0,
        Toggle = 1,
        Value = 2,
        Action = 3,
        Navigation = 4,
        Empty = 5,
        Button = 6,
        ButtonPair = 7,
        SharedFolder = 8,
    }

    public abstract class SettingRow
    {
        public string Id { get; init; }
        public abstract SettingRowType Type { get; }
        public int? TitleRes { get; init; }
        public Func<Context, string> TitleProvider { get; init; }
        public int? SubtitleRes { get; init; }
        public Func<Context, string> SubtitleProvider { get; init; }
        public int? KeywordsRes { get; init; }
        public int? IconRes { get; init; }
        public string ParentId { get; init; }
        public bool DisableIfParentDisabled { get; init; } = true;
        public Func<bool> VisiblePredicate { get; init; }
        public int? MoreInfoTextRes { get; init; }
        public bool Indent { get; init; } = true;
        public Action<ISettingsHost, SettingRow> MoreInfoOnClick { get; init; }

        public string ResolveTitle(Context ctx)
        {
            if (TitleProvider != null) return TitleProvider(ctx) ?? string.Empty;
            if (TitleRes.HasValue) return ctx.GetString(TitleRes.Value);
            return string.Empty;
        }

        public string ResolveSubtitle(Context ctx)
        {
            if (SubtitleProvider != null) return SubtitleProvider(ctx);
            if (SubtitleRes.HasValue) return ctx.GetString(SubtitleRes.Value);
            return null;
        }

        public string ResolveKeywords(Context ctx)
        {
            if (!KeywordsRes.HasValue) return string.Empty;
            return ctx.GetString(KeywordsRes.Value) ?? string.Empty;
        }
    }

    public sealed class HeaderRow : SettingRow
    {
        public override SettingRowType Type => SettingRowType.Header;
    }

    public enum SettingStatusKind { None, Running, Success, Failure, HideDot }

    /// <summary>
    /// Optional inline status shown below a toggle's title: a spinner (Running) or a colored
    /// dot (Success/Failure) with status text. None hides the status line.
    /// </summary>
    public readonly struct SettingStatus
    {
        public SettingStatusKind Kind { get; init; }
        public string Text { get; init; }
    }

    public sealed class ToggleRow : SettingRow
    {
        public override SettingRowType Type => SettingRowType.Toggle;
        public Func<bool> Getter { get; init; }
        public Action<bool> Setter { get; init; }
        public IReadOnlyList<string> DependentRowIds { get; init; } = Array.Empty<string>();

        /// <summary>When non-null, an inline status line (spinner / colored dot + text) is shown
        /// below the title and refreshed whenever a subscribed listener fires.</summary>
        public Func<SettingStatus> StatusProvider { get; init; }
        /// <summary>Subscribe the given handler to whatever events should trigger a status refresh.</summary>
        public Action<EventHandler<EventArgs>> AddStatusListener { get; init; }
        /// <summary>Detach a handler previously passed to AddStatusListener</summary>
        public Action<EventHandler<EventArgs>> RemoveStatusListener { get; init; }
    }

    public sealed class ValueRow : SettingRow
    {
        public override SettingRowType Type => SettingRowType.Value;
        public Func<Context, string> ValueProvider { get; init; }
        public Action<ISettingsHost, ValueRow> OnClick { get; init; }
        /// <summary>Override the default chevron drawable. Null = use the standard right chevron.</summary>
        public int? TrailingIconRes { get; init; }

        /// <summary>When non-null, an inline status line (spinner / colored dot + text) is shown
        /// below the subtitle and refreshed whenever a subscribed listener fires.</summary>
        public Func<SettingStatus> StatusProvider { get; init; }
        public Action<EventHandler<EventArgs>> AddStatusListener { get; init; }
        public Action<EventHandler<EventArgs>> RemoveStatusListener { get; init; }
    }

    public sealed class ActionRow : SettingRow
    {
        public override SettingRowType Type => SettingRowType.Action;
        public int? ButtonTextRes { get; init; }
        public Func<Context, string> ButtonTextProvider { get; init; }
        public Action<ISettingsHost, ActionRow> OnClick { get; init; }
        public bool Destructive { get; init; }
    }

    /// <summary>
    /// Used for standout buttons (i.e. Restore defaults).
    /// </summary>
    public sealed class ButtonRow : SettingRow
    {
        public override SettingRowType Type => SettingRowType.Button;
        public Action<ISettingsHost, ButtonRow> OnClick { get; init; }
        public bool Destructive { get; init; }
        public bool Enabled { get; init; } = true;
        public bool FullWidth { get; init; } = false;
    }

    /// <summary>
    /// A row with two buttons
    /// </summary>
    public sealed class ButtonPairRow : SettingRow
    {
        public override SettingRowType Type => SettingRowType.ButtonPair;
        public int? LeftTextRes { get; init; }
        public Func<Context, string> LeftTextProvider { get; init; }
        public Action<ISettingsHost, ButtonPairRow> LeftOnClick { get; init; }
        public int? RightTextRes { get; init; }
        public Func<Context, string> RightTextProvider { get; init; }
        public Action<ISettingsHost, ButtonPairRow> RightOnClick { get; init; }
    }

    /// <summary>
    /// Upload folder rows w status name, subtitle, edit/delete actions
    /// and trailing edit (pencil) + remove (trash) actions.
    /// </summary>
    public sealed class SharedFolderRow : SettingRow
    {
        public override SettingRowType Type => SettingRowType.SharedFolder;
        public UploadDirectoryEntry Entry { get; init; }
        public Action<ISettingsHost, SharedFolderRow> OnEdit { get; init; }
        public Action<ISettingsHost, SharedFolderRow> OnRemove { get; init; }
    }

    public sealed class NavigationRow : SettingRow
    {
        public override SettingRowType Type => SettingRowType.Navigation;
        public Type ActivityType { get; init; }
        public Action<ISettingsHost, NavigationRow> OnClick { get; init; }
    }

    public sealed class EmptyStateRow : SettingRow
    {
        public override SettingRowType Type => SettingRowType.Empty;
    }
}
