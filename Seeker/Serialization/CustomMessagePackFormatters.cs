using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MessagePack;
using MessagePack.Formatters;
using Soulseek;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Seeker.Serialization
{
    public class FileItemFormatter : IMessagePackFormatter<Soulseek.File>
    {
        public void Serialize(ref MessagePackWriter writer, Soulseek.File value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteArrayHeader(8);
            writer.WriteInt32(value.Code);
            writer.Write(value.Filename);
            writer.WriteInt64(value.Size);
            writer.Write(value.Extension);
            writer.Write(value.IsLatin1Decoded);
            writer.Write(value.IsDirectoryLatin1Decoded);
            writer.WriteInt32(value.AttributeCount);

            writer.WriteArrayHeader(value.Attributes.Count);
            var standardFileAttributeFormatter = options.Resolver.GetFormatterWithVerify<FileAttribute>();
            foreach (var fileAttr in value.Attributes)
            {
                standardFileAttributeFormatter.Serialize(ref writer, fileAttr, options);
            }

        }

        Soulseek.File IMessagePackFormatter<Soulseek.File>.Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            options.Security.DepthStep(ref reader);

            int code = -1;
            string filename = null;
            long size = -1;
            string ext = null;
            bool isLatin1Decoded = false;
            bool isDirectoryLatinDecoded = false;
            int attributeCount = 0;
            List<FileAttribute> fileAttrs = null;

            // Loop over *all* array elements independently of how many we expect,
            // since if we're serializing an older/newer version of this object it might
            // vary in number of elements that were serialized, but the contract of the formatter
            // is that exactly one data structure must be read, regardless.
            // Alternatively, we could check that the size of the array/map is what we expect
            // and throw if it is not.
            int count = reader.ReadArrayHeader();
            for (int i = 0; i < count; i++)
            {
                switch (i)
                {
                    case 0:
                        code = reader.ReadInt32();
                        break;
                    case 1:
                        filename = reader.ReadString();
                        break;
                    case 2:
                        size = reader.ReadInt64();
                        break;
                    case 3:
                        ext = reader.ReadString();
                        break;
                    case 4:
                        isLatin1Decoded = reader.ReadBoolean();
                        break;
                    case 5:
                        isDirectoryLatinDecoded = reader.ReadBoolean();
                        break;
                    case 6:
                        attributeCount = reader.ReadInt32();
                        break;
                    case 7:
                        var length = reader.ReadArrayHeader();
                        var classBFormatter = options.Resolver.GetFormatterWithVerify<FileAttribute>();
                        fileAttrs = new List<FileAttribute>(length);
                        for (int j = 0; j < length; j++)
                        {
                            var file1 = classBFormatter.Deserialize(ref reader, options);
                            fileAttrs.Add(file1);
                        }
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }

            reader.Depth--;
            return new Soulseek.File(code, filename, size, ext, fileAttrs, isLatin1Decoded, isDirectoryLatinDecoded);
        }
    }

    public class BrowseResponseFormatter : IMessagePackFormatter<Soulseek.BrowseResponse>
    {
        public void Serialize(ref MessagePackWriter writer, Soulseek.BrowseResponse value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteArrayHeader(2);
            writer.WriteArrayHeader(value.Directories.Count);

            var standardFileAttributeFormatter = options.Resolver.GetFormatterWithVerify<Soulseek.Directory>();
            foreach (var dir in value.Directories)
            {
                standardFileAttributeFormatter.Serialize(ref writer, dir, options);
            }

            writer.WriteArrayHeader(value.LockedDirectories.Count);
            foreach (var dir in value.LockedDirectories)
            {
                standardFileAttributeFormatter.Serialize(ref writer, dir, options);
            }
        }

        Soulseek.BrowseResponse IMessagePackFormatter<Soulseek.BrowseResponse>.Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            options.Security.DepthStep(ref reader);

            List<Soulseek.Directory> dirs = null;
            List<Soulseek.Directory> lockedDirs = null;

            // Loop over *all* array elements independently of how many we expect,
            // since if we're serializing an older/newer version of this object it might
            // vary in number of elements that were serialized, but the contract of the formatter
            // is that exactly one data structure must be read, regardless.
            // Alternatively, we could check that the size of the array/map is what we expect
            // and throw if it is not.
            int count = reader.ReadArrayHeader();
            for (int i = 0; i < count; i++)
            {
                switch (i)
                {
                    case 0:
                        {
                            var length = reader.ReadArrayHeader();
                            var classBFormatter = options.Resolver.GetFormatterWithVerify<Directory>();
                            dirs = new List<Directory>(length);
                            for (int j = 0; j < length; j++)
                            {
                                var dir = classBFormatter.Deserialize(ref reader, options);
                                dirs.Add(dir);
                            }
                        }
                        break;
                    case 1:
                        {
                            var length = reader.ReadArrayHeader();
                            var classBFormatter = options.Resolver.GetFormatterWithVerify<Directory>();
                            lockedDirs = new List<Directory>(length);
                            for (int j = 0; j < length; j++)
                            {
                                var file1 = classBFormatter.Deserialize(ref reader, options);
                                lockedDirs.Add(file1);
                            }
                        }
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }

            reader.Depth--;
            return new Soulseek.BrowseResponse(dirs, lockedDirs);
        }
    }

    public class DirectoryItemFormatter : IMessagePackFormatter<Soulseek.Directory>
    {
        public void Serialize(ref MessagePackWriter writer, Soulseek.Directory value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteArrayHeader(2);
            writer.Write(value.Name);
 
            writer.WriteArrayHeader(value.Files.Count);
            var standardFileAttributeFormatter = options.Resolver.GetFormatterWithVerify<Soulseek.File>();
            foreach (var file in value.Files)
            {
                standardFileAttributeFormatter.Serialize(ref writer, file, options);
            }
        }

        Soulseek.Directory IMessagePackFormatter<Soulseek.Directory>.Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            options.Security.DepthStep(ref reader);

            int fileCount = -1;
            string name = null;
            List<Soulseek.File> files = null;

            // Loop over *all* array elements independently of how many we expect,
            // since if we're serializing an older/newer version of this object it might
            // vary in number of elements that were serialized, but the contract of the formatter
            // is that exactly one data structure must be read, regardless.
            // Alternatively, we could check that the size of the array/map is what we expect
            // and throw if it is not.
            int count = reader.ReadArrayHeader();
            for (int i = 0; i < count; i++)
            {
                switch (i)
                {
                    case 0:
                        name = reader.ReadString();
                        break;
                    case 1:
                        var length = reader.ReadArrayHeader();
                        var classBFormatter = options.Resolver.GetFormatterWithVerify<Soulseek.File>();
                        files = new List<Soulseek.File>(length);
                        for (int j = 0; j < length; j++)
                        {
                            var file1 = classBFormatter.Deserialize(ref reader, options);
                            files.Add(file1);
                        }
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }

            reader.Depth--;
            return new Soulseek.Directory(name, files);
        }
    }

    public class SearchResponseFormatter : IMessagePackFormatter<SearchResponse>
    {
        public void Serialize(ref MessagePackWriter writer, SearchResponse value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteArrayHeader(9);
            writer.WriteInt32(value.FileCount);
            writer.WriteInt32(value.LockedFileCount);
            writer.Write(value.Username);
            writer.WriteInt32(value.UploadSpeed);
            writer.WriteInt32(value.FreeUploadSlots);
            writer.WriteInt64(value.QueueLength);
            writer.WriteInt32(value.Token);


            writer.WriteArrayHeader(value.Files.Count);
            var slskFileFormatter = options.Resolver.GetFormatterWithVerify<Soulseek.File>();
            foreach (var file in value.Files)
            {
                slskFileFormatter.Serialize(ref writer, file, options);
            }

            writer.WriteArrayHeader(value.LockedFiles.Count);
            slskFileFormatter = options.Resolver.GetFormatterWithVerify<Soulseek.File>();
            foreach (var file in value.LockedFiles)
            {
                slskFileFormatter.Serialize(ref writer, file, options);
            }
        }

        SearchResponse IMessagePackFormatter<SearchResponse>.Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }


            int fileCount = -1;
            int lockedFileCount = -1;
            string username = null;
            int uploadSpeed = -1;
            int freeUploadSlots = -1;
            long queueLength = -1;
            int token = -1;

            List<Soulseek.File> files = new List<Soulseek.File>();
            List<Soulseek.File> lockedFiles = new List<Soulseek.File>();


            int count = reader.ReadArrayHeader();
            for (int i = 0; i < count; i++)
            {
                switch (i)
                {
                    case 0:
                        fileCount = reader.ReadInt32();
                        break;
                    case 1:
                        lockedFileCount = reader.ReadInt32();
                        break;
                    case 2:
                        username = reader.ReadString();
                        break;
                    case 3:
                        uploadSpeed = reader.ReadInt32();
                        break;
                    case 4:
                        freeUploadSlots = reader.ReadInt32();
                        break;
                    case 5:
                        queueLength = reader.ReadInt64();
                        break;
                    case 6:
                        token = reader.ReadInt32();
                        break;
                    case 7:
                        var length = reader.ReadArrayHeader();
                        var classBFormatter = options.Resolver.GetFormatterWithVerify<Soulseek.File>();
                        for (int j = 0; j < length; j++)
                        {
                            var file1 = classBFormatter.Deserialize(ref reader, options);
                            files.Add(file1);
                        }
                        break;
                    case 8:
                        var length2 = reader.ReadArrayHeader();
                        var classBFormatter2 = options.Resolver.GetFormatterWithVerify<Soulseek.File>();
                        for (int j = 0; j < length2; j++)
                        {
                            var file1 = classBFormatter2.Deserialize(ref reader, options);
                            lockedFiles.Add(file1);
                        }
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }



            options.Security.DepthStep(ref reader);


            reader.Depth--;
            return new SearchResponse(username, token, freeUploadSlots, uploadSpeed, queueLength, files, lockedFiles);
        }
    }

    public class UserListItemFormatter : IMessagePackFormatter<UserListItem>
    {
        public void Serialize(ref MessagePackWriter writer, UserListItem value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteArrayHeader(6);
            writer.Write(value.Username);
            writer.WriteInt32((int)value.Role);
            writer.Write(value.DoesNotExist);

            var userStatusFormatter = options.Resolver.GetFormatterWithVerify<Soulseek.UserStatus>();
            userStatusFormatter.Serialize(ref writer, value.UserStatus, options);

            var userDataFormatter = options.Resolver.GetFormatterWithVerify<Soulseek.UserData>(); // this does not need a custom formatter...
            userDataFormatter.Serialize(ref writer, value.UserData, options);

            var userInfoFormatter = options.Resolver.GetFormatterWithVerify<Soulseek.UserInfo>();
            userInfoFormatter.Serialize(ref writer, value.UserInfo, options);
        }

        UserListItem IMessagePackFormatter<UserListItem>.Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            string username = null;
            UserRole role = UserRole.Friend;
            bool doesNotExist = false;
            Soulseek.UserStatus userStatus = null;
            Soulseek.UserData userData = null;
            Soulseek.UserInfo userInfo = null;

            int count = reader.ReadArrayHeader();
            for (int i = 0; i < count; i++)
            {
                switch (i)
                {
                    case 0:
                        username = reader.ReadString();
                        break;
                    case 1:
                        role = (UserRole)reader.ReadInt32();
                        break;
                    case 2:
                        doesNotExist = reader.ReadBoolean();
                        break;
                    case 3:
                        var userStatusFormatter = options.Resolver.GetFormatterWithVerify<Soulseek.UserStatus>();
                        userStatus = userStatusFormatter.Deserialize(ref reader, options);
                        break;
                    case 4:
                        var userDataFormatter = options.Resolver.GetFormatterWithVerify<Soulseek.UserData>();
                        userData = userDataFormatter.Deserialize(ref reader, options);
                        break;
                    case 5:
                        var userInfoFormatter = options.Resolver.GetFormatterWithVerify<Soulseek.UserInfo>();
                        userInfo = userInfoFormatter.Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }



            options.Security.DepthStep(ref reader);


            reader.Depth--;

            var userListItem = new UserListItem(username, role);
            userListItem.DoesNotExist = doesNotExist;
            userListItem.UserStatus = userStatus;
            userListItem.UserInfo = userInfo;
            userListItem.UserData = userData;
            return userListItem;
        }
    }

    public class UserStatusFormatter : IMessagePackFormatter<Soulseek.UserStatus>
    {
        public void Serialize(ref MessagePackWriter writer, Soulseek.UserStatus value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteArrayHeader(2);
            writer.Write(value.IsPrivileged);
            writer.WriteInt32((int)value.Presence);
        }

        Soulseek.UserStatus IMessagePackFormatter<Soulseek.UserStatus>.Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            bool isPrivileged = false;
            UserPresence presence = UserPresence.Online;

            int count = reader.ReadArrayHeader();
            for (int i = 0; i < count; i++)
            {
                switch (i)
                {
                    case 0:
                        isPrivileged = reader.ReadBoolean();
                        break;
                    case 1:
                        presence = (UserPresence)reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }



            options.Security.DepthStep(ref reader);


            reader.Depth--;
            return new Soulseek.UserStatus(presence, isPrivileged);
        }
    }


    public class UserInfoFormatter : IMessagePackFormatter<Soulseek.UserInfo>
    {
        public void Serialize(ref MessagePackWriter writer, Soulseek.UserInfo value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteArrayHeader(6);
            writer.Write(value.QueueLength);
            writer.Write(value.Picture);
            writer.Write(value.Description);
            writer.Write(value.HasFreeUploadSlot);
            writer.Write(value.HasPicture);
            writer.Write(value.UploadSlots);
        }

        Soulseek.UserInfo IMessagePackFormatter<Soulseek.UserInfo>.Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            int queueLength = 0;
            byte[] picture = null;
            string description = null;
            bool hasFreeUploadSlot = false;
            bool hasPicture = false;
            int uploadSlots = 0;

            int count = reader.ReadArrayHeader();
            for (int i = 0; i < count; i++)
            {
                switch (i)
                {
                    case 0:
                        queueLength = reader.ReadInt32();
                        break;
                    case 1:
                        var byteSpan = reader.ReadBytes();
                        if (byteSpan.HasValue)
                        {
                            picture = byteSpan.Value.ToArray<byte>();
                        }
                        else
                        {
                            picture = null;
                        }
                        break;
                    case 2:
                        description = reader.ReadString();
                        break;
                    case 3:
                        hasFreeUploadSlot = reader.ReadBoolean();
                        break;
                    case 4:
                        hasPicture = reader.ReadBoolean();
                        break;
                    case 5:
                        uploadSlots = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }



            options.Security.DepthStep(ref reader);


            reader.Depth--;
            return new Soulseek.UserInfo(description, hasPicture, picture, uploadSlots, queueLength, hasFreeUploadSlot);
        }
    }

}