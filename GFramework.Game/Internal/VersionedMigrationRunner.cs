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

namespace GFramework.Game.Internal;

/// <summary>
///     提供版本化数据迁移链的共享执行逻辑。
/// </summary>
/// <remarks>
///     该运行器只负责“按版本号推进”的公共约束，包括：
///     前向注册校验、缺失链路失败、声明目标版本与实际结果版本一致性，以及避免版本回退或死循环。
///     它不关心具体存储、日志、回写或异常吞吐策略；这些由调用方负责。
/// </remarks>
internal static class VersionedMigrationRunner
{
    /// <summary>
    ///     校验迁移注册是否表示一次有效的前向升级。
    /// </summary>
    /// <param name="subjectName">迁移所作用的主体名称，例如设置类型或存档类型。</param>
    /// <param name="migrationKind">用于异常消息的迁移类别名称。</param>
    /// <param name="fromVersion">源版本。</param>
    /// <param name="toVersion">目标版本。</param>
    /// <param name="paramName">异常中要使用的参数名。</param>
    /// <exception cref="ArgumentException">目标版本不大于源版本时抛出。</exception>
    internal static void ValidateForwardOnlyRegistration(
        string subjectName,
        string migrationKind,
        int fromVersion,
        int toVersion,
        string paramName)
    {
        if (toVersion <= fromVersion)
        {
            throw new ArgumentException(
                $"{migrationKind} for {subjectName} must advance the version number.",
                paramName);
        }
    }

