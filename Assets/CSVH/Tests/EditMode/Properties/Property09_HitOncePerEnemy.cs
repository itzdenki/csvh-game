// Feature: tower-defense-vn, Property 9: Mỗi Đạn gây sát thương cho mỗi Quái tối đa một lần
// Validates: Requirements 3.6

using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using CSVH.Core.Combat;

namespace CSVH.Tests.Edit.Properties
{
    public class Property09_HitOncePerEnemy
    {
        [Test]
        public void TryRegisterHitReturnsTrueExactlyOncePerEnemy()
        {
            PbtRunner.RunForAll<int[]>(hitSequence =>
            {
                if (hitSequence == null) hitSequence = System.Array.Empty<int>();
                var logic = new ProjectileLogic();
                var truthCount = new Dictionary<int, int>();
                foreach (var id in hitSequence)
                {
                    if (logic.TryRegisterHit(id))
                    {
                        if (!truthCount.ContainsKey(id)) truthCount[id] = 0;
                        truthCount[id]++;
                    }
                }
                // Each id appears at most once with truthCount == 1
                return truthCount.Values.All(v => v == 1);
            });
        }
    }
}
