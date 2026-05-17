using System;
using System.Collections.Generic;

namespace Seeker.Helpers.AnchoredMenu
{
    public enum AnchoredMenuRowKind
    {
        Plain,
        Checkable,
        Submenu,
    }

    public class AnchoredMenuConfig
    {
        public List<AnchoredMenuRow> Rows { get; } = new List<AnchoredMenuRow>();
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
