using System;
using System.Collections.Generic;
using System.Text;

//ported this to c# from Java
namespace AndriodApp1
{
    /**
	 * HTML Un-escaper by Nick Frolov.
	 * <p>
	 * With improvement suggested by Axel Dörfler.
	 * <p>
	 * Replaced character map with HTML5 characters from<a href="https://www.w3schools.com/charsets/ref_html_entities_a.asp">
	 * https://www.w3schools.com/charsets/ref_html_entities_a.asp</a>
	 *
	 * @author Nick Frolov, Mark Jeronimus
	 */
    // Created 2020-06-22
    public class HTMLUtilities
    {
        // Tables optimized for smallest .class size (without resorting to compression)
        private static readonly String[] NAMES =
                {"excl", "quot", "num", "dollar", "percnt", "amp", "apos", "lpar", "rpar", "ast", "midast", "plus", "comma",
             "period", "sol", "colon", "semi", "lt", "equals", "GT", "quest", "commat", "lbrack", "lsqb", "bsol",
             "rbrack", "rsqb", "Hat", "lowbar", "UnderBar", "DiacriticalGrave", "grave", "lbrace", "lcub", "verbar",
             "vert", "VerticalLine", "rbrace", "rcub", "nbsp", "NonBreakingSpace", "iexcl", "cent", "pound", "curren",
             "yen", "brvbar", "sect", "die", "Dot", "DoubleDot", "uml", "copy", "ordf", "laquo", "not", "shy",
             "circledR", "reg", "macr", "strns", "deg", "plusmn", "pm", "sup2", "sup3", "acute", "DiacriticalAcute",
             "micro", "para", "CenterDot", "centerdot", "middot", "cedil", "Cedilla", "sup1", "ordm", "raquo", "frac14",
             "frac12", "half", "frac34", "iquest", "Agrave", "Aacute", "Acirc", "Atilde", "Auml", "angst", "Aring",
             "AElig", "Ccedil", "Egrave", "Eacute", "Ecirc", "Euml", "Igrave", "Iacute", "Icirc", "Iuml", "ETH",
             "Ntilde", "Ograve", "Oacute", "Ocirc", "Otilde", "Ouml", "times", "Oslash", "Ugrave", "Uacute", "Ucirc",
             "Uuml", "Yacute", "THORN", "szlig", "agrave", "aacute", "acirc", "atilde", "auml", "aring", "aelig",
             "ccedil", "egrave", "eacute", "ecirc", "euml", "igrave", "iacute", "icirc", "iuml", "eth", "ntilde",
             "ograve", "oacute", "ocirc", "otilde", "ouml", "div", "divide", "oslash", "ugrave", "uacute", "ucirc",
             "uuml", "yacute", "thorn", "yuml", "Amacr", "amacr", "Abreve", "abreve", "Aogon", "aogon", "Cacute",
             "cacute", "Ccirc", "ccirc", "Cdot", "cdot", "Ccaron", "ccaron", "Dcaron", "dcaron", "Dstrok", "dstrok",
             "Emacr", "emacr", "Edot", "edot", "Eogon", "eogon", "Ecaron", "ecaron", "Gcirc", "gcirc", "Gbreve",
             "gbreve", "Gdot", "gdot", "Gcedil", "Hcirc", "hcirc", "Hstrok", "hstrok", "Itilde", "itilde", "Imacr",
             "imacr", "Iogon", "iogon", "Idot", "imath", "inodot", "IJlig", "ijlig", "Jcirc", "jcirc", "Kcedil",
             "kcedil", "kgreen", "Lacute", "lacute", "Lcedil", "lcedil", "Lcaron", "lcaron", "Lmidot", "lmidot",
             "Lstrok", "lstrok", "Nacute", "nacute", "Ncedil", "ncedil", "Ncaron", "ncaron", "napos", "ENG", "eng",
             "Omacr", "omacr", "Odblac", "odblac", "OElig", "oelig", "Racute", "racute", "Rcedil", "rcedil", "Rcaron",
             "rcaron", "Sacute", "sacute", "Scirc", "scirc", "Scedil", "scedil", "Scaron", "scaron", "Tcedil", "tcedil",
             "Tcaron", "tcaron", "Tstrok", "tstrok", "Utilde", "utilde", "Umacr", "umacr", "Ubreve", "ubreve", "Uring",
             "uring", "Udblac", "udblac", "Uogon", "uogon", "Wcirc", "wcirc", "Ycirc", "ycirc", "Yuml", "Zacute",
             "zacute", "Zdot", "zdot", "Zcaron", "zcaron", "fnof", "imped", "gacute", "jmath", "circ", "caron", "Hacek",
             "Breve", "breve", "DiacriticalDot", "dot", "ring", "ogon", "DiacriticalTilde", "tilde", "dblac",
             "DiacriticalDoubleAcute", "DownBreve", "Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Zeta", "Eta",
             "Theta", "Iota", "Kappa", "Lambda", "Mu", "Nu", "Xi", "Omicron", "Pi", "Rho", "Sigma", "Tau", "Upsilon",
             "Phi", "Chi", "Psi", "ohm", "Omega", "alpha", "beta", "gamma", "delta", "epsi", "epsilon", "zeta", "eta",
             "theta", "iota", "kappa", "lambda", "mu", "nu", "xi", "omicron", "pi", "rho", "sigmaf", "sigmav",
             "varsigma", "sigma", "tau", "upsi", "upsilon", "phi", "chi", "psi", "omega", "thetasym", "thetav",
             "vartheta", "Upsi", "upsih", "phiv", "straightphi", "varphi", "piv", "varpi", "Gammad", "digamma",
             "gammad", "kappav", "varkappa", "rhov", "varrho", "epsiv", "straightepsilon", "varepsilon", "backepsilon",
             "bepsi", "IOcy", "DJcy", "GJcy", "Jukcy", "DScy", "Iukcy", "YIcy", "Jsercy", "LJcy", "NJcy", "TSHcy",
             "KJcy", "Ubrcy", "DZcy", "Acy", "Bcy", "Vcy", "Gcy", "Dcy", "IEcy", "ZHcy", "Zcy", "Icy", "Jcy", "Kcy",
             "Lcy", "Mcy", "Ncy", "Ocy", "Pcy", "Rcy", "Scy", "Tcy", "Ucy", "Fcy", "KHcy", "TScy", "CHcy", "SHcy",
             "SHCHcy", "HARDcy", "Ycy", "SOFTcy", "Ecy", "YUcy", "YAcy", "acy", "bcy", "vcy", "gcy", "dcy", "iecy",
             "zhcy", "zcy", "icy", "jcy", "kcy", "lcy", "mcy", "ncy", "ocy", "pcy", "rcy", "scy", "tcy", "ucy", "fcy",
             "khcy", "tscy", "chcy", "shcy", "shchcy", "hardcy", "ycy", "softcy", "ecy", "yucy", "yacy", "iocy", "djcy",
             "gjcy", "jukcy", "dscy", "iukcy", "yicy", "jsercy", "ljcy", "njcy", "tshcy", "kjcy", "ubrcy", "dzcy",
             "ensp", "emsp", "emsp13", "emsp14", "numsp", "puncsp", "thinsp", "ThinSpace", "hairsp", "VeryThinSpace",
             "ZeroWidthSpace", "zwnj", "zwj", "lrm", "rlm", "dash", "hyphen", "ndash", "mdash", "horbar", "Verbar",
             "Vert", "lsquo", "OpenCurlyQuote", "CloseCurlyQuote", "rsquo", "rsquor", "lsquor", "sbquo", "ldquo",
             "OpenCurlyDoubleQuote", "CloseCurlyDoubleQuote", "rdquo", "rdquor", "bdquo", "ldquor", "dagger", "ddagger",
             "bull", "bullet", "nldr", "hellip", "mldr", "permil", "pertenk", "prime", "Prime", "tprime", "backprime",
             "bprime", "lsaquo", "rsaquo", "oline", "OverBar", "caret", "hybull", "frasl", "bsemi", "qprime",
             "MediumSpace", "NoBreak", "af", "ApplyFunction", "InvisibleTimes", "it", "ic", "InvisibleComma", "euro",
             "tdot", "TripleDot", "DotDot", "complexes", "Copf", "incare", "gscr", "hamilt", "HilbertSpace", "Hscr",
             "Hfr", "Poincareplane", "Hopf", "quaternions", "planckh", "hbar", "hslash", "planck", "plankv", "imagline",
             "Iscr", "Ifr", "Im", "image", "imagpart", "lagran", "Laplacetrf", "Lscr", "ell", "naturals", "Nopf",
             "numero", "copysr", "weierp", "wp", "Popf", "primes", "Qopf", "rationals", "realine", "Rscr", "Re", "real",
             "realpart", "Rfr", "reals", "Ropf", "rx", "TRADE", "trade", "integers", "Zopf", "mho", "zeetrf", "Zfr",
             "iiota", "bernou", "Bernoullis", "Bscr", "Cayleys", "Cfr", "escr", "Escr", "expectation", "Fouriertrf",
             "Fscr", "Mellintrf", "Mscr", "phmmat", "order", "orderof", "oscr", "alefsym", "aleph", "beth", "gimel",
             "daleth", "CapitalDifferentialD", "DD", "dd", "DifferentialD", "ee", "ExponentialE", "exponentiale", "ii",
             "ImaginaryI", "frac13", "frac23", "frac15", "frac25", "frac35", "frac45", "frac16", "frac56", "frac18",
             "frac38", "frac58", "frac78", "larr", "LeftArrow", "leftarrow", "ShortLeftArrow", "slarr", "ShortUpArrow",
             "uarr", "UpArrow", "uparrow", "rarr", "RightArrow", "rightarrow", "ShortRightArrow", "srarr", "darr",
             "DownArrow", "downarrow", "ShortDownArrow", "harr", "LeftRightArrow", "leftrightarrow", "UpDownArrow",
             "updownarrow", "varr", "nwarr", "nwarrow", "UpperLeftArrow", "nearr", "nearrow", "UpperRightArrow",
             "LowerRightArrow", "searr", "searrow", "LowerLeftArrow", "swarr", "swarrow", "nlarr", "nleftarrow",
             "nrarr", "nrightarrow", "rarrw", "rightsquigarrow", "Larr", "twoheadleftarrow", "Uarr", "Rarr",
             "twoheadrightarrow", "Darr", "larrtl", "leftarrowtail", "rarrtl", "rightarrowtail", "LeftTeeArrow",
             "mapstoleft", "mapstoup", "UpTeeArrow", "map", "mapsto", "RightTeeArrow", "DownTeeArrow", "mapstodown",
             "hookleftarrow", "larrhk", "hookrightarrow", "rarrhk", "larrlp", "looparrowleft", "looparrowright",
             "rarrlp", "harrw", "leftrightsquigarrow", "nharr", "nleftrightarrow", "Lsh", "lsh", "Rsh", "rsh", "ldsh",
             "rdsh", "crarr", "cularr", "curvearrowleft", "curarr", "curvearrowright", "circlearrowleft", "olarr",
             "circlearrowright", "orarr", "leftharpoonup", "LeftVector", "lharu", "DownLeftVector", "leftharpoondown",
             "lhard", "RightUpVector", "uharr", "upharpoonright", "LeftUpVector", "uharl", "upharpoonleft", "rharu",
             "rightharpoonup", "RightVector", "DownRightVector", "rhard", "rightharpoondown", "dharr",
             "downharpoonright", "RightDownVector", "dharl", "downharpoonleft", "LeftDownVector", "RightArrowLeftArrow",
             "rightleftarrows", "rlarr", "udarr", "UpArrowDownArrow", "LeftArrowRightArrow", "leftrightarrows", "lrarr",
             "leftleftarrows", "llarr", "upuparrows", "uuarr", "rightrightarrows", "rrarr", "ddarr", "downdownarrows",
             "leftrightharpoons", "lrhar", "ReverseEquilibrium", "Equilibrium", "rightleftharpoons", "rlhar", "nlArr",
             "nLeftarrow", "nhArr", "nLeftrightarrow", "nrArr", "nRightarrow", "DoubleLeftArrow", "lArr", "Leftarrow",
             "DoubleUpArrow", "uArr", "Uparrow", "DoubleRightArrow", "Implies", "rArr", "Rightarrow", "dArr",
             "DoubleDownArrow", "Downarrow", "DoubleLeftRightArrow", "hArr", "iff", "Leftrightarrow",
             "DoubleUpDownArrow", "Updownarrow", "vArr", "nwArr", "neArr", "seArr", "swArr", "lAarr", "Lleftarrow",
             "rAarr", "Rrightarrow", "zigrarr", "larrb", "LeftArrowBar", "rarrb", "RightArrowBar", "DownArrowUpArrow",
             "duarr", "loarr", "roarr", "hoarr", "ForAll", "forall", "comp", "complement", "part", "PartialD", "Exists",
             "exist", "nexist", "nexists", "NotExists", "empty", "emptyset", "emptyv", "varnothing", "Del", "nabla",
             "Element", "in", "isin", "isinv", "NotElement", "notin", "notinva", "ni", "niv", "ReverseElement",
             "SuchThat", "notni", "notniva", "NotReverseElement", "prod", "Product", "coprod", "Coproduct", "Sum",
             "sum", "minus", "MinusPlus", "mnplus", "mp", "dotplus", "plusdo", "Backslash", "setminus", "setmn",
             "smallsetminus", "ssetmn", "lowast", "compfn", "SmallCircle", "radic", "Sqrt", "prop", "Proportional",
             "propto", "varpropto", "vprop", "infin", "angrt", "ang", "angle", "angmsd", "measuredangle", "angsph",
             "mid", "shortmid", "smid", "VerticalBar", "nmid", "NotVerticalBar", "nshortmid", "nsmid",
             "DoubleVerticalBar", "par", "parallel", "shortparallel", "spar", "NotDoubleVerticalBar", "npar",
             "nparallel", "nshortparallel", "nspar", "and", "wedge", "or", "vee", "cap", "cup", "int", "Integral",
             "Int", "iiint", "tint", "conint", "ContourIntegral", "oint", "Conint", "DoubleContourIntegral", "Cconint",
             "cwint", "cwconint", "ClockwiseContourIntegral", "cwconintx", "awconint", "there4", "Therefore",
             "therefore", "because", "ratio", "Colon", "Proportion", "dotminus", "minusd", "mDDot", "homtht", "sim",
             "thicksim", "thksim", "Tilde", "backsim", "bsim", "ac", "mstpos", "acd", "VerticalTilde", "wr", "wreath",
             "NotTilde", "nsim", "eqsim", "EqualTilde", "esim", "sime", "simeq", "TildeEqual", "NotTildeEqual", "nsime",
             "nsimeq", "cong", "TildeFullEqual", "simne", "ncong", "NotTildeFullEqual", "ap", "approx", "asymp",
             "thickapprox", "thkap", "TildeTilde", "nap", "napprox", "NotTildeTilde", "ape", "approxeq", "apid",
             "backcong", "bcong", "asympeq", "CupCap", "bump", "Bumpeq", "HumpDownHump", "bumpe", "bumpeq", "HumpEqual",
             "doteq", "DotEqual", "esdot", "doteqdot", "eDot", "efDot", "fallingdotseq", "erDot", "risingdotseq",
             "Assign", "colone", "coloneq", "ecolon", "eqcolon", "ecir", "eqcirc", "circeq", "cire", "wedgeq", "veeeq",
             "triangleq", "trie", "equest", "questeq", "ne", "NotEqual", "Congruent", "equiv", "nequiv", "NotCongruent",
             "le", "leq", "ge", "geq", "GreaterEqual", "lE", "leqq", "LessFullEqual", "gE", "geqq", "GreaterFullEqual",
             "lnE", "lneqq", "gnE", "gneqq", "ll", "Lt", "NestedLessLess", "gg", "Gt", "NestedGreaterGreater",
             "between", "twixt", "NotCupCap", "nless", "nlt", "NotLess", "ngt", "ngtr", "NotGreater", "nle", "nleq",
             "NotLessEqual", "nge", "ngeq", "NotGreaterEqual", "lesssim", "LessTilde", "lsim", "GreaterTilde", "gsim",
             "gtrsim", "nlsim", "NotLessTilde", "ngsim", "NotGreaterTilde", "LessGreater", "lessgtr", "lg", "gl",
             "GreaterLess", "gtrless", "NotLessGreater", "ntlg", "NotGreaterLess", "ntgl", "pr", "prec", "Precedes",
             "sc", "succ", "Succeeds", "prcue", "preccurlyeq", "PrecedesSlantEqual", "sccue", "succcurlyeq",
             "SucceedsSlantEqual", "PrecedesTilde", "precsim", "prsim", "scsim", "SucceedsTilde", "succsim",
             "NotPrecedes", "npr", "nprec", "NotSucceeds", "nsc", "nsucc", "sub", "subset", "sup", "Superset", "supset",
             "nsub", "nsup", "sube", "subseteq", "SubsetEqual", "supe", "SupersetEqual", "supseteq", "NotSubsetEqual",
             "nsube", "nsubseteq", "NotSupersetEqual", "nsupe", "nsupseteq", "subne", "subsetneq", "supne", "supsetneq",
             "cupdot", "UnionPlus", "uplus", "sqsub", "sqsubset", "SquareSubset", "sqsup", "sqsupset", "SquareSuperset",
             "sqsube", "sqsubseteq", "SquareSubsetEqual", "sqsupe", "sqsupseteq", "SquareSupersetEqual", "sqcap",
             "SquareIntersection", "sqcup", "SquareUnion", "CirclePlus", "oplus", "CircleMinus", "ominus",
             "CircleTimes", "otimes", "osol", "CircleDot", "odot", "circledcirc", "ocir", "circledast", "oast",
             "circleddash", "odash", "boxplus", "plusb", "boxminus", "minusb", "boxtimes", "timesb", "dotsquare",
             "sdotb", "RightTee;", "vdash", "dashv", "LeftTee", "DownTee", "top", "bot", "bottom", "perp", "UpTee",
             "models", "DoubleRightTee", "vDash", "Vdash", "Vvdash", "VDash", "nvdash", "nvDash", "nVdash", "nVDash",
             "prurel", "LeftTriangle", "vartriangleleft", "vltri", "RightTriangle", "vartriangleright", "vrtri",
             "LeftTriangleEqual", "ltrie", "trianglelefteq", "RightTriangleEqual", "rtrie", "trianglerighteq", "origof",
             "imof", "multimap", "mumap", "hercon", "intcal", "intercal", "veebar", "barvee", "angrtvb", "lrtri",
             "bigwedge", "Wedge", "xwedge", "bigvee", "Vee", "xvee", "bigcap", "Intersection", "xcap", "bigcup",
             "Union", "xcup", "diam", "Diamond", "diamond", "sdot", "sstarf", "Star", "divideontimes", "divonx",
             "bowtie", "ltimes", "rtimes", "leftthreetimes", "lthree", "rightthreetimes", "rthree", "backsimeq",
             "bsime", "curlyvee", "cuvee", "curlywedge", "cuwed", "Sub", "Subset", "Sup", "Supset", "Cap", "Cup",
             "fork", "pitchfork", "epar", "lessdot", "ltdot", "gtdot", "gtrdot", "Ll", "Gg", "ggg", "leg", "lesseqgtr",
             "LessEqualGreater", "gel", "GreaterEqualLess", "gtreqless", "cuepr", "curlyeqprec", "cuesc", "curlyeqsucc",
             "NotPrecedesSlantEqual", "nprcue", "NotSucceedsSlantEqual", "nsccue", "NotSquareSubsetEqual", "nsqsube",
             "NotSquareSupersetEqual", "nsqsupe", "lnsim", "gnsim", "precnsim", "prnsim", "scnsim", "succnsim", "nltri",
             "NotLeftTriangle", "ntriangleleft", "NotRightTriangle", "nrtri", "ntriangleright", "nltrie",
             "NotLeftTriangleEqual", "ntrianglelefteq", "NotRightTriangleEqual", "nrtrie", "ntrianglerighteq", "vellip",
             "ctdot", "utdot", "dtdot", "disin", "isinsv", "isins", "isindot", "notinvc", "notinvb", "isinE", "nisd",
             "xnis", "nis", "notnivc", "notnivb", "barwedge", "doublebarwedge", "lceil", "LeftCeiling", "rceil",
             "RightCeiling", "LeftFloor", "lfloor", "rfloor", "RightFloor", "drcrop", "dlcrop", "urcrop", "ulcrop",
             "bnot", "profline", "profsurf", "telrec", "target", "ulcorn", "ulcorner", "urcorn", "urcorner", "dlcorn",
             "llcorner", "drcorn", "lrcorner", "frown", "sfrown", "smile", "ssmile", "cylcty", "profalar", "topbot",
             "ovbar", "solbar", "angzarr", "lmoust", "lmoustache", "rmoust", "rmoustache", "OverBracket", "tbrk",
             "bbrk", "UnderBracket", "bbrktbrk", "OverParenthesis", "UnderParenthesis", "OverBrace", "UnderBrace",
             "trpezium", "elinters", "blank", "circledS", "oS", "boxh", "HorizontalLine", "boxv", "boxdr", "boxdl",
             "boxur", "boxul", "boxvr", "boxvl", "boxhd", "boxhu", "boxvh", "boxH", "boxV", "boxdR", "boxDr", "boxDR",
             "boxdL", "boxDl", "boxDL", "boxuR", "boxUr", "boxUR", "boxuL", "boxUl", "boxUL", "boxvR", "boxVr", "boxVR",
             "boxvL", "boxVl", "boxVL", "boxHd", "boxhD", "boxHD", "boxHu", "boxhU", "boxHU", "boxvH", "boxVh", "boxVH",
             "uhblk", "lhblk", "block", "blk14", "blk12", "blk34", "squ", "Square", "square", "blacksquare",
             "FilledVerySmallSquare", "squarf", "squf", "EmptyVerySmallSquare", "rect", "marker", "fltns",
             "bigtriangleup", "xutri", "blacktriangle", "utrif", "triangle", "utri", "blacktriangleright", "rtrif",
             "rtri", "triangleright", "bigtriangledown", "xdtri", "blacktriangledown", "dtrif", "dtri", "triangledown",
             "blacktriangleleft", "ltrif", "ltri", "triangleleft", "loz", "lozenge", "cir", "tridot", "bigcirc",
             "xcirc", "ultri", "urtri", "lltri", "EmptySmallSquare", "FilledSmallSquare", "bigstar", "starf", "star",
             "phone", "female", "male", "spades", "spadesuit", "clubs", "clubsuit", "hearts", "heartsuit",
             "diamondsuit", "diams", "sung", "flat", "natur", "natural", "sharp", "check", "checkmark", "cross", "malt",
             "maltese", "sext", "VerticalSeparator", "lbbrk", "rbbrk", "bsolhsub", "suphsol", "LeftDoubleBracket",
             "lobrk", "RightDoubleBracket", "robrk", "lang", "langle", "LeftAngleBracket", "rang", "rangle",
             "RightAngleBracket", "Lang", "Rang", "loang", "roang", "LongLeftArrow", "longleftarrow", "xlarr",
             "LongRightArrow", "longrightarrow", "xrarr", "LongLeftRightArrow", "longleftrightarrow", "xharr",
             "DoubleLongLeftArrow", "Longleftarrow", "xlArr", "DoubleLongRightArrow", "Longrightarrow", "xrArr",
             "DoubleLongLeftRightArrow", "Longleftrightarrow", "xhArr", "longmapsto", "xmap", "dzigrarr", "nvlArr",
             "nvrArr", "nvHarr", "Map", "lbarr", "bkarow", "rbarr", "lBarr", "dbkarow", "rBarr", "drbkarow", "RBarr",
             "DDotrahd", "UpArrowBar", "DownArrowBar", "Rarrtl", "latail", "ratail", "lAtail", "rAtail", "larrfs",
             "rarrfs", "larrbfs", "rarrbfs", "nwarhk", "nearhk", "hksearow", "searhk", "hkswarow", "swarhk", "nwnear",
             "nesear", "toea", "seswar", "tosa", "swnwar", "rarrc", "cudarrr", "ldca", "rdca", "cudarrl", "larrpl",
             "curarrm", "cularrp", "rarrpl", "harrcir", "Uarrocir", "lurdshar", "ldrushar", "LeftRightVector",
             "RightUpDownVector", "DownLeftRightVector", "LeftUpDownVector", "LeftVectorBar", "RightVectorBar",
             "RightUpVectorBar", "RightDownVectorBar", "DownLeftVectorBar", "DownRightVectorBar", "LeftUpVectorBar",
             "LeftDownVectorBar", "LeftTeeVector", "RightTeeVector", "RightUpTeeVector", "RightDownTeeVector",
             "DownLeftTeeVector", "DownRightTeeVector", "LeftUpTeeVector", "LeftDownTeeVector", "lHar", "uHar", "rHar",
             "dHar", "luruhar", "ldrdhar", "ruluhar", "rdldhar", "lharul", "llhard", "rharul", "lrhard", "udhar",
             "UpEquilibrium", "duhar", "ReverseUpEquilibrium", "RoundImplies", "erarr", "simrarr", "larrsim", "rarrsim",
             "rarrap", "ltlarr", "gtrarr", "subrarr", "suplarr", "lfisht", "rfisht", "ufisht", "dfisht", "lopar",
             "ropar", "lbrke", "rbrke", "lbrkslu", "rbrksld", "lbrksld", "rbrkslu", "langd", "rangd", "lparlt",
             "rpargt", "gtlPar", "ltrPar", "vzigzag", "vangrt", "angrtvbd", "ange", "range", "dwangle", "uwangle",
             "angmsdaa", "angmsdab", "angmsdac", "angmsdad", "angmsdae", "angmsdaf", "angmsdag", "angmsdah", "bemptyv",
             "demptyv", "cemptyv", "raemptyv", "laemptyv", "ohbar", "omid", "opar", "operp", "olcross", "odsold",
             "olcir", "ofcir", "olt", "ogt", "cirscir", "cirE", "solb", "bsolb", "boxbox", "trisb", "rtriltri",
             "LeftTriangleBar", "RightTriangleBar", "iinfin", "infintie", "nvinfin", "eparsl", "smeparsl", "eqvparsl",
             "blacklozenge", "lozf", "RuleDelayed", "dsol", "bigodot", "xodot", "bigoplus", "xoplus", "bigotimes",
             "xotime", "biguplus", "xuplus", "bigsqcup", "xsqcup", "iiiint", "qint", "fpartint", "cirfnint", "awint",
             "rppolint", "scpolint", "npolint", "pointint", "quatint", "intlarhk", "pluscir", "plusacir", "simplus",
             "plusdu", "plussim", "plustwo", "mcomma", "minusdu", "loplus", "roplus", "Cross", "timesd", "timesbar",
             "smashp", "lotimes", "rotimes", "otimesas", "Otimes", "odiv", "triplus", "triminus", "tritime", "intprod",
             "iprod", "amalg", "capdot", "ncup", "ncap", "capand", "cupor", "cupcap", "capcup", "cupbrcap", "capbrcup",
             "cupcup", "capcap", "ccups", "ccaps", "ccupssm", "And", "Or", "andand", "oror", "orslope", "andslope",
             "andv", "orv", "andd", "ord", "wedbar", "sdote", "simdot", "congdot", "easter", "apacir", "apE", "eplus",
             "pluse", "Esim", "Colone", "Equal", "ddotseq", "eDDot", "equivDD", "ltcir", "gtcir", "ltquest", "gtquest",
             "leqslant", "les", "LessSlantEqual", "geqslant", "ges", "GreaterSlantEqual", "lesdot", "gesdot", "lesdoto",
             "gesdoto", "lesdotor", "gesdotol", "lap", "lessapprox", "gap", "gtrapprox", "lne", "lneq", "gne", "gneq",
             "lnap", "lnapprox", "gnap", "gnapprox", "lEg", "lesseqqgtr", "gEl", "gtreqqless", "lsime", "gsime",
             "lsimg", "gsiml", "lgE", "glE", "lesges", "gesles", "els", "eqslantless", "egs", "eqslantgtr", "elsdot",
             "egsdot", "el", "eg", "siml", "simg", "simlE", "simgE", "LessLess", "GreaterGreater", "glj", "gla", "ltcc",
             "gtcc", "lescc", "gescc", "smt", "lat", "smte", "late", "bumpE", "pre", "PrecedesEqual", "preceq", "sce",
             "SucceedsEqual", "succeq", "prE", "scE", "precneqq", "prnE", "scnE", "succneqq", "prap", "precapprox",
             "scap", "succapprox", "precnapprox", "prnap", "scnap", "succnapprox", "Pr", "Sc", "subdot", "supdot",
             "subplus", "supplus", "submult", "supmult", "subedot", "supedot", "subE", "subseteqq", "supE", "supseteqq",
             "subsim", "supsim", "subnE", "subsetneqq", "supnE", "supsetneqq", "csub", "csup", "csube", "csupe",
             "subsup", "supsub", "subsub", "supsup", "suphsub", "supdsub", "forkv", "topfork", "mlcp", "Dashv",
             "DoubleLeftTee", "Vdashl", "Barv", "vBar", "vBarv", "Vbar", "Not", "bNot", "rnmid", "cirmid", "midcir",
             "topcir", "nhpar", "parsim", "parsl", "fflig", "filig", "fllig", "ffilig", "ffllig", "Ascr", "Cscr",
             "Dscr", "Gscr", "Jscr", "Kscr", "Nscr", "Oscr", "Pscr", "Qscr", "Sscr", "Tscr", "Uscr", "Vscr", "Wscr",
             "Xscr", "Yscr", "Zscr", "ascr", "bscr", "cscr", "dscr", "fscr", "hscr", "iscr", "jscr", "kscr", "lscr",
             "mscr", "nscr", "pscr", "qscr", "rscr", "sscr", "tscr", "uscr", "vscr", "wscr", "xscr", "yscr", "zscr",
             "Afr", "Bfr", "Dfr", "Efr", "Ffr", "Gfr", "Jfr", "Kfr", "Lfr", "Mfr", "Nfr", "Ofr", "Pfr", "Qfr", "Sfr",
             "Tfr", "Ufr", "Vfr", "Wfr", "Xfr", "Yfr", "afr", "bfr", "cfr", "dfr", "efr", "ffr", "gfr", "hfr", "ifr",
             "jfr", "kfr", "lfr", "mfr", "nfr", "ofr", "pfr", "qfr", "rfr", "sfr", "tfr", "ufr", "vfr", "wfr", "xfr",
             "yfr", "zfr", "Aopf", "Bopf", "Dopf", "Eopf", "Fopf", "Gopf", "Iopf", "Jopf", "Kopf", "Lopf", "Mopf",
             "Oopf", "Sopf", "Topf", "Uopf", "Vopf", "Wopf", "Xopf", "Yopf", "aopf", "bopf", "copf", "dopf", "eopf",
             "fopf", "gopf", "hopf", "iopf", "jopf", "kopf", "lopf", "mopf", "nopf", "oopf", "popf", "qopf", "ropf",
             "sopf", "topf", "uopf", "vopf", "wopf", "xopf", "yopf", "zopf", "nvlt", "bne", "nvgt", "fjlig",
             "ThickSpace", "nrarrw", "npart", "nang", "caps", "cups", "nvsim", "race", "acE", "nesim", "NotEqualTilde",
             "napid", "nvap", "nbump", "NotHumpDownHump", "nbumpe", "NotHumpEqual", "nedot", "bnequiv", "nvle", "nvge",
             "nlE", "nleqq", "ngE", "ngeqq", "NotGreaterFullEqual", "lvertneqq", "lvnE", "gvertneqq", "gvnE", "nLtv",
             "NotLessLess", "nLt", "nGtv", "NotGreaterGreater", "nGt", "NotSucceedsTilde", "NotSubset", "nsubset",
             "vnsub", "NotSuperset", "nsupset", "vnsup", "varsubsetneq", "vsubne", "varsupsetneq", "vsupne",
             "NotSquareSubset", "NotSquareSuperset", "sqcaps", "sqcups", "nvltrie", "nvrtrie", "nLl", "nGg", "lesg",
             "gesl", "notindot", "notinE", "nrarrc", "NotLeftTriangleBar", "NotRightTriangleBar", "ncongdot", "napE",
             "nleqslant", "nles", "NotLessSlantEqual", "ngeqslant", "nges", "NotGreaterSlantEqual", "NotNestedLessLess",
             "NotNestedGreaterGreater", "smtes", "lates", "NotPrecedesEqual", "npre", "npreceq", "NotSucceedsEqual",
             "nsce", "nsucceq", "nsubE", "nsubseteqq", "nsupE", "nsupseteqq", "varsubsetneqq", "vsubnE",
             "varsupsetneqq", "vsupnE", "nparsl"};
        private static readonly int[] CODEPOINTS =
                {33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 42, 43, 44, 46, 47, 58, 59, 60, 61, 62, 63, 64, 91, 91, 92, 93, 93,
             94, 95, 95, 96, 96, 123, 123, 124, 124, 124, 125, 125, 160, 160, 161, 162, 163, 164, 165, 166, 167, 168,
             168, 168, 168, 169, 170, 171, 172, 173, 174, 174, 175, 175, 176, 177, 177, 178, 179, 180, 180, 181, 182,
             183, 183, 183, 184, 184, 185, 186, 187, 188, 189, 189, 190, 191, 192, 193, 194, 195, 196, 197, 197, 198,
             199, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212, 213, 214, 215, 216, 217, 218, 219,
             220, 221, 222, 223, 224, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240,
             241, 242, 243, 244, 245, 246, 247, 247, 248, 249, 250, 251, 252, 253, 254, 255, 256, 257, 258, 259, 260,
             261, 262, 263, 264, 265, 266, 267, 268, 269, 270, 271, 272, 273, 274, 275, 278, 279, 280, 281, 282, 283,
             284, 285, 286, 287, 288, 289, 290, 292, 293, 294, 295, 296, 297, 298, 299, 302, 303, 304, 305, 305, 306,
             307, 308, 309, 310, 311, 312, 313, 314, 315, 316, 317, 318, 319, 320, 321, 322, 323, 324, 325, 326, 327,
             328, 329, 330, 331, 332, 333, 336, 337, 338, 339, 340, 341, 342, 343, 344, 345, 346, 347, 348, 349, 350,
             351, 352, 353, 354, 355, 356, 357, 358, 359, 360, 361, 362, 363, 364, 365, 366, 367, 368, 369, 370, 371,
             372, 373, 374, 375, 376, 377, 378, 379, 380, 381, 382, 402, 437, 501, 567, 710, 711, 711, 728, 728, 729,
             729, 730, 731, 732, 732, 733, 733, 785, 913, 914, 915, 916, 917, 918, 919, 920, 921, 922, 923, 924, 925,
             926, 927, 928, 929, 931, 932, 933, 934, 935, 936, 937, 937, 945, 946, 947, 948, 949, 949, 950, 951, 952,
             953, 954, 955, 956, 957, 958, 959, 960, 961, 962, 962, 962, 963, 964, 965, 965, 966, 967, 968, 969, 977,
             977, 977, 978, 978, 981, 981, 981, 982, 982, 988, 989, 989, 1008, 1008, 1009, 1009, 1013, 1013, 1013, 1014,
             1014, 1025, 1026, 1027, 1028, 1029, 1030, 1031, 1032, 1033, 1034, 1035, 1036, 1038, 1039, 1040, 1041, 1042,
             1043, 1044, 1045, 1046, 1047, 1048, 1049, 1050, 1051, 1052, 1053, 1054, 1055, 1056, 1057, 1058, 1059, 1060,
             1061, 1062, 1063, 1064, 1065, 1066, 1067, 1068, 1069, 1070, 1071, 1072, 1073, 1074, 1075, 1076, 1077, 1078,
             1079, 1080, 1081, 1082, 1083, 1084, 1085, 1086, 1087, 1088, 1089, 1090, 1091, 1092, 1093, 1094, 1095, 1096,
             1097, 1098, 1099, 1100, 1101, 1102, 1103, 1105, 1106, 1107, 1108, 1109, 1110, 1111, 1112, 1113, 1114, 1115,
             1116, 1118, 1119, 8194, 8195, 8196, 8197, 8199, 8200, 8201, 8201, 8202, 8202, 8203, 8204, 8205, 8206, 8207,
             8208, 8208, 8211, 8212, 8213, 8214, 8214, 8216, 8216, 8217, 8217, 8217, 8218, 8218, 8220, 8220, 8221, 8221,
             8221, 8222, 8222, 8224, 8225, 8226, 8226, 8229, 8230, 8230, 8240, 8241, 8242, 8243, 8244, 8245, 8245, 8249,
             8250, 8254, 8254, 8257, 8259, 8260, 8271, 8279, 8287, 8288, 8289, 8289, 8290, 8290, 8291, 8291, 8364, 8411,
             8411, 8412, 8450, 8450, 8453, 8458, 8459, 8459, 8459, 8460, 8460, 8461, 8461, 8462, 8463, 8463, 8463, 8463,
             8464, 8464, 8465, 8465, 8465, 8465, 8466, 8466, 8466, 8467, 8469, 8469, 8470, 8471, 8472, 8472, 8473, 8473,
             8474, 8474, 8475, 8475, 8476, 8476, 8476, 8476, 8477, 8477, 8478, 8482, 8482, 8484, 8484, 8487, 8488, 8488,
             8489, 8492, 8492, 8492, 8493, 8493, 8495, 8496, 8496, 8497, 8497, 8499, 8499, 8499, 8500, 8500, 8500, 8501,
             8501, 8502, 8503, 8504, 8517, 8517, 8518, 8518, 8519, 8519, 8519, 8520, 8520, 8531, 8532, 8533, 8534, 8535,
             8536, 8537, 8538, 8539, 8540, 8541, 8542, 8592, 8592, 8592, 8592, 8592, 8593, 8593, 8593, 8593, 8594, 8594,
             8594, 8594, 8594, 8595, 8595, 8595, 8595, 8596, 8596, 8596, 8597, 8597, 8597, 8598, 8598, 8598, 8599, 8599,
             8599, 8600, 8600, 8600, 8601, 8601, 8601, 8602, 8602, 8603, 8603, 8605, 8605, 8606, 8606, 8607, 8608, 8608,
             8609, 8610, 8610, 8611, 8611, 8612, 8612, 8613, 8613, 8614, 8614, 8614, 8615, 8615, 8617, 8617, 8618, 8618,
             8619, 8619, 8620, 8620, 8621, 8621, 8622, 8622, 8624, 8624, 8625, 8625, 8626, 8627, 8629, 8630, 8630, 8631,
             8631, 8634, 8634, 8635, 8635, 8636, 8636, 8636, 8637, 8637, 8637, 8638, 8638, 8638, 8639, 8639, 8639, 8640,
             8640, 8640, 8641, 8641, 8641, 8642, 8642, 8642, 8643, 8643, 8643, 8644, 8644, 8644, 8645, 8645, 8646, 8646,
             8646, 8647, 8647, 8648, 8648, 8649, 8649, 8650, 8650, 8651, 8651, 8651, 8652, 8652, 8652, 8653, 8653, 8654,
             8654, 8655, 8655, 8656, 8656, 8656, 8657, 8657, 8657, 8658, 8658, 8658, 8658, 8659, 8659, 8659, 8660, 8660,
             8660, 8660, 8661, 8661, 8661, 8662, 8663, 8664, 8665, 8666, 8666, 8667, 8667, 8669, 8676, 8676, 8677, 8677,
             8693, 8693, 8701, 8702, 8703, 8704, 8704, 8705, 8705, 8706, 8706, 8707, 8707, 8708, 8708, 8708, 8709, 8709,
             8709, 8709, 8711, 8711, 8712, 8712, 8712, 8712, 8713, 8713, 8713, 8715, 8715, 8715, 8715, 8716, 8716, 8716,
             8719, 8719, 8720, 8720, 8721, 8721, 8722, 8723, 8723, 8723, 8724, 8724, 8726, 8726, 8726, 8726, 8726, 8727,
             8728, 8728, 8730, 8730, 8733, 8733, 8733, 8733, 8733, 8734, 8735, 8736, 8736, 8737, 8737, 8738, 8739, 8739,
             8739, 8739, 8740, 8740, 8740, 8740, 8741, 8741, 8741, 8741, 8741, 8742, 8742, 8742, 8742, 8742, 8743, 8743,
             8744, 8744, 8745, 8746, 8747, 8747, 8748, 8749, 8749, 8750, 8750, 8750, 8751, 8751, 8752, 8753, 8754, 8754,
             8754, 8755, 8756, 8756, 8756, 8757, 8758, 8759, 8759, 8760, 8760, 8762, 8763, 8764, 8764, 8764, 8764, 8765,
             8765, 8766, 8766, 8767, 8768, 8768, 8768, 8769, 8769, 8770, 8770, 8770, 8771, 8771, 8771, 8772, 8772, 8772,
             8773, 8773, 8774, 8775, 8775, 8776, 8776, 8776, 8776, 8776, 8776, 8777, 8777, 8777, 8778, 8778, 8779, 8780,
             8780, 8781, 8781, 8782, 8782, 8782, 8783, 8783, 8783, 8784, 8784, 8784, 8785, 8785, 8786, 8786, 8787, 8787,
             8788, 8788, 8788, 8789, 8789, 8790, 8790, 8791, 8791, 8793, 8794, 8796, 8796, 8799, 8799, 8800, 8800, 8801,
             8801, 8802, 8802, 8804, 8804, 8805, 8805, 8805, 8806, 8806, 8806, 8807, 8807, 8807, 8808, 8808, 8809, 8809,
             8810, 8810, 8810, 8811, 8811, 8811, 8812, 8812, 8813, 8814, 8814, 8814, 8815, 8815, 8815, 8816, 8816, 8816,
             8817, 8817, 8817, 8818, 8818, 8818, 8819, 8819, 8819, 8820, 8820, 8821, 8821, 8822, 8822, 8822, 8823, 8823,
             8823, 8824, 8824, 8825, 8825, 8826, 8826, 8826, 8827, 8827, 8827, 8828, 8828, 8828, 8829, 8829, 8829, 8830,
             8830, 8830, 8831, 8831, 8831, 8832, 8832, 8832, 8833, 8833, 8833, 8834, 8834, 8835, 8835, 8835, 8836, 8837,
             8838, 8838, 8838, 8839, 8839, 8839, 8840, 8840, 8840, 8841, 8841, 8841, 8842, 8842, 8843, 8843, 8845, 8846,
             8846, 8847, 8847, 8847, 8848, 8848, 8848, 8849, 8849, 8849, 8850, 8850, 8850, 8851, 8851, 8852, 8852, 8853,
             8853, 8854, 8854, 8855, 8855, 8856, 8857, 8857, 8858, 8858, 8859, 8859, 8861, 8861, 8862, 8862, 8863, 8863,
             8864, 8864, 8865, 8865, 8866, 8866, 8867, 8867, 8868, 8868, 8869, 8869, 8869, 8869, 8871, 8872, 8872, 8873,
             8874, 8875, 8876, 8877, 8878, 8879, 8880, 8882, 8882, 8882, 8883, 8883, 8883, 8884, 8884, 8884, 8885, 8885,
             8885, 8886, 8887, 8888, 8888, 8889, 8890, 8890, 8891, 8893, 8894, 8895, 8896, 8896, 8896, 8897, 8897, 8897,
             8898, 8898, 8898, 8899, 8899, 8899, 8900, 8900, 8900, 8901, 8902, 8902, 8903, 8903, 8904, 8905, 8906, 8907,
             8907, 8908, 8908, 8909, 8909, 8910, 8910, 8911, 8911, 8912, 8912, 8913, 8913, 8914, 8915, 8916, 8916, 8917,
             8918, 8918, 8919, 8919, 8920, 8921, 8921, 8922, 8922, 8922, 8923, 8923, 8923, 8926, 8926, 8927, 8927, 8928,
             8928, 8929, 8929, 8930, 8930, 8931, 8931, 8934, 8935, 8936, 8936, 8937, 8937, 8938, 8938, 8938, 8939, 8939,
             8939, 8940, 8940, 8940, 8941, 8941, 8941, 8942, 8943, 8944, 8945, 8946, 8947, 8948, 8949, 8950, 8951, 8953,
             8954, 8955, 8956, 8957, 8958, 8965, 8966, 8968, 8968, 8969, 8969, 8970, 8970, 8971, 8971, 8972, 8973, 8974,
             8975, 8976, 8978, 8979, 8981, 8982, 8988, 8988, 8989, 8989, 8990, 8990, 8991, 8991, 8994, 8994, 8995, 8995,
             9005, 9006, 9014, 9021, 9023, 9084, 9136, 9136, 9137, 9137, 9140, 9140, 9141, 9141, 9142, 9180, 9181, 9182,
             9183, 9186, 9191, 9251, 9416, 9416, 9472, 9472, 9474, 9484, 9488, 9492, 9496, 9500, 9508, 9516, 9524, 9532,
             9552, 9553, 9554, 9555, 9556, 9557, 9558, 9559, 9560, 9561, 9562, 9563, 9564, 9565, 9566, 9567, 9568, 9569,
             9570, 9571, 9572, 9573, 9574, 9575, 9576, 9577, 9578, 9579, 9580, 9600, 9604, 9608, 9617, 9618, 9619, 9633,
             9633, 9633, 9642, 9642, 9642, 9642, 9643, 9645, 9646, 9649, 9651, 9651, 9652, 9652, 9653, 9653, 9656, 9656,
             9657, 9657, 9661, 9661, 9662, 9662, 9663, 9663, 9666, 9666, 9667, 9667, 9674, 9674, 9675, 9708, 9711, 9711,
             9720, 9721, 9722, 9723, 9724, 9733, 9733, 9734, 9742, 9792, 9794, 9824, 9824, 9827, 9827, 9829, 9829, 9830,
             9830, 9834, 9837, 9838, 9838, 9839, 10003, 10003, 10007, 10016, 10016, 10038, 10072, 10098, 10099, 10184,
             10185, 10214, 10214, 10215, 10215, 10216, 10216, 10216, 10217, 10217, 10217, 10218, 10219, 10220, 10221,
             10229, 10229, 10229, 10230, 10230, 10230, 10231, 10231, 10231, 10232, 10232, 10232, 10233, 10233, 10233,
             10234, 10234, 10234, 10236, 10236, 10239, 10498, 10499, 10500, 10501, 10508, 10509, 10509, 10510, 10511,
             10511, 10512, 10512, 10513, 10514, 10515, 10518, 10521, 10522, 10523, 10524, 10525, 10526, 10527, 10528,
             10531, 10532, 10533, 10533, 10534, 10534, 10535, 10536, 10536, 10537, 10537, 10538, 10547, 10549, 10550,
             10551, 10552, 10553, 10556, 10557, 10565, 10568, 10569, 10570, 10571, 10574, 10575, 10576, 10577, 10578,
             10579, 10580, 10581, 10582, 10583, 10584, 10585, 10586, 10587, 10588, 10589, 10590, 10591, 10592, 10593,
             10594, 10595, 10596, 10597, 10598, 10599, 10600, 10601, 10602, 10603, 10604, 10605, 10606, 10606, 10607,
             10607, 10608, 10609, 10610, 10611, 10612, 10613, 10614, 10616, 10617, 10619, 10620, 10621, 10622, 10623,
             10629, 10630, 10635, 10636, 10637, 10638, 10639, 10640, 10641, 10642, 10643, 10644, 10645, 10646, 10650,
             10652, 10653, 10660, 10661, 10662, 10663, 10664, 10665, 10666, 10667, 10668, 10669, 10670, 10671, 10672,
             10673, 10674, 10675, 10676, 10677, 10678, 10679, 10681, 10683, 10684, 10686, 10687, 10688, 10689, 10690,
             10691, 10692, 10693, 10697, 10701, 10702, 10703, 10704, 10716, 10717, 10718, 10723, 10724, 10725, 10731,
             10731, 10740, 10742, 10752, 10752, 10753, 10753, 10754, 10754, 10756, 10756, 10758, 10758, 10764, 10764,
             10765, 10768, 10769, 10770, 10771, 10772, 10773, 10774, 10775, 10786, 10787, 10788, 10789, 10790, 10791,
             10793, 10794, 10797, 10798, 10799, 10800, 10801, 10803, 10804, 10805, 10806, 10807, 10808, 10809, 10810,
             10811, 10812, 10812, 10815, 10816, 10818, 10819, 10820, 10821, 10822, 10823, 10824, 10825, 10826, 10827,
             10828, 10829, 10832, 10835, 10836, 10837, 10838, 10839, 10840, 10842, 10843, 10844, 10845, 10847, 10854,
             10858, 10861, 10862, 10863, 10864, 10865, 10866, 10867, 10868, 10869, 10871, 10871, 10872, 10873, 10874,
             10875, 10876, 10877, 10877, 10877, 10878, 10878, 10878, 10879, 10880, 10881, 10882, 10883, 10884, 10885,
             10885, 10886, 10886, 10887, 10887, 10888, 10888, 10889, 10889, 10890, 10890, 10891, 10891, 10892, 10892,
             10893, 10894, 10895, 10896, 10897, 10898, 10899, 10900, 10901, 10901, 10902, 10902, 10903, 10904, 10905,
             10906, 10909, 10910, 10911, 10912, 10913, 10914, 10916, 10917, 10918, 10919, 10920, 10921, 10922, 10923,
             10924, 10925, 10926, 10927, 10927, 10927, 10928, 10928, 10928, 10931, 10932, 10933, 10933, 10934, 10934,
             10935, 10935, 10936, 10936, 10937, 10937, 10938, 10938, 10939, 10940, 10941, 10942, 10943, 10944, 10945,
             10946, 10947, 10948, 10949, 10949, 10950, 10950, 10951, 10952, 10955, 10955, 10956, 10956, 10959, 10960,
             10961, 10962, 10963, 10964, 10965, 10966, 10967, 10968, 10969, 10970, 10971, 10980, 10980, 10982, 10983,
             10984, 10985, 10987, 10988, 10989, 10990, 10991, 10992, 10993, 10994, 10995, 11005, 64256, 64257, 64258,
             64259, 64260, 119964, 119966, 119967, 119970, 119973, 119974, 119977, 119978, 119979, 119980, 119982,
             119983, 119984, 119985, 119986, 119987, 119988, 119989, 119990, 119991, 119992, 119993, 119995, 119997,
             119998, 119999, 120000, 120001, 120002, 120003, 120005, 120006, 120007, 120008, 120009, 120010, 120011,
             120012, 120013, 120014, 120015, 120068, 120069, 120071, 120072, 120073, 120074, 120077, 120078, 120079,
             120080, 120081, 120082, 120083, 120084, 120086, 120087, 120088, 120089, 120090, 120091, 120092, 120094,
             120095, 120096, 120097, 120098, 120099, 120100, 120101, 120102, 120103, 120104, 120105, 120106, 120107,
             120108, 120109, 120110, 120111, 120112, 120113, 120114, 120115, 120116, 120117, 120118, 120119, 120120,
             120121, 120123, 120124, 120125, 120126, 120128, 120129, 120130, 120131, 120132, 120134, 120138, 120139,
             120140, 120141, 120142, 120143, 120144, 120146, 120147, 120148, 120149, 120150, 120151, 120152, 120153,
             120154, 120155, 120156, 120157, 120158, 120159, 120160, 120161, 120162, 120163, 120164, 120165, 120166,
             120167, 120168, 120169, 120170, 120171};
        private static readonly long[] COMBINED_DIACRITICALS =
                {0x003C020D2L, 0x003D020E5L, 0x003E020D2L, 0x00660006AL, 0x205F0200AL, 0x219D00338L, 0x220200338L,
             0x2220020D2L, 0x22290FE00L, 0x222A0FE00L, 0x223C020D2L, 0x223D00331L, 0x223E00333L, 0x224200338L,
             0x224200338L, 0x224B00338L, 0x224D020D2L, 0x224E00338L, 0x224E00338L, 0x224F00338L, 0x224F00338L,
             0x225000338L, 0x2261020E5L, 0x2264020D2L, 0x2265020D2L, 0x226600338L, 0x226600338L, 0x226700338L,
             0x226700338L, 0x226700338L, 0x22680FE00L, 0x22680FE00L, 0x22690FE00L, 0x22690FE00L, 0x226A00338L,
             0x226A00338L, 0x226A020D2L, 0x226B00338L, 0x226B00338L, 0x226B020D2L, 0x227F00338L, 0x2282020D2L,
             0x2282020D2L, 0x2282020D2L, 0x2283020D2L, 0x2283020D2L, 0x2283020D2L, 0x228A0FE00L, 0x228A0FE00L,
             0x228B0FE00L, 0x228B0FE00L, 0x228F00338L, 0x229000338L, 0x22930FE00L, 0x22940FE00L, 0x22B4020D2L,
             0x22B5020D2L, 0x22D800338L, 0x22D900338L, 0x22DA0FE00L, 0x22DB0FE00L, 0x22F500338L, 0x22F900338L,
             0x293300338L, 0x29CF00338L, 0x29D000338L, 0x2A6D00338L, 0x2A7000338L, 0x2A7D00338L, 0x2A7D00338L,
             0x2A7D00338L, 0x2A7E00338L, 0x2A7E00338L, 0x2A7E00338L, 0x2AA100338L, 0x2AA200338L, 0x2AAC0FE00L,
             0x2AAD0FE00L, 0x2AAF00338L, 0x2AAF00338L, 0x2AAF00338L, 0x2AB000338L, 0x2AB000338L, 0x2AB000338L,
             0x2AC500338L, 0x2AC500338L, 0x2AC600338L, 0x2AC600338L, 0x2ACB0FE00L, 0x2ACB0FE00L, 0x2ACC0FE00L,
             0x2ACC0FE00L, 0x2AFD020E5L};

