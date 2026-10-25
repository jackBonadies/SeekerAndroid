#!/usr/bin/env python3
"""Static UI contract checks for the Seeker design system (Phase 2A-3).

These are *static* checks only - no Android build, no device, no UI test. They
guard the things a source-only change can silently break:

  * every XML resource still parses,
  * the semantic token layer is complete for both the day and the night theme,
  * the themes still know every custom attribute (an unresolved ``?attr/`` is an
    inflate-time crash, not a warning),
  * the persisted theme preference keys/values stay compatible and still default
    to "follow system",
  * no duplicate style/resource definitions (aapt2 merges them silently, which is
    how the previous duplicated AppTheme/MyActionMode definitions drifted apart),
  * the shared overlays / bottom navigation / reusable selectors are token driven
    (no forced light-or-dark literals in the shared chrome),
  * theme switching never touches the client, the services or the transfers,
  * every ``Resource.*`` id used from C# resolves to a real app resource,
  * no concrete theme contains a locally defined ``?attr/`` / ``?android:attr/``
    reference cycle (including the legacy saved light variants),
  * every legacy theme still resolves its window, primary, surface, text and
    chrome tokens, not just the modern themes.

Requires PyYAML (>= 6) for the Maestro command-schema check. A missing PyYAML is
reported as a failure (with the install command), never silently skipped.

Usage
    python3 Misc/test_ui_contracts.py [-v]
Exit code 0 = all checks passed.
"""

from __future__ import annotations

import os
import json
import re
import sys
import xml.etree.ElementTree as ET

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RES = os.path.join(REPO, "Seeker", "Resources")

# ---------------------------------------------------------------------------
# Documented baseline exclusions.
#
# Every entry here is a known, pre-existing exception that this phase does not
# change. An empty list is the goal: anything added must come with a reason.
# ---------------------------------------------------------------------------
BASELINE_EXCLUSIONS = {
    # Styles that exist in both values/ and values-night/ on purpose would be
    # listed here. Phase 2A removed all of them (values-night/styles.xml now
    # contains only Seeker.Theme.Base), so the list is empty.
    # Seeker.Theme.Base is the single config dependent Material3 parent
    # (Theme.Material3.Light.NoActionBar in values/, Theme.Material3.DayNight.
    # NoActionBar in values-night/). Sharing items through it is what allows both
    # configurations to use one AppTheme definition, so the duplicate is the point.
    "cross_config_style_duplicates": ["Seeker.Theme.Base"],
    # Attributes referenced from C# that are declared by AndroidX/Material, not
    # by this app (so they must not be in values/attrs.xml).
    "library_attrs_used_from_code": [
        "selectableItemBackground",
        "selectableItemBackgroundBorderless",
        "colorPrimary",
        "colorOnSurface",
    ],
}

# Custom attribute names referenced from C# via "Android.Resource.Attribute.*"
# are framework attrs and are skipped by construction.

REQUIRED_DAY_THEME = "DefaultLight"
REQUIRED_NIGHT_THEME = "DefaultDark"
MODERN_DAY_THEME = "ModernLight"
MODERN_NIGHT_THEME = "ModernDark"

# Requested dark palette (Phase 2A brief).
DARK_PALETTE = {
    "dsColorBackground": "#101318",
    "dsColorSurface": "#1B222C",
    "dsColorSurfaceElevated": "#26313D",
    "dsColorPrimary": "#5EE0C0",
    "dsColorTextPrimary": "#F0F3F6",
    "dsColorTextSecondary": "#A8B3BF",
    "dsColorOutline": "#34404D",
}

# Contrast pairs that must be readable in both modern themes (WCAG AA, 4.5:1).
CONTRAST_PAIRS = [
    ("dsColorTextPrimary", "dsColorBackground"),
    ("dsColorTextPrimary", "dsColorSurface"),
    ("dsColorTextSecondary", "dsColorBackground"),
    ("dsColorOnPrimary", "dsColorPrimary"),
]
MIN_CONTRAST = 4.5

# WCAG 1.4.11 non-text contrast: a control boundary / indicator needs 3:1 against
# the surface it sits on. Disabled text is exempt from 4.5:1 by WCAG, but it must
# still be perceivable (not painted the same colour as its surface).
MIN_CONTROL_CONTRAST = 3.0
MIN_DISABLED_CONTRAST = 3.0

# Attrs that are consumed by controls/indicators, i.e. they are an *accent*, never
# chrome. Only these may be named by a control tint attribute in a layout.
CONTROL_LAYOUT_ATTRS = (
    "indicatorColor",
    "trackColor",
    "progressTint",
    "indeterminateTint",
    "buttonTint",
)

# The legacy "mainPurple" family is chrome: an app-bar / status-bar / navigation
# background. ModernLight paints it #FFFFFF and ModernDark #1B222C, so using one
# of these as a foreground, control tint or progress colour paints chrome-on-chrome
# (an invisible spinner / white-on-white selected filter).
CHROME_ATTRS = (
    "mainPurple",
    "mainPurpleForNavBar",
    "mainPurpleStatusBar",
    "whitePurpleForNavBar",
)

# Colour pairs that are actually rendered by the design system, as
# (foreground, background, minimum ratio, label). Every entry is resolved through
# the theme (``?attr/`` and ``@color/`` chains), so a token that silently becomes a
# surface colour fails here instead of only looking wrong on a device.
RESOLVED_CONTRAST_PAIRS = [
    # Material primary / on-primary (buttons, selected filters, FAB).
    ("dsColorOnPrimary", "dsColorPrimary", MIN_CONTRAST, "primary/on-primary"),
    # Body text on both container levels.
    ("dsColorTextPrimary", "dsColorSurface", MIN_CONTRAST, "text/surface"),
    ("dsColorTextPrimary", "dsColorBackground", MIN_CONTRAST, "text/background"),
    ("dsColorTextSecondary", "dsColorSurface", MIN_CONTRAST, "text-secondary/surface"),
    # Disabled label still has to be perceivable.
    ("dsColorTextDisabled", "dsColorSurface", MIN_DISABLED_CONTRAST, "disabled/surface"),
    # Search filter segmented control: selected = primary + on-primary,
    # unselected = surface + text-primary (the pair SearchFragment builds).
    ("dsColorOnPrimary", "dsColorPrimary", MIN_CONTRAST, "filter selected label/background"),
    ("dsColorTextPrimary", "dsColorSurface", MIN_CONTRAST, "filter unselected label/background"),
    # Chrome foreground on chrome surface (app bar, bottom navigation, sheet header).
    ("dsColorChromeForeground", "dsColorChromeSurface", MIN_CONTRAST, "chrome foreground/surface"),
    ("dsColorActionModeForeground", "dsColorActionModeBackground", MIN_CONTRAST, "action mode foreground/background"),
    ("dsColorNavigationForeground", "dsColorNavigationBackground", MIN_CONTROL_CONTRAST, "navigation selected item"),
    # Every progress / accent indicator must be visible on both containers.
    ("dsColorPrimary", "dsColorBackground", MIN_CONTROL_CONTRAST, "primary indicator/background"),
    ("dsColorPrimary", "dsColorSurface", MIN_CONTROL_CONTRAST, "primary indicator/surface"),
    ("dsColorError", "dsColorSurface", MIN_CONTRAST, "error/surface"),
]

# Accent-ish legacy attributes: they are painted *on* a surface, so in every
# modern theme they must not resolve to that surface colour.
ACCENT_ATTRS_ON_SURFACE = (
    "dsColorPrimary",
    "colorPrimary",
    "colorAccent",
    "colorControlActivated",
    "slskAccent",
    "searchTabButtonColor",
    "mainTextColor",
    "checkBoxColor",
    "swipeToRefreshProgress",
    "dsColorAccent",
)

# Reusable token driven drawables (design system primitives).
TOKEN_DRAWABLES = [
    "ds_cell_selector",
    "ds_input_background",
    "ds_chat_input_background",
    "ds_rounded_corner_background",
    "ds_rounded_corner_dialog_background",
    "ds_search_bar_background",
]

# Shell contract: page identities (legacy indices must not move) and the
# bottom-navigation order. Account is page 0 but intentionally hidden from the
# bottom bar and reachable through secondary navigation.
NAV_PAGE_IDENTITIES = {
    "AccountPage": 0,
    "SearchPage": 1,
    "TransfersPage": 2,
    "BrowsePage": 3,
    "MessagesPage": 4,
    "PageCount": 5,
    "DefaultLandingPage": 1,
}
BOTTOM_MENU_ORDER = [
    "navigation_search",
    "navigation_transfers",
    "navigation_browse",
    "navigation_messages",
]

# Persisted preference keys are a compatibility contract: renaming one silently
# resets the user's choice. Values must never change.
PERSISTED_THEME_KEYS = {
    "M_DayNightMode": "Momento_DayNightMode",
    "M_NightVariant": "Momento_NightModeVarient",
    "M_DayVariant": "Momento_DayModeVarient",
}

FAILURES: list[str] = []
PASSED: list[str] = []


def fail(check: str, message: str) -> None:
    FAILURES.append(f"[{check}] {message}")


def ok(check: str, message: str = "") -> None:
    PASSED.append(f"[{check}] {message}".rstrip())


def check(name: str, condition: bool, message: str) -> None:
    if condition:
        ok(name, message)
    else:
        fail(name, message)


def read(path: str) -> str:
    with open(path, encoding="utf-8-sig") as handle:
        return handle.read()


# ---------------------------------------------------------------------------
# resource model
# ---------------------------------------------------------------------------


def qualifier_dir(path: str) -> str:
    return os.path.basename(os.path.dirname(path))


def values_files() -> list[str]:
    out = []
    for entry in sorted(os.listdir(RES)):
        if not entry.startswith("values"):
            continue
        directory = os.path.join(RES, entry)
        if not os.path.isdir(directory):
            continue
        for name in sorted(os.listdir(directory)):
            if name.endswith(".xml"):
                out.append(os.path.join(directory, name))
    return out


class Style:
    __slots__ = ("name", "parent", "items", "config")

    def __init__(self, name: str, parent: str | None, config: str):
        self.name = name
        self.parent = parent
        self.items: dict[str, str] = {}
        self.config = config


def load_styles() -> tuple[dict[tuple[str, str], Style], list[str]]:
    """Returns {(config, style_name): Style} plus a list of duplicate reports."""
    styles: dict[tuple[str, str], Style] = {}
    duplicates: list[str] = []

    for path in values_files():
        root = ET.parse(path).getroot()
        config = qualifier_dir(path)
        for element in root.iter("style"):
            name = element.get("name")
            if not name:
                continue
            parent = element.get("parent")
            if parent is None and "." in name:
                # implicit parent: everything before the last dot
                parent = name.rsplit(".", 1)[0]
            style = Style(name, parent, config)
            for item in element.findall("item"):
                item_name = item.get("name")
                if item_name is None:
                    continue
                style.items[item_name] = (item.text or "").strip()

            key = (config, name)
            if key in styles:
                duplicates.append(f"{config}/{name} defined twice ({path})")
                # keep merging so item checks still see both definitions
                styles[key].items.setdefault("__dup__", path)
            else:
                styles[key] = style

    return styles, duplicates


STYLES, STYLE_DUPLICATES = load_styles()


def style_for(config: str, name: str) -> Style | None:
    """Config aware lookup: values-night overrides values, language dirs ignored."""
    if config in ("values-night",):
        return STYLES.get((config, name)) or STYLES.get(("values", name))
    return STYLES.get(("values", name)) or STYLES.get((config, name))


def chain(config: str, name: str) -> list[Style]:
    out: list[Style] = []
    seen = set()
    current = name
    while current and current not in seen:
        seen.add(current)
        style = style_for(config, current)
        if style is None:
            break
        out.append(style)
        current = style.parent
    return out


def resolved_items(config: str, name: str) -> dict[str, str]:
    items: dict[str, str] = {}
    for style in chain(config, name):
        for key, value in style.items.items():
            items.setdefault(key, value)
    return items


def declared_attrs() -> dict[str, str]:
    root = ET.parse(os.path.join(RES, "values", "attrs.xml")).getroot()
    attrs: dict[str, str] = {}
    duplicates: list[str] = []
    for element in root.iter("attr"):
        name = element.get("name")
        if not name:
            continue
        if name in attrs:
            duplicates.append(name)
        attrs[name] = element.get("format", "")
    if duplicates:
        fail("attrs.unique", f"duplicate <attr> declarations: {sorted(set(duplicates))}")
    else:
        ok("attrs.unique", f"{len(attrs)} attributes declared once each")
    return attrs


def concrete_themes() -> list[str]:
    """Styles that inherit AppTheme.NoActionBar (i.e. the selectable themes)."""
    themes = []
    for (config, name), style in STYLES.items():
        if config != "values":
            continue
        parents = {s.name for s in chain("values", name)}
        if "AppTheme.NoActionBar" in parents and name != "AppTheme.NoActionBar":
            themes.append(name)
    return sorted(themes)


def theme_config(theme: str) -> str:
    """The resource configuration a concrete theme is actually used in."""
    if theme.startswith("DefaultDark") or theme in ("Amoled", "Amoled_Grey", MODERN_NIGHT_THEME):
        return "values-night"
    return "values"


# ---------------------------------------------------------------------------
# colour helpers
# ---------------------------------------------------------------------------


def parse_color(value: str) -> tuple[int, int, int] | None:
    value = (value or "").strip()
    match = re.fullmatch(r"#([0-9a-fA-F]{3,8})", value)
    if not match:
        return None
    digits = match.group(1)
    if len(digits) == 3:
        digits = "".join(c * 2 for c in digits)
    elif len(digits) == 4:
        digits = "".join(c * 2 for c in digits)
    if len(digits) == 8:  # drop alpha
        digits = digits[2:]
    if len(digits) != 6:
        return None
    return int(digits[0:2], 16), int(digits[2:4], 16), int(digits[4:6], 16)


def relative_luminance(rgb: tuple[int, int, int]) -> float:
    channels = []
    for value in rgb:
        srgb = value / 255.0
        channels.append(srgb / 12.92 if srgb <= 0.03928 else ((srgb + 0.055) / 1.055) ** 2.4)
    return 0.2126 * channels[0] + 0.7152 * channels[1] + 0.0722 * channels[2]


def contrast_ratio(a: tuple[int, int, int], b: tuple[int, int, int]) -> float:
    la, lb = relative_luminance(a), relative_luminance(b)
    lighter, darker = max(la, lb), min(la, lb)
    return (lighter + 0.05) / (darker + 0.05)


def load_colors() -> dict[tuple[str, str], str]:
    """{(config, name): value} for values/colors.xml and values-night/colors.xml."""
    out: dict[tuple[str, str], str] = {}
    for config in ("values", "values-night"):
        path = os.path.join(RES, config, "colors.xml")
        if not os.path.exists(path):
            continue
        for element in ET.parse(path).getroot().iter("color"):
            name = element.get("name")
            if name:
                out[(config, name)] = (element.text or "").strip()
    return out


COLORS = load_colors()

ATTR_REF = re.compile(r"^\?attr/([A-Za-z0-9_]+)$")
ANDROID_ATTR_REF = re.compile(r"^\?android:attr/([A-Za-z0-9_]+)$")
COLOR_REF = re.compile(r"^@color/([A-Za-z0-9_]+)$")
FRAMEWORK_REF = re.compile(r"^[@?]android:")


