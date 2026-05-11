using System;
using System.Collections.Generic;

namespace Seeker.Helpers.ActionSheet
{
    public class ActionSheetConfig
    {
        public List<ActionSheetSection> Sections { get; } = new List<ActionSheetSection>();
    }

    public class ActionSheetSection
    {
        public string HeaderText;
        public List<ActionSheetRow> Rows = new List<ActionSheetRow>();
    }

    public class ActionSheetRow
    {
        public int IconResId;
        public string Label;
        public Action OnClick;
    }
}
