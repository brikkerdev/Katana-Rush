using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Runner.LevelGeneration;

public class BiomeEditorWindow : EditorWindow
{
    private BiomeData _biomeData;
    private float _zoom = 1f;
    private Vector2 _panOffset;
    private HashSet<int> _selectedNodeIndices = new HashSet<int>();
    private bool _isDraggingNodes;
    private bool _isPanning;
    private bool _isCreatingConnection;
    private bool _isRectSelecting;
    private Vector2 _rectSelectStart;
    private Vector2 _rectSelectEnd;
    private int _connectionSourceNodeIndex = -1;
    private Vector2 _lastMousePos;

    private const float NODE_WIDTH = 220f;
    private const float NODE_HEADER_HEIGHT = 36f;
    private const float NODE_CONTENT_HEIGHT = 60f;
    private const float NODE_PADDING = 12f;
    private const float GRID_SIZE_SMALL = 20f;
    private const float GRID_SIZE_LARGE = 100f;
    private const float CONNECTION_WIDTH = 2.5f;
    private const float SHADOW_OFFSET = 4f;

    private static readonly Color BackgroundColor = new Color(0.12f, 0.12f, 0.14f);
    private static readonly Color GridColorSmall = new Color(1f, 1f, 1f, 0.03f);
    private static readonly Color GridColorLarge = new Color(1f, 1f, 1f, 0.06f);
    private static readonly Color NodeBodyColor = new Color(0.22f, 0.22f, 0.25f);
    private static readonly Color NodeHeaderColor = new Color(0.28f, 0.28f, 0.32f);
    private static readonly Color NodeBorderColor = new Color(0.35f, 0.35f, 0.4f);
    private static readonly Color NodeShadowColor = new Color(0f, 0f, 0f, 0.3f);
    private static readonly Color SelectedBorderColor = new Color(0.4f, 0.7f, 1f);
    private static readonly Color StartNodeAccentColor = new Color(0.3f, 0.8f, 0.4f);
    private static readonly Color EndNodeAccentColor = new Color(0.8f, 0.3f, 0.3f);
    private static readonly Color ConnectionColor = new Color(0.7f, 0.7f, 0.3f);
    private static readonly Color SelectionRectColor = new Color(0.4f, 0.7f, 1f, 0.25f);
    private static readonly Color SelectionRectBorderColor = new Color(0.4f, 0.7f, 1f, 0.8f);
    private static readonly Color TextColorPrimary = new Color(0.9f, 0.9f, 0.92f);
    private static readonly Color TextColorSecondary = new Color(0.65f, 0.65f, 0.7f);
    private static readonly Color TextColorMuted = new Color(0.5f, 0.5f, 0.55f);
    private static readonly Color ConnectorColor = new Color(0.5f, 0.5f, 0.55f);
    private static readonly Color ToolbarColor = new Color(0.18f, 0.18f, 0.2f);
    private static readonly Color InspectorBgColor = new Color(0.16f, 0.16f, 0.18f);

    private Texture2D _roundedRectTexture;
    private Texture2D _circleTexture;
    private Texture2D _shadowTexture;

    private GUIStyle _headerLabelStyle;
    private GUIStyle _contentLabelStyle;
    private GUIStyle _mutedLabelStyle;
    private GUIStyle _toolbarStyle;
    private GUIStyle _toolbarButtonStyle;
    private GUIStyle _inspectorHeaderStyle;
    private bool _stylesInitialized;

    private Vector2 _inspectorScrollPos;
    private int _hoveredNodeIndex = -1;

    private Dictionary<int, Texture2D> _prefabPreviewCache = new Dictionary<int, Texture2D>();

    private const float MINIMAP_WIDTH = 150f;
    private const float MINIMAP_HEIGHT = 100f;
    private bool _showMinimap = true;

    private int _selectedTab = 0; // 0 = Nodes, 1 = Biome Properties

    [MenuItem("Window/Biome Editor")]
    public static void OpenWindow()
    {
        var window = GetWindow<BiomeEditorWindow>();
        window.titleContent = new GUIContent("Biome Editor", EditorGUIUtility.IconContent("d_TreeEditor.Distribution").image);
        window.minSize = new Vector2(900, 600);
    }

    public static void OpenWindow(BiomeData biomeData)
    {
        var window = GetWindow<BiomeEditorWindow>();
        window.titleContent = new GUIContent("Biome Editor", EditorGUIUtility.IconContent("d_TreeEditor.Distribution").image);
        window.minSize = new Vector2(900, 600);
        window._biomeData = biomeData;
        window._prefabPreviewCache.Clear();

        if (biomeData != null)
        {
            window._panOffset = biomeData.EditorScrollPosition;
            window._zoom = Mathf.Clamp(biomeData.EditorZoom, 0.25f, 2f);
            if (window._zoom == 0) window._zoom = 1f;
        }

        window._selectedNodeIndices.Clear();
    }

    private void OnEnable()
    {
        _stylesInitialized = false;
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoRedo;
    }

    private void OnUndoRedo()
    {
        Repaint();
    }

    private void OnDestroy()
    {
        CleanupTextures();
    }

    private void CleanupTextures()
    {
        if (_roundedRectTexture != null) DestroyImmediate(_roundedRectTexture);
        if (_circleTexture != null) DestroyImmediate(_circleTexture);
        if (_shadowTexture != null) DestroyImmediate(_shadowTexture);
        _prefabPreviewCache.Clear();
    }

    private void EnsureStylesInitialized()
    {
        if (_stylesInitialized && _headerLabelStyle != null) return;

        CreateTextures();

        _headerLabelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(8, 8, 0, 0)
        };
        _headerLabelStyle.normal.textColor = TextColorPrimary;