    /// <summary>
    ///     按目标运行时版本执行连续迁移。
    /// </summary>
    /// <typeparam name="TData">迁移数据类型。</typeparam>
    /// <typeparam name="TMigration">迁移描述类型。</typeparam>
    /// <param name="data">原始加载的数据。</param>
    /// <param name="targetVersion">当前运行时支持的目标版本。</param>
    /// <param name="getVersion">从数据对象提取版本号的委托。</param>
    /// <param name="resolveMigration">根据当前版本查找下一步迁移器的委托。</param>
    /// <param name="getToVersion">从迁移器提取声明目标版本的委托。</param>
    /// <param name="applyMigration">执行单步迁移的委托。</param>
    /// <param name="subjectName">迁移主体名称，用于异常消息。</param>
    /// <param name="migrationKind">迁移类别名称，用于异常消息。</param>
    /// <returns>迁移到目标版本后的数据；如果已经是最新版本，则返回原对象。</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="data" />、<paramref name="getVersion" />、<paramref name="resolveMigration" />、
    ///     <paramref name="getToVersion" /> 或 <paramref name="applyMigration" /> 为 <see langword="null" /> 时抛出。
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="subjectName" /> 或 <paramref name="migrationKind" /> 为空白时抛出。
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     数据版本高于当前运行时、迁移链缺失、迁移器返回 <see langword="null" />、
    ///     迁移结果版本与声明不一致、版本未前进或超出目标版本时抛出。
    /// </exception>
    internal static TData MigrateToTargetVersion<TData, TMigration>(
        TData data,
        int targetVersion,
        Func<TData, int> getVersion,
        Func<int, TMigration?> resolveMigration,
        Func<TMigration, int> getToVersion,
        Func<TMigration, TData, TData> applyMigration,
        string subjectName,
        string migrationKind)
        where TData : class
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(getVersion);
        ArgumentNullException.ThrowIfNull(resolveMigration);
        ArgumentNullException.ThrowIfNull(getToVersion);
        ArgumentNullException.ThrowIfNull(applyMigration);
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectName);
        ArgumentException.ThrowIfNullOrWhiteSpace(migrationKind);

        var currentVersion = getVersion(data);
        EnsureRuntimeVersionIsSupported(currentVersion, targetVersion, subjectName);

        if (currentVersion == targetVersion)
        {
            return data;
        }

        var current = data;

        while (currentVersion < targetVersion)
        {
            var migration = GetRequiredMigration(resolveMigration, currentVersion, subjectName, migrationKind);
            var result = ApplyMigrationStep(
                migration,
                current,
                currentVersion,
                targetVersion,
                getVersion,
                getToVersion,
                applyMigration,
                subjectName,
                migrationKind);
            current = result.Data;
            currentVersion = result.Version;
        }

        return current;
    }

    /// <summary>
    ///     拒绝比当前运行时更高的数据版本，避免迁移器在未知版本上继续执行。
    /// </summary>
    /// <param name="currentVersion">数据当前版本。</param>
    /// <param name="targetVersion">运行时支持的目标版本。</param>
    /// <param name="subjectName">迁移主体名称。</param>
    /// <exception cref="InvalidOperationException">数据版本高于运行时版本时抛出。</exception>
    private static void EnsureRuntimeVersionIsSupported(int currentVersion, int targetVersion, string subjectName)
    {
        if (currentVersion > targetVersion)
        {
            throw new InvalidOperationException(
                $"{subjectName} is version {currentVersion}, which is newer than the current runtime version {targetVersion}.");
        }
    }

    /// <summary>
    ///     解析当前版本必须存在的下一步迁移器，避免在调用循环中重复拼接相同错误。
    /// </summary>
    /// <typeparam name="TMigration">迁移描述类型。</typeparam>
    /// <param name="resolveMigration">迁移解析委托。</param>
    /// <param name="currentVersion">当前版本。</param>
    /// <param name="subjectName">迁移主体名称。</param>
    /// <param name="migrationKind">迁移类别名称。</param>
    /// <returns>已解析的迁移器。</returns>
    /// <exception cref="InvalidOperationException">缺少迁移器时抛出。</exception>
    private static TMigration GetRequiredMigration<TMigration>(
        Func<int, TMigration?> resolveMigration,
        int currentVersion,
        string subjectName,
        string migrationKind)
    {
        var migration = resolveMigration(currentVersion);
        if (migration is null)
        {
            throw new InvalidOperationException(
                $"No {migrationKind} is registered for {subjectName} from version {currentVersion}.");
        }

        return migration;
    }

    /// <summary>
    ///     执行单步迁移并验证声明目标版本、结果版本与运行时上限之间的一致性。
    /// </summary>
    /// <typeparam name="TData">迁移数据类型。</typeparam>
    /// <typeparam name="TMigration">迁移描述类型。</typeparam>
    /// <param name="migration">当前步骤的迁移器。</param>
    /// <param name="current">迁移前数据。</param>
    /// <param name="currentVersion">迁移前版本。</param>
    /// <param name="targetVersion">运行时目标版本。</param>
    /// <param name="getVersion">数据版本读取委托。</param>
    /// <param name="getToVersion">迁移器目标版本读取委托。</param>
    /// <param name="applyMigration">迁移执行委托。</param>
    /// <param name="subjectName">迁移主体名称。</param>
    /// <param name="migrationKind">迁移类别名称。</param>
    /// <returns>迁移后的数据与新版本号。</returns>
    /// <exception cref="InvalidOperationException">
    ///     迁移结果为空、声明目标版本不匹配、版本未前进或超出运行时版本时抛出。
    /// </exception>
    private static (TData Data, int Version) ApplyMigrationStep<TData, TMigration>(
        TMigration migration,
        TData current,
        int currentVersion,
        int targetVersion,
        Func<TData, int> getVersion,
        Func<TMigration, int> getToVersion,
        Func<TMigration, TData, TData> applyMigration,
        string subjectName,
        string migrationKind)
        where TData : class
    {
        var migratedData = applyMigration(migration, current)
            ?? throw new InvalidOperationException(
                $"{migrationKind} for {subjectName} from version {currentVersion} returned null.");
        var migratedVersion = getVersion(migratedData);
        ValidateMigrationResult(
            currentVersion,
            targetVersion,
            migratedVersion,
            getToVersion(migration),
            subjectName,
            migrationKind);
        return (migratedData, migratedVersion);
    }

    /// <summary>
    ///     校验单步迁移结果与声明目标版本一致，并确保版本严格单调递增且不越过运行时版本。
    /// </summary>
    /// <param name="currentVersion">迁移前版本。</param>
    /// <param name="targetVersion">运行时目标版本。</param>
    /// <param name="migratedVersion">迁移后实际版本。</param>
    /// <param name="declaredTargetVersion">迁移器声明的目标版本。</param>
    /// <param name="subjectName">迁移主体名称。</param>
    /// <param name="migrationKind">迁移类别名称。</param>
    /// <exception cref="InvalidOperationException">
    ///     声明目标版本不匹配、版本未前进或超出运行时版本时抛出。
    /// </exception>
    private static void ValidateMigrationResult(
        int currentVersion,
        int targetVersion,
        int migratedVersion,
        int declaredTargetVersion,
        string subjectName,
        string migrationKind)
    {
        if (declaredTargetVersion != migratedVersion)
        {
            throw new InvalidOperationException(
                $"{migrationKind} for {subjectName} declared target version {declaredTargetVersion} " +
                $"but returned version {migratedVersion}.");
        }

        if (migratedVersion <= currentVersion)
        {
            throw new InvalidOperationException(
                $"{migrationKind} for {subjectName} must advance beyond version {currentVersion}.");
        }

        if (migratedVersion > targetVersion)
        {
            throw new InvalidOperationException(
                $"{migrationKind} for {subjectName} produced version {migratedVersion}, " +
                $"which exceeds the current runtime version {targetVersion}.");
        }
    }
}
