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
    public class TabsPagerAdapter : FragmentPagerAdapter
    {
        Fragment login = null;
        Fragment search = null;
        Fragment transfer = null;
        Fragment browse = null;
        public TabsPagerAdapter(FragmentManager fm) : base(fm)
        {
            login = new LoginFragment();
            search = new SearchFragment();
            transfer = new TransfersFragment();
            browse = new BrowseFragment();
        }

        public override int Count => 4;

        public override Fragment GetItem(int position)
        {
            Fragment frag = null;
            switch (position)
            {
                case 0:
                    frag = login;
                    break;
                case 1:
                    frag = search;
                    break;
                case 2:
                    frag = transfer;
                    break;
                case 3:
                    frag = browse;
                    break;
                default:
                    throw new System.Exception("Invalid Tab");
            }
            return frag;
        }

        public override int GetItemPosition(Java.Lang.Object @object)
        {
            return PositionNone;
        }

        public override ICharSequence GetPageTitleFormatted(int position)
        {
            ICharSequence title;
            switch (position)
            {
                case 0:
                    title = new Java.Lang.String(SeekerState.ActiveActivityRef.GetString(Resource.String.account_tab));
                    break;
                case 1:
                    title = new Java.Lang.String(SeekerState.ActiveActivityRef.GetString(Resource.String.searches_tab));
                    break;
                case 2:
                    title = new Java.Lang.String(SeekerState.ActiveActivityRef.GetString(Resource.String.transfer_tab));
                    break;
                case 3:
                    title = new Java.Lang.String(SeekerState.ActiveActivityRef.GetString(Resource.String.browse_tab));
                    break;
                default:
                    throw new System.Exception("Invalid Tab");
            }
            return title;
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