def is_transparent(value: str) -> bool:
    """True for #00RRGGBB / #00000000 style fully transparent colours."""
    match = re.fullmatch(r"#([0-9a-fA-F]{8})", (value or "").strip())
    return bool(match) and match.group(1)[0:2].lower() == "00"


def token_value(config: str, style_name: str, name: str) -> str:
    """Raw item value for ``name`` in the merged chain of ``style_name``."""
    items = resolved_items(config, style_name)
    return items.get(name, "")


def resolve_color(
    config: str,
    style_name: str,
    name: str,
    _seen: tuple[str, ...] = (),
) -> tuple[tuple[int, int, int] | None, str]:
    """Resolve ``name`` through the theme to concrete RGB.

    Follows ``?attr/…`` chains and ``@color/…`` references instead of matching
    source strings, so the contrast assertions test what the device would paint.
    Returns (rgb or None, human readable note about the final value / the reason
    it cannot be resolved).
    """
    if name in _seen:
        return None, f"cycle {name}"
    value = token_value(config, style_name, name)
    if not value:
        return None, f"{name} is not defined in {style_name}"

    if is_transparent(value):
        return None, f"{name} is fully transparent ({value})"

    rgb = parse_color(value)
    if rgb is not None:
        return rgb, value

    match = ATTR_REF.match(value)
    if match:
        return resolve_color(config, style_name, match.group(1), _seen + (name,))

    match = ANDROID_ATTR_REF.match(value)
    if match:
        # Normalise ?android:attr/colorBackground to the local style item named
        # android:colorBackground when the theme defines one. Without this the
        # legacy dsColorBackground -> android:colorBackground -> dsColorBackground
        # cycle hides behind an opaque "framework reference".
        local_name = "android:" + match.group(1)
        if local_name in resolved_items(config, style_name):
            return resolve_color(config, style_name, local_name, _seen + (name,))
        return None, f"framework reference {value}"

    match = COLOR_REF.match(value)
    if match:
        color_name = match.group(1)
        for lookup in ((config, color_name), ("values", color_name)):
            if lookup in COLORS:
                rgb = parse_color(COLORS[lookup])
                return rgb, f"@color/{color_name} ({COLORS[lookup]})"
        return None, f"@color/{color_name} is not declared"

    if FRAMEWORK_REF.match(value):
        return None, f"framework reference {value}"

    return None, f"unresolved value '{value}'"


def local_attr_cycles(config: str, style_name: str) -> list[str]:
    """Every locally defined ?attr/?android:attr cycle reachable in a theme.

    External Material/framework attributes are leaves: an unresolved external
    attribute is not a failure, but a cycle through locally defined items is.
    Traversal starts from *every* merged style item so a cycle cannot hide just
    because no layout happens to read one of its members.
    """
    items = resolved_items(config, style_name)
    cycles: set[str] = set()
    for start in sorted(items):
        path: list[str] = []
        node = start
        while node in items:
            if node in path:
                cycle = path[path.index(node):] + [node]
                cycles.add(" -> ".join(cycle))
                break
            path.append(node)
            value = items[node]
            match = ATTR_REF.match(value)
            if match:
                target = match.group(1)
            else:
                match = ANDROID_ATTR_REF.match(value)
                target = "android:" + match.group(1) if match else None
            if target is None or target not in items:
                break
            node = target
    return sorted(cycles)


def resolved_contrast(config: str, style_name: str, fg: str, bg: str):
    fg_rgb, fg_note = resolve_color(config, style_name, fg)
    bg_rgb, bg_note = resolve_color(config, style_name, bg)
    if fg_rgb is None or bg_rgb is None:
        return None, f"{fg} ({fg_note}) on {bg} ({bg_note})"
    return contrast_ratio(fg_rgb, bg_rgb), f"{fg} ({fg_note}) on {bg} ({bg_note})"


# ---------------------------------------------------------------------------
# checks
# ---------------------------------------------------------------------------


def check_xml_parses() -> None:
    bad = []
    count = 0
    for root_dir, _dirs, files in os.walk(RES):
        for name in files:
            if not name.endswith(".xml"):
                continue
            path = os.path.join(root_dir, name)
            count += 1
            try:
                ET.parse(path)
            except ET.ParseError as error:
                bad.append(f"{os.path.relpath(path, REPO)}: {error}")
    if bad:
        fail("xml.parse", f"{len(bad)} XML file(s) do not parse: " + "; ".join(bad[:10]))
    else:
        ok("xml.parse", f"{count} XML files parse")


def check_style_duplicates() -> None:
    if STYLE_DUPLICATES:
        fail("styles.unique", f"duplicate style definitions: {STYLE_DUPLICATES[:10]}")
    else:
        ok("styles.unique", f"{len(STYLES)} style definitions, no duplicates within a configuration")

    night = {name for (config, name) in STYLES if config == "values-night"}
    day = {name for (config, name) in STYLES if config == "values"}
    overlap = sorted(night & day)
    allowed = set(BASELINE_EXCLUSIONS["cross_config_style_duplicates"])
    unexpected = [name for name in overlap if name not in allowed]
    if unexpected:
        fail(
            "styles.cross_config",
            "styles defined in both values/ and values-night/ (share them instead, or "
            f"document them in BASELINE_EXCLUSIONS): {unexpected}",
        )
    else:
        ok("styles.cross_config", f"night-only styles: {sorted(night)}")


def check_attr_coverage(attrs: dict[str, str]) -> None:
    themes = concrete_themes()
    check("themes.present", bool(themes), f"concrete themes: {themes}")

    for required in (REQUIRED_DAY_THEME, REQUIRED_NIGHT_THEME, MODERN_DAY_THEME, MODERN_NIGHT_THEME):
        check("themes.required", required in themes, f"{required} is a concrete theme")

    missing = {}
    for theme in themes:
        # The day/night contract is evaluated in the configuration that theme is
        # actually used in; legacy night themes must resolve in values-night too.
        config = theme_config(theme)
        items = resolved_items(config, theme)
        unresolved = sorted(name for name in attrs if name not in items)
        if unresolved:
            missing[theme] = unresolved

    if missing:
        detail = "; ".join(f"{theme}: {names[:8]}" for theme, names in missing.items())
        fail("themes.attrs", f"{len(missing)} theme(s) leave custom attributes unresolved: {detail}")
    else:
        ok("themes.attrs", f"all {len(attrs)} custom attributes resolved by {len(themes)} themes")


def check_theme_attr_cycles() -> None:
    """No concrete theme may contain a local theme-attribute reference cycle."""
    offenders = []
    for theme in concrete_themes():
        for cycle in local_attr_cycles(theme_config(theme), theme):
            offenders.append(f"{theme}: {cycle}")
    check(
        "themes.cycles",
        not offenders,
        "no concrete theme has a locally defined ?attr/?android:attr cycle"
        + (f" (cycles {offenders[:5]})" if offenders else ""),
    )


LEGACY_TOKEN_NAMES = (
    "android:windowBackground",
    "dsColorBackground",
    "dsColorLaunchBackground",
    "dsColorPrimary",
    "dsColorSurface",
    "dsColorTextPrimary",
    "dsColorChromeSurface",
    "dsColorChromeForeground",
)


def check_legacy_theme_tokens() -> None:
    """Legacy saved themes must resolve their core tokens, not just Modern*."""
    modern = {MODERN_DAY_THEME, MODERN_NIGHT_THEME}
    legacy = [theme for theme in concrete_themes() if theme not in modern]
    check(
        "themes.legacy",
        len(legacy) == 10,
        f"all 10 legacy themes remain selectable (got {len(legacy)}: {legacy})",
    )
    for theme in legacy:
        config = theme_config(theme)
        for token in LEGACY_TOKEN_NAMES:
            rgb, note = resolve_color(config, theme, token)
            check(
                "themes.legacy_tokens",
                rgb is not None,
                f"{theme}.{token} resolves to a concrete colour ({note})",
            )


def check_tokens_day_night() -> None:
    ds_attrs = {name for name, fmt in declared_attrs_snapshot.items() if name.startswith("dsColor")}
    shape_attrs = {name for name in declared_attrs_snapshot if name.startswith("dsShape")}
    for theme in (MODERN_DAY_THEME, MODERN_NIGHT_THEME):
        items = resolved_items("values", theme)
        missing = sorted(name for name in ds_attrs | shape_attrs if name not in items)
        check("tokens.complete", not missing, f"{theme} defines every ds* token" + (f" (missing {missing})" if missing else ""))

    day = resolved_items("values", MODERN_DAY_THEME)
    night = resolved_items("values", MODERN_NIGHT_THEME)

    for token, expected in DARK_PALETTE.items():
        actual = night.get(token, "")
        check(
            "tokens.palette",
            parse_color(actual) is not None and actual.upper() == expected.upper(),
            f"{MODERN_NIGHT_THEME}.{token} == {expected} (got {actual or '<absent>'})",
        )

    for token in ("dsColorBackground", "dsColorSurface", "dsColorPrimary", "dsColorTextPrimary"):
        day_value, night_value = day.get(token), night.get(token)
        check(
            "tokens.day_night",
            day_value and night_value and day_value.upper() != night_value.upper(),
            f"{token} differs between modern day ({day_value}) and night ({night_value})",
        )

    # Dark palette must actually be dark and the light one light.
    for token, maximum in (("dsColorBackground", 0.06), ("dsColorSurface", 0.06)):
        rgb = parse_color(night.get(token, ""))
        check(
            "tokens.dark",
            rgb is not None and relative_luminance(rgb) <= maximum,
            f"{MODERN_NIGHT_THEME}.{token} is dark",
        )
        rgb_day = parse_color(day.get(token, ""))
        check(
            "tokens.light",
            rgb_day is not None and relative_luminance(rgb_day) >= 0.6,
            f"{MODERN_DAY_THEME}.{token} is light",
        )

    for theme, items in ((MODERN_DAY_THEME, day), (MODERN_NIGHT_THEME, night)):
        for foreground, background in CONTRAST_PAIRS:
            fg = parse_color(items.get(foreground, ""))
            bg = parse_color(items.get(background, ""))
            if fg is None or bg is None:
                fail("tokens.contrast", f"{theme}: {foreground} on {background} uses token references, not colours")
                continue
            ratio = contrast_ratio(fg, bg)
            check(
                "tokens.contrast",
                ratio >= MIN_CONTRAST,
                f"{theme}: {foreground} on {background} = {ratio:.2f}:1 (>= {MIN_CONTRAST})",
            )


def check_theme_helper() -> None:
    enums_path = os.path.join(REPO, "Common", "ThemeEnums.cs")
    helper_path = os.path.join(REPO, "Seeker", "Helpers", "ThemeHelper.cs")
    enums = read(enums_path)
    helper = strip_cs_comments(read(helper_path))

    for enum_name in ("DayThemeType", "NightThemeType"):
        match = re.search(r"enum\s+%s\s*:\s*ushort\s*\{(.*?)\n\s*\}" % enum_name, enums, re.S)
        check("themehelper.enum", match is not None, f"{enum_name} parsed from Common/ThemeEnums.cs")
        if not match:
            continue
        members = re.findall(r"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=", match.group(1), re.M)
        handled = set(re.findall(r"case\s+%s\.([A-Za-z_][A-Za-z0-9_]*)\s*:" % enum_name, helper))
        missing = [member for member in members if member not in handled]
        check(
            "themehelper.total",
            not missing,
            f"every {enum_name} member is mapped by ThemeHelper ({len(members)} members)"
            + (f" (missing {missing})" if missing else ""),
        )

    style_names = {name for (_config, name) in STYLES}
    referenced = set(re.findall(r"Resource\.Style\.([A-Za-z0-9_]+)", helper))
    unresolved = sorted(
        name for name in referenced
        if name not in style_names and name.replace("_", ".") not in style_names
    )
    check(
        "themehelper.styles",
        not unresolved,
        f"every Resource.Style used by ThemeHelper exists ({len(referenced)} ids)"
        + (f" (unresolved {unresolved})" if unresolved else ""),
    )


def check_preferences() -> None:
    key_consts = read(os.path.join(REPO, "Common", "KeyConsts.cs"))
    for field, value in PERSISTED_THEME_KEYS.items():
        pattern = r"%s\s*=\s*\"%s\"" % (field, re.escape(value))
        check(
            "preferences.keys",
            re.search(pattern, key_consts) is not None,
            f'{field} stays "{value}"',
        )

    state = read(os.path.join(REPO, "Common", "PreferencesState.cs"))
    check(
        "preferences.default_state",
        re.search(r"DayNightMode\s*=\s*-1\b", state) is not None,
        "PreferencesState.DayNightMode defaults to -1 (follow system)",
    )
    check(
        "preferences.default_state",
        re.search(r"DayModeVariant\s*=\s*DayThemeType\.Modern\b", state) is not None,
        "PreferencesState.DayModeVariant defaults to Modern",
    )
    check(
        "preferences.default_state",
        re.search(r"NightModeVariant\s*=\s*NightThemeType\.Modern\b", state) is not None,
        "PreferencesState.NightModeVariant defaults to Modern",
    )

    manager = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Helpers", "PreferencesManager.cs")))
    check(
        "preferences.default_system",
        re.search(r"GetInt\(KeyConsts\.M_DayNightMode\s*,\s*-1\)", manager) is not None,
        "a fresh install follows the system night mode",
    )
    check(
        "preferences.default_variant",
        re.search(r"GetInt\(KeyConsts\.M_DayVariant\s*,\s*\(int\)DayThemeType\.Modern\)", manager) is not None,
        "a fresh install defaults to the modern day theme",
    )
    check(
        "preferences.default_variant",
        re.search(r"GetInt\(KeyConsts\.M_NightVariant\s*,\s*\(int\)NightThemeType\.Modern\)", manager) is not None,
        "a fresh install defaults to the modern night theme",
    )
    check(
        "preferences.legacy_values",
        re.search(r"ClassicPurple\s*=\s*0", read(os.path.join(REPO, "Common", "ThemeEnums.cs"))) is not None,
        "legacy enum ints (0 = ClassicPurple) are not renumbered",
    )


# Symbols that would mean "a theme change restarted the network/session/transfers".
SERVICE_SYMBOLS = (
    "SessionService",
    "DownloadService",
    "UploadService",
    "SeekerKeepAliveService",
    "StopService",
    "StopForeground",
    "SoulseekClient",
    "DisconnectAsync",
    "LogoutAsync",
)


def check_theme_switch_isolation() -> None:
    paths = [
        os.path.join(REPO, "Seeker", "Helpers", "ThemeHelper.cs"),
        os.path.join(REPO, "Seeker", "Settings", "Rows", "SettingPickers.cs"),
        os.path.join(REPO, "Seeker", "Main", "ThemeableActivity.cs"),
    ]
    for path in paths:
        text = strip_cs_comments(read(path))
        hits = [symbol for symbol in SERVICE_SYMBOLS if symbol in text]
        check(
            "theme_switch.isolation",
            not hits,
            f"{os.path.relpath(path, REPO)} does not stop the client/services/transfers"
            + (f" (found {hits})" if hits else ""),
        )

    helpers = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Helpers", "UiHelpers.cs")))
    check(
        "theme_switch.isolation",
        re.search(r"void\s+ApplyThemeChange\(Activity[^)]*\)\s*\{[^}]*RecreateActivies\(\)", helpers, re.S) is not None,
        "UiHelpers.ApplyThemeChange only re-creates activities",
    )
    activity = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Main", "ThemeableActivity.cs")))
    check(
        "theme_switch.isolation",
        "IsChangingConfigurations" in activity,
        "a configuration change does not persist/stop transfers",
    )


