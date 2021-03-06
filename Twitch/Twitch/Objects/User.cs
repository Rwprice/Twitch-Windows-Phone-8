﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Windows.Storage;
using Windows.Storage.Streams;
using Microsoft.Phone.Scheduler;

namespace TwitchAPIHandler.Objects
{
    public class User
    {
        private static string liveTileTaskName = "UpdateLiveTileTask";

        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Oauth { get; set; }

        public static async Task<User> GetUserFromOauth(string oauth)
        {
            Uri access_token_path = new Uri(string.Format(PathStrings.GET_USER_PATH, oauth));
            var request = HttpWebRequest.Create(access_token_path);
            request.Method = "GET";
            var response = await HttpRequest(request);
            JToken o = JObject.Parse(response);
            return new User
            {
                Name = (string)o.SelectToken("name"),
                DisplayName = (string)o.SelectToken("display_name"),
                Oauth = oauth
            };
        }

        private static async Task<string> HttpRequest(WebRequest request)
        {
            string received = "";

            using (var response = (HttpWebResponse)(await Task<WebResponse>.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, null)))
            {
                using (var responseStream = response.GetResponseStream())
                {
                    using (var sr = new StreamReader(responseStream))
                    {
                        received = await sr.ReadToEndAsync();
                    }
                }
            }


            return received;
        }

        public static async void SaveUser(User user)
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile textFile = await localFolder.CreateFileAsync("user2", CreationCollisionOption.ReplaceExisting);

            using (IRandomAccessStream textStream = await textFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                using (DataWriter textWriter = new DataWriter(textStream))
                {
                    textWriter.WriteString(user.Name + "\n" + user.DisplayName + "\n"
                        + user.Oauth);
                    await textWriter.StoreAsync();
                }
            }
        }

        public static async Task<User> TryLoadUser()
        {
            try
            {
                string contents;

                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile textFile = await localFolder.GetFileAsync("user2");

                using (IRandomAccessStream textStream = await textFile.OpenReadAsync())
                {
                    using (DataReader textReader = new DataReader(textStream))
                    {
                        uint textLength = (uint)textStream.Size;
                        await textReader.LoadAsync(textLength);
                        contents = textReader.ReadString(textLength);
                    }
                }

                string[] lines = contents.Split('\n');

                return new User
                {
                    Name = lines[0],
                    DisplayName = lines[1],
                    Oauth = lines[2]
                };
            }

            catch { return null; }
        }

        public static async void LogoutUser()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile textFile = await localFolder.GetFileAsync("user2");
            await textFile.DeleteAsync();

            if (ScheduledActionService.Find(liveTileTaskName) != null)
            {
                //if the agent exists, remove and then add it to ensure
                //the agent's schedule is updated to avoid expiration
                ScheduledActionService.Remove(liveTileTaskName);
            }
        }

        public static async Task<bool> IsStreamFollowed(string stream, User user)
        {
            Uri access_token_path = new Uri(string.Format(PathStrings.IS_STREAM_FOLLOWED_PATH, user.Name, stream));
            var request = HttpWebRequest.Create(access_token_path);
            request.Method = "GET";
            string response;

            try
            {
                response = await HttpRequest(request);
                return true;
            }
            catch { return false; }
            
        }

        public static async Task<bool> FollowStream(string stream, User user)
        {
            Uri access_token_path = new Uri(string.Format(PathStrings.FOLLOW_USER, user.Name, stream, user.Oauth));
            var request = HttpWebRequest.Create(access_token_path);
            request.Method = "PUT";
            try
            {
                var response = await HttpRequest(request);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> UnfollowStream(string stream, User user)
        {
            Uri access_token_path = new Uri(string.Format(PathStrings.FOLLOW_USER, user.Name, stream, user.Oauth));
            var request = HttpWebRequest.Create(access_token_path);
            request.Method = "DELETE";
            try
            {
                var response = await HttpRequest(request);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
