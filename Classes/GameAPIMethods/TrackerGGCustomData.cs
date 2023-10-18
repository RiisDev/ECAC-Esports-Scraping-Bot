﻿using System.Diagnostics;
using Newtonsoft.Json.Linq;
using static ECAC_eSports_Bot.Classes.GameAPIMethods.MatchDataType;

namespace ECAC_eSports_Bot.Classes.GameAPIMethods
{
    public class MatchDataType
    {
        public record Match(MatchData MatchData, MapWinPercentage MatchWinPercentage)
        {
            public MatchData MatchData { get; set; } = MatchData;
            public MapWinPercentage MatchWinPercentage { get; set; } = MatchWinPercentage;
        }

        public record MatchData(string? MapName, string? Result, string? AgentPlayed, int? Kills, int? Deaths, int? Assists)
        {
            public string? MapName { get; set; } = MapName;
            public string? Result { get; set; } = Result;
            public string? AgentPlayed { get; set; } = AgentPlayed;
            public int? Kills { get; set; } = Kills;
            public int? Deaths { get; set; } = Deaths;
            public int? Assists { get; set; } = Assists;
        }

        public record MapWinPercentage(string MapName, double WinPercentage)
        {
            public string MapName { get; set; } = MapName;
            public double WinPercentage { get; set; } = WinPercentage;
        }

    }

    public class TrackerGgCustomData : TrackerGg
    {
        public List<Match> Matches { get; }
        public TrackerGgCustomData(List<Match> matches)
        {
            Matches = matches;
        }

        public static List<Match> CalculateMapWinPercentages(List<MatchData> matchDataList, DateTime cutOff)
        {
            List<Match> matches = new();
            List<MatchData> last10Matches = matchDataList.Take(10).ToList();

            IEnumerable<IGrouping<string?, MatchData>> mapGroups = last10Matches.GroupBy(match => match.MapName);

            List<MapWinPercentage> mapWinPercentages = (
                from mapGroup in mapGroups 
                let mapName = mapGroup.Key 
                let totalMatches = mapGroup.Count() 
                let wins = mapGroup.Count(match => match.Result == "victory") 
                let winPercentage = (double)wins / totalMatches * 100 
                select new MapWinPercentage(mapName, winPercentage)
            ).ToList();

            matches.AddRange(
                from mapWinPercentage in mapWinPercentages
                join match in last10Matches on mapWinPercentage.MapName equals match.MapName
                select new Match(match, mapWinPercentage)
            );

            return matches;
        }
        
#pragma warning disable Roslyn.IDE0060
        public static async Task<TrackerGgCustomData> GetAndParseData(string? riotId, DateTime cutOff)
#pragma warning restore Roslyn.IDE0060
        {
            if (!await IsValidUser(riotId, false)) return DefaultStats();
            if (!await IsValidUser(riotId, true)) return DefaultStats();
            if (await TrackerRateLimitCheck(riotId)) Process.GetCurrentProcess().Kill();

            string? jsonData = await GetTrackerJson(riotId, true);
            if (jsonData == null) return DefaultStats();

            JToken json = JToken.Parse(jsonData);

            if (json["data"]?["matches"] is null) return DefaultStats();

            List<MatchData> matches = (
                json["data"]?["matches"]!.Select(
                    matchData => new MatchData(
                        matchData["metadata"]?.Value<string>("mapName"), 
                        matchData["metadata"]?.Value<string>("result"), 
                        matchData["metadata"]?["segments"]?[0]?["metadata"]?.Value<string>("agentName"), 
                        matchData["metadata"]?["segments"]?[0]?["stats"]?["kills"]?.Value<int>("value"), 
                        matchData["metadata"]?["segments"]?[0]?["stats"]?["deaths"]?.Value<int>("value"), 
                        matchData["metadata"]?["segments"]?[0]?["stats"]?["assists"]?.Value<int>("value")
                    )
                ) ?? Array.Empty<MatchData>()).ToList();

            return new TrackerGgCustomData(CalculateMapWinPercentages(matches, DateTime.MinValue));

        }

        public static TrackerGgCustomData DefaultStats()
        {
            return new TrackerGgCustomData(
                new List<Match>
                {
                    new Match(
                        new MatchData(
                        "N/A",
                        "N/A",
                        "Sage",
                        0,
                        0,
                        0
                    ),
                    new MapWinPercentage(
                        "N/A",
                        0.0
                        )
                    )
                });
        }
    }
}
