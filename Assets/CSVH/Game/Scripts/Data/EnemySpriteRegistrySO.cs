// Feature: tower-defense-vn — Enemy sprite registry (Unity layer)
// Maps EnemyConfig.Id → Sprite for the spawner view. Core remains Unity-free;
// this lookup happens at the view boundary in EnemySpawnerView.

using System.Collections.Generic;
using UnityEngine;

namespace CSVH.Game.Data
{
    /// <summary>
    /// ScriptableObject ánh xạ <see cref="CSVH.Core.Config.EnemyConfig.Id"/> →
    /// <see cref="Sprite"/>. Designer điền tại Inspector cho từng Loại_Quái
    /// (vd. Hồ_Tinh → Enemy_Ho_Tinh.png). EnemySpawnerView tra cứu sprite theo
    /// <c>SpawnIntent.Enemy.Id</c> ngay trước khi Instantiate prefab và gán vào
    /// <see cref="SpriteRenderer.sprite"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemySpriteRegistry",
        menuName = "CSVH/Enemy Sprite Registry",
        order = 1)]
    public sealed class EnemySpriteRegistrySO : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            [Tooltip("Khớp đúng với EnemyConfig.Id trong enemies.json (vd. \"Mot_Go\").")]
            public string Id;

            [Tooltip("Sprite hiển thị cho Loại_Quái này.")]
            public Sprite Sprite;

            [Tooltip("Hệ số kích thước riêng (nhân lên trên độ cao chuẩn hóa của spawner). " +
                     "≤ 0 được coi là 1. Vd. boss = 2.5 để to hơn, quái nhỏ < 1.")]
            public float Scale;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        private Dictionary<string, Sprite> _cache;
        private Dictionary<string, float> _scaleCache;

        private void OnEnable()
        {
            // Reset cache khi designer chỉnh entries trong Editor.
            _cache = null;
            _scaleCache = null;
        }

        private void EnsureCache()
        {
            if (_cache != null)
            {
                return;
            }

            int n = _entries?.Count ?? 0;
            _cache = new Dictionary<string, Sprite>(n, System.StringComparer.Ordinal);
            _scaleCache = new Dictionary<string, float>(n, System.StringComparer.Ordinal);
            for (int i = 0; i < n; i++)
            {
                var e = _entries[i];
                if (string.IsNullOrEmpty(e.Id))
                {
                    continue;
                }
                if (e.Sprite != null && !_cache.ContainsKey(e.Id))
                {
                    _cache[e.Id] = e.Sprite;
                }
                if (!_scaleCache.ContainsKey(e.Id))
                {
                    _scaleCache[e.Id] = e.Scale > 0f ? e.Scale : 1f;
                }
            }
        }

        /// <summary>
        /// Tra cứu sprite theo Id. Trả <c>null</c> khi không tìm thấy — caller
        /// (spawner) sẽ giữ nguyên sprite mặc định trên prefab.
        /// </summary>
        public Sprite GetSprite(string id)
        {
            if (string.IsNullOrEmpty(id) || _entries == null) return null;
            EnsureCache();
            return _cache.TryGetValue(id, out var sprite) ? sprite : null;
        }

        /// <summary>
        /// Hệ số kích thước riêng cho Loại_Quái <paramref name="id"/> (mặc định <c>1</c> khi
        /// không cấu hình hoặc ≤ 0). Spawner nhân hệ số này lên độ cao chuẩn hóa.
        /// </summary>
        public float GetScale(string id)
        {
            if (string.IsNullOrEmpty(id) || _entries == null) return 1f;
            EnsureCache();
            return _scaleCache.TryGetValue(id, out var s) && s > 0f ? s : 1f;
        }
    }
}