        private static readonly int MIN_ESCAPE;
        private static readonly int MAX_ESCAPE;
        private static readonly Dictionary<String, int[]> LOOKUP_MAP;

        static HTMLUtilities()
        {
            int minEscape = int.MaxValue;
            int maxEscape = int.MinValue;
            Dictionary<String, int[]> lookupMap = new Dictionary<String, int[]>(NAMES.Length);

            foreach (String name in NAMES)
            {
                minEscape = Math.Min(minEscape, name.Length);
                maxEscape = Math.Max(maxEscape, name.Length);
            }

            for (int i = 0; i < CODEPOINTS.Length; i++)
                lookupMap.Add(NAMES[i], new int[] { CODEPOINTS[i] });

            for (int i = 0; i < COMBINED_DIACRITICALS.Length; i++)
            {
                long combinedDiacritical = COMBINED_DIACRITICALS[i];
                int codepoint1 = (int)(combinedDiacritical >> 20);
                int codepoint2 = (int)(combinedDiacritical & 0xFFFFF);
                lookupMap.Add(NAMES[CODEPOINTS.Length + i], new int[] { codepoint1, codepoint2 });
            }

            MIN_ESCAPE = minEscape;
            MAX_ESCAPE = maxEscape;
            LOOKUP_MAP = lookupMap;
        }