def check_overlays_and_chrome() -> None:
    day_items = resolved_items("values", "AppTheme.AppBarOverlay")
    night_items = resolved_items("values-night", "AppTheme.AppBarOverlay")
    check("overlays.appbar", bool(day_items), "AppTheme.AppBarOverlay is defined for day")
    check(
        "overlays.appbar",
        day_items == night_items,
        "AppTheme.AppBarOverlay is one shared, config independent definition",
    )

    for name in ("AppTheme.AppBarOverlay", "AppTheme.PopupOverlay", "AppPopupOverlay"):
        style = style_for("values", name)
        if style is None:
            fail("overlays.present", f"{name} is missing")
            continue
        parent = style.parent or ""
        check(
            "overlays.theme_aware",
            not re.search(r"\.(Dark|Light)(\.|$)", parent),
            f"{name} does not force a light/dark parent (parent={parent})",
        )
        foreground = [key for key in style.items if key.endswith("textColorPrimary") or key == "colorOnSurface"]
        check(
            "overlays.theme_aware",
            foreground and all(style.items[key].startswith("?attr/ds") for key in foreground),
            f"{name} foregrounds come from tokens ({foreground})",
        )

    layout = ET.parse(os.path.join(RES, "layout", "activity_main.xml")).getroot()
    literals = []
    for element in layout.iter():
        for attribute, value in element.attrib.items():
            if "tint" in attribute.lower() or attribute in ("android:background",) or "itemtextcolor" in attribute.lower():
                if re.search(r"#[0-9a-fA-F]{3,8}", value):
                    literals.append(f"{element.tag}.{attribute}={value}")
    check(
        "chrome.no_literals",
        not literals,
        "activity_main.xml chrome colours are theme driven" + (f" (literals {literals})" if literals else ""),
    )


def check_navigation_shell() -> None:
    main_nav = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Main", "MainNavigation.cs")))
    for name, expected in NAV_PAGE_IDENTITIES.items():
        match = re.search(r"public const int %s\s*=\s*([A-Za-z0-9_]+);" % name, main_nav)
        actual = match.group(1) if match else None
        # DefaultLandingPage is intentionally an alias of SearchPage, not a literal.
        acceptable = {str(expected), "SearchPage"} if name == "DefaultLandingPage" else {str(expected)}
        check(
            "nav.identity",
            actual in acceptable,
            f"MainNavigation.{name} == {expected}" + (f" (got {actual or '<absent>'})" if actual not in acceptable else ""),
        )

    menu = ET.parse(os.path.join(RES, "menu", "navigation_menu.xml")).getroot()
    ids = []
    for item in menu.findall("item"):
        item_id = item.get("{http://schemas.android.com/apk/res/android}id", "")
        ids.append(item_id.rsplit("/", 1)[-1])
    check(
        "nav.bottom_order",
        ids == BOTTOM_MENU_ORDER,
        f"bottom navigation order is {' | '.join(BOTTOM_MENU_ORDER)} (got {ids})",
    )
    check(
        "nav.account_hidden",
        "navigation_home" not in ids,
        "Account/login is not a bottom-navigation item",
    )

    adapter = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Main", "TabsPagerAdapter.cs")))
    check(
        "nav.pager_count",
        "ItemCount => MainNavigation.PageCount" in adapter,
        "TabsPagerAdapter exposes MainNavigation.PageCount pages",
    )
    check(
        "nav.messages_page",
        "case MainNavigation.MessagesPage:" in adapter and "MessagesOverviewFragment" in adapter,
        "pager page 4 creates the real MessagesOverviewFragment",
    )

    activity = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Main", "MainActivity.cs")))
    check(
        "nav.landing",
        "MainNavigation.DefaultLandingPage" in activity and "NormalizeRestoredPage" in activity,
        "MainActivity lands on the default page and normalizes restored pages",
    )
    check(
        "nav.no_swipe",
        "UserInputEnabled = false" in activity,
        "pager swiping is disabled so it cannot reach the hidden account page",
    )
    check(
        "nav.restoration",
        "CurrentPageStateKey" in activity and "LastPrimaryPageStateKey" in activity,
        "current page and last primary destination are saved/restored",
    )
    check(
        "nav.menu_gate",
        "SetChecked(false)" in activity and "MenuItemForPage(position)" in activity,
        "bottom selection is cleared and mapped by page identity (account selects nothing)",
    )
    check(
        "nav.account_back",
        "lastPrimaryPage" in activity and "pager.CurrentItem == MainNavigation.AccountPage" in activity,
        "back from account returns to the last primary destination",
    )

    navigation = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Main", "MainActivity.Navigation.cs")))
    check(
        "nav.secondary_sheet",
        "ShowSecondaryNavigationSheet" in navigation and "ActionSheetConfig" in navigation,
        "secondary navigation is built on the existing ActionSheet primitive",
    )
    for symbol in ("target_user_list", "wishlist", "chatrooms", "settings", "about_title", "shutdown", "nav_account"):
        check(
            "nav.preserved",
            f"Resource.String.{symbol}" in navigation,
            f"secondary navigation preserves {symbol}",
        )

    shell_menu = ET.parse(os.path.join(RES, "menu", "shell_menu.xml")).getroot()
    shell_ids = [item.get("{http://schemas.android.com/apk/res/android}id", "").rsplit("/", 1)[-1] for item in shell_menu.findall("item")]
    check(
        "nav.top_entry",
        shell_ids == ["action_shell_menu"],
        f"toolbar exposes the secondary navigation entry (got {shell_ids})",
    )

    messages = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Messages", "MessagesOverviewFragment.cs")))
    check(
        "nav.messages_host",
        "IMessagesOverviewHost" in messages,
        "MessagesOverviewFragment is hosted through the narrow host interface",
    )
    main_decl = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Main", "MainActivity.cs")))
    messages_activity = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Messages", "MessagesActivity.cs")))
    check(
        "nav.messages_hosts",
        "Seeker.Messages.IMessagesOverviewHost" in main_decl and "IMessagesOverviewHost" in messages_activity,
        "both MainActivity and MessagesActivity implement IMessagesOverviewHost",
    )

    selector_path = os.path.join(RES, "color", "ds_nav_item_tint.xml")
    if not os.path.exists(selector_path):
        fail("nav.selector", "missing res/color/ds_nav_item_tint.xml")
    else:
        selector = read(selector_path)
        for token in ("dsColorNavigationForeground", "dsColorNavigationForegroundMuted", "dsColorTextDisabled"):
            check(
                "nav.selector",
                "?attr/" + token in selector,
                f"bottom selector uses {token} for selected/unselected/disabled",
            )

    helpers = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Helpers", "UiHelpers.cs")))
    system_bar = helpers[helpers.find("ApplySystemBarIconContrast"):]
    system_bar = system_bar[: system_bar.find("ApplyThemeChange")]
    check(
        "theme.system_bar_attrs",
        "WindowLightStatusBar" in system_bar and "WindowLightNavigationBar" in system_bar,
        "system bar icon contrast reads the concrete theme's windowLight* attributes",
    )
    check(
        "theme.system_bar_attrs",
        "!ThemeHelper.IsNightMode(activity)" not in system_bar,
        "system bar icon contrast no longer assumes light icons from the day/night configuration",
    )

    helper = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Helpers", "ThemeHelper.cs")))
    for method in ("NormalizeDayTheme", "NormalizeNightTheme"):
        start = helper.find(method)
        end = helper.find("public static", start + len(method))
        body = helper[start:end if end != -1 else None]
        range_check = body.find("ushort.MaxValue")
        cast = body.find("(ushort)storedValue")
        check(
            "theme.normalize_range",
            range_check != -1 and cast != -1 and range_check < cast,
            f"{method} rejects out-of-ushort-range values before casting",
        )


def check_drawables() -> None:
    for name in TOKEN_DRAWABLES:
        path = os.path.join(RES, "drawable", f"{name}.xml")
        if not os.path.exists(path):
            fail("drawables.present", f"missing reusable drawable {name}.xml")
            continue
        content = read(path)
        ok("drawables.present", f"{name}.xml exists")
        check(
            "drawables.tokenised",
            "?attr/" in content and not re.search(r"#[0-9a-fA-F]{6,8}", content),
            f"{name}.xml uses ?attr tokens and no hardcoded colours",
        )

    themes = read(os.path.join(RES, "values", "themes.xml"))
    for attribute in ("CustomSelector", "rounded_input_edit", "chatInputBackground"):
        check(
            "drawables.used",
            re.search(r'name="%s">@drawable/ds_' % attribute, themes) is not None,
            f"modern themes point {attribute} at a reusable token drawable",
        )


def check_semantic_color_usage() -> None:
    """Chrome attrs stay chrome; controls, indicators and text use accent tokens.

    The legacy ``mainPurple`` family is the app-bar / status-bar / navigation
    background. ModernLight resolves it to #FFFFFF (a white app bar) and
    ModernDark to #1B222C (the surface), so using it as a foreground, a control
    tint or a progress colour paints chrome on chrome - an invisible spinner or a
    white-on-white selected filter. This check keeps it in chrome backgrounds only.
    """
    chrome_context_error = []
    background_attrs = {"background", "backgroundTint"}

    layout_dir = os.path.join(RES, "layout")
    for name in sorted(os.listdir(layout_dir)):
        if not name.endswith(".xml"):
            continue
        root = ET.parse(os.path.join(layout_dir, name)).getroot()
        for element in root.iter():
            for key, value in element.attrib.items():
                if not isinstance(key, str):
                    continue
                match = re.fullmatch(r"\?attr/([A-Za-z0-9_]+)", (value or "").strip())
                if not match:
                    continue
                attr = match.group(1)
                local = key.rsplit("}", 1)[-1]
                if attr in CHROME_ATTRS and local not in background_attrs:
                    chrome_context_error.append(f"layout/{name}:{local}=?attr/{attr}")
    check(
        "colour.chrome_context",
        not chrome_context_error,
        "chrome colour attrs are only used as chrome backgrounds in layouts"
        + (f" (misuse {chrome_context_error[:10]})" if chrome_context_error else ""),
    )

    # Any control tint attribute in a layout must name an accent token, and that
    # token has to stay visible on the theme background (asserted below).
    control_attr_names = set()
    for name in sorted(os.listdir(layout_dir)):
        if not name.endswith(".xml"):
            continue
        root = ET.parse(os.path.join(layout_dir, name)).getroot()
        for element in root.iter():
            for key, value in element.attrib.items():
                if not isinstance(key, str):
                    continue
                local = key.rsplit("}", 1)[-1]
                if local not in CONTROL_LAYOUT_ATTRS:
                    continue
                match = re.fullmatch(r"\?attr/([A-Za-z0-9_]+)", (value or "").strip())
                if match:
                    control_attr_names.add(match.group(1))
                else:
                    control_attr_names.add(f"literal:{value}")
    literals = sorted(a for a in control_attr_names if a.startswith("literal:"))
    check("colour.control_tint", not literals, f"control tints are theme attributes, not literals (got {literals})")

    # A control tint must be an accent in *every* modern theme. Resolving it and
    # demanding 3:1 against the background catches "indicator painted with the
    # chrome colour" without freezing the legacy palettes.
    for theme in (MODERN_DAY_THEME, MODERN_NIGHT_THEME):
        bg, bg_note = resolve_color("values", theme, "dsColorBackground")
        for attr in sorted(a for a in control_attr_names if not a.startswith("literal:")):
            rgb, note = resolve_color("values", theme, attr)
            if rgb is None or bg is None:
                fail("colour.control_visible", f"{theme}: {attr} ({note}) vs background ({bg_note})")
                continue
            ratio = contrast_ratio(rgb, bg)
            check(
                "colour.control_visible",
                ratio >= MIN_CONTROL_CONTRAST,
                f"{theme}: control tint {attr} ({note}) = {ratio:.2f}:1 on the background (>= {MIN_CONTROL_CONTRAST})",
            )

    # Chrome attrs in drawables / colour state lists: allowed as a shape fill
    # (solid/stroke), never as a state-list colour (that is a text/control tint).
    drawable_error = []
    for folder in ("drawable", "drawable-night", "color", "color-night"):
        directory = os.path.join(RES, folder)
        if not os.path.isdir(directory):
            continue
        for name in sorted(os.listdir(directory)):
            if not name.endswith(".xml"):
                continue
            root = ET.parse(os.path.join(directory, name)).getroot()
            for element in root.iter():
                value = (element.get("{http://schemas.android.com/apk/res/android}color") or "").strip()
                match = re.fullmatch(r"\?attr/([A-Za-z0-9_]+)", value)
                if not match or match.group(1) not in CHROME_ATTRS:
                    continue
                if element.tag not in ("solid", "stroke"):
                    drawable_error.append(f"{folder}/{name}:<{element.tag}> ?attr/{match.group(1)}")
    check(
        "colour.chrome_drawable",
        not drawable_error,
        "chrome attrs are only shape fills (never state-list colours) in drawables/selectors"
        + (f" (misuse {drawable_error[:10]})" if drawable_error else ""),
    )

    # C# must not reach for a chrome attribute at all: every control/foreground
    # colour has a semantic token.
    code_error = []
    for current, dirs, files in os.walk(os.path.join(REPO, "Seeker")):
        dirs[:] = [d for d in dirs if d not in ("obj", "bin")]
        for name in files:
            if not name.endswith(".cs"):
                continue
            source = strip_cs_comments(read(os.path.join(current, name)))
            for attr in CHROME_ATTRS:
                if re.search(r"Resource\.Attribute\.%s\b" % attr, source):
                    code_error.append(f"{os.path.relpath(os.path.join(current, name), REPO)}:{attr}")
    check(
        "colour.chrome_code",
        not code_error,
        "no C# code resolves a chrome colour attribute"
        + (f" (offenders {code_error[:10]})" if code_error else ""),
    )

    # Search filter segmented control: selected = primary/on-primary, unselected =
    # surface/text-primary. Asserted on the method bodies, not on a comment.
    fragment = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Search", "SearchFragment.cs")))
    bg_body = method_body(fragment, "GetSegmentedButtonBgTint(Android.Content.Context ctx)")
    text_body = method_body(fragment, "GetSegmentedButtonTextTint(Android.Content.Context ctx)")
    check(
        "colour.filter_selected",
        "Resource.Attribute.dsColorPrimary" in bg_body and "Resource.Attribute.dsColorSurface" in bg_body,
        "selected filter background is dsColorPrimary, unselected is dsColorSurface",
    )
    check(
        "colour.filter_selected",
        "Resource.Attribute.dsColorOnPrimary" in text_body and "Resource.Attribute.dsColorTextPrimary" in text_body,
        "selected filter label is dsColorOnPrimary, unselected is dsColorTextPrimary",
    )
    check(
        "colour.filter_selected",
        "mainPurple" not in bg_body + text_body and "Color.White" not in bg_body + text_body,
        "the filter tints contain no chrome attribute and no literal white",
    )

    # Documented meaning of the chrome attrs in the modern themes (they are chrome,
    # they are not the accent). If this ever changes, the chrome/chrome misuse above
    # would silently become acceptable again.
    for theme in (MODERN_DAY_THEME, MODERN_NIGHT_THEME):
        chrome, chrome_note = resolve_color("values", theme, "dsColorChromeSurface")
        purple, purple_note = resolve_color("values", theme, "mainPurple")
        check(
            "colour.chrome_meaning",
            chrome is not None and chrome == purple,
            f"{theme}: mainPurple resolves to the chrome surface ({purple_note} vs {chrome_note})",
        )
        accent, accent_note = resolve_color("values", theme, "dsColorPrimary")
        check(
            "colour.chrome_meaning",
            accent is not None and chrome is not None and accent != chrome,
            f"{theme}: dsColorPrimary ({accent_note}) is not the chrome surface ({chrome_note})",
        )


