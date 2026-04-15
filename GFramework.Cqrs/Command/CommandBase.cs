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

using GFramework.Cqrs.Abstractions.Cqrs.Command;

namespace GFramework.Core.Cqrs.Command;

/// <summary>
///     为携带输入模型的 CQRS 命令提供统一基类。
/// </summary>
/// <typeparam name="TInput">命令输入类型。</typeparam>
/// <typeparam name="TResponse">命令响应类型。</typeparam>
/// <param name="input">命令执行所需的输入对象。</param>
/// <remarks>
///     该类型继续保留在历史公开命名空间中，以避免调用方因 runtime 程序集拆分而批量修改继承层次。
///     具体实现现由 <c>GFramework.Cqrs</c> 程序集承载，并通过 type forward 维持旧程序集兼容性。
/// </remarks>
public abstract class CommandBase<TInput, TResponse>(TInput input) : ICommand<TResponse>
    where TInput : ICommandInput
{
    /// <summary>
    ///     获取命令执行时携带的输入对象。
    /// </summary>
    public TInput Input => input;
}
