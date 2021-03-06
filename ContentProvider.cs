﻿using System;
using System.Collections.Generic;
using System.IO;
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
        string remDir;
        string dir;

        public ContentProvider(string remDir, string dir)
        {
            this.remDir = remDir;
            this.dir = dir;
        }

        public static int[] init(string remDir, string dir)
        {
            var cont = new ContentProvider(remDir, dir);
            var task = Task.Run((Func<Task<int[]>>)cont.Run);
            task.Wait();
            return task.Result;
        }

        public static string[] load()
        {
            var cont = new ContentProvider("", "");
            var task = Task.Run((Func<Task<string[]>>)cont.AllFolders);
            task.Wait();
            return task.Result;
        }

        public async Task<int[]> Run()
        {
            try
            {
                string accessToken = await this.GetAccessToken();
                using (var dbx = new DropboxClient(accessToken))
                {
                    var full = await dbx.Users.GetCurrentAccountAsync();
                    Console.WriteLine("{0} - {1}", full.Name.DisplayName, full.Email);

                    Dictionary<string, int> files = await Compare(dbx, dir, remDir);
                    return await Sync(dbx, files);
                }
            }catch(Exception e) { Console.WriteLine(e.InnerException); return new int[2]{0,0}; }
        }

        public static string hasher(string path)
        {
            var hasher = new DropboxContentHasher();
            byte[] buf = new byte[1024];
            using (var file = File.OpenRead(path))
            {
                while (true)
                {
                    int n = file.Read(buf, 0, buf.Length);
                    if (n <= 0) break;  // EOF
                    hasher.TransformBlock(buf, 0, n, buf, 0);
                }
            }

            hasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            string hexHash = DropboxContentHasher.ToHex(hasher.Hash);
            return hexHash;
        }

        async Task<Dictionary<string, int>> Compare(DropboxClient dbx, string path, string dpath)
        {
            Dictionary<string, int> ret = new Dictionary<string, int>();
            Dictionary<string, string> hashes = new Dictionary<string, string>();
            Dictionary<string, DateTime> Hfiles = new Dictionary<string, DateTime>();
            try
            {
                string[] file = System.IO.Directory.GetFiles(path + dpath);
                foreach (string i in file)
                {
                    Hfiles.Add(i.Replace("/", "\\"), File.GetLastWriteTimeUtc(i));
                    hashes.Add(i.Replace("/", "\\"), hasher(i.Replace("/", "\\")));
                }
            }catch(Exception e) { }
            Dictionary<string, DateTime> Dfiles = new Dictionary<string, DateTime>();
            try
            {
                var list = await dbx.Files.ListFolderAsync(dpath.ToLower());
                foreach (var item in list.Entries.Where(i => i.IsFile))
                {
                    string hitemPath = path + item.PathDisplay.Replace("/", "\\");
                    if (!hashes.Contains(new KeyValuePair<string, string>(hitemPath, item.AsFile.ContentHash))) Dfiles.Add(item.PathDisplay, item.AsFile.ServerModified);
                    else
                    {
                        hashes.Remove(hitemPath);
                        Hfiles.Remove(hitemPath);
                    }
                }
            }catch(Exception e) { }

            foreach(var Hfile in Hfiles)
            {
                string DfilePath = Hfile.Key.Replace(path, "").Replace("\\", "/");
                if (Dfiles.ContainsKey(DfilePath))
                {
                    DateTime date;
                    Dfiles.TryGetValue(DfilePath, out date);
                    if (date.Subtract(Hfile.Value).TotalMinutes < 0)
                    {
                        ret.Add(Hfile.Key, 0);
                    }
                    else
                    {
                        ret.Add(DfilePath, 1);
                    }
                }
                else
                {
                    ret.Add(Hfile.Key, 0);
                }
            }
            
            foreach(var Dfile in Dfiles)
            {
                string HfilePath = Dfile.Key.Insert(0, path).Replace("/", "\\");
                if (!ret.ContainsKey(HfilePath))
                {
                    ret.Add(Dfile.Key, 1);
                }
            }

            try
            {
                var Dfolders = await dbx.Files.ListFolderAsync(dpath.ToLower());
                foreach (var folder in Dfolders.Entries.Where(i => i.IsFolder))
                {
                    Dictionary<string, int> files = await Compare(dbx, path, folder.PathDisplay);
                    foreach (var file in files)
                    {
                        ret.Add(file.Key, file.Value);
                    }
                }
            }catch(Exception e) { }

            try
            {
                string[] Hfolders = Directory.GetDirectories(path + dpath);
                foreach (string folder in Hfolders)
                {
                    Dictionary<string, int> files = await Compare(dbx, path, folder.Replace(path, "").Replace("\\", "/"));
                    foreach (var file in files)
                    {
                        ret.Add(file.Key, file.Value);
                    }
                }
            }catch(Exception e) { }

            return ret;
        }

        async Task<int[]> Sync(DropboxClient dbx, Dictionary<string, int> files)
        {
            int[] ret = new int[2]{0,0};
            foreach (var file in files)
            {
                if(file.Value == 1)
                {
                    try
                    {
                        await Download(dbx, file.Key);
                        ret[0]++;
                    }catch(Exception e)
                    {
                        Console.WriteLine("File" + file.Key + "wasn't downloaded");
                    }
                }
                else
                {
                    try
                    {
                        await Upload(dbx, file.Key);
                        ret[1]++;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("File" + file.Key + "wasn't uploaded");
                    }
                }
            }
            return ret;
        }

        async Task Download(DropboxClient dbx, string file)
        {

            using (var response = await dbx.Files.DownloadAsync(file))
            {
                System.IO.Directory.CreateDirectory(dir + file.Remove(file.LastIndexOf("/")));
                System.IO.File.WriteAllBytes(dir + file, response.GetContentAsByteArrayAsync().Result);
            }
        }

        async Task Upload(DropboxClient dbx, string file)
        {
            using (var mem = new MemoryStream(File.ReadAllBytes(file)))
            {
                string dfile = file.Replace(dir, "").Replace("\\", "/");
                string folder = dfile.Remove(dfile.LastIndexOf("/")+1);
                dbx.Files.CreateFolderV2Async(new CreateFolderArg(folder));
                var updated = await dbx.Files.UploadAsync(
                    dfile,
                    WriteMode.Overwrite.Instance,
                    body: mem);
            }
        }

        async Task<string[]> AllFolders()
        {
            try
            {
                string accessToken = await this.GetAccessToken();
                using (var dbx = new DropboxClient(accessToken))
                {
                    var list = await dbx.Files.ListFolderAsync(string.Empty);
                    string[] ret = new string[list.Entries.Where(i => i.IsFolder).Count()];
                    int g = 0;
                    foreach (var item in list.Entries.Where(i => i.IsFolder))
                    {
                        ret[g] = item.PathDisplay;
                        g++;
                    }
                    return ret;
                }
            }catch(Exception e){ Console.WriteLine(e.InnerException); return null;}
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