        public static String unescapeHtml(String input)
        {
            StringBuilder result = null;

            int len = input.Length;
            int start = 0;
            int escStart = 0;
            while (true)
            {
                // Look for '&'
                while (escStart < len && input[escStart] != '&')
                    escStart++;

                if (escStart == len)
                    break;

                escStart++;

                // Found '&'. Look for ';'
                int escEnd = escStart;
                while (escEnd < len && escEnd - escStart < MAX_ESCAPE + 1 && input[escEnd] != ';')
                    escEnd++;

                if (escEnd == len)
                    break;

                // Bail if this is not a potential HTML entity.
                if (escEnd - escStart < MIN_ESCAPE || escEnd - escStart == MAX_ESCAPE + 1)
                {
                    escStart++;
                    continue;
                }

                // Check the kind of entity
                if (input[escStart] == '#')
                {
                    // Numeric entity
                    int numStart = escStart + 1;
                    int radix;

                    char firstChar = input[numStart];
                    if (firstChar == 'x' || firstChar == 'X')
                    {
                        numStart++;
                        radix = 16;
                    }
                    else
                    {
                        radix = 10;
                    }

                    try
                    {
                        int entityValue = Convert.ToInt32(input.Substring(numStart, escEnd - numStart), radix);

                        if (result == null)
                            result = new StringBuilder(input.Length);

                        result.Append(input, start, escStart - 1 - start);

                        if (entityValue > 0xFFFF)
                        {
                            result.Append(Char.ConvertFromUtf32(entityValue));
                        }
                        else
                        {
                            result.Append((char)entityValue);
                        }
                    }
                    catch (Exception ignored)
                    {
                        escStart++;
                        continue;
                    }
                }
                else
                {
                    // Named entity
                    int[] codePoints = LOOKUP_MAP[input.Substring(escStart, escEnd - escStart)];
                    if (codePoints == null)
                    {
                        escStart++;
                        continue;
                    }

                    if (result == null)
                        result = new StringBuilder(input.Length);

                    result.Append(input, start, escStart - 1 - start);
                    foreach (int codePoint in codePoints)
                        result.Append(codePoint);
                }

                // Skip escape
                start = escEnd + 1;
                escStart = start;
            }

            if (result != null)
            {
                result.Append(input, start, len - start);
                return result.ToString();
            }

            return input;
        }
    }

}