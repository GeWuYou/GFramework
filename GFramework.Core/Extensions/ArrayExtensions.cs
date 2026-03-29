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
        return x >= 0 && y >= 0 &&
               x < array.GetLength(0) &&
               y < array.GetLength(1);
    }
}