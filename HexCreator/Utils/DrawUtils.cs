using System;
using System.Collections.Generic;
using UnityEngine;

namespace HexCreator.Utils
{
    internal static class DrawUtils
    {
        public static List<Line> GetHexLines(Vector3 center, float radius, HexOrientation hexOrientation, HexDirection hexDirection, float additionalRotation = 0.0f)
        {
            additionalRotation -= hexOrientation switch
            {
                HexOrientation.PointyTop => 60.0f,
                HexOrientation.FlatTop => 30.0f,
                _ => throw new ArgumentOutOfRangeException(nameof(hexOrientation), hexOrientation, null)
            };

            var hexPoints = new Vector3[7];
            hexPoints[0] = center;
            
            switch (hexDirection)
            {
                case HexDirection.Clockwise:
                {
                    for (var i = 0; i < 6; i++)
                    {
                        var angleDeg = 60 * i + additionalRotation - 60.0f;
                        var angleRad = Mathf.PI / 180 * angleDeg;

                        var x = center.x + radius * Mathf.Sin(angleRad);
                        var y = center.y + radius * Mathf.Cos(angleRad);
                        const float z = 0.0f;
                        
                        hexPoints[6 - i] = new Vector3(x, y, z);
                    }
                    break;
                }
                case HexDirection.Counterclockwise:
                {
                    for (var i = 0; i < 6; i++)
                    {
                        var angleDeg = 60 * i + additionalRotation + 60.0f + 180.0f;
                        var angleRad = Mathf.PI / 180 * angleDeg;

                        var x = center.x + radius * Mathf.Sin(angleRad);
                        var y = center.y + radius * Mathf.Cos(angleRad);
                        const float z = 0.0f;

                        hexPoints[i + 1] = new Vector3(x, y, z);
                    }
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(hexDirection), hexDirection, null);
            }

            return new List<Line>
            {
                new Line { Point1 = hexPoints[0], Point2 = hexPoints[1]},
                new Line { Point1 = hexPoints[0], Point2 = hexPoints[2]},
                new Line { Point1 = hexPoints[0], Point2 = hexPoints[3]},
                new Line { Point1 = hexPoints[0], Point2 = hexPoints[4]},
                new Line { Point1 = hexPoints[0], Point2 = hexPoints[5]},
                new Line { Point1 = hexPoints[0], Point2 = hexPoints[6]},
                
                new Line { Point1 = hexPoints[1], Point2 = hexPoints[2]},
                new Line { Point1 = hexPoints[2], Point2 = hexPoints[3]},
                new Line { Point1 = hexPoints[3], Point2 = hexPoints[4]},
                new Line { Point1 = hexPoints[4], Point2 = hexPoints[5]},
                new Line { Point1 = hexPoints[5], Point2 = hexPoints[6]},
                new Line { Point1 = hexPoints[6], Point2 = hexPoints[1]},
            };
        }
        
        internal class Line
        {
            public Vector3 Point1 { get; set; }
            public Vector3 Point2 { get; set; }
        }
    }
}
