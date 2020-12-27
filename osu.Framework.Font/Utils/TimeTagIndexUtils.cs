﻿// Copyright (c) andy840119 <andy840119@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Utils
{
    public static class TimeTagIndexUtils
    {
        public static TimeTagIndex Clamp(TimeTagIndex value, int minValue, int maxValue)
        {
            return new TimeTagIndex(Math.Clamp(value.Index, minValue, maxValue), value.State);
        }
    }
}
