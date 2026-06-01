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
            [Tooltip("Khớp đúng với EnemyConfig.Id trong enemies.json (vd. \"Hồ_Tinh\").")]
            public string Id;

            [Tooltip("Sprite hiển thị cho Loại_Quái này.")]
            public Sprite Sprite;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        private Dictionary<string, Sprite> _cache;

        private void OnEnable()
        {
            // Reset cache khi designer chỉnh entries trong Editor.
            _cache = null;
        }

        /// <summary>
        /// Tra cứu sprite theo Id. Trả <c>null</c> khi không tìm thấy — caller
        /// (spawner) sẽ giữ nguyên sprite mặc định trên prefab.
        /// </summary>
        public Sprite GetSprite(string id)
        {
            if (string.IsNullOrEmpty(id) || _entries == null) return null;

            if (_cache == null)
            {
                _cache = new Dictionary<string, Sprite>(_entries.Count, System.StringComparer.Ordinal);
                for (int i = 0; i < _entries.Count; i++)
                {
                    var e = _entries[i];
                    if (!string.IsNullOrEmpty(e.Id) && e.Sprite != null && !_cache.ContainsKey(e.Id))
                    {
                        _cache[e.Id] = e.Sprite;
                    }
                }
            }

            return _cache.TryGetValue(id, out var sprite) ? sprite : null;
        }
    }
}