def check_color_contrast_contracts() -> None:
    """Real contrast ratios for the colour pairs the design system paints.

    Every value is resolved through the theme (``?attr/`` chains and ``@color/``
    references), so this measures the colour the device would actually render
    instead of matching source strings.
    """
    for theme in (MODERN_DAY_THEME, MODERN_NIGHT_THEME):
        for foreground, background, minimum, label in RESOLVED_CONTRAST_PAIRS:
            ratio, note = resolved_contrast("values", theme, foreground, background)
            if ratio is None:
                fail("colour.contrast", f"{theme}: {label} cannot be resolved ({note})")
                continue
            check(
                "colour.contrast",
                ratio >= minimum,
                f"{theme}: {label} = {ratio:.2f}:1 (>= {minimum}) [{note}]",
            )

    # Accent attributes must never resolve to the surface they are painted on.
    for theme in (MODERN_DAY_THEME, MODERN_NIGHT_THEME):
        surface, surface_note = resolve_color("values", theme, "dsColorSurface")
        for attr in ACCENT_ATTRS_ON_SURFACE:
            rgb, note = resolve_color("values", theme, attr)
            if rgb is None or surface is None:
                fail("colour.accent_on_surface", f"{theme}: {attr} cannot be resolved ({note})")
                continue
            ratio = contrast_ratio(rgb, surface)
            check(
                "colour.accent_on_surface",
                ratio >= MIN_CONTROL_CONTRAST,
                f"{theme}: {attr} ({note}) is visible on the surface = {ratio:.2f}:1 (>= {MIN_CONTROL_CONTRAST})",
            )

    # Colour state lists must stay readable: every item colour resolves to an accent
    # or text colour, not to the surface it is drawn on.
    color_dir = os.path.join(RES, "color")
    for name in sorted(os.listdir(color_dir)) if os.path.isdir(color_dir) else []:
        if not name.endswith(".xml"):
            continue
        root = ET.parse(os.path.join(color_dir, name)).getroot()
        attrs = []
        for item in root.iter("item"):
            value = (item.get("{http://schemas.android.com/apk/res/android}color") or "").strip()
            match = re.fullmatch(r"\?attr/([A-Za-z0-9_]+)", value)
            if match:
                attrs.append(match.group(1))
        for theme in (MODERN_DAY_THEME, MODERN_NIGHT_THEME):
            bg, bg_note = resolve_color("values", theme, "dsColorBackground")
            for attr in attrs:
                rgb, note = resolve_color("values", theme, attr)
                if rgb is None or bg is None:
                    fail("colour.selector", f"{theme}: color/{name} {attr} cannot be resolved ({note})")
                    continue
                ratio = contrast_ratio(rgb, bg)
                check(
                    "colour.selector",
                    ratio >= MIN_CONTROL_CONTRAST,
                    f"{theme}: color/{name} {attr} ({note}) = {ratio:.2f}:1 on the background (>= {MIN_CONTROL_CONTRAST})",
                )


def check_color_parity() -> None:
    def names(path: str) -> set[str]:
        root = ET.parse(path).getroot()
        out = set()
        duplicates = []
        for element in root.iter("color"):
            name = element.get("name")
            if not name:
                continue
            if name in out:
                duplicates.append(name)
            out.add(name)
        if duplicates:
            fail("colors.unique", f"{os.path.relpath(path, REPO)}: duplicate {sorted(set(duplicates))}")
        return out

    day = names(os.path.join(RES, "values", "colors.xml"))
    night = names(os.path.join(RES, "values-night", "colors.xml"))
    only_day = sorted(day - night)
    only_night = sorted(night - day)
    check(
        "colors.parity",
        not only_day and not only_night,
        f"{len(day)} colour resources have day/night parity"
        + (f" (day only {only_day}, night only {only_night})" if only_day or only_night else ""),
    )


def check_dimens() -> None:
    root = ET.parse(os.path.join(RES, "values", "dimens.xml")).getroot()
    names = {element.get("name") for element in root.iter("dimen")}
    for required in ("ds_space_sm", "ds_space_lg", "ds_corner_md", "ds_touch_target_min"):
        check("dimens.tokens", required in names, f"dimen {required} exists")
    touch = None
    for element in root.iter("dimen"):
        if element.get("name") == "ds_touch_target_min":
            touch = (element.text or "").strip()
    check(
        "dimens.touch_target",
        touch == "48dp",
        f"ds_touch_target_min is the 48dp accessibility minimum (got {touch})",
    )


def check_typography() -> None:
    names = {name for (_config, name) in STYLES}
    for required in (
        "TextAppearance.Seeker.Headline",
        "TextAppearance.Seeker.Title",
        "TextAppearance.Seeker.Body",
        "TextAppearance.Seeker.Label",
        "TextAppearance.Seeker.Caption",
    ):
        check("typography.present", required in names, f"typography style {required} exists")


def strip_cs_comments(text: str) -> str:
    text = re.sub(r"/\*.*?\*/", "", text, flags=re.S)
    text = re.sub(r"//[^\n]*", "", text)
    return text


def check_code_resource_ids() -> None:
    """Every Resource.* id used from C# must resolve to an app resource."""
    style_names = {name for (_config, name) in STYLES}
    drawable_names = set()
    for directory in ("drawable", "drawable-night", "mipmap-anydpi-v26", "mipmap-hdpi", "mipmap-mdpi",
                      "mipmap-xhdpi", "mipmap-xxhdpi", "mipmap-xxxhdpi"):
        path = os.path.join(RES, directory)
        if not os.path.isdir(path):
            continue
        for name in os.listdir(path):
            drawable_names.add(os.path.splitext(name)[0])

    color_names = set()
    for directory in ("values", "values-night"):
        root = ET.parse(os.path.join(RES, directory, "colors.xml")).getroot()
        color_names |= {element.get("name") for element in root.iter("color")}

    attrs = declared_attrs_snapshot
    library_attrs = set(BASELINE_EXCLUSIONS["library_attrs_used_from_code"])

    references = {"Style": set(), "Drawable": set(), "Color": set(), "Attribute": set()}
    for root_dir, _dirs, files in os.walk(os.path.join(REPO, "Seeker")):
        if os.sep + "obj" in root_dir or os.sep + "bin" in root_dir:
            continue
        if "Soulseek.NET-master" in root_dir or "Mono.Nat-master" in root_dir:
            continue
        for name in files:
            if not name.endswith(".cs"):
                continue
            text = strip_cs_comments(read(os.path.join(root_dir, name)))
            for kind in references:
                references[kind] |= set(
                    re.findall(r"(?<!Android\.)Resource\.%s\.([A-Za-z_][A-Za-z0-9_]*)" % kind, text)
                )

    unresolved_style = sorted(
        name for name in references["Style"]
        if name not in style_names and name.replace("_", ".") not in style_names
    )
    unresolved_drawable = sorted(name for name in references["Drawable"] if name not in drawable_names)
    unresolved_color = sorted(name for name in references["Color"] if name not in color_names)
    unresolved_attr = sorted(
        name for name in references["Attribute"] if name not in attrs and name not in library_attrs
    )

    check("refs.style", not unresolved_style, f"{len(references['Style'])} Resource.Style ids resolve" + (f" (bad {unresolved_style})" if unresolved_style else ""))
    check("refs.drawable", not unresolved_drawable, f"{len(references['Drawable'])} Resource.Drawable ids resolve" + (f" (bad {unresolved_drawable})" if unresolved_drawable else ""))
    check("refs.color", not unresolved_color, f"{len(references['Color'])} Resource.Color ids resolve" + (f" (bad {unresolved_color})" if unresolved_color else ""))
    check(
        "refs.attribute",
        not unresolved_attr,
        f"{len(references['Attribute'])} Resource.Attribute ids resolve"
        + (f" (bad {unresolved_attr})" if unresolved_attr else ""),
    )


SEARCH_RESULT_LAYOUTS = [
    "search_result_modern_top",
    "search_result_modern_bottom",
    "search_result_simple_top",
    "search_result_simple_bottom",
    "search_result_compact",
]

SEARCH_STRINGS = [
    "search_here",
    "search_field_description",
    "search_tabs_description",
    "search_stop",
    "advanced_filters",
    "advanced_filters_description",
    "search_sort_button",
    "search_active_filters",
    "search_filter_text_prefix",
    "search_filter_format_prefix",
    "search_filter_bitrate_prefix",
    "search_filter_chips",
    "search_disconnected_title",
    "search_not_logged_in_title",
    "search_result_bitrate_unit",
    "search_result_speed_unit",
    "search_result_free_slot",
    "search_result_queue",
    "search_result_files_suffix",
    "search_result_file_suffix",
    "download_result_description",
    "download_result_description_for",
    "download_group_result_description",
    "search_result_row_description",
    "search_result_group_row_description",
    "Filename",
    "FileSize",
    "default_search_sort",
    "keywords_default_search_sort",
    "search_result_selected_state",
]


def android_id(element) -> str:
    return element.get("{http://schemas.android.com/apk/res/android}id", "").rsplit("/", 1)[-1]


def layout_ids(name: str) -> set[str]:
    root = ET.parse(os.path.join(RES, "layout", f"{name}.xml")).getroot()
    return {android_id(element) for element in root.iter() if android_id(element)}


def find_layout_element(name: str, element_id: str):
    root = ET.parse(os.path.join(RES, "layout", f"{name}.xml")).getroot()
    for element in root.iter():
        if android_id(element) == element_id:
            return element
    return None


def text_size_sp(name: str, element_id: str) -> float | None:
    """Numeric sp value of an android:textSize, or None for a token/absent value."""
    element = find_layout_element(name, element_id)
    if element is None:
        return None
    value = element.get("{http://schemas.android.com/apk/res/android}textSize") or ""
    match = re.fullmatch(r"([0-9]+(?:\.[0-9]+)?)sp", value)
    return float(match.group(1)) if match else None


def method_body(source: str, signature: str) -> str:
    """Best-effort brace-matched body for a C# method, for contract assertions."""
    start = source.find(signature)
    if start == -1:
        return ""
    brace = source.find("{", start)
    if brace == -1:
        return ""
    depth = 0
    for index in range(brace, len(source)):
        if source[index] == "{":
            depth += 1
        elif source[index] == "}":
            depth -= 1
            if depth == 0:
                return source[brace + 1:index]
    return ""


def check_search_ui() -> None:
    # Search field: prominent, stable id, history is still wired in C#.
    custom = read(os.path.join(RES, "layout", "custom_menu_layout.xml"))
    search_field = find_layout_element("custom_menu_layout", "searchHere")
    check("search.field", search_field is not None, "custom_menu_layout keeps @id/searchHere")
    if search_field is not None:
        check(
            "search.field",
            search_field.get("{http://schemas.android.com/apk/res/android}hint") == "@string/search_here",
            "search field keeps the search_here hint",
        )
        check(
            "search.field",
            search_field.get("{http://schemas.android.com/apk/res/android}layout_height") == "wrap_content"
            and search_field.get("{http://schemas.android.com/apk/res/android}minHeight") == "@dimen/ds_touch_target_min",
            "search field wraps to content with a 48dp minimum (large font safe)",
        )
        check(
            "search.field",
            search_field.get("{http://schemas.android.com/apk/res/android}contentDescription") == "@string/search_field_description",
            "search field has an accessibility description",
        )
    check(
        "search.history",
        "PreferencesState.SearchHistory" in strip_cs_comments(read(os.path.join(REPO, "Seeker", "Search", "SearchFragment.cs"))),
        "search history is still bound to the search field",
    )
    tabs = find_layout_element("custom_menu_layout", "search_tabs")
    check(
        "search.tabs",
        tabs is not None
        and tabs.get("{http://schemas.android.com/apk/res/android}layout_width") == "@dimen/ds_touch_target_min"
        and tabs.get("{http://schemas.android.com/apk/res/android}layout_height") == "@dimen/ds_touch_target_min",
        "search tabs affordance is a 48dp target",
    )

    menu = ET.parse(os.path.join(RES, "menu", "search_menu.xml")).getroot()
    menu_ids = [android_id(item) for item in menu.findall("item")]
    for required in ("action_search", "action_advanced_search", "action_sort_results_by"):
        check("search.menu", required in menu_ids, f"search menu exposes {required}")

    searches_ids = layout_ids("searches")
    for required in (
        "searchEmptyStateFlipper",
        "searchLoadingView",
        "noResultsView",
        "allFilteredView",
        "noSearchesView",
        "searchFailedView",
        "searchFailedTitle",
        "searchFailedReason",
        "filterText",
        "bsbutton",
        "searchResultsHeader",
    ):
        check("search.states", required in searches_ids, f"searches.xml keeps {required}")
    filter_text = find_layout_element("searches", "filterText")
    check(
        "search.filters",
        filter_text is not None
        and filter_text.get("{http://schemas.android.com/apk/res/android}minHeight") == "@dimen/ds_touch_target_min",
        "advanced filter text field is a 48dp target",
    )
    fab = find_layout_element("searches", "bsbutton")
    check(
        "search.filters",
        fab is not None
        and fab.get("{http://schemas.android.com/apk/res/android}contentDescription") == "@string/advanced_filters_description",
        "advanced filter FAB has an accessibility description",
    )

    header = ET.parse(os.path.join(RES, "layout", "search_results_header.xml")).getroot()
    header_ids = {android_id(element) for element in header.iter() if android_id(element)}
    for required in ("searchResultsHeaderText", "searchSortButton", "searchActiveFiltersText"):
        check("search.header", required in header_ids, f"result header keeps {required}")

    # Every supported result variant keeps its identity, gains a metadata line and a download
    # action, and no longer clips the file name to a single fixed line.
    for name in SEARCH_RESULT_LAYOUTS:
        ids = layout_ids(name)
        for required in ("folderNameTextView", "resultMetaTextView", "downloadActionButton", "selectionCheckbox", "searchItemPrimaryRow"):
            check("search.rows", required in ids, f"{name} keeps {required}")
        folder = find_layout_element(name, "folderNameTextView")
        check(
            "search.rows",
            folder is not None
            and folder.get("{http://schemas.android.com/apk/res/android}maxLines") in ("2", "3", "4")
            and folder.get("{http://schemas.android.com/apk/res/android}singleLine") != "true",
            f"{name} gives the file name room for multiple lines",
        )
        download = find_layout_element(name, "downloadActionButton")
        check(
            "search.rows",
            download is not None
            and download.get("{http://schemas.android.com/apk/res/android}contentDescription") == "@string/download_result_description"
            and download.get("{http://schemas.android.com/apk/res/android}layout_width") == "@dimen/ds_touch_target_min"
            and download.get("{http://schemas.android.com/apk/res/android}layout_height") == "@dimen/ds_touch_target_min",
            f"{name} has an accessible 48dp download action",
        )
        literals = [
            value
            for element in ET.parse(os.path.join(RES, "layout", f"{name}.xml")).getroot().iter()
            for attribute, value in element.attrib.items()
            if ("color" in attribute.lower() or attribute == "android:background")
            and re.search(r"#[0-9a-fA-F]{6,8}", value)
        ]
        check("search.rows", not literals, f"{name} row colours are theme driven")

    # Bindings reuse the existing download dialog and sorting/filter algorithms.
    item_views = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Search", "SearchItemViewsUnified.cs")))
    for symbol in ("SearchResultMetadata.Create", "resultMetaTextView", "downloadActionButton", "ContentDescription"):
        check("search.bindings", symbol in item_views, f"result row binding uses {symbol}")

    adapter = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Search", "SearchAdapter.cs")))
    for symbol in ("UnifiedDownloadClick", "UnifiedRowLongClick", "ShowDownloadDialog", "BuildActiveFilterSummary", "ShowChangeSortOrderDialog"):
        check("search.bindings", symbol in adapter, f"search adapter wires {symbol}")

    fragment = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Search", "SearchFragment.cs")))
    for symbol in ("action_advanced_search", "OpenAdvancedFilters", "searchFailedTitle", "FailedStateTitle"):
        check("search.bindings", symbol in fragment, f"SearchFragment wires {symbol}")

    sort_layout = ET.parse(os.path.join(RES, "layout", "changeresultsortorder.xml")).getroot()
    sort_ids = [android_id(element) for element in sort_layout.iter()]
    for required in ("availability", "speed", "folderNameAlpha", "bitRate", "fileName", "fileSize"):
        check("search.sorting", required in sort_ids, f"sort dialog keeps backend option {required}")

    strings = {element.get("name") for element in ET.parse(os.path.join(RES, "values", "strings.xml")).getroot().iter("string")}
    for required in SEARCH_STRINGS:
        check("search.strings", required in strings, f"string {required} exists")

    messages = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Messages", "MessagesOverviewFragment.cs")))
    check(
        "messages.freshness",
        "OnResume" in messages and "RefreshAdapter()" in messages,
        "MessagesOverviewFragment refreshes read markers on resume",
    )
    messages_adapter = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Messages", "MessagesOverviewRecyclerAdapter.cs")))
    check(
        "messages.undo_host",
        "SnackbarAnchor" in messages_adapter,
        "swipe undo anchors the snackbar through the host independent fragment view",
    )


