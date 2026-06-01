// Feature: tower-defense-vn, Property 20: Round-trip âm lượng có clamp và mặc định
// Validates: Requirements 12.1, 12.2, 12.3

using System;
using FsCheck;
using NUnit.Framework;
using CSVH.Core.Storage;

namespace CSVH.Tests.Edit.Properties
{
    public class Property20_VolumeRoundTrip
    {
        [Test]
        public void VolumeIsClampedAndRoundTrips()
        {
            PbtRunner.RunForAll<float>(v =>
            {
                var s = new InMemoryStorageService();
                s.WriteVolume(VolumeChannel.Music, v);
                float read = s.ReadVolume(VolumeChannel.Music);
                float expected = float.IsNaN(v) ? 1f : Math.Clamp(v, 0f, 1f);
                return read == expected;
            });
        }

        [Test]
        public void VolumeChannelsAreIndependent()
        {
            PbtRunner.RunForAll<float, float>((vMusic, vSfx) =>
            {
                var s = new InMemoryStorageService();
                s.WriteVolume(VolumeChannel.Music, vMusic);
                s.WriteVolume(VolumeChannel.Sfx, vSfx);
                float emusic = float.IsNaN(vMusic) ? 1f : Math.Clamp(vMusic, 0f, 1f);
                float esfx = float.IsNaN(vSfx) ? 1f : Math.Clamp(vSfx, 0f, 1f);
                return s.ReadVolume(VolumeChannel.Music) == emusic
                    && s.ReadVolume(VolumeChannel.Sfx) == esfx;
            });
        }

        [Test]
        public void DefaultVolumeIsOne()
        {
            var s = new InMemoryStorageService();
            Assert.AreEqual(1f, s.ReadVolume(VolumeChannel.Music));
            Assert.AreEqual(1f, s.ReadVolume(VolumeChannel.Sfx));
        }
    }
}
