using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Commands;

namespace PMCDBot.Modules
{
    public class RateYourMusic : ModuleBase<SocketCommandContext>
    {
        [Command("search")]
        [Summary("Searches for an artist on RateYourMusic.")]
        private async Task SearchAsync([Remainder] [Summary("The text to search")] string query)
        {
            string clean = query.Replace(" ", "+");
            string url = $"https://rateyourmusic.com/search?searchtype=a&searchterm={clean}";
            await ReplyAsync(url);
        }
    }
}
