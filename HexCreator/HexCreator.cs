using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HexCreator.Utils;
using UnityEditor;
using UnityEngine;

namespace HexCreator
{
    public class HexCreator : EditorWindow
    {
        private string _radiusEdit = "5";
        private int _selectedRadiusType = 0;
        private readonly string[] _radiusTypes = { "Outer", "Inner" };
        private int _selectedOrientation = 0;
        private readonly string[] _orientations = { "Flat-Top", "Pointy-Top" };
        private int _selectedDirection = 0;
        private readonly string[] _directions = { "Clockwise", "Counter Clockwise" };
        private int _selectedAxis = 0;
        // private readonly string[] _axes = { "XZ+Y", "XY-Z", "XY+Z", "XZ-Y", "YZ+X", "YZ-X" };
        private float _additionalRotation = 0.0f;

        [MenuItem("Tools/Hex Creator")]
        private static void Init()
        {
            var window = (HexCreator)GetWindow(typeof(HexCreator));
            window.titleContent.text = "Hex Creator";
            window.maxSize = new Vector2(500, 650);
            window.minSize = window.maxSize;
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Hex Options", EditorStyles.boldLabel);

            GUI.Label(new Rect(10, 20, 100, 20), "Radius");
            _radiusEdit = GUI.TextField(new Rect(110, 20, 50, 20), _radiusEdit);
            _selectedRadiusType = EditorGUI.Popup(new Rect(164, 20, 200, 24), _selectedRadiusType, _radiusTypes);
            var radiusType = _selectedRadiusType == 0 ? HexRadius.Outer : HexRadius.Inner;
            
            GUI.Label(new Rect(10, 45, 100, 20), "Orientation");
            _selectedOrientation = EditorGUI.Popup(new Rect(110, 45, 254, 24), _selectedOrientation, _orientations);
            var orientation = _selectedOrientation == 0 ? HexOrientation.FlatTop : HexOrientation.PointyTop;
            
            GUI.Label(new Rect(10, 70, 100, 20), "Direction");
            _selectedDirection = EditorGUI.Popup(new Rect(110, 70, 254, 24), _selectedDirection, _directions);
            var direction = _selectedDirection == 0 ? HexDirection.Clockwise : HexDirection.Counterclockwise;
            
            // GUI.Label(new Rect(10, 120, 100, 20), "Axis");
            // _selectedAxis = EditorGUI.Popup(new Rect(110, 120, 254, 24), _selectedAxis, _axes);
            var axis = _selectedAxis switch
            {
                0 => HexAxes.XZPlusY,
                1 => HexAxes.XYMinusZ,
                2 => HexAxes.XYPlusZ,
                3 => HexAxes.XZMinusY,
                4 => HexAxes.YZPlusX,
                5 => HexAxes.YZMinusX,
                _ => HexAxes.XYPlusZ
            };

            GUI.Label(new Rect(10, 95, 100, 20), "Rotate");
            _additionalRotation = EditorGUI.FloatField(new Rect(110, 95, 70, 20), _additionalRotation);
        
            if (GUI.Button(new Rect(position.width - 123, 20, 115, 120), "Create Hex"))
            {
                var targetMeshFilepath = GetTargetMeshFilepath();
                AssetDatabase.DeleteAsset(targetMeshFilepath);
                var m = new Mesh
                {
                    vertices = GetVertices(orientation, radiusType, direction, axis, _additionalRotation),
                    triangles = GetTriangles(direction, axis)
                };

                m.RecalculateBounds();
                m.RecalculateNormals();
                m.RecalculateTangents();
        
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                AssetDatabase.CreateAsset(m, targetMeshFilepath);
                
                Debug.Log($"Created in {targetMeshFilepath}");
            }

            DrawHex(orientation, radiusType, direction);
        }