def check_search_result_group_contract() -> None:
    """A SearchResponse row is a folder/group, never a single file.

    The row must keep the folder visible in every result variant, show the representative
    file name and file count as secondary information, and describe the download action as
    the grouped file selection it really opens.
    """
    check(
        "search.groups",
        len(SEARCH_RESULT_LAYOUTS) == 5,
        f"all {len(SEARCH_RESULT_LAYOUTS)} result variants are covered",
    )

    # Every variant keeps a visible folder/identity line plus a secondary metadata line.
    for name in SEARCH_RESULT_LAYOUTS:
        ids = layout_ids(name)
        check("search.groups", "folderNameTextView" in ids, f"{name} keeps a visible folder/identity line")
        check("search.groups", "resultMetaTextView" in ids, f"{name} keeps a secondary metadata line")
        folder = find_layout_element(name, "folderNameTextView")
        width = folder.get("{http://schemas.android.com/apk/res/android}layout_width") if folder is not None else None
        weight = folder.get("{http://schemas.android.com/apk/res/android}layout_weight") if folder is not None else None
        check(
            "search.groups",
            folder is not None
            and folder.get("{http://schemas.android.com/apk/res/android}visibility") != "gone"
            and (width != "0dp" or weight is not None),
            f"{name} folder/identity line is visible and sized",
        )

    metadata = strip_cs_comments(read(os.path.join(REPO, "Common", "Search", "SearchResultMetadata.cs")))
    check("search.groups", "public bool IsGroup" in metadata, "metadata exposes group identity")
    check("search.groups", "public string PrimaryText" in metadata, "metadata exposes the primary row identity")
    check(
        "search.groups",
        re.search(r"meta\.PrimaryText\s*=\s*meta\.IsGroup\s*&&[^;]*meta\.FolderName", metadata, re.S) is not None,
        "the group folder is the primary row identity",
    )
    check(
        "search.groups",
        re.search(r"meta\.SecondaryText\s*=\s*meta\.IsGroup\s*\?\s*meta\.FileName", metadata) is not None,
        "the representative file name is the secondary identity of a group",
    )
    check(
        "search.groups",
        "public string DetailLineWithFileCount" in metadata,
        "metadata offers a file count prefixed line for variants without a count field",
    )
    check(
        "search.groups",
        re.search(r"GetDominantFileTypeAndBitRate\s*\(\s*hideLocked\s*,\s*out\s*_\)", metadata) is not None,
        "the group dominant bitrate is not rendered as representative file metadata",
    )
    check(
        "search.groups",
        re.search(r"meta\.Bitrate\s*=\s*FormatFileBitrate\s*\(\s*first\b", metadata) is not None,
        "representative file quality comes from that file's own attributes",
    )
    check(
        "search.groups",
        "meta.FileCount == 1 ? labels.File : labels.Files" in metadata,
        "the file count reads naturally for single file and group rows",
    )

    item_views = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Search", "SearchItemViewsUnified.cs")))
    check(
        "search.groups",
        "viewFoldername.Text = meta.PrimaryText" in item_views,
        "the shared bind puts the folder/group identity on the visible identity line",
    )
    check(
        "search.groups",
        "HasFileCountField ? meta.DetailLine : meta.DetailLineWithFileCount" in item_views,
        "variants without a count field still show the file count",
    )
    check(
        "search.groups",
        "download_group_result_description" in item_views and "meta.IsGroup" in item_views,
        "the download action describes the grouped file selection it opens",
    )
    check(
        "search.groups",
        "search_result_group_row_description" in item_views,
        "group rows expose folder, count and representative file to accessibility",
    )
    for variant in (
        "SearchItemViewSimpleBottom",
        "SearchItemViewSimpleTop",
        "SearchItemViewModernBottom",
        "SearchItemViewModernTop",
        "SearchItemViewCompact",
    ):
        check("search.groups", f"class {variant}" in item_views, f"{variant} is bound through the shared metadata")
    # Simple top/bottom, the modern base (top inherits it) and compact all bind it directly.
    set_item_binds = re.findall(r"public override void setItem\(SearchResponse item, int position\)\s*\{\s*var meta = BindMetadata\(item\);?", item_views)
    check(
        "search.groups",
        len(set_item_binds) >= 4,
        f"all result variants bind the shared metadata projection (found {len(set_item_binds)})",
    )

    adapter = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Search", "SearchAdapter.cs")))
    check("search.taps", "RecyclerView.NoPosition" in adapter, "tap handlers know RecyclerView.NoPosition")
    check("search.taps", "TryGetBoundPosition" in adapter, "tap handlers validate the binding position")
    for handler in ("UnifiedRowClick", "UnifiedRowLongClick", "UnifiedDownloadClick"):
        body = re.search(r"private void %s\([^)]*\)\s*\{(.*?)\n            \}" % handler, adapter, re.S)
        check(
            "search.taps",
            body is not None and "TryGetBoundPosition" in body.group(1),
            f"{handler} validates the adapter position before opening the dialog",
        )
    check(
        "search.taps",
        re.search(r"adapterPosition\s*>=\s*localDataSet\.Count", adapter) is not None,
        "the bound position is checked against the current data set size",
    )
    check(
        "search.taps",
        "ShowDownloadDialog(itemRoot.ViewHolder.BindingAdapterPosition)" not in adapter,
        "no tap handler indexes the data set with an unvalidated position",
    )



def check_phase6_search_selection_and_sorting() -> None:
    """Phase 6A: filename/size sorts (ids preserved) and real cross-result selection."""
    enums = strip_cs_comments(read(os.path.join(REPO, "Common", "SearchEnums.cs")))
    for name, value in (("Available", 0), ("Fastest", 1), ("FolderAlphabetical", 2),
                        ("BitRate", 3), ("Filename", 4), ("FileSize", 5)):
        check(
            "search.sort_ids",
            re.search(r"\b%s\s*=\s*%d\b" % (name, value), enums) is not None,
            f"SearchResultSorting.{name} keeps the persisted id {value}",
        )

    comparable = strip_cs_comments(read(os.path.join(REPO, "Common", "Search", "SearchResultComparable.cs")))
    check("search.sort_semantics", "GetRepresentativeFile" in comparable,
          "sorting uses one representative-file rule")
    check("search.sort_semantics", "GetFileNameFromFile" in comparable,
          "filename sorting compares the file name, not the entire path")
    check("search.sort_semantics", "StringComparison.OrdinalIgnoreCase" in comparable
          and "StringComparison.Ordinal" in comparable,
          "filename sorting is case-insensitive with a deterministic ordinal tie-break")
    check("search.sort_semantics", "xFile?.Size" in comparable and "yFile?.Size" in comparable,
          "file-size sorting reads the representative file size in O(1)")
    check("search.sort_semantics", "SearchResultSorting.Filename" in comparable
          and "SearchResultSorting.FileSize" in comparable,
          "the comparer handles both new sort options")

    labels = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Search", "SearchSortLabels.cs")))
    for value in ("Available", "Fastest", "FolderAlphabetical", "BitRate", "Filename", "FileSize"):
        check("search.sort_labels", "SearchResultSorting.%s" % value in labels,
              f"shared sort labels cover {value}")
    adapter = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Search", "SearchAdapter.cs")))
    check("search.sort_labels", "SearchSortLabels.Label" in adapter,
          "the result header uses the shared sort label")

    catalog = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Settings", "Rows", "SettingsCatalog.cs")))
    check("search.default_sort", '"search.default_sort"' in catalog,
          "settings exposes a default-sort row")
    check("search.default_sort", "SearchResultSortLabel" in catalog and "PickDefaultSearchSort" in catalog,
          "the default-sort row uses the shared labels and existing option picker")
    pickers = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Settings", "Rows", "SettingPickers.cs")))
    for value in ("Available", "Fastest", "FolderAlphabetical", "BitRate", "Filename", "FileSize"):
        check("search.default_sort", "SearchResultSorting.%s" % value in pickers,
              f"default-sort picker offers {value}")

    batch_menu = ET.parse(os.path.join(RES, "menu", "search_batch_menu.xml")).getroot()
    batch_ids = [android_id(element) for element in batch_menu.iter() if android_id(element)]
    for required in ("action_download_batch", "action_queue_paused_batch", "select_all"):
        check("search.selection_menu", required in batch_ids, f"search batch menu exposes {required}")

    selection = strip_cs_comments(read(os.path.join(REPO, "Common", "Search", "SearchSelection.cs")))
    for symbol in ("SearchResultIdentity", "HashSet<string>", "Toggle", "PruneTo", "Resolve"):
        check("search.selection_state", symbol in selection, f"selection state provides {symbol}")
    check(
        "search.selection_state",
        "response.Files.First().Filename" in selection and "response.LockedFiles.First().Filename" in selection,
        "selection identity uses the stable representative file",
    )

    action = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Search", "SearchActionMode.cs")))
    for symbol in ("SearchActionModeCallback", "EnterSelectionMode", "ToggleSelectionAt",
                   "SelectAllVisibleResults", "DownloadSelectedResults", "GetDisplayedResponseAt",
                   "searchSelection.PruneTo", "BrowseUtils.GetFullFileInfos",
                   "response.GetFiles(hideLocked)", "SessionService.Instance.RunWithReconnect"):
        check("search.selection_bindings", symbol in action, f"selection action mode wires {symbol}")
    check("search.selection_bindings", "GroupBy(response => response.Username)" in action,
          "batch download keeps each selected result's own username")
    check("search.selection_bindings", "Resource.Id.select_all" in action,
          "the action mode supports select all")
    check("search.selection_bindings", "Resource.Id.action_download_batch" in action,
          "the action mode downloads the selected results")

    adapter = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Search", "SearchAdapter.cs")))
    check("search.selection_bindings", "ApplySelectionState" in adapter,
          "row bind applies the visible selection indicator")
    check("search.selection_bindings", "EnterSelectionMode" in adapter,
          "long press enters the selection action mode")
    check("search.selection_bindings", "fragment.IsSelectionMode" in adapter,
          "row taps toggle selection while the action mode is active")

    fragment = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Search", "SearchFragment.cs")))
    check("search.selection_lifecycle", "ClearSelectionAndFinishActionMode" in fragment,
          "selection is cleared when the tab/search is replaced")
    check("search.selection_lifecycle", "PruneSelectionToVisible" in fragment,
          "filtered-out selections are pruned from the current view")

    item_views = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Search", "SearchItemViewsUnified.cs")))
    for symbol in ("selectionIndicator", "ApplySelectionState", "check_circle", "search_result_selected_state"):
        check("search.selection_indicator", symbol in item_views, f"result rows expose {symbol}")

    maestro = os.path.join(REPO, "MaestroTests", "search_selection_sort.yaml")
    check("search.maestro", os.path.exists(maestro), "selection+sorting Maestro flow exists")
    if os.path.exists(maestro):
        flow = read(maestro)
        for symbol in ("longPressOn", "searchSortButton", "File name", "Download selected"):
            check("search.maestro", symbol in flow, f"Maestro flow exercises {symbol}")


# ---------------------------------------------------------------------------
# Phase 5: messaging / chatrooms / settings / login / users / remaining chrome
# ---------------------------------------------------------------------------

SETTINGS_CATEGORY_HEADERS = [
    ("h.account_connection", "section_account_connection"),
    ("h.downloads", "section_downloads"),
    # Title changed to "Uploads & sharing", but the row id stays the pre-existing "h.sharing"
    # so the sharing deep-link scroll anchor keeps working.
    ("h.sharing", "section_uploads_sharing"),
    ("h.search_wishlist", "section_search_wishlist"),
    ("h.notifications", "notification"),
    ("h.appearance", "section_appearance"),
    ("h.privacy", "section_privacy"),
    ("h.advanced", "section_advanced"),
    ("h.about", "about_title"),
]

