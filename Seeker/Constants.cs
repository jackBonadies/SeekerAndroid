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

namespace Seeker
{
    public static class Constants
    {
        public const string SharedPrefFile = "SoulSeekPrefs";
        public const int DefaultSearchResults = 250;
        public const string AndroidForegroundSpecialUseMetadata = "android.app.PROPERTY_SPECIAL_USE_FGS_SUBTYPE";
        public const string AndroidForegroundSpecialUseDescription = "This service implements the client side Soulseek protocol, which allows one to search for, download, and share files with other people on the Soulseek network. Soulseek is a peer-to-peer network that requires maintaining an active connection to the network for as long as the user desires. Since others users on the network can send search and browse requests to this client at any time, the service must remain active and ready to respond. Because of this continuous availability requirement, the service cannot be limited to 6 hours a day, and special permission is required to ensure the user gets the best possible experience with the application.";
    }
}
