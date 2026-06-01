// Feature: tower-defense-vn, Property 16: Số Đợt đơn điệu tăng nghiêm ngặt
// Validates: Requirements 7.4, 7.5

using System;
using System.Collections.Generic;
using FsCheck;
using NUnit.Framework;
using CSVH.Core.Common;
using CSVH.Core.Config;
using CSVH.Core.Wave;

namespace CSVH.Tests.Edit.Properties
{
    public class Property16_CurrentWaveMonotonic
    {
        [Test]
        public void CurrentWaveStrictlyIncreases()
        {
            PbtRunner.RunForAll<PositiveInt>(clears =>
            {
                // Cap số lần OnWaveCleared để mỗi case chạy nhanh, bám sát Property 16:
                // CurrentWave là dãy đơn điệu tăng nghiêm ngặt qua mọi chuỗi OnWaveCleared.
                int n = Math.Min(clears.Get, 50);

                var enemy = new EnemyConfig("E1", "Quái", 10f, 1f, 5f, 0f, 1, 1, 1);
                var spawn = new SpawnEntry("E1", 1, 1f);
                var wave = new WaveConfig(
                    1,
                    new[] { spawn },
                    new[] { new FieldPoint(0f, 5f) },
                    1f);
                var dict = new Dictionary<string, EnemyConfig> { { "E1", enemy } };
                var scheduler = new WaveScheduler(new[] { wave }, dict);

                scheduler.Start();
                // Force into Active (và sau đó Cleared) bằng tick đủ dài qua Pha_Chuẩn_Bị.
                scheduler.Tick(2f, 0);

                int prev = scheduler.CurrentWave;
                for (int i = 0; i < n; i++)
                {
                    // Tiến tới Cleared nếu chưa: state machine guard giữ Tick không sinh
                    // SpawnIntent khi đã Cleared, nên gọi an toàn lặp đi lặp lại.
                    scheduler.Tick(10f, 0);
                    if (scheduler.State == WaveState.Cleared || scheduler.State == WaveState.Active)
                    {
                        scheduler.OnWaveCleared();
                    }
                    // Sau OnWaveCleared phải ở Preparing với CurrentWave tăng đúng 1.
                    if (scheduler.CurrentWave <= prev) return false;
                    prev = scheduler.CurrentWave;
                    // Tick qua Pha_Chuẩn_Bị để vào Active và (do cấu hình tối thiểu) Cleared
                    // ngay trong cùng Tick — sẵn sàng cho iteration kế tiếp.
                    scheduler.Tick(5f, 0);
                }
                return true;
            });
        }
    }
}
