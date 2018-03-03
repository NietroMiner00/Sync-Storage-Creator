using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Api.Files;
using Dropbox.Api.Team;

namespace Sync_Storage_Creator_Windows
{
    class ContentProvider
    {

        static DropboxClient dbx;

        public static async Task Run()
        {
            using (dbx = new DropboxClient(""))
            {
                var full = await dbx.Users.GetCurrentAccountAsync();
                Console.WriteLine("{0} - {1}", full.Name.DisplayName, full.Email);
            }
        }

    }
}
