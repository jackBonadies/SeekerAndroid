namespace Common
{
    /// <summary>
    /// Day (light) theme variants.
    /// </summary>
    /// <remarks>
    /// These values are persisted by int (KeyConsts.M_DayVariant), so existing
    /// entries must never be renumbered or removed: a stored value of 0/1/2/3 is
    /// an explicit user choice of a legacy theme and has to keep resolving to the
    /// same style. New variants are only ever appended.
    /// </remarks>
    public enum DayThemeType : ushort
    {
        ClassicPurple = 0,
        Red = 1,
        Blue = 2,
        Grey = 3,

        /// <summary>Phase 2A design system default (light palette).</summary>
        Modern = 4,
    }

    /// <summary>
    /// Night (dark) theme variants. Persisted by int (KeyConsts.M_NightVariant),
    /// see <see cref="DayThemeType"/> for the append-only rule.
    /// </summary>
    public enum NightThemeType : ushort
    {
        ClassicPurple = 0,
        Grey = 1,
        Blue = 2,
        Red = 3,
        AmoledClassicPurple = 4,
        AmoledGrey = 5,

        /// <summary>Phase 2A design system default (dark palette).</summary>
        Modern = 6,
    }
}
