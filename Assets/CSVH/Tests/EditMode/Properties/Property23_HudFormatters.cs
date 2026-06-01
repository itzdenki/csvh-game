// Feature: tower-defense-vn, Property 23: HUD formatter strings
// Validates: Requirements 4.4, 4.5, 5.5, 7.3, 7.6, 8.4

using System;
using FsCheck;
using NUnit.Framework;
using CSVH.Core.Hud;

namespace CSVH.Tests.Edit.Properties
{
    public class Property23_HudFormatters
    {
        [Test]
        public void WaveFormatsCorrectly()
        {
            PbtRunner.RunForAll<PositiveInt>(nP =>
            {
                int n = Math.Min(nP.Get, int.MaxValue);
                return Format.Wave(n) == "Đợt " + n + "/∞";
            });
        }

        [Test]
        public void NextWaveFormatsCorrectly()
        {
            PbtRunner.RunForAll<PositiveInt>(nP =>
            {
                int n = Math.Min(nP.Get, int.MaxValue - 1);
                return Format.NextWave(n) == "Đợt kế tiếp: " + (n + 1);
            });
        }

        [Test]
        public void CountdownFormatsCorrectly()
        {
            PbtRunner.RunForAll<NonNegativeInt>(secP =>
            {
                int sec = secP.Get;
                return Format.Countdown(sec) == "Đếm ngược: " + sec;
            });
        }

        [Test]
        public void HpFormatsCorrectly()
        {
            PbtRunner.RunForAll<PositiveInt, NonNegativeInt>((maxP, curRaw) =>
            {
                int max = maxP.Get;
                int cur = curRaw.Get % (max + 1);
                return Format.Hp(cur, max) == cur + "/" + max;
            });
        }

        [Test]
        public void LevelFormatsCorrectly()
        {
            PbtRunner.RunForAll<PositiveInt>(lvlP =>
            {
                return Format.Level(lvlP.Get) == "Cấp: " + lvlP.Get;
            });
        }

        [Test]
        public void ExpRatioInClampedRange()
        {
            PbtRunner.RunForAll<NonNegativeInt, PositiveInt>((curP, reqP) =>
            {
                int cur = curP.Get;
                int req = reqP.Get;
                float r = Format.ExpRatio(cur, req);
                return r >= 0f && r <= 1f;
            });
        }

        [Test]
        public void ExpRatioReturnsZeroWhenRequiredNotPositive()
        {
            Assert.AreEqual(0f, Format.ExpRatio(50, 0));
            Assert.AreEqual(0f, Format.ExpRatio(50, -1));
        }
    }
}
