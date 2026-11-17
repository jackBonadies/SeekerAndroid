using Seeker.Helpers.ActionSheet;
using System;
using System.Collections.Generic;

namespace Seeker.Helpers.AnchoredMenu
{
    public enum AnchoredMenuRowKind
    {
        Plain,
        Checkable,
        Submenu,
        Divider,
    }

    public class AnchoredMenuConfig
    {
        public List<AnchoredMenuRow> Rows { get; } = new List<AnchoredMenuRow>();

        public bool HasRows()
        {
            foreach (var row in Rows)
            {
                if (row.Kind != AnchoredMenuRowKind.Divider)
                {
                    return true;
                }
            }
            return false;
        }

        public AnchoredMenuConfig AddRow(int iconResId, string label, Action onClick, bool destructive = false)
        {
            Rows.Add(new AnchoredMenuRow
            {
                IconResId = iconResId,
                Label = label,
                OnClick = onClick,
                Destructive = destructive,
            });
            return this;
        }

        public AnchoredMenuConfig AddDivider()
        {
            Rows.Add(new AnchoredMenuRow { Kind = AnchoredMenuRowKind.Divider });
            return this;
        }

        public AnchoredMenuConfig AddRows(ActionSheetSection section)
        {
            foreach (var row in section.Rows)
            {
                Rows.Add(new AnchoredMenuRow
                {
                    IconResId = row.IconResId,
                    Label = row.Label,
                    Destructive = row.Destructive,
                    OnClick = row.OnClick,
                });
            }
            return this;
        }

        public static AnchoredMenuConfig FromSection(ActionSheetSection section)
        {
            return new AnchoredMenuConfig().AddRows(section);
        }

        // leading, trailing and doubled dividers left behind by conditional rows
        internal List<AnchoredMenuRow> GetNormalizedRows()
        {
            var result = new List<AnchoredMenuRow>(Rows.Count);
            foreach (var row in Rows)
            {
                if (row.Kind == AnchoredMenuRowKind.Divider)
                {
                    if (result.Count == 0 || result[result.Count - 1].Kind == AnchoredMenuRowKind.Divider)
                    {
                        continue;
                    }
                }
                result.Add(row);
            }
            while (result.Count > 0 && result[result.Count - 1].Kind == AnchoredMenuRowKind.Divider)
            {
                result.RemoveAt(result.Count - 1);
            }
            return result;
        }
    }

    public class AnchoredMenuRow
    {
        public AnchoredMenuRowKind Kind = AnchoredMenuRowKind.Plain;
        public int IconResId;
        public string Label;
        public bool Destructive;
        public Action OnClick;
        public Func<bool> GetChecked;
        public Action<bool> OnChecked;
        public AnchoredMenuConfig SubMenu;
        public string SubMenuTitle;
    }
}
