﻿namespace BotV2.DataTypes.ECAC
{
    public record TeamStats(double WinCount, double LossCount, double WinPercentage)
    {
        public double WinCount { get; set; } = WinCount;
        public double LossCount { get; set; } = LossCount;
        public double WinPercentage { get; set; } = WinPercentage;
    }
}