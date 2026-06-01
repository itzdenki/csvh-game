// Feature: tower-defense-vn
// Helper chạy FsCheck thuần (không qua FsCheck.NUnit) — Unity Test Framework
// đi kèm NUnit 3.5 nên FsCheck.NUnit ≥ 2.7 (yêu cầu NUnit ≥ 3.6+) không tương thích.
// Cách dùng: trong test gắn [Test], gọi PbtRunner.RunForAll(...) — runner sẽ tự
// sample đầu vào từ Arbitrary mặc định (hoặc Arbitrary tuỳ biến truyền vào) và
// chạy property `maxTest` lần. Khi gặp counter-example, ném NUnit AssertionException.

using System;
using FsCheck;
using NUnit.Framework;

namespace CSVH.Tests.Edit.Properties
{
    internal static class PbtRunner
    {
        private const int DefaultMaxTest = 100;
        private const int DefaultSize = 50;

        /// <summary>
        /// PBT 1 tham số: dùng Arbitrary mặc định cho <typeparamref name="T"/>.
        /// </summary>
        public static void RunForAll<T>(Func<T, bool> property, int maxTest = DefaultMaxTest)
        {
            var arb = Arb.From<T>();
            RunForAll(arb, property, maxTest);
        }

        /// <summary>
        /// PBT 1 tham số với Arbitrary tuỳ biến (dùng cho generators trong test fixture).
        /// </summary>
        public static void RunForAll<T>(Arbitrary<T> arbitrary, Func<T, bool> property, int maxTest = DefaultMaxTest)
        {
            if (arbitrary == null) throw new ArgumentNullException(nameof(arbitrary));
            if (property == null) throw new ArgumentNullException(nameof(property));

            // Gen.Sample(size, n, gen) trả về mảng n giá trị cùng size, deterministic theo
            // seed mặc định của FsCheck.Random — đủ tốt cho repro test failure.
            var samples = FsCheck.Gen.Sample<T>(DefaultSize, maxTest, arbitrary.Generator);
            for (int i = 0; i < samples.Length; i++)
            {
                var value = samples[i];
                bool ok;
                try { ok = property(value); }
                catch (Exception ex)
                {
                    Assert.Fail($"Property threw at iteration {i + 1}/{maxTest} for input: {Describe(value)}\n{ex}");
                    return;
                }
                if (!ok)
                {
                    Assert.Fail($"Property failed at iteration {i + 1}/{maxTest} for input: {Describe(value)}");
                }
            }
        }

        public static void RunForAll<T1, T2>(Func<T1, T2, bool> property, int maxTest = DefaultMaxTest)
        {
            RunForAll<Tuple<T1, T2>>(t => property(t.Item1, t.Item2), maxTest);
        }

        public static void RunForAll<T1, T2, T3>(Func<T1, T2, T3, bool> property, int maxTest = DefaultMaxTest)
        {
            RunForAll<Tuple<T1, T2, T3>>(t => property(t.Item1, t.Item2, t.Item3), maxTest);
        }

        public static void RunForAll<T1, T2, T3, T4>(Func<T1, T2, T3, T4, bool> property, int maxTest = DefaultMaxTest)
        {
            RunForAll<Tuple<T1, T2, T3, T4>>(t => property(t.Item1, t.Item2, t.Item3, t.Item4), maxTest);
        }

        private static string Describe(object value)
        {
            if (value == null) return "<null>";
            try { return value.ToString(); }
            catch { return value.GetType().Name; }
        }
    }
}
