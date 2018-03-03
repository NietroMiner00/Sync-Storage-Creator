﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Dropbox.Api;
using Dropbox.Api.Files;
using Dropbox.Api.Team;
using Sync_Storage_Creator_Windows.Properties;

namespace Sync_Storage_Creator_Windows
{
    class ContentProvider
    {

        private const string ApiKey = "776ru0jpqrbetf9";

        public static void init()
        {
            var cont = new ContentProvider();
            var task = Task.Run((Func<Task>)cont.Run);
            task.Wait();
        }

        public async Task Run()
        {
            string accessToken = await this.GetAccessToken();
            using (var dbx = new DropboxClient(accessToken))
            {
                var full = await dbx.Users.GetCurrentAccountAsync();
                Console.WriteLine("{0} - {1}", full.Name.DisplayName, full.Email);

                await Sync(dbx, "/sync/");
            }
        }

        async Task Sync(DropboxClient dbx, string path)
        {
            var list = await dbx.Files.ListFolderAsync(path, recursive:true);

            foreach (var item in list.Entries.Where(i => i.IsFile))
            {
                string path2 = item.PathDisplay.Replace(item.Name, "");
                await Download(dbx, path2, item.Name);
            }

        }

        async Task Download(DropboxClient dbx, string folder, string file)
        {
            using (var response = await dbx.Files.DownloadAsync(folder + file))
            {
                System.IO.Directory.CreateDirectory("c:\\users\\Daniel\\Desktop\\Dropbox\\" + folder);
                System.IO.File.WriteAllBytes("c:\\users\\Daniel\\Desktop\\Dropbox\\" + folder + file, response.GetContentAsByteArrayAsync().Result);
            }
        }

        private async Task<string> GetAccessToken()
        {
            var accessToken = Settings.Default.AccessToken;

            if (string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("Waiting for credentials.");
                var completion = new TaskCompletionSource<Tuple<string, string>>();

                var thread = new Thread(() =>
                {
                    try
                    {
                        var login = new LoginForm(ApiKey);
                        login.ShowDialog();
                        if (login.Result)
                        {
                            completion.TrySetResult(Tuple.Create(login.AccessToken, login.Uid));
                        }
                        else
                        {
                            completion.TrySetCanceled();
                        }
                    }
                    catch (Exception e)
                    {
                        completion.TrySetException(e);
                    }
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();

                try
                {
                    var result = await completion.Task;
                    Console.WriteLine("and back...");

                    accessToken = result.Item1;
                    var uid = result.Item2;
                    Console.WriteLine("Uid: {0}", uid);

                    Settings.Default.AccessToken = accessToken;
                    Settings.Default.Uid = uid;

                    Settings.Default.Save();
                }
                catch (Exception e)
                {
                    e = e.InnerException ?? e;
                    Console.WriteLine("Error: {0}", e.Message);
                    return null;
                }
            }

            return accessToken;
        }

    }
}