# Every pre-existing row identity must survive the category regroup. Renaming one silently
# resets dependent-row wiring, live status tickers and the settings search index.
SETTINGS_REQUIRED_ROW_IDS = [
    "account.open_account", "account.privileges", "account.change_password",
    "account.edit_user_info",
    "network.listening", "network.port", "network.port_check", "network.upnp",
    "general.auto_away", "general.start_service", "general.startstop_service",
    "downloads.folder", "downloads.create_subfolders", "downloads.username_subfolders",
    "downloads.skip_subfolders_single", "downloads.override_incomplete",
    "downloads.incomplete_path", "downloads.incomplete_clear", "downloads.file_backed",
    "downloads.auto_clear", "downloads.auto_retry",
    "sharing.enable", "sharing.add_folder", "sharing.rescan_browse", "sharing.metered",
    "sharing.require_vpn", "sharing.auto_clear_uploads",
    "search.max_results", "search.smart_filter", "search.smart_filter_configure",
    "search.free_slots", "search.show_locked_search", "search.show_locked_browse",
    "search.remember_history", "search.clear_history",
    "general.notif_folder_complete", "general.notif_toast",
    "appearance.language", "appearance.day_night", "appearance.day_variant",
    "appearance.night_variant",
    "general.allow_private_invites", "general.remember_recent_users",
    "general.clear_recent_users",
    "network.download_speed", "network.upload_speed", "network.concurrent_downloads",
    "network.concurrent_uploads", "account.diagnostics", "account.import_data",
    "account.export_data", "account.force_filesystem", "account.restore_defaults",
    "about.open_about",
]

MESSAGE_COMPOSE_LAYOUTS = ["messages_inner_layout", "chatroom_inner_layout"]

# Files that intentionally keep literal colours: the notification builder uses the Android
# notification-compatible black/white defaults rather than app window tokens.
LITERAL_COLOR_ALLOWED_CS = {"MessageController.cs"}

PHASE5_CS_DIRS = ("Messages", "Chatroom", "Login", "Users", "Settings")

TRANSFER_ROW_LAYOUTS = [
    "transfer_single_item",
    "transfer_folder_item",
]

TRANSFER_STRINGS = [
    "transfer_row_actions",
    "transfer_percent_format",
    "transfers_tabs_description",
    "Downloads",
    "Uploads",
]

BROWSE_STRINGS = [
    "browse_header_stats",
    "browse_download_action",
    "browse_row_actions",
    "browse_folder_meta",
]


def check_transfers_ui() -> None:
    """Transfers: unified Downloads/Uploads tabs, compact rows, per-row actions."""
    ids = layout_ids("transfers")
    for required in ("transfersTabs", "transfersTabDownloads", "transfersTabUploads", "recyclerView1", "noTransfersView"):
        check("transfers.tabs", required in ids, f"transfers.xml keeps {required}")

    tab_labels = {
        "transfersTabDownloads": "@string/Downloads",
        "transfersTabUploads": "@string/Uploads",
    }
    for tab, expected_text in tab_labels.items():
        element = find_layout_element("transfers", tab)
        check(
            "transfers.tabs",
            element is not None
            and element.get("{http://schemas.android.com/apk/res/android}text") == expected_text,
            f"{tab} is clearly labelled {expected_text}",
        )
        check(
            "transfers.tabs",
            element is not None
            and element.get("{http://schemas.android.com/apk/res/android}layout_height") == "@dimen/ds_touch_target_min",
            f"{tab} is a 48dp touch target",
        )

    recycler = find_layout_element("transfers", "recyclerView1")
    check(
        "transfers.tabs",
        recycler is not None
        and recycler.get("{http://schemas.android.com/apk/res/android}layout_below") == "@id/transfersTabs",
        "the transfer list sits below the Downloads/Uploads tabs",
    )

    for name in TRANSFER_ROW_LAYOUTS:
        row_ids = layout_ids(name)
        for required in (
            "textViewStatusAdditionalInfo",
            "textViewSize",
            "textViewPercent",
            "textViewSpeed",
            "textViewTimeRemaining",
            "rowActionButton",
        ):
            check("transfers.rows", required in row_ids, f"{name} keeps {required}")

        percent = find_layout_element(name, "textViewPercent")
        check(
            "transfers.rows",
            percent is not None
            and percent.get("{http://schemas.android.com/apk/res/android}textColor") == "?attr/dsColorTextPrimary",
            f"{name} percent label uses a semantic text token",
        )

        action = find_layout_element(name, "rowActionButton")
        check(
            "transfers.rows",
            action is not None
            and action.get("{http://schemas.android.com/apk/res/android}contentDescription") == "@string/transfer_row_actions"
            and action.get("{http://schemas.android.com/apk/res/android}layout_width") == "@dimen/ds_touch_target_min"
            and action.get("{http://schemas.android.com/apk/res/android}layout_height") == "@dimen/ds_touch_target_min",
            f"{name} has an accessible 48dp per-row action",
        )

    item_views = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Transfers", "TransferItemViews.cs")))
    for symbol in (
        "TransferPresentation.ProgressPercent",
        "TransferPresentation.MeaningfulRemainingTime",
        "GetPercentTextView",
        "GetRowActionButton",
        "SetPercentText",
        "rowActionButton",
        "cachedDlColor",
    ):
        check("transfers.bindings", symbol in item_views, f"TransferItemViews uses {symbol}")

    adapters = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Transfers", "TransferAdapters.cs")))
    for symbol in ("WireRowAction", "GetRowActionButton", "ShowContextMenu", "setSelectedItem", "ToggleItemBatchSelect", "IsInBatchSelectMode"):
        check("transfers.bindings", symbol in adapters, f"TransferAdapters wires {symbol}")

    fragment = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Transfers", "TransfersFragment.cs")))
    for symbol in (
        "SetUploadsMode",
        "UpdateTransfersTabs",
        "transfersTabDownloads",
        "transfersTabUploads",
        "THROTTLE_PROGRESS_UPDATED_RATE",
        "TransferEventRouter.ProgressUpdated",
        "TransferDebouncer",
    ):
        check("transfers.bindings", symbol in fragment, f"TransfersFragment preserves {symbol}")

    strings = {element.get("name") for element in ET.parse(os.path.join(RES, "values", "strings.xml")).getroot().iter("string")}
    for required in TRANSFER_STRINGS:
        check("transfers.strings", required in strings, f"string {required} exists")

    # The segmented tab text tint must be a theme-token state list, not a literal.
    tint_path = os.path.join(RES, "color", "ds_segment_text.xml")
    check("transfers.tabs_token", os.path.exists(tint_path), "ds_segment_text.xml exists")
    if os.path.exists(tint_path):
        tint = read(tint_path)
        check(
            "transfers.tabs_token",
            "?attr/dsColorOnPrimaryContainer" in tint and "?attr/dsColorTextSecondary" in tint,
            "segmented tab text uses selected/unselected semantic tokens",
        )


def check_browse_ui() -> None:
    """Browse: username/status header, 48dp breadcrumbs, row metadata + download affordance."""
    ids = layout_ids("browse")
    for required in (
        "browseUserHeader",
        "browseUserHeaderName",
        "browseUserHeaderStatus",
        "recyclerViewHorizontalPath",
        "listViewDirectories",
        "noBrowseView",
        "filterText",
        "bsbutton",
    ):
        check("browse.header", required in ids, f"browse.xml keeps {required}")

    breadcrumb = find_layout_element("browse", "recyclerViewHorizontalPath")
    check(
        "browse.breadcrumb",
        breadcrumb is not None
        and breadcrumb.get("{http://schemas.android.com/apk/res/android}minHeight") == "@dimen/ds_touch_target_min",
        "breadcrumb strip reserves the 48dp touch height",
    )
    breadcrumb_node = find_layout_element("tree_path_item_view", "folderName")
    check(
        "browse.breadcrumb",
        breadcrumb_node is not None
        and breadcrumb_node.get("{http://schemas.android.com/apk/res/android}minHeight") == "@dimen/ds_touch_target_min"
        and breadcrumb_node.get("{http://schemas.android.com/apk/res/android}minWidth") == "@dimen/ds_touch_target_min"
        and breadcrumb_node.get("{http://schemas.android.com/apk/res/android}clickable") == "true",
        "breadcrumb nodes are 48dp clickable touch targets",
    )
    check(
        "browse.breadcrumb",
        "?attr/dsColor" in read(os.path.join(RES, "layout", "tree_path_item_view.xml")),
        "breadcrumb separator/ink uses a semantic token",
    )

    row_ids = layout_ids("browse_response_item")
    for required in (
        "displayName",
        "fileDetails",
        "folderIcon",
        "folderIndicator",
        "selectionCheckbox",
        "downloadButton",
        "actionContainer",
    ):
        check("browse.rows", required in row_ids, f"browse_response_item keeps {required}")

    root = find_layout_element("browse_response_item", "containingViewGroup")
    check(
        "browse.rows",
        root is not None
        and root.get("{http://schemas.android.com/apk/res/android}minHeight") == "@dimen/ds_touch_target_min",
        "browse rows are at least a 48dp touch target",
    )
    download = find_layout_element("browse_response_item", "downloadButton")
    check(
        "browse.rows",
        download is not None
        and download.get("{http://schemas.android.com/apk/res/android}contentDescription") == "@string/browse_download_action"
        and download.get("{http://schemas.android.com/apk/res/android}layout_width") == "@dimen/ds_touch_target_min"
        and download.get("{http://schemas.android.com/apk/res/android}layout_height") == "@dimen/ds_touch_target_min",
        "browse rows expose an accessible 48dp download action",
    )
    overflow = find_layout_element("browse_response_item", "folderIndicator")
    check(
        "browse.rows",
        overflow is not None
        and overflow.get("{http://schemas.android.com/apk/res/android}contentDescription") == "@string/browse_row_actions"
        and overflow.get("{http://schemas.android.com/apk/res/android}layout_height") == "@dimen/ds_touch_target_min",
        "folder overflow/peer actions are an accessible 48dp target",
    )
    check(
        "browse.rows",
        "?attr/dsColor" in read(os.path.join(RES, "layout", "browse_response_item.xml")),
        "browse row colours are semantic tokens",
    )

    adapter = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Browse", "BrowseAdapter.cs")))
    for symbol in ("DownloadButton", "OnDownloadButtonClick", "GetFolderDetailLine", "IsInBatchSelectMode", "SetSelectedBackground"):
        check("browse.bindings", symbol in adapter, f"BrowseAdapter wires {symbol}")

    fragment = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Browse", "BrowseFragment.cs")))
    for symbol in (
        "OnDownloadButtonClick",
        "DownloadUserFilesEntry(false, false, new List<DataItem>",
        "EnqueueFilesFireAndForget",
        "UpdateBrowseHeader",
        "GoUpDirectory",
        "ToggleBatchSelect",
        "BrowseActionMode",
    ):
        check("browse.bindings", symbol in fragment, f"BrowseFragment preserves {symbol}")
    for action in ("ShowFolderContextMenu", "ShowFileContextMenu", "QueueFolderAsPaused", "QueueSelectedAsPaused", "CopyURL"):
        check("browse.peer_actions", action in fragment, f"browse keeps existing action {action}")

    strings = {element.get("name") for element in ET.parse(os.path.join(RES, "values", "strings.xml")).getroot().iter("string")}
    for required in BROWSE_STRINGS:
        check("browse.strings", required in strings, f"string {required} exists")



def check_phase5_settings() -> None:
    """Settings keeps every row identity and preference wiring, regrouped into categories."""
    catalog = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Settings", "Rows", "SettingsCatalog.cs")))
    headers = re.findall(
        r'new\s+HeaderRow\s*\{\s*Id\s*=\s*"([^"]+)"\s*,\s*TitleRes\s*=\s*Resource\.String\.([A-Za-z0-9_]+)',
        catalog,
    )
    expected_ids = [row_id for row_id, _title in SETTINGS_CATEGORY_HEADERS]
    actual_ids = [row_id for row_id, _title in headers]
    check(
        "settings.categories",
        actual_ids == expected_ids,
        f"settings categories are in the requested order (got {actual_ids})",
    )
    for row_id, title in headers:
        expected_title = dict(SETTINGS_CATEGORY_HEADERS).get(row_id)
        check(
            "settings.categories",
            expected_title == title,
            f"{row_id} uses the expected section string ({title})",
        )

    row_ids = re.findall(r'\bId\s*=\s*"([^"]+)"\s*,', catalog)
    duplicates = sorted({row_id for row_id in row_ids if row_ids.count(row_id) > 1})
    check("settings.rows", not duplicates, f"no duplicate static row id{'' if not duplicates else f's: {duplicates}'}")
    missing = sorted(set(SETTINGS_REQUIRED_ROW_IDS) - set(row_ids))
    check(
        "settings.rows",
        not missing,
        f"all {len(SETTINGS_REQUIRED_ROW_IDS)} pre-existing row ids survive the regroup"
        + (f" (missing {missing})" if missing else ""),
    )

    strings = {element.get("name") for element in ET.parse(os.path.join(RES, "values", "strings.xml")).getroot().iter("string")}
    for _row_id, title in SETTINGS_CATEGORY_HEADERS:
        check("settings.categories", title in strings, f"section string {title} exists")
    for key in ("setting_subtitle_subfolders", "setting_subtitle_upnp", "setting_subtitle_metered",
                "setting_subtitle_vpn", "setting_subtitle_auto_away", "setting_subtitle_concurrent",
                "setting_subtitle_day_night", "setting_subtitle_about"):
        check("settings.subtitles", key in strings, f"concise subtitle {key} exists")

    # Dependencies, live status providers and SAF-backed pickers are behaviour, not decoration.
    for dep in (
        '"downloads.incomplete_path"',
        '"search.smart_filter_configure"',
        '"sharing.add_folder"',
        '"sharing.rescan_browse"',
        '"network.port"',
        '"network.port_check"',
        '"network.upnp"',
        '"general.startstop_service"',
    ):
        check("settings.dependencies", dep in catalog, f"dependent row {dep} is still declared")
    check("settings.status", catalog.count("StatusProvider") >= 3, "sharing / UPnP / privileges status tickers remain")
    for symbol in ("LaunchDownloadFolderPicker", "LaunchIncompleteFolderPicker", "LaunchAddSharedFolderPicker"):
        check("settings.saf", f"h.{symbol}" in catalog or f"h, r) => h.{symbol}" in catalog,
              f"SAF picker {symbol} remains wired")
    check("settings.about", "AboutBottomSheet.Show" in catalog, "About category opens the existing About sheet")

    host = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Settings", "SettingsActivity.Host.cs")))
    check(
        "settings.scroll",
        'IndexOfRow("h.sharing")' in host,
        "the sharing deep-link scrolls to the stable h.sharing anchor (retitled Uploads & sharing)",
    )

    for name in ("row_setting_action", "row_setting_button_card", "row_setting_button_pair",
                 "row_setting_nav", "row_setting_shared_folder", "row_setting_toggle",
                 "row_setting_value"):
        path = os.path.join(RES, "layout", f"{name}.xml")
        root = ET.parse(path).getroot()
        heights = []
        for element in root.iter():
            value = element.get("{http://schemas.android.com/apk/res/android}minHeight") or ""
            if value.endswith("dp") and value[:-2].isdigit():
                heights.append(int(value[:-2]))
            elif value == "@dimen/ds_touch_target_min":
                heights.append(48)
        check("settings.touch", not heights or min(heights) >= 48,
              f"{name} rows/controls are at least 48dp")


