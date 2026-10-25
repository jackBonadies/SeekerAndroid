/*
 * Copyright 2021 Seeker
 *
 * This file is part of Seeker
 *
 * Seeker is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Seeker is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Seeker. If not, see <http://www.gnu.org/licenses/>.
 */

using Seeker.Extensions.SearchResponseExtensions;
using Seeker.Helpers;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Content;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
using Java.Lang;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Seeker
{
    public class TabsPagerAdapter : AndroidX.ViewPager2.Adapter.FragmentStateAdapter
    {
        public TabsPagerAdapter(FragmentActivity fa) : base(fa)
        {
        }

        public override int ItemCount => MainNavigation.PageCount;

        //FragmentStateAdapter owns the fragment instances; this just produces a fresh fragment for a
        //given tab. The page titles that used to live here are gone with the (unused) TabLayout —
        //tab labels come from the BottomNavigationView menu. Page 0 stays the account/login page
        //(hidden from the bottom menu) and page 4 is the Messages overview.
        public override Fragment CreateFragment(int position)
        {
            switch (position)
            {
                case MainNavigation.AccountPage:
                    return new LoginFragment();
                case MainNavigation.SearchPage:
                    return new SearchFragment();
                case MainNavigation.TransfersPage:
                    return new TransfersFragment();
                case MainNavigation.BrowsePage:
                    return new BrowseFragment();
                case MainNavigation.MessagesPage:
                    return new Seeker.Messages.MessagesOverviewFragment();
                default:
                    throw new System.Exception("Invalid Tab");
            }
        }
    }

    public class CustomLinearLayoutManager : LinearLayoutManager
    {
        public CustomLinearLayoutManager(Context c) : base(c)
        {

        }
        //Generate constructors

        public override bool SupportsPredictiveItemAnimations()
        {
            bool old = base.SupportsPredictiveItemAnimations();
            return false;
        }

    }
}
