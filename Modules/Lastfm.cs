using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Discord.Commands;

namespace PMCDBot.Modules
{
    public class Lastfm : ModuleBase<SocketCommandContext>
    {
        private readonly IConfigurationRoot _config;

        public Lastfm(IConfigurationRoot config)
        {
            _config = config;
				}

        [Command("fm")]
        [Summary("Looks up now playing and recent songs.")]
        private async Task NowPlaying([Remainder] [Summary("Username to look up")] string username)
        {
            string url = $"https://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user={username}&api_key={_config["last.fm"]}&format=json&limit=2";
            string json_body = Get(url);
            dynamic recent_tracks = JsonConvert.DeserializeObject(json_body);
            dynamic current = recent_tracks.recenttracks.track[0];
            dynamic previous = recent_tracks.recenttracks.track[1];

            string reply = $"<https://www.last.fm/user/{username}>\n";
            reply += $"Current: {current.artist["#text"]} - {current.name} [{current.album["#text"]}]\n";
            reply += $"Previous: {previous.artist["#text"]} - {previous.name} [{previous.album["#text"]}]";

            await ReplyAsync(reply);
        }

        private string Get(string uri)
        {
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}