def check_phase5_messages_chatrooms() -> None:
    """Messages and chatrooms share one themed, accessible, IME-aware visual system."""
    ids = layout_ids("messages_overview")
    for required in ("recyclerViewOverview", "noMessagesView", "fabNewMessage"):
        check("messages.overview", required in ids, f"messages_overview keeps {required}")
    fab = find_layout_element("messages_overview", "fabNewMessage")
    check(
        "messages.overview",
        fab is not None
        and fab.get("{http://schemas.android.com/apk/res/android}contentDescription") == "@string/msg_user",
        "new-message FAB keeps an accessible label",
    )
    check(
        "messages.overview",
        "?attr/dsColorPrimary" in read(os.path.join(RES, "layout", "messages_overview.xml"))
        and "?attr/dsColorOnPrimary" in read(os.path.join(RES, "layout", "messages_overview.xml")),
        "new-message FAB uses primary/on-primary tokens (not white-on-white in ModernLight)",
    )

    item_ids = layout_ids("message_overview_item")
    for required in ("username", "statusIndicator", "dateTimeAgo", "unreadBadge", "message", "selectionCheckbox"):
        check("messages.list", required in item_ids, f"message_overview_item keeps {required}")
    item_root = ET.parse(os.path.join(RES, "layout", "message_overview_item.xml")).getroot()
    check(
        "messages.list",
        item_root.get("{http://schemas.android.com/apk/res/android}minHeight") == "@dimen/ds_touch_target_min",
        "conversation rows are a 48dp target",
    )
    check(
        "messages.list",
        not re.search(r"#[0-9a-fA-F]{6,8}", read(os.path.join(RES, "layout", "message_overview_item.xml"))),
        "conversation row colours are theme driven",
    )

    overview_adapter = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Messages", "MessagesOverviewRecyclerAdapter.cs")))
    check("messages.list", "ContentDescription = username" in overview_adapter,
          "conversation rows expose an accessible peer summary")
    check("messages.list", "unread_count" in overview_adapter,
          "the accessible summary includes the unread count")
    check("messages.list", "ContentDescription = SeekerApplication.GetString(statusLabelRes)" in overview_adapter,
          "peer presence has an accessible label")
    check("messages.list", "dsColorError" in overview_adapter and "#ed4a51" not in overview_adapter,
          "swipe-to-delete background is a semantic error token")

    for name in ("messages_inner_item_fromMe", "messages_inner_item_toMe", "group_messages_inner_item_toMe"):
        content = read(os.path.join(RES, "layout", f"{name}.xml"))
        check("messages.bubbles", "?attr/dsColor" in content, f"{name} bubble colours come from tokens")
        check("messages.bubbles", not re.search(r"#[0-9a-fA-F]{6,8}", content), f"{name} has no literal UI colours")
        for required in ("text_gchat_message", "text_gchat_timestamp"):
            check("messages.bubbles", required in content, f"{name} keeps {required}")

    inner_adapter = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Messages", "MessagesInnerRecyclerAdapter.cs")))
    for token in ("dsColorPrimaryContainer", "dsColorOnPrimaryContainer", "dsColorError", "dsColorOnError"):
        check("messages.bubbles", token in inner_adapter, f"sent/failed bubbles use {token}")

    for name in MESSAGE_COMPOSE_LAYOUTS:
        edit = find_layout_element(name, "edit_gchat_message")
        send = find_layout_element(name, "button_gchat_send")
        check(
            "messages.compose",
            edit is not None
            and edit.get("{http://schemas.android.com/apk/res/android}layout_height") == "wrap_content"
            and edit.get("{http://schemas.android.com/apk/res/android}minHeight") == "@dimen/ds_touch_target_min"
            and edit.get("{http://schemas.android.com/apk/res/android}imeOptions") == "actionSend",
            f"{name} message field wraps to content with a 48dp minimum (large font safe)",
        )
        check(
            "messages.compose",
            send is not None
            and send.get("{http://schemas.android.com/apk/res/android}layout_width") == "@dimen/ds_touch_target_min"
            and send.get("{http://schemas.android.com/apk/res/android}layout_height") == "@dimen/ds_touch_target_min"
            and send.get("{http://schemas.android.com/apk/res/android}contentDescription") == "@string/send_message",
            f"{name} send button is an accessible 48dp target",
        )

    insets = read(os.path.join(REPO, "Seeker", "Helpers", "BottomOnlyInsetsListener.cs"))
    check("messages.ime", "WindowInsetsCompat.Type.Ime()" in insets,
          "the shared bottom inset listener accounts for the keyboard")
    for path, symbol in (
        ("Seeker/Messages/MessagesInnerFragment.cs", "BottomOnlyInsetsListener"),
        ("Seeker/Chatroom/ChatroomInnerFragment.cs", "BottomOnlyInsetsListener"),
    ):
        source = strip_cs_comments(read(os.path.join(REPO, path)))
        check("messages.ime", symbol in source, f"{path} applies the IME-aware insets listener")
    for path in ("Seeker/Messages/MessagesActivity.cs", "Seeker/Chatroom/ChatroomActivity.cs"):
        source = read(os.path.join(REPO, path))
        check("messages.ime", "WindowSoftInputMode = Android.Views.SoftInput.AdjustResize" in source,
              f"{os.path.basename(path)} resizes for the keyboard")

    # Chatrooms keep join/create/leave/user-list/room actions while reusing the same bubbles.
    menu = ET.parse(os.path.join(RES, "menu", "chatroom_overview_list_menu.xml")).getroot()
    menu_ids = [android_id(element) for element in menu.iter() if android_id(element)]
    for required in ("create_room_action", "refresh_room_list_action"):
        check("chatrooms.actions", required in menu_ids, f"chatroom list keeps {required}")
    activity = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Chatroom", "ChatroomActivity.cs")))
    check("chatrooms.actions", "ShowEditCreateChatroomDialog" in activity and "CreateRoomApi" in activity,
          "chatroom creation is preserved")
    inner = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Chatroom", "ChatroomInnerFragment.cs")))
    for symbol in ("JoinRoomApi", "ShowUserListDialog", "DropMembershipOrOwnershipApi", "ShowAllTickersDialog"):
        check("chatrooms.actions", symbol in inner, f"chatroom keeps {symbol}")
    adapters = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Chatroom", "ChatroomAdapters.cs")))
    for symbol in ("leaveRoom", "UnreadRooms", "ContentDescription"):
        check("chatrooms.actions", symbol in adapters, f"chatroom adapter keeps {symbol}")
    joined = read(os.path.join(RES, "layout", "chatroom_overview_joined_item.xml"))
    check("chatrooms.actions", "@string/leave_room_desc" in joined, "leave-room action has an accessible label")
    for name in ("chatroom_overview_item", "chatroom_overview_joined_item", "chatroom_overview_category_item",
                 "room_user_list_item", "chatroom_connect_disconnect_item"):
        root = ET.parse(os.path.join(RES, "layout", f"{name}.xml")).getroot()
        check(
            "chatrooms.rows",
            root.get("{http://schemas.android.com/apk/res/android}minHeight") == "@dimen/ds_touch_target_min",
            f"{name} is a 48dp row/target",
        )
    check(
        "chatrooms.rows",
        "?attr/dsColorOnPrimary" in read(os.path.join(RES, "layout", "chatroom_overview_category_item.xml")),
        "category header text follows the accent/on-accent tokens",
    )


def check_phase5_hardcoded_ui() -> None:
    """No scoped layout keeps a literal black/white/accent; meaningful notification colours stay."""
    literals = []
    for name in sorted(os.listdir(os.path.join(RES, "layout"))):
        if not name.endswith(".xml"):
            continue
        content = read(os.path.join(RES, "layout", name))
        for match in re.findall(r"#[0-9a-fA-F]{3,8}", content):
            literals.append(f"{name}:{match}")
    check(
        "chrome.literals",
        not literals,
        "all layout colours are theme driven"
        + (f" (literals {literals[:12]})" if literals else ""),
    )

    offenders = []
    for directory in PHASE5_CS_DIRS:
        root_dir = os.path.join(REPO, "Seeker", directory)
        for current, _dirs, files in os.walk(root_dir):
            if os.sep + "obj" in current or os.sep + "bin" in current:
                continue
            for name in files:
                if not name.endswith(".cs") or name in LITERAL_COLOR_ALLOWED_CS:
                    continue
                source = strip_cs_comments(read(os.path.join(current, name)))
                if re.search(r"Color\.ParseColor\(\s*\"#", source):
                    offenders.append(f"{directory}/{name}:ParseColor")
                if re.search(r"\bColor\.(White|Black|Red|Gray)\b", source):
                    offenders.append(f"{directory}/{name}:named")
    check(
        "chrome.literals",
        not offenders,
        "scoped C# uses semantic attributes instead of literal UI colours"
        + (f" (offenders {offenders[:12]})" if offenders else ""),
    )

    # Coverage for the other remaining surfaces touched in this phase.
    for name in ("downloaddialog", "wishlist_header"):
        content = read(os.path.join(RES, "layout", f"{name}.xml"))
        check("chrome.literals", "dsColorChromeForeground" in content or "dsColorOnPrimary" in content,
              f"{name} foregrounds follow the chrome/on-primary tokens")
    for name in ("browse", "searches"):
        content = read(os.path.join(RES, "layout", f"{name}.xml"))
        check("chrome.literals", "?attr/dsColorPrimary" in content and "?attr/dsColorOnPrimary" in content,
              f"{name} FAB uses primary/on-primary tokens")


def check_phase5_cross_surface_state() -> None:
    """Browse status is honest/cached, tap positions are bounded, ETA state is explicit."""
    fragment = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Browse", "BrowseFragment.cs")))
    for symbol in (
        "GetCachedUserPresenceText",
        "CommonState.UserList",
        "GetStatusFromItem",
        "browse_header_status",
    ):
        check("browse.status", symbol in fragment, f"browse header uses {symbol}")
    check(
        "browse.status",
        "DoesNotExist" in fragment,
        "browse header distinguishes a cached offline user from an unknown status",
    )

    adapter = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Browse", "BrowseAdapter.cs")))
    check("browse.positions", "IsValidTapPosition" in adapter, "browse tap handlers validate the position")
    check(
        "browse.positions",
        re.search(r"position\s*<\s*localDataSet\.Count", adapter) is not None,
        "browse tap positions are checked against the current adapter count",
    )

    transfer_views = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Transfers", "TransferItemViews.cs")))
    transfers_fragment = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Transfers", "TransfersFragment.cs")))
    # The ETA must be gated by the display preference AND the row's real transfer state. Asserting
    # the old `MeaningfulRemainingTime(item.GetRemainingTime(), showTimeRemaining)` call rewarded
    # passing the UI preference as the in-progress flag; check the state is now propagated instead.
    eta_sites = [
        ("TransferItemViews", re.findall(r"SetTimeRemainingText\(([^;]*?)\);", transfer_views)),
        ("TransfersFragment", re.findall(r"SetTimeRemainingText\(([^;]*?)\);", transfers_fragment)),
    ]
    for label, calls in eta_sites:
        check("transfers.eta", bool(calls), f"{label} still binds the ETA formatter")
        for call in calls:
            args = [arg.strip() for arg in call.split(",")]
            check(
                "transfers.eta",
                len(args) == 6
                and re.search("state", args[4], re.IGNORECASE) is not None
                and "TimeRemaining" in args[5],
                f"{label} passes the row's transfer state separately from the display preference ({call.strip()})",
            )
    check(
        "transfers.eta",
        "ShouldQueryEta(showTimeRemaining, state)" in transfer_views,
        "the ETA estimate is not even queried unless the preference is on and the row is in progress",
    )
    check(
        "transfers.eta",
        "MeaningfulRemainingTime(item.GetRemainingTime(), showTimeRemaining, state)" in transfer_views,
        "MeaningfulRemainingTime receives both the preference and the row state",
    )

    pickers = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Settings", "Rows", "SettingPickers.cs")))
    for symbol in ("follow_system", "always_light", "always_dark"):
        check("settings.theme", f"Resource.String.{symbol}" in pickers, f"theme selector offers {symbol}")
    for value in ("-1", "1", "2"):
        check("settings.theme", re.search(r",\s*%s\)" % value, pickers) is not None, f"theme option value {value} is preserved")


def check_font_scale_contracts() -> None:
    """Touched rows keep primary >= 14sp and metadata >= 12sp, and text inputs grow."""
    for name in SEARCH_RESULT_LAYOUTS:
        primary = text_size_sp(name, "folderNameTextView")
        metadata = text_size_sp(name, "resultMetaTextView")
        check("font.search", primary is not None and primary >= 14,
              f"{name} primary identity is >= 14sp (got {primary})")
        check("font.search", metadata is not None and metadata >= 12,
              f"{name} metadata line is >= 12sp (got {metadata})")

    transfer_primary = {
        "transfer_single_item": "textViewFileName",
        "transfer_folder_item": "textViewFoldername",
    }
    transfer_metadata = {
        "transfer_single_item": [
            "textViewUser", "textViewStatusAdditionalInfo", "textViewSize",
            "textViewPercent", "textViewSpeed", "textViewTimeRemaining",
            "textViewTimeRemainingSeparator",
        ],
        "transfer_folder_item": [
            "textViewUser", "textViewStatusAdditionalInfo", "textViewSize",
            "textViewPercent", "textViewSpeed", "textViewTimeRemaining",
            "textViewTimeRemainingSeparator", "filesRemaining", "currentFile",
        ],
    }
    for name, element_id in transfer_primary.items():
        size = text_size_sp(name, element_id)
        check("font.transfers", size is not None and size >= 14,
              f"{name}.{element_id} primary label is >= 14sp (got {size})")
    for name, ids in transfer_metadata.items():
        for element_id in ids:
            size = text_size_sp(name, element_id)
            check("font.transfers", size is not None and size >= 12,
                  f"{name}.{element_id} metadata is >= 12sp (got {size})")

    timestamp_ids = {
        "messages_inner_item_fromMe": "text_gchat_timestamp_me",
        "messages_inner_item_toMe": "text_gchat_timestamp_other",
        "group_messages_inner_item_toMe": "text_gchat_timestamp_other",
        "message_overview_item": "dateTimeAgo",
    }
    for name, element_id in timestamp_ids.items():
        size = text_size_sp(name, element_id)
        check("font.messages", size is not None and size >= 12,
              f"{name}.{element_id} timestamp is >= 12sp (got {size})")
    message_size = text_size_sp("message_overview_item", "message")
    check("font.messages", message_size is not None and message_size >= 14,
          f"message preview is >= 14sp primary text (got {message_size})")

    browse_primary = text_size_sp("browse_response_item", "displayName")
    browse_metadata = text_size_sp("browse_response_item", "fileDetails")
    check("font.browse", browse_primary is not None and browse_primary >= 14,
          f"browse row primary label is >= 14sp (got {browse_primary})")
    check("font.browse", browse_metadata is not None and browse_metadata >= 12,
          f"browse row metadata is >= 12sp (got {browse_metadata})")

    # A vertical LinearLayout child with weight must use 0dp, not fill_parent.
    recycler = find_layout_element("messages_inner_layout", "recycler_messages")
    check(
        "font.messages",
        recycler is not None
        and recycler.get("{http://schemas.android.com/apk/res/android}layout_height") == "0dp"
        and recycler.get("{http://schemas.android.com/apk/res/android}layout_weight") == "1",
        "messages_inner_layout recycler is 0dp + weight 1 above the compose bar",
    )


