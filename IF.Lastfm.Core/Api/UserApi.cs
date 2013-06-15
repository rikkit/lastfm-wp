﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IF.Lastfm.Core.Api.Enums;
using IF.Lastfm.Core.Api.Helpers;
using IF.Lastfm.Core.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IF.Lastfm.Core.Api
{
    public class UserApi : IUserApi
    {
        public IAuth Auth { get; private set; }

        public UserApi(IAuth auth)
        {
            Auth = auth;
        }

        /// <summary>
        /// TODO paging
        /// </summary>
        /// <param name="span"></param>
        /// <param name="startIndex"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public async Task<PageResponse<Album>> GetTopAlbums(LastStatsTimeSpan span, int startIndex = 0, int amount = LastFm.DefaultPageLength)
        {
            const string apiMethod = "user.getTopAlbums";

            var parameters = new Dictionary<string, string>
                                 {
                                     {"user", Auth.User.Username},
                                     {"period", span.GetApiName()}
                                 };

            var apiUrl = LastFm.FormatApiUrl(apiMethod, Auth.ApiKey, parameters);

            var httpClient = new HttpClient();
            var lastResponse = await httpClient.GetAsync(apiUrl);
            var json = await lastResponse.Content.ReadAsStringAsync();

            LastFmApiError error;
            if (LastFm.IsResponseValid(json, out error) && lastResponse.IsSuccessStatusCode)
            {
                var jtoken = JsonConvert.DeserializeObject<JToken>(json);

                var albumsToken = jtoken.SelectToken("topalbums").SelectToken("album");

                var albums = albumsToken.Children().Select(Album.ParseJToken);

                return PageResponse<Album>.CreateSuccessResponse(albums);
            }
            else
            {
                return PageResponse<Album>.CreateErrorResponse(error);
            }
        }

        /// <summary>
        /// Gets scrobbles and stuff
        /// </summary>
        /// <param name="username"></param>
        /// <param name="since"></param>
        /// <param name="pagenumber"></param>
        /// <param name="endIndex"></param>
        /// <returns></returns>
        public async Task<PageResponse<Track>> GetRecentScrobbles(string username, DateTime since, int pagenumber, int count = LastFm.DefaultPageLength)
        {
            const string apiMethod = "user.getRecentTracks";

            var parameters = new Dictionary<string, string>
                                 {
                                     {"user", Auth.User.Username},
                                     {"from", since.ToUnixTimestamp().ToString()},
                                     {"page", pagenumber.ToString()},
                                     {"limit", count.ToString()}
                                 };

            var apiUrl = LastFm.FormatApiUrl(apiMethod, Auth.ApiKey, parameters);

            var httpClient = new HttpClient();
            var lastResponse = await httpClient.GetAsync(apiUrl);
            var json = await lastResponse.Content.ReadAsStringAsync();

            LastFmApiError error;
            if (LastFm.IsResponseValid(json, out error) && lastResponse.IsSuccessStatusCode)
            {
                var jtoken = JsonConvert.DeserializeObject<JToken>(json).SelectToken("recenttracks");

                var tracksToken = jtoken.SelectToken("track");
                
                var tracks = new List<Track>();
                foreach (var track in tracksToken.Children())
                {
                    var t = Track.ParseJToken(track);
                    var date = track.SelectToken("date");
                    var stamp = date.Value<double>("uts");
                    t.TimePlayed = stamp.ToDateTimeUtc();

                    tracks.Add(t);
                }

                var pageresponse = PageResponse<Track>.CreateSuccessResponse(tracks);

                var attrToken = jtoken.SelectToken("@attr");

                if (attrToken != null)
                {
                    pageresponse.Page = attrToken.Value<int>("page");
                    pageresponse.TotalPages = attrToken.Value<int>("totalPages");
                }

                return pageresponse;
            }
            else
            {
                return PageResponse<Track>.CreateErrorResponse(error);
            }
        }
    }
}