        _contentLabelStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 10,
            wordWrap = true,
            padding = new RectOffset(4, 4, 2, 2)
        };
        _contentLabelStyle.normal.textColor = TextColorSecondary;

        _mutedLabelStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 9,
            fontStyle = FontStyle.Italic
        };
        _mutedLabelStyle.normal.textColor = TextColorMuted;

        _toolbarStyle = new GUIStyle();
        _toolbarStyle.normal.background = CreateSolidTexture(ToolbarColor);
        _toolbarStyle.fixedHeight = 32;
        _toolbarStyle.padding = new RectOffset(8, 8, 4, 4);

        _toolbarButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
        {
            fontSize = 11,
            padding = new RectOffset(12, 12, 4, 4)
        };

        _inspectorHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            padding = new RectOffset(0, 0, 4, 4)
        };
        _inspectorHeaderStyle.normal.textColor = TextColorPrimary;

        _stylesInitialized = true;
    }

    private void CreateTextures()
    {
        CleanupTextures();

        int size = 32;
        _roundedRectTexture = new Texture2D(size, size);
        _roundedRectTexture.hideFlags = HideFlags.DontSave;

        float radius = size * 0.25f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Min(x, size - 1 - x);
                float dy = Mathf.Min(y, size - 1 - y);

                float cornerDist = 0;
                if (dx < radius && dy < radius)
                {
                    cornerDist = Mathf.Sqrt((radius - dx) * (radius - dx) + (radius - dy) * (radius - dy));
                }

                float alpha = cornerDist > radius ? 0 : 1;
                _roundedRectTexture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }
        _roundedRectTexture.Apply();

        int circleSize = 16;
        _circleTexture = new Texture2D(circleSize, circleSize);
        _circleTexture.hideFlags = HideFlags.DontSave;

        Vector2 center = new Vector2(circleSize / 2f, circleSize / 2f);
        float circleRadius = circleSize / 2f - 1;

        for (int y = 0; y < circleSize; y++)
        {
            for (int x = 0; x < circleSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(circleRadius - dist + 0.5f);
                _circleTexture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }
        _circleTexture.Apply();

        int shadowSize = 16;
        _shadowTexture = new Texture2D(shadowSize, shadowSize);
        _shadowTexture.hideFlags = HideFlags.DontSave;

        for (int y = 0; y < shadowSize; y++)
        {
            for (int x = 0; x < shadowSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(shadowSize / 2f, shadowSize / 2f));
                float alpha = Mathf.Clamp01(1 - dist / (shadowSize / 2f));
                alpha = alpha * alpha * 0.5f;
                _shadowTexture.SetPixel(x, y, new Color(0, 0, 0, alpha));
            }
        }
        _shadowTexture.Apply();
    }

    private Texture2D CreateSolidTexture(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        tex.hideFlags = HideFlags.DontSave;
        return tex;
    }

    private void OnGUI()
    {
        EnsureStylesInitialized();

        DrawToolbar();

        if (_biomeData == null)
        {
            DrawNoDataMessage();
            return;
        }

        if (_biomeData.SegmentNodes == null)
        {
            _biomeData.SegmentNodes = new SegmentNodeData[0];
        }

        float toolbarHeight = 32;
        float inspectorWidth = 300;
        Rect canvasRect = new Rect(0, toolbarHeight, position.width - inspectorWidth, position.height - toolbarHeight);
        Rect inspectorRect = new Rect(position.width - inspectorWidth, toolbarHeight, inspectorWidth, position.height - toolbarHeight);

        DrawCanvas(canvasRect);
        DrawInspector(inspectorRect);

        if (_showMinimap)
        {
            DrawMinimap(canvasRect);
        }

        DrawStatusBar(canvasRect);

        ProcessEvents(canvasRect);

        if (_isCreatingConnection || _isRectSelecting)
        {
            Repaint();
        }
        else if (_isDraggingNodes || _isPanning)
        {
            Repaint();
        }
    }

    #region Toolbar

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(_toolbarStyle, GUILayout.Height(32));

        EditorGUILayout.LabelField("Biome:", GUILayout.Width(50));
        EditorGUI.BeginChangeCheck();
        _biomeData = (BiomeData)EditorGUILayout.ObjectField(_biomeData, typeof(BiomeData), false, GUILayout.Width(180));
        if (EditorGUI.EndChangeCheck() && _biomeData != null)
        {
            _panOffset = _biomeData.EditorScrollPosition;
            _zoom = _biomeData.EditorZoom > 0 ? _biomeData.EditorZoom : 1f;
            _selectedNodeIndices.Clear();
            _prefabPreviewCache.Clear();
        }

        GUILayout.Space(20);

        if (_biomeData != null)
        {
            var addIcon = EditorGUIUtility.IconContent("d_Toolbar Plus");
            if (GUILayout.Button(new GUIContent(" Add Node", addIcon.image), _toolbarButtonStyle, GUILayout.Width(100)))
            {
                AddNode();
            }

            GUILayout.Space(8);

            var centerIcon = EditorGUIUtility.IconContent("d_SceneViewCamera");
            if (GUILayout.Button(new GUIContent(" Center", centerIcon.image), _toolbarButtonStyle, GUILayout.Width(80)))
            {
                CenterView();
            }

            var fitIcon = EditorGUIUtility.IconContent("d_ViewToolZoom");
            if (GUILayout.Button(new GUIContent(" Fit All", fitIcon.image), _toolbarButtonStyle, GUILayout.Width(80)))
            {
                FitAllNodes();
            }

            GUILayout.FlexibleSpace();

            var minimapIcon = EditorGUIUtility.IconContent("d_SceneViewVisibility");
            _showMinimap = GUILayout.Toggle(_showMinimap, new GUIContent(" Map", minimapIcon.image), _toolbarButtonStyle, GUILayout.Width(60));

            GUILayout.Space(10);

            GUILayout.Label("", GUILayout.Width(20));
            var zoomOutIcon = EditorGUIUtility.IconContent("d_ViewToolZoom On");
            GUILayout.Label(zoomOutIcon, GUILayout.Width(18));
            _zoom = GUILayout.HorizontalSlider(_zoom, 0.25f, 2f, GUILayout.Width(100));
            GUILayout.Label($"{_zoom * 100:F0}%", EditorStyles.miniLabel, GUILayout.Width(40));

            GUILayout.Space(10);

            var saveIcon = EditorGUIUtility.IconContent("d_SaveAs");
            if (GUILayout.Button(new GUIContent(" Save", saveIcon.image), _toolbarButtonStyle, GUILayout.Width(70)))
            {
                SaveBiomeData();
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region Canvas

    private void DrawCanvas(Rect canvasRect)
    {
        EditorGUI.DrawRect(canvasRect, BackgroundColor);

        GUI.BeginClip(canvasRect);
        Rect localRect = new Rect(0, 0, canvasRect.width, canvasRect.height);

        DrawGrid(localRect);
        DrawConnections();
        
        if (_isCreatingConnection)
        {
            DrawCreatingConnection();
        }

        for (int i = 0; i < _biomeData.SegmentNodes.Length; i++)
        {
            DrawNode(i);
        }

        if (_isRectSelecting)
        {
            DrawSelectionRect();
        }

        GUI.EndClip();
    }

    private void DrawGrid(Rect canvasRect)
    {
        Handles.BeginGUI();

        float smallSpacing = GRID_SIZE_SMALL * _zoom;
        if (smallSpacing >= 10)
        {
            DrawGridLines(canvasRect, smallSpacing, GridColorSmall);
        }

        float largeSpacing = GRID_SIZE_LARGE * _zoom;
        DrawGridLines(canvasRect, largeSpacing, GridColorLarge);

        Vector2 origin = _panOffset;
        if (origin.x >= 0 && origin.x <= canvasRect.width && origin.y >= 0 && origin.y <= canvasRect.height)
        {
            Handles.color = new Color(1, 1, 1, 0.15f);
            Handles.DrawLine(new Vector3(origin.x - 10, origin.y), new Vector3(origin.x + 10, origin.y));
            Handles.DrawLine(new Vector3(origin.x, origin.y - 10), new Vector3(origin.x, origin.y + 10));
        }

        Handles.EndGUI();
    }

    private void DrawGridLines(Rect canvasRect, float spacing, Color color)
    {
        Handles.color = color;

        float offsetX = _panOffset.x % spacing;
        float offsetY = _panOffset.y % spacing;

        int numLinesX = Mathf.CeilToInt(canvasRect.width / spacing) + 1;
        int numLinesY = Mathf.CeilToInt(canvasRect.height / spacing) + 1;

        for (int i = 0; i <= numLinesX; i++)
        {
            float x = offsetX + i * spacing;
            Handles.DrawLine(new Vector3(x, 0), new Vector3(x, canvasRect.height));
        }

        for (int i = 0; i <= numLinesY; i++)
        {
            float y = offsetY + i * spacing;
            Handles.DrawLine(new Vector3(0, y), new Vector3(canvasRect.width, y));
        }
    }

    private void DrawNode(int index)
    {
        if (index < 0 || index >= _biomeData.SegmentNodes.Length) return;

        var node = _biomeData.SegmentNodes[index];
        if (node == null) return;

        Vector2 nodePos = node.NodePosition * _zoom + _panOffset;
        float nodeWidth = NODE_WIDTH * _zoom;
        float nodeHeight = (NODE_HEADER_HEIGHT + NODE_CONTENT_HEIGHT) * _zoom;

        bool isSelected = _selectedNodeIndices.Contains(index);
        bool isHovered = _hoveredNodeIndex == index;

        Rect nodeRect = new Rect(nodePos.x, nodePos.y, nodeWidth, nodeHeight);

        Color accentColor = node.IsStartNode ? StartNodeAccentColor : (node.IsEndNode ? EndNodeAccentColor : Color.clear);

        Handles.BeginGUI();
        Handles.color = NodeShadowColor;
        Handles.DrawAAPolyLine(_shadowTexture, SHADOW_OFFSET * _zoom, new Vector3[]
        {
            new Vector3(nodeRect.x + 4, nodeRect.y + nodeRect.height - 4),
            new Vector3(nodeRect.x + 4, nodeRect.y + 4),
            new Vector3(nodeRect.x + nodeRect.width - 4, nodeRect.y + 4),
            new Vector3(nodeRect.x + nodeRect.width - 4, nodeRect.y + nodeRect.height - 4),
            new Vector3(nodeRect.x + 4, nodeRect.y + nodeRect.height - 4)
        });
        Handles.EndGUI();

        EditorGUI.DrawRect(nodeRect, NodeBodyColor);

        Color borderColor = isSelected ? SelectedBorderColor : (isHovered ? new Color(0.5f, 0.5f, 0.55f) : NodeBorderColor);
        if (node.IsStartNode || node.IsEndNode)
        {
            borderColor = accentColor;
        }

        DrawRoundedRectOutline(nodeRect, borderColor, 2f);

        Rect headerRect = new Rect(nodeRect.x, nodeRect.y, nodeRect.width, NODE_HEADER_HEIGHT * _zoom);
        EditorGUI.DrawRect(headerRect, accentColor == Color.clear ? NodeHeaderColor : new Color(NodeHeaderColor.r + accentColor.r * 0.3f, NodeHeaderColor.g + accentColor.g * 0.3f, NodeHeaderColor.b + accentColor.b * 0.3f));

        Rect contentRect = new Rect(nodeRect.x + NODE_PADDING * _zoom, nodeRect.y + NODE_HEADER_HEIGHT * _zoom + NODE_PADDING * _zoom, nodeRect.width - NODE_PADDING * 2 * _zoom, nodeRect.height - NODE_HEADER_HEIGHT * _zoom - NODE_PADDING * 2 * _zoom);

        string segmentName = node.Segment != null ? node.Segment.name : "None";
        string displayText = node.NodeName + "\n" + segmentName;

        if (node.Connections != null && node.Connections.Length > 0)
        {
            displayText += $"\nOut: {node.Connections.Length}";
        }
        
        displayText += $"\nW: {node.Weight:F1} CD: {node.Cooldown}";

        float iconSize = 48f * _zoom;
        float textOffset = iconSize + NODE_PADDING * _zoom;

        if (node.Segment != null)
        {
            if (!_prefabPreviewCache.TryGetValue(index, out Texture2D preview) || preview == null)
            {
                GameObject segmentPrefab = node.Segment.gameObject;
                preview = AssetPreview.GetAssetPreview(segmentPrefab);
                if (preview != null)
                {
                    _prefabPreviewCache[index] = preview;
                }
            }

            if (preview != null)
            {
                Rect iconRect = new Rect(contentRect.x, contentRect.y + (contentRect.height - iconSize) / 2, iconSize, iconSize);
                GUI.DrawTexture(iconRect, preview, ScaleMode.ScaleToFit);
            }
        }

        Rect textRect = new Rect(contentRect.x + textOffset, contentRect.y, contentRect.width - textOffset, contentRect.height);
        GUI.Label(textRect, displayText, _contentLabelStyle);

        if (isSelected)
        {
            Rect deleteButtonRect = new Rect(nodeRect.xMax - 20 * _zoom, nodeRect.y + 4 * _zoom, 16 * _zoom, 16 * _zoom);
            if (GUI.Button(deleteButtonRect, "x", EditorStyles.miniButton))
            {
                DeleteSelectedNodes();
            }
        }
    }

    private void DrawRoundedRectOutline(Rect rect, Color color, float width)
    {
        Handles.BeginGUI();
        Handles.color = color;
        Handles.DrawAAPolyLine(width, new Vector3[]
        {
            new Vector3(rect.x + 4, rect.y),
            new Vector3(rect.x + rect.width - 4, rect.y),
            new Vector3(rect.x + rect.width, rect.y + 4),
            new Vector3(rect.x + rect.width, rect.y + rect.height - 4),
            new Vector3(rect.x + rect.width - 4, rect.y + rect.height),
            new Vector3(rect.x + 4, rect.y + rect.height),
            new Vector3(rect.x, rect.y + rect.height - 4),
            new Vector3(rect.x, rect.y + 4),
            new Vector3(rect.x + 4, rect.y)
        });
        Handles.EndGUI();
    }

    private void DrawConnections()
    {
        if (_biomeData.SegmentNodes == null) return;

        Handles.BeginGUI();

        for (int i = 0; i < _biomeData.SegmentNodes.Length; i++)
        {
            var node = _biomeData.SegmentNodes[i];
            if (node == null || node.Connections == null) continue;

            Vector2 startPos = node.NodePosition * _zoom + _panOffset + new Vector2(NODE_WIDTH * _zoom / 2, (NODE_HEADER_HEIGHT + NODE_CONTENT_HEIGHT) * _zoom);

            for (int j = 0; j < node.Connections.Length; j++)
            {
                int targetIndex = node.Connections[j];
                if (targetIndex < 0 || targetIndex >= _biomeData.SegmentNodes.Length) continue;

                var targetNode = _biomeData.SegmentNodes[targetIndex];
                if (targetNode == null) continue;

                Vector2 endPos = targetNode.NodePosition * _zoom + _panOffset + new Vector2(NODE_WIDTH * _zoom / 2, NODE_HEADER_HEIGHT * _zoom / 2);

                DrawConnectionLine(startPos, endPos, ConnectionColor);
            }
        }

        Handles.EndGUI();
    }

    private void DrawConnectionLine(Vector2 start, Vector2 end, Color color)
    {
        Handles.color = color;

        Vector2 direction = (end - start).normalized;
        float distance = Vector2.Distance(start, end);
        
        Vector2 midPoint = (start + end) / 2f;
        
        float curveOffset = distance * 0.1f;
        Vector2 controlPoint1 = start + direction * distance * 0.25f + new Vector2(0, curveOffset);
        Vector2 controlPoint2 = end - direction * distance * 0.25f + new Vector2(0, curveOffset);

        Handles.DrawBezier(start, end, controlPoint1, controlPoint2, color, null, CONNECTION_WIDTH * _zoom);

        Vector2 arrowPos = end - direction * 10 * _zoom;
        float arrowSize = 8 * _zoom;
        
        Vector2 perpendicular = Vector2.Perpendicular(direction);
        
        Handles.DrawAAConvexPolygon(new Vector3[]
        {
            arrowPos,
            arrowPos - direction * arrowSize + perpendicular * arrowSize * 0.5f,
            arrowPos - direction * arrowSize - perpendicular * arrowSize * 0.5f
        });
    }

    private void DrawCreatingConnection()
    {
        if (_connectionSourceNodeIndex < 0 || _connectionSourceNodeIndex >= _biomeData.SegmentNodes.Length) return;

        var sourceNode = _biomeData.SegmentNodes[_connectionSourceNodeIndex];
        Vector2 startPos = sourceNode.NodePosition * _zoom + _panOffset + new Vector2(NODE_WIDTH * _zoom / 2, (NODE_HEADER_HEIGHT + NODE_CONTENT_HEIGHT) * _zoom);

        Vector2 mousePos = Event.current.mousePosition;
        Vector2 endPos = mousePos;

        Handles.BeginGUI();
        Handles.color = ConnectionColor;
        Handles.DrawBezier(startPos, endPos, startPos + new Vector2(50, 0), endPos - new Vector2(50, 0), ConnectionColor, null, CONNECTION_WIDTH * _zoom);
        Handles.EndGUI();
    }

    private void DrawSelectionRect()
    {
        Vector2 rectStart = _rectSelectStart;
        Vector2 rectEnd = _rectSelectEnd;

        Vector2 min = Vector2.Min(rectStart, rectEnd);
        Vector2 max = Vector2.Max(rectStart, rectEnd);

        Rect selectionRect = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);

        EditorGUI.DrawRect(selectionRect, SelectionRectColor);
        DrawRoundedRectOutline(selectionRect, SelectionRectBorderColor, 1f);
    }

    #endregion

    #region Inspector

    private void DrawInspector(Rect inspectorRect)
    {
        EditorGUI.DrawRect(inspectorRect, InspectorBgColor);

        GUILayout.BeginArea(inspectorRect);
        _inspectorScrollPos = EditorGUILayout.BeginScrollView(_inspectorScrollPos);

        EditorGUILayout.LabelField("Biome Editor", _inspectorHeaderStyle);
        EditorGUILayout.Space();

        _selectedTab = GUILayout.Toolbar(_selectedTab, new string[] { "Nodes", "Biome" });
        EditorGUILayout.Space();

        if (_selectedTab == 0)
        {
            EditorGUILayout.LabelField("Node Inspector", _inspectorHeaderStyle);
            EditorGUILayout.Space();

            if (_selectedNodeIndices.Count == 1)
            {
                int nodeIndex = _selectedNodeIndices.First();
                if (nodeIndex >= 0 && nodeIndex < _biomeData.SegmentNodes.Length)
                {
                    DrawNodeInspector(_biomeData.SegmentNodes[nodeIndex]);
                }
            }
            else if (_selectedNodeIndices.Count > 1)
            {
                EditorGUILayout.LabelField($"Multiple nodes selected ({_selectedNodeIndices.Count})", _mutedLabelStyle);
                EditorGUILayout.Space();

                if (GUILayout.Button("Delete Selected"))
                {
                    DeleteSelectedNodes();
                }
            }
            else
            {
                EditorGUILayout.LabelField("No node selected", _mutedLabelStyle);
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Segment Nodes Summary", _inspectorHeaderStyle);
            EditorGUILayout.Space();

            DrawBiomeProperties();
        }
        else
        {
            DrawBiomePropertiesTab();
        }

        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawNodeInspector(SegmentNodeData node)
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Node Name:", _contentLabelStyle);
        node.NodeName = EditorGUILayout.TextField(node.NodeName);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Segment:", _contentLabelStyle);
        EditorGUILayout.BeginHorizontal();
        LevelSegment newSegment = (LevelSegment)EditorGUILayout.ObjectField(node.Segment, typeof(LevelSegment), false);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            ShowSegmentSelector(node);
        }
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_biomeData, "Modify Node Segment");
            node.Segment = newSegment;
        }

        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        node.IsStartNode = EditorGUILayout.Toggle("Is Start Node", node.IsStartNode);
        node.IsEndNode = EditorGUILayout.Toggle("Is End Node", node.IsEndNode);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_biomeData, "Toggle Node Type");
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField($"Connections ({node.Connections?.Length ?? 0}):", _contentLabelStyle);
        
        if (node.Connections != null && node.Connections.Length > 0)
        {
            for (int i = 0; i < node.Connections.Length; i++)
            {
                int connIndex = node.Connections[i];
                string targetName = "Invalid";
                if (connIndex >= 0 && connIndex < _biomeData.SegmentNodes.Length)
                {
                    var targetNode = _biomeData.SegmentNodes[connIndex];
                    targetName = targetNode != null ? targetNode.NodeName : "Null";
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  → {connIndex}: {targetName}", _mutedLabelStyle);
                if (GUILayout.Button("x", GUILayout.Width(20)))
                {
                    node.RemoveConnection(connIndex);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.LabelField("  No connections", _mutedLabelStyle);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Create Connection Mode"))
        {
            StartConnectionMode();
        }

        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        node.Weight = EditorGUILayout.Slider("Weight", node.Weight, 0f, 10f);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_biomeData, "Modify Node Weight");
        }

        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        node.Cooldown = EditorGUILayout.IntSlider("Cooldown", node.Cooldown, 0, 20);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_biomeData, "Modify Node Cooldown");
        }
    }

    private void DrawBiomeProperties()
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Segment Nodes:", _contentLabelStyle);
        
        if (_biomeData.SegmentNodes != null && _biomeData.SegmentNodes.Length > 0)
        {
            float totalWeight = 0;
            int nodesWithSegments = 0;
            foreach (var node in _biomeData.SegmentNodes)
            {
                if (node != null && node.Segment != null)
                {
                    totalWeight += node.Weight;
                    nodesWithSegments++;
                }
            }

            for (int i = 0; i < _biomeData.SegmentNodes.Length; i++)
            {
                var node = _biomeData.SegmentNodes[i];
                if (node == null) continue;

                string nodeStatus = node.Segment != null 
                    ? $"{node.Segment.name}: W={node.Weight:F1} CD={node.Cooldown}"
                    : $"Empty Node: W={node.Weight:F1} CD={node.Cooldown}";
                
                if (node.IsStartNode) nodeStatus += " [START]";
                if (node.IsEndNode) nodeStatus += " [END]";
                
                EditorGUILayout.LabelField($"  {nodeStatus}", _mutedLabelStyle);
            }

            EditorGUILayout.LabelField($"  {nodesWithSegments} nodes with segments, {totalWeight:F1} total weight", _mutedLabelStyle);
        }
        else
        {
            EditorGUILayout.LabelField("  No segment nodes. Click 'Add Node' to create nodes,", _mutedLabelStyle);
            EditorGUILayout.LabelField("  then select a node and assign a LevelSegment in the inspector.", _mutedLabelStyle);
        }

        EditorGUILayout.Space();

        if (_selectedNodeIndices.Count > 0)
        {
            bool isMultiSelect = _selectedNodeIndices.Count > 1;
            
            if (isMultiSelect)
            {
                EditorGUILayout.LabelField($"Selected Nodes: {_selectedNodeIndices.Count} nodes", _contentLabelStyle);
                
                float minWeight = float.MaxValue, maxWeight = float.MinValue;
                int minCooldown = int.MaxValue, maxCooldown = int.MinValue;
                bool weightsIdentical = true;
                bool cooldownsIdentical = true;
                
                foreach (int idx in _selectedNodeIndices)
                {
                    if (idx >= 0 && idx < _biomeData.SegmentNodes.Length)
                    {
                        var n = _biomeData.SegmentNodes[idx];
                        if (n != null)
                        {
                            if (n.Weight < minWeight) minWeight = n.Weight;
                            if (n.Weight > maxWeight) maxWeight = n.Weight;
                            if (n.Cooldown < minCooldown) minCooldown = n.Cooldown;
                            if (n.Cooldown > maxCooldown) maxCooldown = n.Cooldown;
                        }
                    }
                }
                
                weightsIdentical = Mathf.Abs(maxWeight - minWeight) < 0.001f;
                cooldownsIdentical = minCooldown == maxCooldown;
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Multi-Edit Properties", _inspectorHeaderStyle);
                
                float newWeight = weightsIdentical ? minWeight : minWeight;
                EditorGUI.BeginChangeCheck();
                newWeight = EditorGUILayout.FloatField(weightsIdentical ? "Weight:" : "Weight: (mixed)", newWeight);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_biomeData, "Set Nodes Weight");
                    foreach (int idx in _selectedNodeIndices)
                    {
                        if (idx >= 0 && idx < _biomeData.SegmentNodes.Length && _biomeData.SegmentNodes[idx] != null)
                        {
                            _biomeData.SegmentNodes[idx].Weight = newWeight;
                        }
                    }
                }
                
                int newCooldown = cooldownsIdentical ? minCooldown : minCooldown;
                EditorGUI.BeginChangeCheck();
                newCooldown = EditorGUILayout.IntField(cooldownsIdentical ? "Cooldown:" : "Cooldown: (mixed)", newCooldown);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_biomeData, "Set Nodes Cooldown");
                    foreach (int idx in _selectedNodeIndices)
                    {
                        if (idx >= 0 && idx < _biomeData.SegmentNodes.Length && _biomeData.SegmentNodes[idx] != null)
                        {
                            _biomeData.SegmentNodes[idx].Cooldown = newCooldown;
                        }
                    }
                }
                
                EditorGUILayout.Space();
                
                bool hasStartNode = false, hasEndNode = false;
                foreach (int idx in _selectedNodeIndices)
                {
                    if (idx >= 0 && idx < _biomeData.SegmentNodes.Length && _biomeData.SegmentNodes[idx] != null)
                    {
                        if (_biomeData.SegmentNodes[idx].IsStartNode) hasStartNode = true;
                        if (_biomeData.SegmentNodes[idx].IsEndNode) hasEndNode = true;
                    }
                }
                
                EditorGUILayout.LabelField("Node Flags", _inspectorHeaderStyle);
                
                bool newIsStart = EditorGUILayout.Toggle("Is Start Node:", hasStartNode);
                if (newIsStart != hasStartNode)
                {
                    Undo.RecordObject(_biomeData, "Set Nodes Start Flag");
                    foreach (int idx in _selectedNodeIndices)
                    {
                        if (idx >= 0 && idx < _biomeData.SegmentNodes.Length && _biomeData.SegmentNodes[idx] != null)
                        {
                            _biomeData.SegmentNodes[idx].IsStartNode = newIsStart;
                        }
                    }
                }
                
                bool newIsEnd = EditorGUILayout.Toggle("Is End Node:", hasEndNode);
                if (newIsEnd != hasEndNode)
                {
                    Undo.RecordObject(_biomeData, "Set Nodes End Flag");
                    foreach (int idx in _selectedNodeIndices)
                    {
                        if (idx >= 0 && idx < _biomeData.SegmentNodes.Length && _biomeData.SegmentNodes[idx] != null)
                        {
                            _biomeData.SegmentNodes[idx].IsEndNode = newIsEnd;
                        }
                    }
                }
            }
            else
            {
                int selectedIndex = _selectedNodeIndices.First();
                EditorGUILayout.LabelField($"Selected Node: {selectedIndex}", _contentLabelStyle);
                
                if (_biomeData.SegmentNodes != null && selectedIndex >= 0 && selectedIndex < _biomeData.SegmentNodes.Length)
                {
                    var node = _biomeData.SegmentNodes[selectedIndex];
                    if (node != null)
                    {
                        EditorGUI.BeginChangeCheck();
                        var newSegment = (LevelSegment)EditorGUILayout.ObjectField("Level Segment", node.Segment, typeof(LevelSegment), false);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(_biomeData, "Set Node Segment");
                            node.Segment = newSegment;
                        }
                        
                        EditorGUILayout.Space();
                        
                        EditorGUI.BeginChangeCheck();
                        float newWeight = EditorGUILayout.FloatField("Weight:", node.Weight);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(_biomeData, "Set Node Weight");
                            node.Weight = newWeight;
                        }
                        
                        EditorGUI.BeginChangeCheck();
                        int newCooldown = EditorGUILayout.IntField("Cooldown:", node.Cooldown);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(_biomeData, "Set Node Cooldown");
                            node.Cooldown = newCooldown;
                        }
                    }
                }
            }
        }
        else
        {
            EditorGUILayout.LabelField("Select a node to assign a LevelSegment", _mutedLabelStyle);
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField($"Sequence Nodes: {_biomeData.SegmentNodes?.Length ?? 0}", _contentLabelStyle);

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(_biomeData);
        }
    }

    private void ShowSegmentSelector(SegmentNodeData node)
    {
        SerializedObject so = new SerializedObject(_biomeData);
        SerializedProperty nodesProperty = so.FindProperty("segmentNodes");
        
        if (nodesProperty == null || !nodesProperty.isArray) return;
        
        int nodeIndex = -1;
        for (int i = 0; i < _biomeData.SegmentNodes.Length; i++)
        {
            if (_biomeData.SegmentNodes[i] == node)
            {
                nodeIndex = i;
                break;
            }
        }
        
        if (nodeIndex < 0) return;
        
        SerializedProperty nodeProperty = nodesProperty.GetArrayElementAtIndex(nodeIndex);
        SerializedProperty segmentProperty = nodeProperty.FindPropertyRelative("segment");
        
        if (segmentProperty != null)
        {
            ComponentPrefabSelectorWindow.Show(segmentProperty, typeof(LevelSegment));
        }
    }

    private void DrawBiomePropertiesTab()
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Biome Identity", _inspectorHeaderStyle);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Name:", _contentLabelStyle);
        string newName = EditorGUILayout.TextField(_biomeData.BiomeName);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Environment", _inspectorHeaderStyle);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Environment Prefab:", _contentLabelStyle);
        GameObject envPrefab = (GameObject)EditorGUILayout.ObjectField(_biomeData.EnvironmentPrefab, typeof(GameObject), false);

        EditorGUILayout.LabelField("Background Prefab:", _contentLabelStyle);
        GameObject bgPrefab = (GameObject)EditorGUILayout.ObjectField(_biomeData.BackgroundImagePrefab, typeof(GameObject), false);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Length", _inspectorHeaderStyle);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Min Length:", _contentLabelStyle);
        float newMinLength = EditorGUILayout.FloatField(_biomeData.MinLength);

        EditorGUILayout.LabelField("Max Length:", _contentLabelStyle);
        float newMaxLength = EditorGUILayout.FloatField(_biomeData.MaxLength);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Difficulty", _inspectorHeaderStyle);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Min Difficulty:", _contentLabelStyle);
        float newMinDiff = EditorGUILayout.Slider(_biomeData.MinDifficulty, 0f, 1f);

        EditorGUILayout.LabelField("Max Difficulty:", _contentLabelStyle);
        float newMaxDiff = EditorGUILayout.Slider(_biomeData.MaxDifficulty, 0f, 1f);

        EditorGUILayout.Space();

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_biomeData, "Modify Biome Properties");
            _biomeData.BiomeName = newName;
            _biomeData.EnvironmentPrefab = envPrefab;
            _biomeData.BackgroundImagePrefab = bgPrefab;
            _biomeData.MinLength = newMinLength;
            _biomeData.MaxLength = newMaxLength;
            _biomeData.MinDifficulty = newMinDiff;
            _biomeData.MaxDifficulty = newMaxDiff;
            EditorUtility.SetDirty(_biomeData);
        }
    }

    #endregion

    #region Minimap

    private void DrawMinimap(Rect canvasRect)
    {
        float minimapX = canvasRect.x + 10;
        float minimapY = canvasRect.y + 10;
        Rect minimapRect = new Rect(minimapX, minimapY, MINIMAP_WIDTH, MINIMAP_HEIGHT);

        GUI.Box(minimapRect, "", EditorStyles.textArea);

        if (_biomeData.SegmentNodes == null || _biomeData.SegmentNodes.Length == 0) return;

        Bounds nodeBounds = GetNodesBounds();
        if (nodeBounds.size.x <= 0 || nodeBounds.size.y <= 0) return;

        float scaleX = (MINIMAP_WIDTH - 20) / nodeBounds.size.x;
        float scaleY = (MINIMAP_HEIGHT - 20) / nodeBounds.size.y;
        float scale = Mathf.Min(scaleX, scaleY) * 0.8f;

        Vector2 offset = new Vector2(
            MINIMAP_WIDTH / 2 - (nodeBounds.center.x * scale),
            MINIMAP_HEIGHT / 2 - (nodeBounds.center.y * scale)
        );

        for (int i = 0; i < _biomeData.SegmentNodes.Length; i++)
        {
            var node = _biomeData.SegmentNodes[i];
            if (node == null) continue;

            Vector2 nodePos = node.NodePosition * scale + offset;
            Color nodeColor = _selectedNodeIndices.Contains(i) ? SelectedBorderColor : NodeBorderColor;

            EditorGUI.DrawRect(new Rect(minimapX + nodePos.x - 2, minimapY + nodePos.y - 2, 4, 4), nodeColor);

            if (node.Connections != null)
            {
                for (int j = 0; j < node.Connections.Length; j++)
                {
                    int targetIndex = node.Connections[j];
                    if (targetIndex >= 0 && targetIndex < _biomeData.SegmentNodes.Length)
                    {
                        var targetNode = _biomeData.SegmentNodes[targetIndex];
                        if (targetNode != null)
                        {
                            Vector2 targetPos = targetNode.NodePosition * scale + offset;
                            Handles.BeginGUI();
                            Handles.color = ConnectionColor;
                            Handles.DrawLine(new Vector3(minimapX + nodePos.x, minimapY + nodePos.y), new Vector3(minimapX + targetPos.x, minimapY + targetPos.y));
                            Handles.EndGUI();
                        }
                    }
                }
            }
        }
    }

    private Bounds GetNodesBounds()
    {
        Bounds bounds = new Bounds();
        bool initialized = false;

        if (_biomeData.SegmentNodes != null)
        {
            for (int i = 0; i < _biomeData.SegmentNodes.Length; i++)
            {
                var node = _biomeData.SegmentNodes[i];
                if (node == null) continue;

                if (!initialized)
                {
                    bounds = new Bounds(new Vector3(node.NodePosition.x, node.NodePosition.y, 0), Vector3.zero);
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(new Vector3(node.NodePosition.x, node.NodePosition.y, 0));
                }
            }
        }

        if (!initialized)
        {
            bounds = new Bounds(Vector3.zero, new Vector3(100, 100, 0));
        }

        return bounds;
    }

    #endregion

    #region Status Bar

    private void DrawStatusBar(Rect canvasRect)
    {
        Rect statusBarRect = new Rect(0, canvasRect.height - 24, canvasRect.width, 24);
        EditorGUI.DrawRect(statusBarRect, ToolbarColor);

        string statusText = $"Nodes: {_biomeData.SegmentNodes?.Length ?? 0} | Selected: {_selectedNodeIndices.Count} | Zoom: {_zoom * 100:F0}%";
        GUI.Label(statusBarRect, statusText, _mutedLabelStyle);
    }

    #endregion

    #region Events

    private void ProcessEvents(Rect canvasRect)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown)
        {
            _lastMousePos = e.mousePosition;

            if (e.button == 0)
            {
                int clickedNodeIndex = GetNodeIndexAtPosition(e.mousePosition - canvasRect.position);

                if (clickedNodeIndex >= 0)
                {
                    if (_isCreatingConnection)
                    {
                        if (_connectionSourceNodeIndex != clickedNodeIndex)
                        {
                            var sourceNode = _biomeData.SegmentNodes[_connectionSourceNodeIndex];
                            if (!sourceNode.HasConnectionTo(clickedNodeIndex))
                            {
                                Undo.RecordObject(_biomeData, "Add Connection");
                                sourceNode.AddConnection(clickedNodeIndex);
                            }
                        }
                        _isCreatingConnection = false;
                        _connectionSourceNodeIndex = -1;
                    }
                    else
                    {
                        if (!_selectedNodeIndices.Contains(clickedNodeIndex))
                        {
                            if (!e.shift)
                            {
                                _selectedNodeIndices.Clear();
                            }
                            _selectedNodeIndices.Add(clickedNodeIndex);
                        }

                        _isDraggingNodes = true;
                    }
                }
                else
                {
                    if (!e.shift)
                    {
                        _selectedNodeIndices.Clear();
                    }

                    _isRectSelecting = true;
                    _rectSelectStart = e.mousePosition - canvasRect.position;
                    _rectSelectEnd = _rectSelectStart;
                }
            }
            else if (e.button == 1)
            {
                _isPanning = true;
            }
        }
        else if (e.type == EventType.MouseDrag)
        {
            Vector2 delta = e.mousePosition - _lastMousePos;
            _lastMousePos = e.mousePosition;

            if (_isDraggingNodes)
            {
                DragSelectedNodes(delta / _zoom);
            }
            else if (_isPanning)
            {
                _panOffset += delta;
            }
            else if (_isRectSelecting)
            {
                _rectSelectEnd = e.mousePosition - canvasRect.position;
                UpdateRectSelection(canvasRect);
            }
        }
        else if (e.type == EventType.MouseUp)
        {
            _isDraggingNodes = false;
            _isPanning = false;
            _isRectSelecting = false;
        }
        else if (e.type == EventType.ScrollWheel)
        {
            float oldZoom = _zoom;
            _zoom -= e.delta.y * 0.01f;
            _zoom = Mathf.Clamp(_zoom, 0.25f, 2f);

            Vector2 mousePos = e.mousePosition - canvasRect.position;
            _panOffset = mousePos - (mousePos - _panOffset) * (_zoom / oldZoom);
        }

        _hoveredNodeIndex = GetNodeIndexAtPosition(e.mousePosition - canvasRect.position);
    }

    private int GetNodeIndexAtPosition(Vector2 position)
    {
        if (_biomeData.SegmentNodes == null) return -1;

        for (int i = _biomeData.SegmentNodes.Length - 1; i >= 0; i--)
        {
            var node = _biomeData.SegmentNodes[i];
            if (node == null) continue;

            Vector2 nodePos = node.NodePosition * _zoom + _panOffset;
            float nodeWidth = NODE_WIDTH * _zoom;
            float nodeHeight = (NODE_HEADER_HEIGHT + NODE_CONTENT_HEIGHT) * _zoom;

            Rect nodeRect = new Rect(nodePos.x, nodePos.y, nodeWidth, nodeHeight);

            if (nodeRect.Contains(position))
            {
                return i;
            }
        }

        return -1;
    }

    private void DragSelectedNodes(Vector2 delta)
    {
        Undo.RecordObject(_biomeData, "Move Nodes");

        foreach (int nodeIndex in _selectedNodeIndices)
        {
            if (nodeIndex >= 0 && nodeIndex < _biomeData.SegmentNodes.Length)
            {
                var node = _biomeData.SegmentNodes[nodeIndex];
                if (node != null)
                {
                    node.NodePosition += delta;
                }
            }
        }
    }

    private void UpdateRectSelection(Rect canvasRect)
    {
        Vector2 min = Vector2.Min(_rectSelectStart, _rectSelectEnd);
        Vector2 max = Vector2.Max(_rectSelectStart, _rectSelectEnd);

        Rect selectionRect = new Rect(min.x, min.y, max.x - min.y, max.y - min.y);

        _selectedNodeIndices.Clear();

        for (int i = 0; i < _biomeData.SegmentNodes.Length; i++)
        {
            var node = _biomeData.SegmentNodes[i];
            if (node == null) continue;

            Vector2 nodePos = node.NodePosition * _zoom + _panOffset;
            float nodeWidth = NODE_WIDTH * _zoom;
            float nodeHeight = (NODE_HEADER_HEIGHT + NODE_CONTENT_HEIGHT) * _zoom;

            Rect nodeRect = new Rect(nodePos.x, nodePos.y, nodeWidth, nodeHeight);

            if (selectionRect.Overlaps(nodeRect))
            {
                _selectedNodeIndices.Add(i);
            }
        }
    }

    #endregion

    #region Actions

    private void AddNode()
    {
        Undo.RecordObject(_biomeData, "Add Node");

        var newNodes = new List<SegmentNodeData>(_biomeData.SegmentNodes);
        int newIndex = newNodes.Count;
        var newNode = new SegmentNodeData(newIndex);
        
        if (newIndex == 0)
        {
            newNode.IsStartNode = true;
        }

        newNodes.Add(newNode);
        _biomeData.SegmentNodes = newNodes.ToArray();

        _selectedNodeIndices.Clear();
        _selectedNodeIndices.Add(newIndex);
    }

    private void DeleteSelectedNodes()
    {
        if (_selectedNodeIndices.Count == 0) return;

        Undo.RecordObject(_biomeData, "Delete Nodes");

        List<SegmentNodeData> nodes = new List<SegmentNodeData>(_biomeData.SegmentNodes);
        
        List<int> sortedIndices = new List<int>(_selectedNodeIndices);
        sortedIndices.Sort((a, b) => b.CompareTo(a));

        HashSet<int> deletedIndices = new HashSet<int>(sortedIndices);

        foreach (int index in sortedIndices)
        {
            if (index >= 0 && index < nodes.Count)
            {
                nodes.RemoveAt(index);
            }
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null)
            {
                nodes[i].NodeIndex = i;

                List<int> newConnections = new List<int>();
                foreach (int conn in nodes[i].Connections)
                {
                    if (!deletedIndices.Contains(conn))
                    {
                        if (conn < sortedIndices[0])
                        {
                            newConnections.Add(conn);
                        }
                        else
                        {
                            int adjustedConn = conn;
                            foreach (int deletedIdx in sortedIndices)
                            {
                                if (conn > deletedIdx)
                                {
                                    adjustedConn--;
                                }
                            }
                            newConnections.Add(adjustedConn);
                        }
                    }
                }
                nodes[i].Connections = newConnections.ToArray();
            }
        }

        _biomeData.SegmentNodes = nodes.ToArray();
        _selectedNodeIndices.Clear();
    }

    private void StartConnectionMode()
    {
        if (_selectedNodeIndices.Count == 1)
        {
            _isCreatingConnection = true;
            _connectionSourceNodeIndex = _selectedNodeIndices.First();
        }
    }

    private void CenterView()
    {
        if (_biomeData.SegmentNodes == null || _biomeData.SegmentNodes.Length == 0)
        {
            _panOffset = Vector2.zero;
            return;
        }

        Bounds bounds = GetNodesBounds();
        _panOffset = new Vector2(position.width / 2 - bounds.center.x * _zoom, position.height / 2 - bounds.center.y * _zoom);
    }

    private void FitAllNodes()
    {
        if (_biomeData.SegmentNodes == null || _biomeData.SegmentNodes.Length == 0)
        {
            _zoom = 1f;
            _panOffset = Vector2.zero;
            return;
        }

        Bounds bounds = GetNodesBounds();
        float canvasWidth = position.width - 300;
        float canvasHeight = position.height - 32;

        float scaleX = (canvasWidth - 100) / bounds.size.x;
        float scaleY = (canvasHeight - 100) / bounds.size.y;
        _zoom = Mathf.Min(scaleX, scaleY, 2f);
        _zoom = Mathf.Max(_zoom, 0.25f);

        _panOffset = new Vector2(canvasWidth / 2 - bounds.center.x * _zoom, canvasHeight / 2 - bounds.center.y * _zoom);
    }

    private void SaveBiomeData()
    {
        _biomeData.EditorScrollPosition = _panOffset;
        _biomeData.EditorZoom = _zoom;
        EditorUtility.SetDirty(_biomeData);
        AssetDatabase.SaveAssets();
    }

    private void DrawNoDataMessage()
    {
        GUILayout.BeginArea(new Rect(0, 32, position.width, position.height - 32));
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("No Biome Data selected", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.EndArea();
    }

    #endregion
}
