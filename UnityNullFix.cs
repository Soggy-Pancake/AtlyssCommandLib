using System;
using System.Collections.Generic;
using System.Text;

namespace AtlyssCommandLib;
internal static class UnityNullFix {
    public static T? NC<T>(this T? obj) where T : UnityEngine.Object? => obj ? obj : null;
}