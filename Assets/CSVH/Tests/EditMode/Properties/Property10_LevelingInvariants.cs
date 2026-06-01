// Feature: tower-defense-vn, Property 10: Bất biến hệ leveling
// Validates: Requirements 4.2, 4.3, 4.5

using System;
using FsCheck;
using NUnit.Framework;
using CSVH.Core.Progression;

namespace CSVH.Tests.Edit.Properties
{
    public class Property10_LevelingInvariants
    {
        [Test]
        public void LevelingInvariantsHold()
        {
            PbtRunner.RunForAll<PositiveInt, NonNegativeInt[]>((baseRequiredP, expChunks) =>
            {
                if (expChunks == null) expChunks = System.Array.Empty<NonNegativeInt>();
                // Kẹp đầu vào để tránh kịch bản tràn cực đoan; trọng tâm là bất biến vòng đời
                // (Requirements 4.2, 4.3, 4.5), không phải biên int.MaxValue (đã có test riêng).
                int baseRequired = Math.Min(baseRequiredP.Get, 1000);
                float scale = 1.5f;
                var sys = new LevelingSystem(baseRequired, scale);

                int prevLevel = sys.Level;
                foreach (var chunk in expChunks)
                {
                    int amount = Math.Min(chunk.Get, 10000);
                    sys.AddExp(amount);

                    // Bất biến sau mỗi bước AddExp:
                    if (sys.Level < prevLevel) return false;          // monotonic Level
                    if (sys.RequiredExp <= 0) return false;            // RequiredExp luôn dương
                    if (sys.CurrentExp < 0) return false;              // 0 ≤ CurrentExp
                    if (sys.CurrentExp >= sys.RequiredExp) return false; // CurrentExp < RequiredExp

                    prevLevel = sys.Level;
                }
                return true;
            });
        }
    }
}
