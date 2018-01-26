using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Npgsql;
using Discord.Commands;

namespace PMCDBot.Modules
{
    [Group("fm")]
    public class Lastfm : ModuleBase<SocketCommandContext>
    {
        private readonly IConfigurationRoot _config;
        // TODO: Get PG Connection into a sensible place
        private NpgsqlConnection _conn;

        public Lastfm(IConfigurationRoot config)
        {
            _config = config;
            var connString = "Host=/var/run/postgresql/;Username=phin;Database=pmcdbot";
            _conn = new NpgsqlConnection(connString);
            _conn.Open();
				}

        [Command]
        [Summary("Looks up now playing and recent songs for a user.")]
        private async Task NowPlaying([Summary("Username to look up")] string username = null)
        {
            if (username == null) {
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = _conn;
                    cmd.CommandText = "SELECT lastfm_username FROM lastfm WHERE discord_username=@u AND discord_discriminator=@d";
                    cmd.Parameters.AddWithValue("u", Context.User.Username);
                    cmd.Parameters.AddWithValue("d", Context.User.Discriminator);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            username = reader.GetString(0);
                        } else {
                            await ReplyAsync("You must be the owner of the bot to run this command.");
                        }
                    }
                }
            }
            string url = $"https://ws.audioscrobbler.com/2.0/?method=user.getrecenttracks&user={username}&api_key={_config["last.fm"]}&format=json&limit=2";
            string json_body = Get(url);
            dynamic recent_tracks = JsonConvert.DeserializeObject(json_body);
            string reply;
            if (recent_tracks.error != null) {
                if (recent_tracks.error == 6) {
                    reply = "User not found.";
                } else {
                    reply = "An error occured.";
                }
            } else {
                dynamic current = recent_tracks.recenttracks.track[0];
                dynamic previous = recent_tracks.recenttracks.track[1];

                reply = $"<https://www.last.fm/user/{username}>\n";
                reply += $"Current: {current.artist["#text"]} - {current.name} [{current.album["#text"]}]\n";
                reply += $"Previous: {previous.artist["#text"]} - {previous.name} [{previous.album["#text"]}]";
            }
            await ReplyAsync(reply);
        }

        [Command("set")]
        [Summary("Set a default username to look up when using fm")]
        private async Task SetUsername([Summary("Username to set")] string username)
        {
            using (var cmd = new NpgsqlCommand())
            {
                cmd.Connection = _conn;
                cmd.CommandText =
                  @"INSERT INTO lastfm (
                      lastfm_username,
                      discord_username,
                      discord_discriminator
                    ) VALUES (
                      @luser,
                      @duser,
                      @discriminator
                    ) ON CONFLICT (discord_username, discord_discriminator) DO UPDATE SET lastfm_username = excluded.lastfm_username;";
                cmd.Parameters.AddWithValue("luser", username);
                cmd.Parameters.AddWithValue("duser", Context.User.Username);
                cmd.Parameters.AddWithValue("discriminator", Context.User.Discriminator);
                cmd.ExecuteNonQuery();
            }

            await ReplyAsync("Username set!");
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
