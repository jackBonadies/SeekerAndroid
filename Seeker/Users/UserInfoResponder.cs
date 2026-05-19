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
using AndroidX.DocumentFile.Provider;
using Common;
using Seeker.Helpers;
using Soulseek;
using System.Net;
using System.Threading.Tasks;

namespace Seeker
{
    public static class UserInfoResponder
    {
        public const string USER_INFO_PIC_DIR = "user_info_picture";

        public static Task<UserInfo> HandleRequest(string uname, IPEndPoint ipEndPoint)
        {
            if (UserListService.Instance.IsUserInIgnoreList(uname))
            {
                return Task.FromResult(new UserInfo(string.Empty, 0, 0, false));
            }
            string bio = PreferencesState.UserInfoBio ?? string.Empty;
            byte[] picture = GetUserInfoPicture();
            int uploadSlots = 1;
            int queueLength = 0;
            bool hasFreeSlots = true;
            if (!PreferencesState.SharingOn) //in my experience even if someone is sharing nothing they say 1 upload slot and yes free slots.. but idk maybe 0 and no makes more sense??
            {
                uploadSlots = 0;
                queueLength = 0;
                hasFreeSlots = false;
            }

            return Task.FromResult(new UserInfo(bio, uploadSlots, queueLength, hasFreeSlots, picture));
        }

        private static byte[] GetUserInfoPicture()
        {
            if (PreferencesState.UserInfoPictureName == null || PreferencesState.UserInfoPictureName == string.Empty)
            {
                return null;
            }
            Java.IO.File userInfoPicDir = new Java.IO.File(SeekerApplication.ApplicationContext.FilesDir, USER_INFO_PIC_DIR);
            if (!userInfoPicDir.Exists())
            {
                Logger.Firebase("!userInfoPicDir.Exists()");
                return null;
            }

            Java.IO.File userInfoPic = new Java.IO.File(userInfoPicDir, PreferencesState.UserInfoPictureName);
            if (!userInfoPic.Exists())
            {
                //I could imagine a race condition causing this...
                Logger.Firebase("!userInfoPic.Exists()");
                return null;
            }
            DocumentFile documentFile = DocumentFile.FromFile(userInfoPic);
            System.IO.Stream imageStream = SeekerApplication.ApplicationContext.ContentResolver.OpenInputStream(documentFile.Uri);
            byte[] picFile = new byte[imageStream.Length];
            imageStream.Read(picFile, 0, (int)imageStream.Length);
            return picFile;
        }
    }
}
