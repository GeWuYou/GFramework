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

namespace GFramework.Cqrs.Abstractions.Cqrs.Command;

/// <summary>
///     表示一个流式 CQRS 命令。
/// </summary>
/// <typeparam name="TResponse">流式响应元素类型。</typeparam>
public interface IStreamCommand<out TResponse> : IStreamRequest<TResponse>;