def check_selection_lifecycle_and_actions() -> None:
    """Search mode is owned by the Search destination/view and contextual actions stay contextual."""
    activity = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Main", "MainActivity.cs")))
    page_selected = method_body(activity, "public void OnPagerPageSelected(int position)")
    check("search.lifecycle",
          "position != MainNavigation.SearchPage" in page_selected
          and "ClearSelectionAndFinishActionMode" in page_selected,
          "leaving Search (including account/deep-link page changes) finishes search selection")
    for existing in ("TransfersActionMode", "BrowseActionMode", "MessagesOverviewActionMode"):
        check("search.lifecycle", existing in page_selected,
              f"page changes still finish {existing}")

    fragment = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Search", "SearchFragment.cs")))
    destroy = method_body(fragment, "public override void OnDestroyView()")
    check("search.lifecycle",
          "ClearSelectionAndFinishActionMode" in destroy
          and "searchSelection.Clear" in destroy
          and "searchActionMode = null" in destroy,
          "OnDestroyView drops the selection and the action-mode reference")
    sort_body = method_body(fragment, "private void UpdateSortAvailability(")
    check("search.lifecycle",
          "ClearSelectionAndFinishActionMode" not in sort_body
          and "searchSelection.Clear" not in sort_body,
          "ordinary sorting never clears a live selection")
    stream_body = method_body(fragment, "private static void ApplySearchResults(")
    check("search.lifecycle",
          "ClearSelectionAndFinishActionMode" not in stream_body
          and "searchSelection.Clear" not in stream_body,
          "ordinary streaming additions never clear a live selection")
    clear_tab = method_body(fragment, "private static void ClearTab(")
    check("search.lifecycle",
          "ClearSelectionAndFinishActionMode" in clear_tab,
          "a new search still clears identities that belong to the replaced result set")

    item_views = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Search", "SearchItemViewsUnified.cs")))
    check("search.contextual_download",
          "downloadActionButton.Visibility = selectionMode ? ViewStates.Gone : ViewStates.Visible" in item_views,
          "the per-row download action hides while the selection action mode is active")
    adapter = strip_cs_comments(read(os.path.join(REPO, "Seeker", "Search", "SearchAdapter.cs")))
    download_click = method_body(adapter, "private void UnifiedDownloadClick(")
    check("search.contextual_download",
          "fragment.IsSelectionMode" in download_click
          and "ToggleSelectionAt" in download_click
          and "ShowDownloadDialog" in download_click,
          "a stale contextual download tap toggles selection instead of opening the dialog")


def check_maestro_regression_flows() -> None:
    """Maestro coverage stays on supported commands and current ids."""
    maestro_dir = os.path.join(REPO, "MaestroTests")
    flows = {}
    for current, _dirs, files in os.walk(maestro_dir):
        for name in files:
            if not name.endswith(".yaml"):
                continue
            path = os.path.join(current, name)
            flows[os.path.relpath(path, maestro_dir)] = read(path)

    stale = [name for name, text in flows.items() if "navigation_home" in text]
    check("maestro.ids", not stale, f"no flow still taps navigation_home (got {stale})")
    theme_flow = flows.get("common/change_theme.yaml", "")
    check("maestro.theme",
          'text: "Modern"' in theme_flow and "text1" not in theme_flow,
          "theme variants are selected by label, not the old list index")

    core = flows.get("screenshots_core.yaml", "")
    for surface in ("_settings", "_chatrooms", "_userinfo"):
        check("maestro.screenshots", surface in core,
              f"screenshots_core captures the {surface[1:]} surface")
    for destination in ("navigation_search", "navigation_transfers", "navigation_browse", "navigation_messages"):
        check("maestro.screenshots", destination in core,
              f"screenshots_core covers {destination}")

    follow = flows.get("follow_system_theme.yaml", "")
    # Whether setDarkMode / assertDarkMode / assertLightMode exist is *not* decided
    # here - `check_maestro_command_schema` validates them against the checked-in copy
    # of the official command vocabulary (MaestroTests/schema/maestro_commands.json,
    # which records where it was taken from). This check only asserts that the flow
    # still exercises follow-system in both appearances.
    check("maestro.follow_system",
          "Follow system" in follow and "setDarkMode" in follow and "assertDarkMode" in follow
          and "assertLightMode" in follow,
          "follow-system flow switches and asserts both device appearances")

    recreation = flows.get("recreation_navigation.yaml", "")
    check("maestro.recreation",
          "setOrientation" in recreation and "launchApp" in recreation
          and "navigation_" in recreation,
          "recreation flow restores a tab after orientation/activity recreation")

    large = flows.get("large_font_sample.yaml", "")
    check("maestro.large_font", bool(large), "large-font sample flow exists")
    check("maestro.large_font",
          "setFontScale" not in large,
          "large-font flow does not invent a font-scale command (none exists)")
    check("maestro.large_font",
          "takeScreenshot" in large and "optional_login" in large,
          "large-font flow uses supported commands and the shared mock login path")
    check("maestro.large_font",
          "font_scale" in large,
          "large-font flow documents the external adb font_scale precondition")


def maestro_flows() -> dict[str, str]:
    """{relative path: text} for every Maestro flow in the repository."""
    maestro_dir = os.path.join(REPO, "MaestroTests")
    flows: dict[str, str] = {}
    for current, _dirs, files in os.walk(maestro_dir):
        for name in files:
            if not name.endswith(".yaml"):
                continue
            path = os.path.join(current, name)
            flows[os.path.relpath(path, maestro_dir)] = read(path)
    return flows


def _walk_maestro_steps(steps) -> list:
    """Yield every command step, including the ones nested in runFlow/retry blocks.

    A step is normally a one-key mapping (``- tapOn: …``), but a command that takes no
    argument is written as a bare scalar (``- back``) and PyYAML hands that back as a
    plain string - both forms have to be validated.
    """
    out = []
    if not isinstance(steps, list):
        return out
    for step in steps:
        if isinstance(step, str):
            out.append(step)
            continue
        if not isinstance(step, dict):
            continue
        out.append(step)
        for key in ("runFlow", "retry"):
            block = step.get(key)
            if isinstance(block, dict) and isinstance(block.get("commands"), list):
                out.extend(_walk_maestro_steps(block["commands"]))
    return out


def check_maestro_command_schema() -> None:
    """Validate the authored flows against the official Maestro command vocabulary.

    This is a *static* check: it parses the YAML and checks every command key, and the
    values of the commands whose grammar is closed, against
    MaestroTests/schema/maestro_commands.json. That file is a mechanical copy of
    YamlFluentCommand (the class Maestro's YAML parser maps one step to) plus the
    documented value enums, and it records the upstream URLs and retrieval date.

    It proves the flows contain no invented command (`setFontScale`, `setTheme`, ...).
    It does NOT prove the flows pass on a device - that needs a real run, see
    Misc/run_maestro_theme_matrix.sh.
    """
    schema_path = os.path.join(REPO, "MaestroTests", "schema", "maestro_commands.json")
    if not os.path.exists(schema_path):
        fail("maestro.schema", "MaestroTests/schema/maestro_commands.json is missing")
        return
    schema = json.loads(read(schema_path))

    provenance = schema.get("provenance", {})
    for key in ("retrieved", "upstreamFileUrl", "docsIndexUrl", "docsCommandsUrl"):
        check("maestro.schema", bool(provenance.get(key)), f"schema provenance records {key}")
    check(
        "maestro.schema",
        "YamlFluentCommand.kt" in provenance.get("upstreamFile", ""),
        "schema is derived from Maestro's own YAML command class",
    )

    commands = set(schema.get("commands", []))
    check("maestro.schema", len(commands) >= 50, f"schema carries the full command vocabulary ({len(commands)} keys)")
    required = (
        "tapOn", "longPressOn", "assertVisible", "assertNotVisible", "back", "inputText",
        "pressKey", "runFlow", "takeScreenshot", "scrollUntilVisible", "launchApp",
        "waitForAnimationToEnd", "setOrientation", "setDarkMode", "assertDarkMode",
        "assertLightMode",
    )
    missing = [name for name in required if name not in commands]
    check("maestro.schema", not missing, f"schema knows every command the flows rely on (missing {missing})")

    not_commands = schema.get("notCommands", {})
    for fake in ("setFontScale", "setTheme"):
        check("maestro.schema", fake in not_commands, f"schema documents {fake} as not a Maestro command")
        check("maestro.schema", fake not in commands, f"{fake} is not in the command vocabulary")

    grammar = schema.get("valueGrammar", {})
    no_argument = set(schema.get("noArgumentCommands", []))
    bare_allowed = set(schema.get("bareFormAllowed", {}).get("commands", []))
    check(
        "maestro.schema",
        no_argument.issubset(bare_allowed),
        "every no-argument command is also allowed in bare form",
    )

    flows = maestro_flows()
    check("maestro.schema", bool(flows), f"found {len(flows)} Maestro flows to validate")

    try:
        import yaml  # noqa: PLC0415 - declared tool dependency, checked explicitly
    except ImportError:
        fail(
            "maestro.schema",
            "PyYAML is required for the Maestro command-schema check; install it with "
            "`python3 -m pip install 'PyYAML>=6'` (absence is a failure, not a skip)",
        )
        return

    unknown = []
    bad_values = []
    missing_flow_files = []
    unparsed = []
    bare_commands = set()
    app_ids = set()

    for name, text in sorted(flows.items()):
        flow_path = os.path.join(REPO, "MaestroTests", name)
        try:
            documents = [document for document in yaml.safe_load_all(text) if document is not None]
        except Exception as error:  # noqa: BLE001 - any YAML error must fail the check
            unparsed.append(f"{name}: {error}")
            continue

        header = documents[0] if documents and isinstance(documents[0], dict) else {}
        if header.get("appId"):
            app_ids.add(header["appId"])
        steps = []
        for document in documents:
            if isinstance(document, list):
                steps.extend(document)

        for step in _walk_maestro_steps(steps):
            if isinstance(step, str):
                if step not in commands:
                    unknown.append(f"{name}:{step}")
                elif step not in bare_allowed:
                    bad_values.append(f"{name}:{step} needs an explicit value")
                bare_commands.add(step)
                continue
            if len(step) != 1:
                unknown.append(f"{name}:ambiguous step {sorted(step)}")
                continue
            key, value = next(iter(step.items()))
            if key not in commands:
                unknown.append(f"{name}:{key}")
                continue
            if key in grammar:
                allowed = [item.lower() for item in grammar[key]]
                if not isinstance(value, str) or value.lower() not in allowed:
                    bad_values.append(f"{name}:{key}={value!r}")
            if key in no_argument and value not in (None, {}):
                bad_values.append(f"{name}:{key} takes no argument (got {value!r})")
            if key == "takeScreenshot":
                path_value = value if isinstance(value, str) else (value or {}).get("path")
                if not path_value:
                    bad_values.append(f"{name}:takeScreenshot without a path")
            if key in ("runFlow", "retry"):
                target = value if isinstance(value, str) else (value or {}).get("file")
                if target and not target.startswith("http"):
                    resolved = os.path.normpath(os.path.join(os.path.dirname(flow_path), target))
                    if not os.path.exists(resolved):
                        missing_flow_files.append(f"{name} -> {target}")

    check("maestro.schema", not unparsed, f"every flow is valid YAML ({unparsed[:5]})")
    check("maestro.schema", not unknown, f"every command is a real Maestro command ({unknown[:10]})")
    check("maestro.schema", not bad_values, f"command values match the documented grammar ({bad_values[:10]})")
    check("maestro.schema", not missing_flow_files, f"every runFlow target exists ({missing_flow_files[:10]})")
    check(
        "maestro.schema",
        {"assertDarkMode", "assertLightMode"}.issubset(bare_commands),
        f"the no-argument assertions are parsed as bare commands (got {sorted(bare_commands)})",
    )

    manifest = read(os.path.join(REPO, "Seeker", "Properties", "AndroidManifest.xml"))
    match = re.search(r'package="([A-Za-z0-9_.]+)"', manifest)
    manifest_package = match.group(1) if match else ""
    wrong = sorted(app_id for app_id in app_ids if app_id != manifest_package)
    check(
        "maestro.schema",
        bool(app_ids) and not wrong,
        f"every flow targets the manifest package {manifest_package}" + (f" (got {wrong})" if wrong else ""),
    )

    # The external-precondition path has to exist and stay documented: it is the way to
    # run the appearance matrix without depending on an in-flow command.
    script = os.path.join(REPO, "Misc", "run_maestro_theme_matrix.sh")
    check("maestro.precondition", os.path.exists(script), "Misc/run_maestro_theme_matrix.sh exists")
    if os.path.exists(script):
        body = read(script)
        check("maestro.precondition", "cmd uimode night" in body,
              "theme matrix uses the documented `adb shell cmd uimode night yes|no` precondition")
        check("maestro.precondition", "maestro test" in body,
              "theme matrix calls the Maestro CLI")
        check("maestro.precondition", os.access(script, os.X_OK),
              "theme matrix script is executable")


def check_scoped_styles_exist() -> None:
    """Styles referenced from layouts (android:theme=…) must exist."""
    missing = []
    for root_dir, _dirs, files in os.walk(RES):
        for name in files:
            if not name.endswith(".xml"):
                continue
            path = os.path.join(root_dir, name)
            if os.sep + "values" in root_dir:
                continue
            text = read(path)
            for reference in re.findall(r"@style/([A-Za-z0-9_.]+)", text):
                if reference.startswith("Widget.") or reference.startswith("Theme.") or reference.startswith("TextAppearance."):
                    continue
                if reference.startswith("ShapeAppearance"):
                    continue
                if reference not in {n for (_c, n) in STYLES}:
                    missing.append(f"{os.path.relpath(path, REPO)} -> @style/{reference}")
    check("styles.referenced", not missing, f"layout referenced styles exist" + (f" (missing {missing[:10]})" if missing else ""))


declared_attrs_snapshot: dict[str, str] = {}


def main() -> int:
    global declared_attrs_snapshot
    declared_attrs_snapshot = declared_attrs()

    check_xml_parses()
    check_style_duplicates()
    check_attr_coverage(declared_attrs_snapshot)
    check_theme_attr_cycles()
    check_legacy_theme_tokens()
    check_tokens_day_night()
    check_theme_helper()
    check_preferences()
    check_theme_switch_isolation()
    check_overlays_and_chrome()
    check_navigation_shell()
    check_search_ui()
    check_search_result_group_contract()
    check_phase6_search_selection_and_sorting()
    check_transfers_ui()
    check_browse_ui()
    check_phase5_settings()
    check_phase5_messages_chatrooms()
    check_phase5_hardcoded_ui()
    check_phase5_cross_surface_state()
    check_font_scale_contracts()
    check_selection_lifecycle_and_actions()
    check_maestro_regression_flows()
    check_maestro_command_schema()
    check_drawables()
    check_color_parity()
    check_semantic_color_usage()
    check_color_contrast_contracts()
    check_dimens()
    check_typography()
    check_code_resource_ids()
    check_scoped_styles_exist()

    verbose = "-v" in sys.argv or "--verbose" in sys.argv
    if verbose:
        for line in PASSED:
            print("PASS " + line)
    for line in FAILURES:
        print("FAIL " + line)

    print(f"\n{len(PASSED)} checks passed, {len(FAILURES)} failed")
    return 1 if FAILURES else 0


if __name__ == "__main__":
    sys.exit(main())
