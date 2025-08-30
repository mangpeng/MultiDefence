#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using RpcDebug;

public class RpcMonitorWindow : EditorWindow
{
    // UI state
    private Vector2 _scroll;
    private string _search;
    private bool _showClientRpc = true;
    private bool _showServerRpc = true;
    private bool _showServerToClient = true;  // Blue
    private bool _showClientToServer = true;  // Red

    private bool _paused = false;
    private bool _autoScroll = true;

    // Time filter: last N minutes (0 = all)
    private readonly int[] _minuteChoices = { 0, 1, 5, 15, 60, 180, 720, 1440 };
    private int _minuteChoiceIndex = 0;

    // Click-to-filter sets
    private readonly HashSet<string> _methodFilter = new();
    private readonly HashSet<ulong> _senderFilter = new();

    // Selection / details panel
    private int _selectedIndex = -1; // index in the filtered list

    [MenuItem("Window/Networking/RPC Monitor")]
    public static void Open() => GetWindow<RpcMonitorWindow>("RPC Monitor");

    private void OnEnable()
    {
        _search = string.Empty;
        EditorApplication.update += Repaint;
    }

    private void OnDisable()
    {
        EditorApplication.update -= Repaint;
    }

    private void OnGUI()
    {
        // Queue → buffer 동기화 (일시정지 시에는 갱신 보류)
        if (!_paused)
            RpcLogger.Flush();

        DrawToolbar();
        EditorGUILayout.Space(4);
        DrawTagFilters();
        EditorGUILayout.Space(4);
        DrawListWithDetails();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            // 검색창
            var searchStyle = EditorStyles.toolbarSearchField ?? EditorStyles.textField;
            _search = GUILayout.TextField(_search ?? string.Empty, searchStyle, GUILayout.MinWidth(200));

            if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(20)))
                _search = string.Empty;

            GUILayout.Space(10);

            // 시간 필터
            GUILayout.Label("Last (min):", EditorStyles.miniLabel, GUILayout.Width(65));
            _minuteChoiceIndex = EditorGUILayout.Popup(_minuteChoiceIndex,
                _minuteChoices.Select(m => m == 0 ? "All" : m.ToString()).ToArray(),
                GUILayout.Width(70));

            GUILayout.FlexibleSpace();

            // 토글들
            _showClientRpc = GUILayout.Toggle(_showClientRpc, "ClientRpc", EditorStyles.toolbarButton);
            _showServerRpc = GUILayout.Toggle(_showServerRpc, "ServerRpc", EditorStyles.toolbarButton);
            _showServerToClient = GUILayout.Toggle(_showServerToClient, "S→C (blue)", EditorStyles.toolbarButton);
            _showClientToServer = GUILayout.Toggle(_showClientToServer, "C→S (red)", EditorStyles.toolbarButton);

            _paused = GUILayout.Toggle(_paused, _paused ? "Paused" : "Live", EditorStyles.toolbarButton);
            _autoScroll = GUILayout.Toggle(_autoScroll, "AutoScroll", EditorStyles.toolbarButton);

            // 필터 초기화
            if (GUILayout.Button("Clear Filters", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                _methodFilter.Clear();
                _senderFilter.Clear();
                _search = string.Empty;
                _selectedIndex = -1;
            }

            // CSV 내보내기
            if (GUILayout.Button("Export CSV", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                ExportCsv();
            }
        }
    }

    private void DrawTagFilters()
    {
        var entries = RpcLogger.Entries;
        if (entries == null || entries.Count == 0)
        {
            EditorGUILayout.HelpBox("No RPC logs yet. Call RpcLogger.Log(...) inside your RPC methods.", MessageType.Info);
            return;
        }

        var methodGroups = entries.Select(e => e.Method).Where(m => !string.IsNullOrEmpty(m)).Distinct().OrderBy(s => s);
        var senderGroups = entries.Select(e => e.SenderClientId).Distinct().OrderBy(id => id);

        GUILayout.Label("Methods:", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            foreach (var m in methodGroups)
            {
                var on = _methodFilter.Contains(m);
                var toggled = GUILayout.Toggle(on, m, "Button");
                if (toggled != on)
                {
                    if (toggled) _methodFilter.Add(m);
                    else _methodFilter.Remove(m);
                }
            }
        }

        GUILayout.Label("Senders:", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            foreach (var id in senderGroups)
            {
                var label = $"Sender {id}";
                var on = _senderFilter.Contains(id);
                var toggled = GUILayout.Toggle(on, label, "Button");
                if (toggled != on)
                {
                    if (toggled) _senderFilter.Add(id);
                    else _senderFilter.Remove(id);
                }
            }
        }
    }

    private void DrawListWithDetails()
    {
        var entries = RpcLogger.Entries ?? (IReadOnlyList<RpcLogEntry>)Array.Empty<RpcLogEntry>();
        var filtered = entries.Where(FilterByAll).OrderByDescending(e => e.Timestamp).ToList();

        // 리스트
        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(position.height * 0.6f));
        for (int i = 0; i < filtered.Count; i++)
        {
            var e = filtered[i];
            var color = e.Direction == RpcDirection.ServerToClient ? Color.cyan : Color.red;

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                var prev = GUI.color;
                GUI.color = color;
                var title = $"{e.Timestamp:HH:mm:ss.fff}  [{e.Kind}]  {e.Direction}  {e.Method}";
                if (GUILayout.Button(title, EditorStyles.boldLabel))
                {
                    _selectedIndex = i;
                }
                GUI.color = prev;

                GUILayout.Label($"Sender: {e.SenderClientId}   Targets: {e.Targets}");

                if (!string.IsNullOrEmpty(e.PayloadSummary))
                {
                    // 한 줄 요약만 먼저 표시
                    GUILayout.Label($"Payload: {TrimForOneLine(e.PayloadSummary)}");
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button($"Filter: {e.Method}", GUILayout.Width(160)))
                    {
                        if (!_methodFilter.Add(e.Method)) _methodFilter.Remove(e.Method);
                    }
                    if (GUILayout.Button($"Filter: Sender {e.SenderClientId}", GUILayout.Width(190)))
                    {
                        if (!_senderFilter.Add(e.SenderClientId)) _senderFilter.Remove(e.SenderClientId);
                    }
                    if (GUILayout.Button("Copy Row", GUILayout.Width(100)))
                    {
                        EditorGUIUtility.systemCopyBuffer =
                            $"{e.Timestamp:o},{e.Kind},{e.Direction},{e.Method},{e.SenderClientId},{e.Targets},{e.PayloadSummary}";
                    }
                }
            }
        }
        EditorGUILayout.EndScrollView();

        // 자동 스크롤
        if (_autoScroll && Event.current.type == EventType.Repaint && !_paused)
            _scroll.y = float.MaxValue;

        // 상세 패널 (선택된 로그)
        DrawDetailsPanel(filtered);
    }

    private void DrawDetailsPanel(List<RpcLogEntry> filtered)
    {
        GUILayout.Label("Details", EditorStyles.boldLabel);
        using (new GUILayout.VerticalScope(GUI.skin.box))
        {
            if (_selectedIndex < 0 || _selectedIndex >= filtered.Count)
            {
                GUILayout.Label("Select a row to see details.");
                return;
            }

            var e = filtered[_selectedIndex];
            EditorGUILayout.LabelField("Time", e.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            EditorGUILayout.LabelField("Kind", e.Kind.ToString());
            EditorGUILayout.LabelField("Direction", e.Direction.ToString());
            EditorGUILayout.LabelField("Method", e.Method);
            EditorGUILayout.LabelField("Sender", e.SenderClientId.ToString());
            EditorGUILayout.LabelField("Targets", e.Targets);

            GUILayout.Space(6);
            GUILayout.Label("Payload", EditorStyles.boldLabel);

            var looksLikeJson = !string.IsNullOrEmpty(e.PayloadSummary) &&
                                (e.PayloadSummary.TrimStart().StartsWith("{") || e.PayloadSummary.TrimStart().StartsWith("["));

            // 탭 비슷한 스위치
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(looksLikeJson ? "JSON view available" : "Plain text", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Copy Payload"))
                    EditorGUIUtility.systemCopyBuffer = e.PayloadSummary ?? string.Empty;
            }

            // JSON이면 Pretty Print 버튼
            if (looksLikeJson)
            {
                if (GUILayout.Button("Pretty Print JSON"))
                {
                    try
                    {
                        _prettyCache = PrettyJson(e.PayloadSummary);
                        _prettyOk = true;
                    }
                    catch
                    {
                        _prettyOk = false;
                        _prettyCache = "Failed to pretty-print JSON.";
                    }
                }

                if (_prettyOk && !string.IsNullOrEmpty(_prettyCache))
                {
                    EditorGUILayout.TextArea(_prettyCache, GUILayout.MinHeight(100));
                }
                else
                {
                    EditorGUILayout.TextArea(e.PayloadSummary ?? string.Empty, GUILayout.MinHeight(100));
                }
            }
            else
            {
                EditorGUILayout.TextArea(e.PayloadSummary ?? string.Empty, GUILayout.MinHeight(100));
            }
        }
    }

    private string _prettyCache;
    private bool _prettyOk;

    // --- Helpers ---

    private bool FilterByAll(RpcLogEntry e)
    {
        if (!_showClientRpc && e.Kind == RpcKind.ClientRpc) return false;
        if (!_showServerRpc && e.Kind == RpcKind.ServerRpc) return false;
        if (!_showServerToClient && e.Direction == RpcDirection.ServerToClient) return false;
        if (!_showClientToServer && e.Direction == RpcDirection.ClientToServer) return false;

        if (_methodFilter.Count > 0 && !_methodFilter.Contains(e.Method)) return false;
        if (_senderFilter.Count > 0 && !_senderFilter.Contains(e.SenderClientId)) return false;

        // 시간 필터
        int minutes = _minuteChoices[Mathf.Clamp(_minuteChoiceIndex, 0, _minuteChoices.Length - 1)];
        if (minutes > 0)
        {
            DateTime threshold = DateTime.Now.AddMinutes(-minutes);
            if (e.Timestamp < threshold) return false;
        }

        // 검색어
        if (!string.IsNullOrEmpty(_search))
        {
            var s = _search.ToLowerInvariant();
            bool hit =
                (!string.IsNullOrEmpty(e.Method) && e.Method.ToLowerInvariant().Contains(s)) ||
                (!string.IsNullOrEmpty(e.PayloadSummary) && e.PayloadSummary.ToLowerInvariant().Contains(s)) ||
                (!string.IsNullOrEmpty(e.Targets) && e.Targets.ToLowerInvariant().Contains(s));
            if (!hit) return false;
        }
        return true;
    }

    private static string TrimForOneLine(string text, int max = 120)
    {
        if (string.IsNullOrEmpty(text)) return "";
        text = text.Replace("\n", " ").Replace("\r", " ");
        return text.Length <= max ? text : text.Substring(0, max) + " …";
    }

    /// <summary>아주 가벼운 JSON pretty printer (의존성 없이 동작)</summary>
    private static string PrettyJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return string.Empty;

        var sb = new StringBuilder(json.Length * 2);
        int indent = 0;
        bool inString = false;
        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];
            switch (c)
            {
                case '"':
                    sb.Append(c);
                    // 이스케이프된 쿼트인지 확인
                    bool escaped = false;
                    int j = i;
                    while (j > 0 && json[--j] == '\\') escaped = !escaped;
                    if (!escaped) inString = !inString;
                    break;

                case '{':
                case '[':
                    sb.Append(c);
                    if (!inString)
                    {
                        sb.Append('\n');
                        indent++;
                        sb.Append(new string(' ', indent * 2));
                    }
                    break;

                case '}':
                case ']':
                    if (!inString)
                    {
                        sb.Append('\n');
                        indent = Math.Max(0, indent - 1);
                        sb.Append(new string(' ', indent * 2));
                        sb.Append(c);
                    }
                    else sb.Append(c);
                    break;

                case ',':
                    sb.Append(c);
                    if (!inString)
                    {
                        sb.Append('\n');
                        sb.Append(new string(' ', indent * 2));
                    }
                    break;

                case ':':
                    sb.Append(inString ? ":" : ": ");
                    break;

                default:
                    sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    private void ExportCsv()
    {
        var entries = RpcLogger.Entries ?? (IReadOnlyList<RpcLogEntry>)Array.Empty<RpcLogEntry>();
        var rows = entries.Where(FilterByAll).OrderBy(e => e.Timestamp).ToList();
        if (rows.Count == 0)
        {
            EditorUtility.DisplayDialog("Export CSV", "No rows to export with current filters.", "OK");
            return;
        }

        string path = EditorUtility.SaveFilePanel("Export RPC Logs", "", "rpc_logs.csv", "csv");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            using var sw = new StreamWriter(path, false, new UTF8Encoding(true));
            sw.WriteLine("Timestamp,Kind,Direction,Method,SenderClientId,Targets,Payload");

            foreach (var e in rows)
            {
                // CSV escaping
                string Esc(string s)
                {
                    if (s == null) return "";
                    bool needQuote = s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
                    s = s.Replace("\"", "\"\"");
                    return needQuote ? $"\"{s}\"" : s;
                }

                sw.WriteLine(
                    $"{e.Timestamp:O}," +
                    $"{e.Kind}," +
                    $"{e.Direction}," +
                    $"{Esc(e.Method)}," +
                    $"{e.SenderClientId}," +
                    $"{Esc(e.Targets)}," +
                    $"{Esc(e.PayloadSummary)}"
                );
            }

            sw.Flush();
            EditorUtility.RevealInFinder(path);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RPC Monitor] CSV export failed: {ex.Message}");
            EditorUtility.DisplayDialog("Export CSV", "Failed to export. See console for details.", "OK");
        }
    }
}
#endif
