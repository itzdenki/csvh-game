// CSVH live-state probe — paste as the `code` argument of the UnityMCP
// `execute_code` tool while the Editor is in Play mode.
//
// IMPORTANT: execute_code wraps this as a METHOD BODY. Do NOT add `using`
// directives (they cause "Unexpected symbol" errors) — fully-qualify every
// type instead, as below. Default compiler is CodeDom (C# 6); string
// interpolation works, but newer C# features may not.
//
// Verified working 2026-06-06 against csvh@... Unity 6000.4.8f1:
//   isPlaying=True timeScale=1
//   enemyCount=3
//   tower=Tower pos=(8.00, -5.00, 0.00) rotZ=0.0

var enemies = GameObject.FindObjectsByType<CSVH.Game.Spawning.EnemyView>(FindObjectsSortMode.None);
var tower   = GameObject.FindFirstObjectByType<CSVH.Game.Tower.TowerView>();
var spawner = GameObject.FindFirstObjectByType<CSVH.Game.Spawning.EnemySpawnerView>();

var sb = new System.Text.StringBuilder();
sb.AppendLine($"isPlaying={Application.isPlaying} timeScale={Time.timeScale}");
sb.AppendLine($"enemyCount={enemies.Length}");
if (tower != null)
    sb.AppendLine($"tower={tower.name} pos={tower.transform.position} rotZ={tower.transform.eulerAngles.z:F1}");
sb.AppendLine($"spawnerPresent={(spawner != null)}");
return sb.ToString();
