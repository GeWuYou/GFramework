// Copyright (c) 2026 GeWuYou
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Diagnostics.CodeAnalysis;

namespace GFramework.Core.Extensions;

/// <summary>
/// 数组扩展方法类，提供二维数组的边界检查等实用功能。
/// </summary>
public static class ArrayExtensions
{
    /// <summary>
    /// 检查二维数组的给定坐标是否在有效边界内。
    /// </summary>
    /// <typeparam name="T">数组元素类型。</typeparam>
    /// <param name="array">要检查的二维数组。</param>
    /// <param name="x">要检查的 X 坐标（第一维索引）。</param>
    /// <param name="y">要检查的 Y 坐标（第二维索引）。</param>
    /// <returns>如果坐标在数组边界内则返回 true；否则返回 false。</returns>
    public static bool IsInBounds<T>(this T[,] array, int x, int y)
    {
        // 在热路径中缓存维度长度，避免每次比较都重复读取数组元数据。
        var width = array.GetLength(0);
        var height = array.GetLength(1);

        return x >= 0 && y >= 0 &&
               x < width &&
               y < height;
    }


    /// <summary>
    /// 获取二维数组指定位置的元素，如果越界则返回默认值。
    /// </summary>
    /// <typeparam name="T">数组元素类型。</typeparam>
    /// <param name="array">要访问的二维数组。</param>
    /// <param name="x">X 坐标（第一维索引）。</param>
    /// <param name="y">Y 坐标（第二维索引）。</param>
    /// <returns>如果在边界内返回该位置的元素；否则返回类型的默认值。</returns>
    public static T? GetOrDefault<T>(this T[,] array, int x, int y)
    {
        return array.IsInBounds(x, y) ? array[x, y] : default;
    }

    /// <summary>
    /// 获取二维数组指定位置的元素，如果越界则返回指定的回退值。
    /// </summary>
    /// <typeparam name="T">数组元素类型。</typeparam>
    /// <param name="array">要访问的二维数组。</param>
    /// <param name="x">X 坐标（第一维索引）。</param>
    /// <param name="y">Y 坐标（第二维索引）。</param>
    /// <param name="fallback">当坐标越界时返回的回退值。</param>
    /// <returns>如果在边界内返回该位置的元素；否则返回指定的回退值。</returns>
    public static T GetOr<T>(this T[,] array, int x, int y, T fallback)
    {
        return array.IsInBounds(x, y) ? array[x, y] : fallback;
    }

    /// <summary>
    /// 尝试获取二维数组指定位置的元素。
    /// </summary>
    /// <typeparam name="T">数组元素类型。</typeparam>
    /// <param name="array">要访问的二维数组。</param>
    /// <param name="x">X 坐标（第一维索引）。</param>
    /// <param name="y">Y 坐标（第二维索引）。</param>
    /// <param name="value">
    /// 输出参数，用于存储获取到的元素值；当返回 false 时，该参数会被赋值为 <c>default(T)</c>。
    /// </param>
    /// <returns>如果成功获取元素则返回 true；否则返回 false。</returns>
    public static bool TryGet<T>(this T[,] array, int x, int y, [MaybeNullWhen(false)] out T value)
    {
        if (array.IsInBounds(x, y))
        {
            value = array[x, y];
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// 获取二维数组中某个位置的四个方向邻居坐标（上、下、左、右）。
    /// </summary>
    /// <typeparam name="T">数组元素类型。</typeparam>
    /// <param name="array">源二维数组。</param>
    /// <param name="x">中心位置的 X 坐标。</param>
    /// <param name="y">中心位置的 Y 坐标。</param>
    /// <returns>按顺序返回所有在边界内的邻居坐标。</returns>
    public static IEnumerable<(int x, int y)> GetNeighbors4<T>(this T[,] array, int x, int y)
    {
        var dirs = new (int dx, int dy)[]
        {
            (0, -1), (0, 1),
            (-1, 0), (1, 0),
        };

        foreach (var (dx, dy) in dirs)
        {
            var nx = x + dx;
            var ny = y + dy;

            if (array.IsInBounds(nx, ny))
                yield return (nx, ny);
        }
    }

    /// <summary>
    /// 获取二维数组中某个位置的八个方向邻居坐标（包括对角线）。
    /// </summary>
    /// <typeparam name="T">数组元素类型。</typeparam>
    /// <param name="array">源二维数组。</param>
    /// <param name="x">中心位置的 X 坐标。</param>
    /// <param name="y">中心位置的 Y 坐标。</param>
    /// <returns>按顺序返回所有在边界内的邻居坐标（不包括中心位置）。</returns>
    public static IEnumerable<(int x, int y)> GetNeighbors8<T>(this T[,] array, int x, int y)
    {
        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                var nx = x + dx;
                var ny = y + dy;

                if (array.IsInBounds(nx, ny))
                    yield return (nx, ny);
            }
        }
    }

    /// <summary>
    /// 枚举二维数组中的所有元素及其坐标。
    /// </summary>
    /// <typeparam name="T">数组元素类型。</typeparam>
    /// <param name="array">要枚举的二维数组。</param>
    /// <returns>依次返回每个元素的坐标和值的元组。</returns>
    public static IEnumerable<(int x, int y, T value)> Enumerate<T>(this T[,] array)
    {
        // 缓存维度长度，确保双层循环不会在每次迭代时重复读取数组元数据。
        var width = array.GetLength(0);
        var height = array.GetLength(1);

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                yield return (x, y, array[x, y]);
            }
        }
    }

    /// <summary>
    /// 获取二维数组的宽度（第一维长度）。
    /// </summary>
    /// <typeparam name="T">数组元素类型。</typeparam>
    /// <param name="array">要获取宽度的二维数组。</param>
    /// <returns>数组的第一维长度。</returns>
    public static int Width<T>(this T[,] array) => array.GetLength(0);

    /// <summary>
    /// 获取二维数组的高度（第二维长度）。
    /// </summary>
    /// <typeparam name="T">数组元素类型。</typeparam>
    /// <param name="array">要获取高度的二维数组。</param>
    /// <returns>数组的第二维长度。</returns>
    public static int Height<T>(this T[,] array) => array.GetLength(1);
}