        private void DrawHex(HexOrientation orientation, HexRadius radiusType, HexDirection direction)
        {
            const int rectX = 0;
            const int rectY = 150;
            const int rectHeight = 500;
            const int rectWidth = 500;
            var center = new Vector3(rectWidth / 2 + rectX, rectHeight / 2 + rectY, 0);

            var hexLines = DrawUtils.GetHexLines(center, 200, orientation, direction, _additionalRotation);
            foreach (var line in hexLines)
            {
                Handles.DrawLine(line.Point1, line.Point2, 4.0f);
            }

            var defaultLabelFontSize = GUI.skin.label.fontSize;
            var defaultLabelFontColor = GUI.contentColor;
            var defaultHandlesColor = Handles.color;
            GUI.skin.label.fontSize = 20;
            GUI.contentColor = Color.red;
            
            Handles.color = Color.cyan;
            var discRadius = radiusType == HexRadius.Outer ? 200.0f : Mathf.Sqrt(3) * 200.0f / 2;
            Handles.DrawWireDisc(center, Vector3.forward, discRadius);
            Handles.color = defaultHandlesColor;
            
            // Draw 
            var points = new List<Vector3> { center };
            points.AddRange(hexLines.GetRange(6, 6).Select(line => line.Point1));
            for (var i = 0; i < 7; i++)
            {
                // Draw point labels
                EditorGUI.DrawRect(new Rect(points[i].x, points[i].y - 30, 20, 20), Color.black);
                Handles.Label(new Vector3(points[i].x + 4, points[i].y - 27, center.z), (i).ToString());
                
                // Draw red points
                Handles.color = Color.red;
                Handles.DrawSolidDisc(points[i], Vector3.forward, 5.0f);
                Handles.color = defaultHandlesColor;
            }

            // Draw triangle indices
            GUI.contentColor = Color.yellow;
            var smallHexLinesAdditionalRotation = direction == HexDirection.Clockwise ? _additionalRotation - 30.0f : _additionalRotation + 30.0f;
            var smallHexLines = DrawUtils.GetHexLines(center, 115, orientation, direction, smallHexLinesAdditionalRotation);
            for (var i = 0; i < 6; i++)
            {
                EditorGUI.DrawRect(new Rect(smallHexLines[i + 6].Point1.x - 4, smallHexLines[i + 6].Point1.y - 3, 20, 20), Color.black);
                Handles.Label(new Vector3(smallHexLines[i + 6].Point1.x, smallHexLines[i + 6].Point1.y, center.z), (i).ToString());
            }

            // Revert
            GUI.skin.label.fontSize = defaultLabelFontSize;
            GUI.contentColor = defaultLabelFontColor;
        }

        private Vector3[] GetPointyTopVertices(HexRadius hexRadius, HexDirection hexDirection, HexAxes hexAxes, float additionalRotation = 0.0f)
        {
            // default:
            //    1
            // 6 /  \ 2
            //   | 0 | 3
            // 5 \  /
            //    4
            
            return GetVerticesByOrientation(HexOrientation.PointyTop, hexRadius, hexDirection, hexAxes, additionalRotation);
        }
        
        private Vector3[] GetFlatTopVertices(HexRadius hexRadius, HexDirection hexDirection, HexAxes hexAxes, float additionalRotation = 0.0f)
        {
            // default:
            //  1 ___ 2
            //  6/ 0 \3
            //   \___/
            //   5   4

            return GetVerticesByOrientation(HexOrientation.FlatTop, hexRadius, hexDirection, hexAxes, additionalRotation);
        }

        private Vector3[] GetVerticesByOrientation(HexOrientation hexOrientation, HexRadius hexRadius, HexDirection hexDirection, HexAxes hexAxes, float additionalRotation = 0.0f)
        {
            var radius = float.Parse(_radiusEdit);
            switch (hexRadius)
            {
                case HexRadius.Outer:
                {
                    break;
                }
                case HexRadius.Inner:
                {
                    radius = radius * 2 / Mathf.Sqrt(3);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(hexRadius), hexRadius, null);
            }

            switch (hexOrientation)
            {
                case HexOrientation.PointyTop:
                {
                    break;
                }
                case HexOrientation.FlatTop:
                {
                    additionalRotation -= 30.0f;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(hexOrientation), hexOrientation, null);
            }

            var points = new Vector3[7];
            points[0] = new Vector3(0.0f, 0.0f, 0.0f);

            switch (hexDirection)
            {
                case HexDirection.Clockwise:
                {
                    for (var i = 0; i < 6; i++)
                    {
                        var angleDeg = 60 * i + additionalRotation;
                        var angleRad = Mathf.PI / 180 * angleDeg;

                        var x = 0.0f;
                        var y = 0.0f;
                        var z = 0.0f;

                        switch (hexAxes)
                        {
                            case HexAxes.XYPlusZ:
                            case HexAxes.XYMinusZ:
                            {
                                x = radius * Mathf.Sin(angleRad);
                                y = radius * Mathf.Cos(angleRad);
                                break;
                            }
                            case HexAxes.XZPlusY:
                            case HexAxes.XZMinusY:
                            {
                                x = radius * Mathf.Sin(angleRad);
                                z = radius * Mathf.Cos(angleRad);
                                break;
                            }
                            case HexAxes.YZPlusX:
                            case HexAxes.YZMinusX:
                            {
                                y = radius * Mathf.Sin(angleRad);
                                z = radius * Mathf.Cos(angleRad);
                                break;
                            }
                            default:
                                throw new ArgumentOutOfRangeException(nameof(hexAxes), hexAxes, null);
                        }
                        
                        points[i + 1] = new Vector3(x, y, z);
                    }
                    break;
                }
                case HexDirection.Counterclockwise:
                {
                    for (var i = 0; i < 6; i++)
                    {
                        var angleDeg = 60 * i + additionalRotation + 60.0f;
                        var angleRad = Mathf.PI / 180 * angleDeg;
                        
                        var x = 0.0f;
                        var y = 0.0f;
                        var z = 0.0f;

                        switch (hexAxes)
                        {
                            case HexAxes.XYPlusZ:
                            case HexAxes.XYMinusZ:
                            {
                                x = radius * Mathf.Sin(angleRad);
                                y = radius * Mathf.Cos(angleRad);
                                break;
                            }
                            case HexAxes.XZPlusY:
                            case HexAxes.XZMinusY:
                            {
                                x = radius * Mathf.Sin(angleRad);
                                z = radius * Mathf.Cos(angleRad);
                                break;
                            }
                            case HexAxes.YZPlusX:
                            case HexAxes.YZMinusX:
                            {
                                y = radius * Mathf.Sin(angleRad);
                                z = radius * Mathf.Cos(angleRad);
                                break;
                            }
                            default:
                                throw new ArgumentOutOfRangeException(nameof(hexAxes), hexAxes, null);
                        }
                        
                        points[6 - i] = new Vector3(x, y, z);
                    }
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(hexDirection), hexDirection, null);
            }

            return points;
        }
        
        private Vector3[] GetVertices(HexOrientation hexOrientation, HexRadius hexRadius, HexDirection hexDirection, HexAxes hexAxes, float additionalRotation = 0.0f)
        {
            return hexOrientation switch
            {
                HexOrientation.FlatTop => GetFlatTopVertices(hexRadius, hexDirection, hexAxes, additionalRotation),
                HexOrientation.PointyTop => GetPointyTopVertices(hexRadius, hexDirection, hexAxes, additionalRotation),
                _ => throw new ArgumentOutOfRangeException(nameof(hexOrientation), hexOrientation, null)
            };
        }

        private static int[] GetTriangles(HexDirection hexDirection, HexAxes hexAxes)
        {
            switch (hexDirection)
            {
                case HexDirection.Clockwise:
                {
                    switch (hexAxes)
                    {
                        case HexAxes.XZPlusY:
                        {
                            return new [] { 0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 5, 0, 5, 6, 0, 6, 1 };
                        }
                        case HexAxes.XYPlusZ:
                        {
                            break;
                        }
                        case HexAxes.XYMinusZ:
                        {
                            break;
                        }
                        case HexAxes.XZMinusY:
                        {
                            break;
                        }
                        case HexAxes.YZPlusX:
                        {
                            break;
                        }
                        case HexAxes.YZMinusX:
                        {
                            break;
                        }
                        default:
                        {
                            throw new ArgumentOutOfRangeException(nameof(hexAxes), hexAxes, null);
                        }
                    }
                    break;
                }
                case HexDirection.Counterclockwise:
                    switch (hexAxes)
                    {
                        case HexAxes.XZPlusY:
                        {
                            return new [] { 2, 1, 0, 3, 2, 0, 4, 3, 0, 5, 4, 0, 6, 5, 0, 1, 6, 0 };
                        }
                        case HexAxes.XYPlusZ:
                        {
                            break;
                        }
                        case HexAxes.XYMinusZ:
                        {
                            break;
                        }
                        case HexAxes.XZMinusY:
                        {
                            break;
                        }
                        case HexAxes.YZPlusX:
                        {
                            break;
                        }
                        case HexAxes.YZMinusX:
                        {
                            break;
                        }
                        default:
                        {
                            throw new ArgumentOutOfRangeException(nameof(hexAxes), hexAxes, null);
                        }
                    }
                    break;
                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(hexDirection), hexDirection, null);
                }
            }
            return new [] { 0 };
        }

        private string GetTargetMeshFilepath()
        {
            var ms = MonoScript.FromScriptableObject(this);
            var scriptFilePath = AssetDatabase.GetAssetPath(ms);
            var fi = new FileInfo(scriptFilePath);
            if (fi.Directory?.Parent == null) return "";
            var scriptFileFolder = fi.Directory.Parent.ToString();
            var fullPath = Path.Combine(scriptFileFolder, "Hex.mesh").Replace('\\', '/');
            return fullPath.Substring(fullPath.IndexOf("Assets", StringComparison.Ordinal), fullPath.Length - fullPath.IndexOf("Assets", StringComparison.Ordinal));
        }
    }
}
