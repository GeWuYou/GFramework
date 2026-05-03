// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Cqrs.SourceGenerators.Cqrs;

/// <summary>
///     为当前编译程序集生成 CQRS 处理器注册器，以减少运行时的程序集反射扫描成本。
/// </summary>
public sealed partial class CqrsHandlerRegistryGenerator
{
    /// <summary>
    ///     生成程序集级 CQRS handler 注册器源码。
    /// </summary>
    /// <param name="registrations">
    ///     已整理并排序的 handler 注册描述。方法会据此生成 <c>CqrsHandlerRegistry.g.cs</c>，其中包含直接注册、实现类型反射注册、精确运行时类型查找等分支。
    /// </param>
    /// <param name="reflectionFallbackEmission">
    ///     当前轮次选定的程序集级 reflection fallback 元数据发射策略。
    ///     调用方必须先确保：若该策略包含 fallback handlers，则当前 runtime 已声明支持对应的 fallback attribute 契约；
    ///     否则应在进入本方法前报告诊断并放弃生成，而不是输出会静默漏注册的半成品注册器。
    /// </param>
    /// <returns>完整的注册器源代码文本。</returns>
    /// <remarks>
    ///     当 <paramref name="reflectionFallbackEmission" /> 不包含任何 fallback handlers 时，
    ///     输出只包含程序集级 <c>CqrsHandlerRegistryAttribute</c> 和注册器实现。
    ///     当其包含 fallback handlers 且 runtime 合同可用时，输出还会附带一个或多个程序集级
    ///     <c>CqrsReflectionFallbackAttribute</c>，让运行时补齐生成阶段无法精确表达的剩余 handler。
    ///     该方法本身不报告诊断；“fallback 必需但 runtime 契约缺失”的错误由调用方在进入本方法前处理。
    /// </remarks>
    private static string GenerateSource(
        GenerationEnvironment generationEnvironment,
        IReadOnlyList<ImplementationRegistrationSpec> registrations,
        ReflectionFallbackEmissionSpec reflectionFallbackEmission)
    {
        var sourceShape = CreateGeneratedRegistrySourceShape(generationEnvironment, registrations);
        var builder = new StringBuilder();
        AppendGeneratedSourcePreamble(builder, reflectionFallbackEmission);
        AppendGeneratedRegistryType(builder, registrations, sourceShape);
        return builder.ToString();
    }

    /// <summary>
    ///     预先计算生成注册器需要的辅助分支，让主源码发射流程保持线性且避免重复扫描注册集合。
    /// </summary>
    /// <param name="registrations">已整理并排序的 handler 注册描述。</param>
    /// <returns>当前生成输出需要启用的结构分支。</returns>
    private static GeneratedRegistrySourceShape CreateGeneratedRegistrySourceShape(
        GenerationEnvironment generationEnvironment,
        IReadOnlyList<ImplementationRegistrationSpec> registrations)
    {
        var hasReflectedImplementationRegistrations = registrations.Any(static registration =>
            !registration.ReflectedImplementationRegistrations.IsDefaultOrEmpty);
        var hasPreciseReflectedRegistrations = registrations.Any(static registration =>
            !registration.PreciseReflectedRegistrations.IsDefaultOrEmpty);
        var hasReflectionTypeLookups = registrations.Any(static registration =>
            !string.IsNullOrWhiteSpace(registration.ReflectionTypeMetadataName));
        var hasExternalAssemblyTypeLookups = registrations.Any(static registration =>
            registration.PreciseReflectedRegistrations.Any(static preciseRegistration =>
                preciseRegistration.ServiceTypeArguments.Any(ContainsExternalAssemblyTypeLookup)));
        var requestInvokerEmissions = CreateRequestInvokerEmissions(
            generationEnvironment.SupportsRequestInvokerProvider,
            registrations);
        var streamInvokerEmissions = CreateStreamInvokerEmissions(
            generationEnvironment.SupportsStreamInvokerProvider,
            registrations);

        return new GeneratedRegistrySourceShape(
            hasReflectedImplementationRegistrations,
            hasPreciseReflectedRegistrations,
            hasReflectionTypeLookups,
            hasExternalAssemblyTypeLookups,
            generationEnvironment.SupportsRequestInvokerProvider,
            requestInvokerEmissions,
            generationEnvironment.SupportsStreamInvokerProvider,
            streamInvokerEmissions);
    }

    /// <summary>
    ///     从可直接表达 handler 接口的注册描述中提取 request invoker 发射计划。
    /// </summary>
    /// <param name="supportsRequestInvokerProvider">
    ///     指示当前 runtime 是否同时暴露 <c>ICqrsRequestInvokerProvider</c> 与
    ///     <c>IEnumeratesCqrsRequestInvokerDescriptors</c> 契约；若不支持，则本方法必须返回空结果并让后续发射路径整体跳过。
    /// </param>
    /// <param name="registrations">已按稳定顺序整理完成的 handler 注册描述。</param>
    /// <returns>
    ///     由 direct registration 或 reflected-implementation registration 上的
    ///     <c>RequestInvokerRegistration</c> 派生出的 <see cref="RequestInvokerEmissionSpec" /> 集合。
    ///     <c>methodIndex</c> 按 <paramref name="registrations" /> 与其 direct registration 的遍历顺序单调递增，
    ///     因而只要上游排序稳定，生成的 invoker 方法名与描述符顺序就跨运行保持稳定。
    /// </returns>
    /// <remarks>
    ///     缺少 <c>RequestInvokerRegistration</c> 的 direct registration 会被显式跳过，而不会生成半成品 provider 成员；
    ///     调用方应把“为什么没有 request invoker registration”对应的诊断留在更早的建模阶段，而不是在源码发射阶段兜底。
    /// </remarks>
    private static ImmutableArray<RequestInvokerEmissionSpec> CreateRequestInvokerEmissions(
        bool supportsRequestInvokerProvider,
        IReadOnlyList<ImplementationRegistrationSpec> registrations)
    {
        if (!supportsRequestInvokerProvider)
            return ImmutableArray<RequestInvokerEmissionSpec>.Empty;

        var builder = ImmutableArray.CreateBuilder<RequestInvokerEmissionSpec>();
        var methodIndex = 0;
        foreach (var registration in registrations)
        {
            foreach (var directRegistration in registration.DirectRegistrations)
            {
                if (directRegistration.RequestInvokerRegistration is not { } requestInvokerRegistration)
                    continue;

                builder.Add(new RequestInvokerEmissionSpec(
                    requestInvokerRegistration.RequestTypeDisplayName,
                    requestInvokerRegistration.ResponseTypeDisplayName,
                    directRegistration.HandlerInterfaceDisplayName,
                    methodIndex++));
            }

            foreach (var reflectedRegistration in registration.ReflectedImplementationRegistrations)
            {
                if (reflectedRegistration.RequestInvokerRegistration is not { } requestInvokerRegistration)
                    continue;

                builder.Add(new RequestInvokerEmissionSpec(
                    requestInvokerRegistration.RequestTypeDisplayName,
                    requestInvokerRegistration.ResponseTypeDisplayName,
                    reflectedRegistration.HandlerInterfaceDisplayName,
                    methodIndex++));
            }
        }

        return builder.ToImmutable();
    }

    /// <summary>
    ///     从可直接表达 handler 接口的注册描述中提取 stream invoker 发射计划。
    /// </summary>
    /// <param name="supportsStreamInvokerProvider">
    ///     指示当前 runtime 是否同时暴露 <c>ICqrsStreamInvokerProvider</c> 与
    ///     <c>IEnumeratesCqrsStreamInvokerDescriptors</c> 契约；若不支持，则本方法必须返回空结果并让后续发射路径整体跳过。
    /// </param>
    /// <param name="registrations">已按稳定顺序整理完成的 handler 注册描述。</param>
    /// <returns>
    ///     由 direct registration 或 reflected-implementation registration 上的
    ///     <c>StreamInvokerRegistration</c> 派生出的 <see cref="StreamInvokerEmissionSpec" /> 集合。
    ///     <c>methodIndex</c> 按 <paramref name="registrations" /> 与其 direct registration 的遍历顺序单调递增，
    ///     因而只要上游排序稳定，生成的 invoker 方法名与描述符顺序就跨运行保持稳定。
    /// </returns>
    private static ImmutableArray<StreamInvokerEmissionSpec> CreateStreamInvokerEmissions(
        bool supportsStreamInvokerProvider,
        IReadOnlyList<ImplementationRegistrationSpec> registrations)
    {
        if (!supportsStreamInvokerProvider)
            return ImmutableArray<StreamInvokerEmissionSpec>.Empty;

        var builder = ImmutableArray.CreateBuilder<StreamInvokerEmissionSpec>();
        var methodIndex = 0;
        foreach (var registration in registrations)
        {
            foreach (var directRegistration in registration.DirectRegistrations)
            {
                if (directRegistration.StreamInvokerRegistration is not { } streamInvokerRegistration)
                    continue;

                builder.Add(new StreamInvokerEmissionSpec(
                    streamInvokerRegistration.RequestTypeDisplayName,
                    streamInvokerRegistration.ResponseTypeDisplayName,
                    directRegistration.HandlerInterfaceDisplayName,
                    methodIndex++));
            }

            foreach (var reflectedRegistration in registration.ReflectedImplementationRegistrations)
            {
                if (reflectedRegistration.StreamInvokerRegistration is not { } streamInvokerRegistration)
                    continue;

                builder.Add(new StreamInvokerEmissionSpec(
                    streamInvokerRegistration.RequestTypeDisplayName,
                    streamInvokerRegistration.ResponseTypeDisplayName,
                    reflectedRegistration.HandlerInterfaceDisplayName,
                    methodIndex++));
            }
        }

        return builder.ToImmutable();
    }

    /// <summary>
    ///     发射生成文件头、nullable 指令以及注册器所需的程序集级元数据特性。
    /// </summary>
    /// <param name="builder">生成源码构造器。</param>
    /// <param name="reflectionFallbackEmission">需要写入程序集级 reflection fallback 特性的元数据策略。</param>
    private static void AppendGeneratedSourcePreamble(
        StringBuilder builder,
        ReflectionFallbackEmissionSpec reflectionFallbackEmission)
    {
        builder.AppendLine("// <auto-generated />");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        if (reflectionFallbackEmission.HasFallbackHandlers)
        {
            AppendReflectionFallbackAttributes(builder, reflectionFallbackEmission);
            builder.AppendLine();
        }

        builder.Append("[assembly: global::");
        builder.Append(CqrsRuntimeNamespace);
        builder.Append(".CqrsHandlerRegistryAttribute(typeof(global::");
        builder.Append(GeneratedNamespace);
        builder.Append('.');
        builder.Append(GeneratedTypeName);
        builder.AppendLine("))]");
    }

    /// <summary>
    ///     发射程序集级 reflection fallback 元数据特性，供运行时补齐生成阶段无法精确表达的 handler。
    /// </summary>
    /// <param name="builder">生成源码构造器。</param>
    /// <param name="reflectionFallbackEmission">需要写入特性的 fallback 元数据策略。</param>
    private static void AppendReflectionFallbackAttributes(
        StringBuilder builder,
        ReflectionFallbackEmissionSpec reflectionFallbackEmission)
    {
        for (var index = 0; index < reflectionFallbackEmission.Attributes.Length; index++)
        {
            if (index > 0)
            {
                builder.AppendLine();
            }

            AppendReflectionFallbackAttribute(builder, reflectionFallbackEmission.Attributes[index]);
        }
    }

    /// <summary>
    ///     发射单个程序集级 reflection fallback 元数据特性实例。
    /// </summary>
    private static void AppendReflectionFallbackAttribute(
        StringBuilder builder,
        ReflectionFallbackAttributeEmissionSpec attributeEmission)
    {
        builder.Append("[assembly: global::");
        builder.Append(CqrsRuntimeNamespace);
        builder.Append(".CqrsReflectionFallbackAttribute(");

        for (var index = 0; index < attributeEmission.Values.Length; index++)
        {
            if (index > 0)
                builder.Append(", ");

            if (attributeEmission.EmitDirectTypeReferences)
            {
                builder.Append("typeof(");
                builder.Append(attributeEmission.Values[index]);
                builder.Append(')');
            }
            else
            {
                builder.Append('"');
                builder.Append(EscapeStringLiteral(attributeEmission.Values[index]));
                builder.Append('"');
            }
        }

        builder.Append(")]");
    }

    /// <summary>
    ///     发射生成注册器类型本体，包括 <c>Register</c> 方法和运行时反射辅助方法。
    /// </summary>
    /// <param name="builder">生成源码构造器。</param>
    /// <param name="registrations">已排序的 handler 注册描述。</param>
    /// <param name="sourceShape">当前输出需要启用的结构分支。</param>
    private static void AppendGeneratedRegistryType(
        StringBuilder builder,
        IReadOnlyList<ImplementationRegistrationSpec> registrations,
        GeneratedRegistrySourceShape sourceShape)
    {
        builder.AppendLine();
        builder.Append("namespace ");
        builder.Append(GeneratedNamespace);
        builder.AppendLine(";");
        builder.AppendLine();
        builder.Append("internal sealed class ");
        builder.Append(GeneratedTypeName);
        builder.Append(" : global::");
        builder.Append(CqrsRuntimeNamespace);
        builder.Append(".ICqrsHandlerRegistry");
        if (sourceShape.HasRequestInvokerProvider)
        {
            builder.Append(", global::");
            builder.Append(CqrsRuntimeNamespace);
            builder.Append(".ICqrsRequestInvokerProvider, global::");
            builder.Append(CqrsRuntimeNamespace);
            builder.Append(".IEnumeratesCqrsRequestInvokerDescriptors");
        }

        if (sourceShape.HasStreamInvokerProvider)
        {
            builder.Append(", global::");
            builder.Append(CqrsRuntimeNamespace);
            builder.Append(".ICqrsStreamInvokerProvider, global::");
            builder.Append(CqrsRuntimeNamespace);
            builder.Append(".IEnumeratesCqrsStreamInvokerDescriptors");
        }

        builder.AppendLine();
        builder.AppendLine("{");
        AppendRegisterMethod(builder, registrations, sourceShape);

        if (sourceShape.HasRequestInvokerProvider)
        {
            builder.AppendLine();
            AppendRequestInvokerProviderMembers(builder, sourceShape.RequestInvokerEmissions);
        }

        if (sourceShape.HasStreamInvokerProvider)
        {
            builder.AppendLine();
            AppendStreamInvokerProviderMembers(builder, sourceShape.StreamInvokerEmissions);
        }

        if (sourceShape.HasExternalAssemblyTypeLookups)
        {
            builder.AppendLine();
            AppendReflectionHelpers(builder);
        }

        builder.AppendLine("}");
    }

    /// <summary>
    ///     发射注册器的 <c>Register</c> 方法，保持直接注册和反射注册之间的原始稳定排序。
    /// </summary>
    /// <param name="builder">生成源码构造器。</param>
    /// <param name="registrations">已排序的 handler 注册描述。</param>
    /// <param name="sourceShape">当前输出需要启用的结构分支。</param>
    private static void AppendRegisterMethod(
        StringBuilder builder,
        IReadOnlyList<ImplementationRegistrationSpec> registrations,
        GeneratedRegistrySourceShape sourceShape)
    {
        builder.Append(
            "    public void Register(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services, global::");
        builder.Append(LoggingNamespace);
        builder.AppendLine(".ILogger logger)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (services is null)");
        builder.AppendLine("            throw new global::System.ArgumentNullException(nameof(services));");
        builder.AppendLine("        if (logger is null)");
        builder.AppendLine("            throw new global::System.ArgumentNullException(nameof(logger));");
        if (sourceShape.RequiresRegistryAssemblyVariable)
        {
            builder.AppendLine();
            builder.Append("        var registryAssembly = typeof(global::");
            builder.Append(GeneratedNamespace);
            builder.Append('.');
            builder.Append(GeneratedTypeName);
            builder.AppendLine(").Assembly;");
        }

        if (registrations.Count > 0)
            builder.AppendLine();

        for (var registrationIndex = 0; registrationIndex < registrations.Count; registrationIndex++)
        {
            var registration = registrations[registrationIndex];
            if (!registration.ReflectedImplementationRegistrations.IsDefaultOrEmpty ||
                !registration.PreciseReflectedRegistrations.IsDefaultOrEmpty)
            {
                AppendOrderedImplementationRegistrations(builder, registration, registrationIndex);
            }
            else if (!registration.DirectRegistrations.IsDefaultOrEmpty)
            {
                AppendDirectRegistrations(builder, registration);
            }
        }

        builder.AppendLine("    }");
    }

    /// <summary>
    ///     发射 generated registry 的 request invoker provider 成员。
    /// </summary>
    /// <param name="builder">生成源码构造器。</param>
    /// <param name="requestInvokerEmissions">
    ///     来自 <see cref="CreateRequestInvokerEmissions(bool, IReadOnlyList{ImplementationRegistrationSpec})" /> 的稳定发射计划。
    /// </param>
    /// <remarks>
    ///     该输出包含三部分：描述符数组、provider 查询方法，以及与描述符逐项对应的静态 invoker 方法。
    ///     若发射计划为空，调用方应直接跳过整个 provider 分支，而不是输出空的 registry seam。
    /// </remarks>
    private static void AppendRequestInvokerProviderMembers(
        StringBuilder builder,
        ImmutableArray<RequestInvokerEmissionSpec> requestInvokerEmissions)
    {
        AppendRequestInvokerDescriptorArray(builder, requestInvokerEmissions);
        builder.AppendLine();
        AppendRequestInvokerProviderMethods(builder);

        for (var index = 0; index < requestInvokerEmissions.Length; index++)
        {
            builder.AppendLine();
            AppendRequestInvokerMethod(builder, requestInvokerEmissions[index]);
        }
    }

    /// <summary>
    ///     发射 generated registry 的 request invoker 描述符数组。
    /// </summary>
    /// <param name="builder">生成源码构造器。</param>
    /// <param name="requestInvokerEmissions">当前要输出的 request invoker 发射计划。</param>
    /// <remarks>
    ///     每个条目都会把请求类型、响应类型和对应的静态 invoker 方法打包成
    ///     <c>CqrsRequestInvokerDescriptorEntry</c>，供 registrar 在注册阶段写入 dispatcher 的弱缓存。
    /// </remarks>
    private static void AppendRequestInvokerDescriptorArray(
        StringBuilder builder,
        ImmutableArray<RequestInvokerEmissionSpec> requestInvokerEmissions)
    {
        builder.AppendLine("    private static readonly global::GFramework.Cqrs.CqrsRequestInvokerDescriptorEntry[] RequestInvokerDescriptors =");
        builder.AppendLine("    [");

        for (var index = 0; index < requestInvokerEmissions.Length; index++)
        {
            var emission = requestInvokerEmissions[index];
            builder.Append("        new global::");
            builder.Append(CqrsRuntimeNamespace);
            builder.Append(".CqrsRequestInvokerDescriptorEntry(typeof(");
            builder.Append(emission.RequestTypeDisplayName);
            builder.Append("), typeof(");
            builder.Append(emission.ResponseTypeDisplayName);
            builder.Append("), new global::");
            builder.Append(CqrsRuntimeNamespace);
            builder.Append(".CqrsRequestInvokerDescriptor(typeof(");
            builder.Append(emission.HandlerInterfaceDisplayName);
            builder.Append("), typeof(");
            builder.Append(GeneratedTypeName);
            builder.Append(").GetMethod(nameof(InvokeRequestHandler");
            builder.Append(emission.MethodIndex);
            builder.Append("), global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Static)!))");
            builder.AppendLine(index == requestInvokerEmissions.Length - 1 ? string.Empty : ",");
        }

        builder.AppendLine("    ];");
    }

    /// <summary>
    ///     发射 generated registry 对 request invoker provider 契约的实现方法。
    /// </summary>
    /// <param name="builder">生成源码构造器。</param>
    /// <remarks>
    ///     默认 runtime 真正消费的是 <c>GetDescriptors()</c> 暴露的完整描述符集合，并在注册阶段一次性写入缓存；
    ///     <c>TryGetDescriptor(...)</c> 保留为显式查询接口，因此这里使用线性扫描即可保持生成代码简单且无额外字典分配。
    /// </remarks>
    private static void AppendRequestInvokerProviderMethods(StringBuilder builder)
    {
        builder.Append("    global::System.Collections.Generic.IReadOnlyList<global::");
        builder.Append(CqrsRuntimeNamespace);
        builder.Append(".CqrsRequestInvokerDescriptorEntry> global::");
        builder.Append(CqrsRuntimeNamespace);
        builder.AppendLine(".IEnumeratesCqrsRequestInvokerDescriptors.GetDescriptors()");
        builder.AppendLine("    {");
        builder.AppendLine("        return RequestInvokerDescriptors;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.Append("    public bool TryGetDescriptor(global::System.Type requestType, global::System.Type responseType, out global::");
        builder.Append(CqrsRuntimeNamespace);
        builder.AppendLine(".CqrsRequestInvokerDescriptor? descriptor)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (requestType is null)");
        builder.AppendLine("            throw new global::System.ArgumentNullException(nameof(requestType));");
        builder.AppendLine("        if (responseType is null)");
        builder.AppendLine("            throw new global::System.ArgumentNullException(nameof(responseType));");
        builder.AppendLine();
        builder.AppendLine("        foreach (var entry in RequestInvokerDescriptors)");
        builder.AppendLine("        {");
        builder.AppendLine("            if (entry.RequestType == requestType && entry.ResponseType == responseType)");
        builder.AppendLine("            {");
        builder.AppendLine("                descriptor = entry.Descriptor;");
        builder.AppendLine("                return true;");
        builder.AppendLine("            }");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        descriptor = null;");
        builder.AppendLine("        return false;");
        builder.AppendLine("    }");
    }

    /// <summary>
    ///     为单个 request invoker 描述符发射对应的静态强类型桥接方法。
    /// </summary>
    /// <param name="builder">生成源码构造器。</param>
    /// <param name="emission">当前要输出的 invoker 发射计划。</param>
    /// <remarks>
    ///     这些方法的编号与 <see cref="RequestInvokerEmissionSpec.MethodIndex" /> 一一对应，
    ///     dispatcher 通过描述符里的 <see cref="MethodInfo" /> 把 object 形参桥接回强类型 handler 与 request。
    /// </remarks>
    private static void AppendRequestInvokerMethod(StringBuilder builder, RequestInvokerEmissionSpec emission)
    {
        builder.Append("    private static global::System.Threading.Tasks.ValueTask<");
        builder.Append(emission.ResponseTypeDisplayName);
        builder.Append("> InvokeRequestHandler");
        builder.Append(emission.MethodIndex);
        builder.Append("(object handler, object request, global::System.Threading.CancellationToken cancellationToken)");
        builder.AppendLine();
        builder.AppendLine("    {");
        builder.Append("        var typedHandler = (");
        builder.Append(emission.HandlerInterfaceDisplayName);
        builder.AppendLine(")handler;");
        builder.Append("        var typedRequest = (");
        builder.Append(emission.RequestTypeDisplayName);
        builder.AppendLine(")request;");
        builder.AppendLine("        return typedHandler.Handle(typedRequest, cancellationToken);");
        builder.AppendLine("    }");
    }

    /// <summary>
    ///     发射 generated registry 的 stream invoker provider 成员。
    /// </summary>
    /// <param name="builder">生成源码构造器。</param>
    /// <param name="streamInvokerEmissions">当前要输出的 stream invoker 发射计划。</param>
    private static void AppendStreamInvokerProviderMembers(
        StringBuilder builder,
        ImmutableArray<StreamInvokerEmissionSpec> streamInvokerEmissions)
    {
        AppendStreamInvokerDescriptorArray(builder, streamInvokerEmissions);
        builder.AppendLine();
        AppendStreamInvokerProviderMethods(builder);

        for (var index = 0; index < streamInvokerEmissions.Length; index++)
        {
            builder.AppendLine();
            AppendStreamInvokerMethod(builder, streamInvokerEmissions[index]);
        }
    }

    /// <summary>
    ///     发射 generated registry 的 stream invoker 描述符数组。
    /// </summary>
    private static void AppendStreamInvokerDescriptorArray(
        StringBuilder builder,
        ImmutableArray<StreamInvokerEmissionSpec> streamInvokerEmissions)
    {
        builder.AppendLine("    private static readonly global::GFramework.Cqrs.CqrsStreamInvokerDescriptorEntry[] StreamInvokerDescriptors =");
        builder.AppendLine("    [");

        for (var index = 0; index < streamInvokerEmissions.Length; index++)
        {
            var emission = streamInvokerEmissions[index];
            builder.Append("        new global::");
            builder.Append(CqrsRuntimeNamespace);
            builder.Append(".CqrsStreamInvokerDescriptorEntry(typeof(");
            builder.Append(emission.RequestTypeDisplayName);
            builder.Append("), typeof(");
            builder.Append(emission.ResponseTypeDisplayName);
            builder.Append("), new global::");
            builder.Append(CqrsRuntimeNamespace);
            builder.Append(".CqrsStreamInvokerDescriptor(typeof(");
            builder.Append(emission.HandlerInterfaceDisplayName);
            builder.Append("), typeof(");
            builder.Append(GeneratedTypeName);
            builder.Append(").GetMethod(nameof(InvokeStreamHandler");
            builder.Append(emission.MethodIndex);
            builder.Append("), global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Static)!))");
            builder.AppendLine(index == streamInvokerEmissions.Length - 1 ? string.Empty : ",");
        }

        builder.AppendLine("    ];");
    }

    /// <summary>
    ///     发射 generated registry 对 stream invoker provider 契约的实现方法。
    /// </summary>
    private static void AppendStreamInvokerProviderMethods(StringBuilder builder)
    {
        builder.Append("    global::System.Collections.Generic.IReadOnlyList<global::");
        builder.Append(CqrsRuntimeNamespace);
        builder.Append(".CqrsStreamInvokerDescriptorEntry> global::");
        builder.Append(CqrsRuntimeNamespace);
        builder.AppendLine(".IEnumeratesCqrsStreamInvokerDescriptors.GetDescriptors()");
        builder.AppendLine("    {");
        builder.AppendLine("        return StreamInvokerDescriptors;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.Append("    public bool TryGetDescriptor(global::System.Type requestType, global::System.Type responseType, out global::");
        builder.Append(CqrsRuntimeNamespace);
        builder.AppendLine(".CqrsStreamInvokerDescriptor? descriptor)");
        builder.AppendLine("    {");
        builder.AppendLine("        if (requestType is null)");
        builder.AppendLine("            throw new global::System.ArgumentNullException(nameof(requestType));");
        builder.AppendLine("        if (responseType is null)");
        builder.AppendLine("            throw new global::System.ArgumentNullException(nameof(responseType));");
        builder.AppendLine();
        builder.AppendLine("        foreach (var entry in StreamInvokerDescriptors)");
        builder.AppendLine("        {");
        builder.AppendLine("            if (entry.RequestType == requestType && entry.ResponseType == responseType)");
        builder.AppendLine("            {");
        builder.AppendLine("                descriptor = entry.Descriptor;");
        builder.AppendLine("                return true;");
        builder.AppendLine("            }");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        descriptor = null;");
        builder.AppendLine("        return false;");
        builder.AppendLine("    }");
    }

    /// <summary>
    ///     为单个 stream invoker 描述符发射对应的静态强类型桥接方法。
    /// </summary>
    private static void AppendStreamInvokerMethod(StringBuilder builder, StreamInvokerEmissionSpec emission)
    {
        builder.Append("    private static object InvokeStreamHandler");
        builder.Append(emission.MethodIndex);
        builder.Append("(object handler, object request, global::System.Threading.CancellationToken cancellationToken)");
        builder.AppendLine();
        builder.AppendLine("    {");
        builder.Append("        var typedHandler = (");
        builder.Append(emission.HandlerInterfaceDisplayName);
        builder.AppendLine(")handler;");
        builder.Append("        var typedRequest = (");
        builder.Append(emission.RequestTypeDisplayName);
        builder.AppendLine(")request;");
        builder.AppendLine("        return typedHandler.Handle(typedRequest, cancellationToken);");
        builder.AppendLine("    }");
    }

    private static void AppendDirectRegistrations(
        StringBuilder builder,
        ImplementationRegistrationSpec registration)
    {
        foreach (var directRegistration in registration.DirectRegistrations)
        {
            AppendServiceRegistration(
                builder,
                $"typeof({directRegistration.HandlerInterfaceDisplayName})",
                $"typeof({directRegistration.ImplementationTypeDisplayName})",
                "        ");
            AppendRegistrationLog(
                builder,
                directRegistration.ImplementationLogName,
                directRegistration.HandlerInterfaceLogName,
                "        ");
        }
    }

    /// <summary>
    ///     发射 <c>AddTransient</c> 调用，调用方负责传入已经按当前分支解析好的 service 和 implementation 表达式。
    /// </summary>
    /// <param name="builder">生成源码构造器。</param>
    /// <param name="serviceTypeExpression">生成代码中的服务类型表达式。</param>
    /// <param name="implementationTypeExpression">生成代码中的实现类型表达式。</param>
    /// <param name="indent">当前生成语句的缩进。</param>
    private static void AppendServiceRegistration(
        StringBuilder builder,
        string serviceTypeExpression,
        string implementationTypeExpression,
        string indent)
    {
        builder.Append(indent);
        builder.AppendLine("global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddTransient(");
        builder.Append(indent);
        builder.AppendLine("    services,");
        builder.Append(indent);
        builder.Append("    ");
        builder.Append(serviceTypeExpression);
        builder.AppendLine(",");
        builder.Append(indent);
        builder.Append("    ");
        builder.Append(implementationTypeExpression);
        builder.AppendLine(");");
    }

    /// <summary>
    ///     发射与注册语句配套的调试日志，保持所有生成注册路径的日志文本完全一致。
    /// </summary>
    /// <param name="builder">生成源码构造器。</param>
    /// <param name="implementationLogName">实现类型日志名。</param>
    /// <param name="handlerInterfaceLogName">handler 接口日志名。</param>
    /// <param name="indent">当前生成语句的缩进。</param>
    private static void AppendRegistrationLog(
        StringBuilder builder,
        string implementationLogName,
        string handlerInterfaceLogName,
        string indent)
    {
        builder.Append(indent);
        builder.Append("logger.Debug(\"Registered CQRS handler ");
        builder.Append(EscapeStringLiteral(implementationLogName));
        builder.Append(" as ");
        builder.Append(EscapeStringLiteral(handlerInterfaceLogName));
        builder.AppendLine(".\");");
    }

    private static void AppendOrderedImplementationRegistrations(
        StringBuilder builder,
        ImplementationRegistrationSpec registration,
        int registrationIndex)
    {
        var orderedRegistrations = CreateOrderedRegistrations(registration);
        var implementationVariableName = $"implementationType{registrationIndex}";
        AppendImplementationTypeVariable(builder, registration, implementationVariableName);

        builder.Append("        if (");
        builder.Append(implementationVariableName);
        builder.AppendLine(" is not null)");
        builder.AppendLine("        {");

        foreach (var orderedRegistration in orderedRegistrations)
        {
            AppendOrderedRegistration(
                builder,
                registration,
                orderedRegistration,
                registrationIndex,
                implementationVariableName);
        }

        builder.AppendLine("        }");
    }

    /// <summary>
    ///     合并直接注册、实现类型反射注册和精确反射注册，并按 handler 接口日志名排序以保持生成输出稳定。
    /// </summary>
    /// <param name="registration">单个实现类型聚合后的注册描述。</param>
    /// <returns>带有来源类型和原始索引的有序注册列表。</returns>
    private static List<OrderedRegistrationSpec> CreateOrderedRegistrations(ImplementationRegistrationSpec registration)
    {
        var orderedRegistrations = new List<OrderedRegistrationSpec>(
            registration.DirectRegistrations.Length +
            registration.ReflectedImplementationRegistrations.Length +
            registration.PreciseReflectedRegistrations.Length);
        for (var directIndex = 0; directIndex < registration.DirectRegistrations.Length; directIndex++)
        {
            orderedRegistrations.Add(new OrderedRegistrationSpec(
                registration.DirectRegistrations[directIndex].HandlerInterfaceLogName,
                OrderedRegistrationKind.Direct,
                directIndex));
        }

        for (var reflectedIndex = 0;
             reflectedIndex < registration.ReflectedImplementationRegistrations.Length;
             reflectedIndex++)
        {
            orderedRegistrations.Add(new OrderedRegistrationSpec(
                registration.ReflectedImplementationRegistrations[reflectedIndex].HandlerInterfaceLogName,
                OrderedRegistrationKind.ReflectedImplementation,
                reflectedIndex));
        }

        for (var preciseIndex = 0;
             preciseIndex < registration.PreciseReflectedRegistrations.Length;
             preciseIndex++)
        {
            orderedRegistrations.Add(new OrderedRegistrationSpec(
                registration.PreciseReflectedRegistrations[preciseIndex].HandlerInterfaceLogName,
                OrderedRegistrationKind.PreciseReflected,
                preciseIndex));
        }

        orderedRegistrations.Sort(static (left, right) =>
            StringComparer.Ordinal.Compare(left.HandlerInterfaceLogName, right.HandlerInterfaceLogName));
        return orderedRegistrations;
    }

    /// <summary>
    ///     发射实现类型变量。公开类型直接使用 <c>typeof</c>，不可直接引用的实现类型则从当前程序集反射解析。
    /// </summary>
    /// <param name="builder">生成源码构造器。</param>
    /// <param name="registration">单个实现类型聚合后的注册描述。</param>
    /// <param name="implementationVariableName">生成代码中的实现类型变量名。</param>
    private static void AppendImplementationTypeVariable(
        StringBuilder builder,
        ImplementationRegistrationSpec registration,
        string implementationVariableName)
    {
        if (string.IsNullOrWhiteSpace(registration.ReflectionTypeMetadataName))
        {
            builder.Append("        var ");
            builder.Append(implementationVariableName);
            builder.Append(" = typeof(");
            builder.Append(registration.ImplementationTypeDisplayName);
            builder.AppendLine(");");
        }
        else
        {
            builder.Append("        var ");
            builder.Append(implementationVariableName);
            builder.Append(" = registryAssembly.GetType(\"");
            builder.Append(EscapeStringLiteral(registration.ReflectionTypeMetadataName!));
            builder.AppendLine("\", throwOnError: false, ignoreCase: false);");
        }
    }

    /// <summary>
    ///     根据注册来源发射单条有序注册，确保混合直接和反射路径时仍按 handler 接口名稳定输出。
    /// </summary>
    /// <param name="builder">生成源码构造器。</param>
    /// <param name="registration">单个实现类型聚合后的注册描述。</param>
    /// <param name="orderedRegistration">带来源类型和原始索引的排序项。</param>
    /// <param name="registrationIndex">实现类型在整体注册列表中的索引，用于生成稳定变量名。</param>
    /// <param name="implementationVariableName">生成代码中的实现类型变量名。</param>
    private static void AppendOrderedRegistration(
        StringBuilder builder,
        ImplementationRegistrationSpec registration,
        OrderedRegistrationSpec orderedRegistration,
        int registrationIndex,
        string implementationVariableName)
    {
        switch (orderedRegistration.Kind)
        {
            case OrderedRegistrationKind.Direct:
                AppendOrderedDirectRegistration(
                    builder,
                    registration,
                    registration.DirectRegistrations[orderedRegistration.Index],
                    implementationVariableName);
                break;
            case OrderedRegistrationKind.ReflectedImplementation:
                AppendOrderedReflectedImplementationRegistration(
                    builder,
                    registration,
                    registration.ReflectedImplementationRegistrations[orderedRegistration.Index],
                    implementationVariableName);
                break;
            case OrderedRegistrationKind.PreciseReflected:
                AppendOrderedPreciseReflectedRegistration(
                    builder,
                    registration,
                    registration.PreciseReflectedRegistrations[orderedRegistration.Index],
                    registrationIndex,
                    orderedRegistration.Index,
                    implementationVariableName);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported ordered CQRS registration kind {orderedRegistration.Kind}.");
        }
    }

    /// <summary>
    ///     发射实现类型已通过变量解析、handler 接口可直接引用的直接注册语句。
    /// </summary>
    /// <param name="builder">生成源码构造器。</param>
    /// <param name="registration">单个实现类型聚合后的注册描述。</param>
    /// <param name="directRegistration">当前直接注册项。</param>
    /// <param name="implementationVariableName">生成代码中的实现类型变量名。</param>
    private static void AppendOrderedDirectRegistration(
        StringBuilder builder,
        ImplementationRegistrationSpec registration,
        HandlerRegistrationSpec directRegistration,
        string implementationVariableName)
    {
        AppendServiceRegistration(
            builder,
            $"typeof({directRegistration.HandlerInterfaceDisplayName})",
            implementationVariableName,
            "            ");
        AppendRegistrationLog(
            builder,
            registration.ImplementationLogName,
            directRegistration.HandlerInterfaceLogName,
            "            ");
    }

    /// <summary>
    ///     发射实现类型需要反射解析、handler 接口可直接引用的注册语句。
    /// </summary>
    /// <param name="builder">生成源码构造器。</param>
    /// <param name="registration">单个实现类型聚合后的注册描述。</param>
    /// <param name="reflectedRegistration">当前实现类型反射注册项。</param>
    /// <param name="implementationVariableName">生成代码中的实现类型变量名。</param>
    private static void AppendOrderedReflectedImplementationRegistration(
        StringBuilder builder,
        ImplementationRegistrationSpec registration,
        ReflectedImplementationRegistrationSpec reflectedRegistration,
        string implementationVariableName)
    {
        AppendServiceRegistration(
            builder,
            $"typeof({reflectedRegistration.HandlerInterfaceDisplayName})",
            implementationVariableName,
            "            ");
        AppendRegistrationLog(
            builder,
            registration.ImplementationLogName,
            reflectedRegistration.HandlerInterfaceLogName,
            "            ");
    }

    /// <summary>
    ///     发射 handler 接口需要运行时精确构造的注册语句。
    /// </summary>
    /// <param name="builder">生成源码构造器。</param>
    /// <param name="registration">单个实现类型聚合后的注册描述。</param>
    /// <param name="preciseRegistration">当前精确反射注册项。</param>
    /// <param name="registrationIndex">实现类型在整体注册列表中的索引。</param>
    /// <param name="orderedRegistrationIndex">当前注册项在原始精确反射注册集合中的索引。</param>
    /// <param name="implementationVariableName">生成代码中的实现类型变量名。</param>
    private static void AppendOrderedPreciseReflectedRegistration(
        StringBuilder builder,
        ImplementationRegistrationSpec registration,
        PreciseReflectedRegistrationSpec preciseRegistration,
        int registrationIndex,
        int orderedRegistrationIndex,
        string implementationVariableName)
    {
        var registrationVariablePrefix = $"serviceType{registrationIndex}_{orderedRegistrationIndex}";
        AppendPreciseReflectedTypeResolution(
            builder,
            preciseRegistration.ServiceTypeArguments,
            registrationVariablePrefix,
            implementationVariableName,
            preciseRegistration.OpenHandlerTypeDisplayName,
            registration.ImplementationLogName,
            preciseRegistration.HandlerInterfaceLogName,
            3);
    }

    private static void AppendPreciseReflectedTypeResolution(
        StringBuilder builder,
        ImmutableArray<RuntimeTypeReferenceSpec> serviceTypeArguments,
        string registrationVariablePrefix,
        string implementationVariableName,
        string openHandlerTypeDisplayName,
        string implementationLogName,
        string handlerInterfaceLogName,
        int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);
        var reflectedArgumentNames = new List<string>();
        var resolvedArgumentNames = AppendServiceTypeArgumentResolutions(
            builder,
            serviceTypeArguments,
            registrationVariablePrefix,
            reflectedArgumentNames,
            indent);

        if (reflectedArgumentNames.Count > 0)
            indent = AppendReflectedArgumentGuardStart(builder, reflectedArgumentNames, indent);

        AppendClosedGenericServiceTypeCreation(
            builder,
            registrationVariablePrefix,
            openHandlerTypeDisplayName,
            resolvedArgumentNames,
            indent);
        AppendServiceRegistration(builder, registrationVariablePrefix, implementationVariableName, indent);
        AppendRegistrationLog(builder, implementationLogName, handlerInterfaceLogName, indent);

        if (reflectedArgumentNames.Count > 0)
        {
            builder.Append(new string(' ', indentLevel * 4));
            builder.AppendLine("}");
        }
    }

    /// <summary>
    ///     递归发射每个 handler 泛型实参的运行时类型解析表达式。
    /// </summary>
    /// <param name="builder">生成源码构造器。</param>
    /// <param name="serviceTypeArguments">handler 服务类型的运行时泛型实参描述。</param>
    /// <param name="registrationVariablePrefix">当前注册项的稳定变量名前缀。</param>
    /// <param name="reflectedArgumentNames">需要空值检查的反射解析变量集合。</param>
    /// <param name="indent">当前生成语句的缩进。</param>
    /// <returns>可传给 <c>MakeGenericType</c> 的实参表达式。</returns>
    private static string[] AppendServiceTypeArgumentResolutions(
        StringBuilder builder,
        ImmutableArray<RuntimeTypeReferenceSpec> serviceTypeArguments,
        string registrationVariablePrefix,
        ICollection<string> reflectedArgumentNames,
        string indent)
    {
        var resolvedArgumentNames = new string[serviceTypeArguments.Length];
        for (var argumentIndex = 0; argumentIndex < serviceTypeArguments.Length; argumentIndex++)
        {
            resolvedArgumentNames[argumentIndex] = AppendRuntimeTypeReferenceResolution(
                builder,
                serviceTypeArguments[argumentIndex],
                $"{registrationVariablePrefix}Argument{argumentIndex}",
                reflectedArgumentNames,
                indent);
        }

        return resolvedArgumentNames;
    }

    /// <summary>
    ///     为运行时反射解析出的泛型实参发射空值保护块，避免生成注册器注册无法完整构造的服务类型。
    /// </summary>
    /// <param name="builder">生成源码构造器。</param>
    /// <param name="reflectedArgumentNames">需要参与空值检查的变量名。</param>
    /// <param name="indent">保护块开始前的缩进。</param>
    /// <returns>保护块内部应使用的下一层缩进。</returns>
    private static string AppendReflectedArgumentGuardStart(
        StringBuilder builder,
        IReadOnlyList<string> reflectedArgumentNames,
        string indent)
    {
        builder.Append(indent);
        builder.Append("if (");
        for (var index = 0; index < reflectedArgumentNames.Count; index++)
        {
            if (index > 0)
                builder.Append(" && ");

            builder.Append(reflectedArgumentNames[index]);
            builder.Append(" is not null");
        }

        builder.AppendLine(")");
        builder.Append(indent);
        builder.AppendLine("{");
        return $"{indent}    ";
    }

    /// <summary>
    ///     发射关闭 handler 服务类型的 <c>MakeGenericType</c> 构造语句。
    /// </summary>
    /// <param name="builder">生成源码构造器。</param>
    /// <param name="registrationVariablePrefix">生成代码中的服务类型变量名。</param>
    /// <param name="openHandlerTypeDisplayName">开放 handler 接口类型显示名。</param>
    /// <param name="resolvedArgumentNames">已解析的泛型实参表达式。</param>
    /// <param name="indent">当前生成语句的缩进。</param>
    private static void AppendClosedGenericServiceTypeCreation(
        StringBuilder builder,
        string registrationVariablePrefix,
        string openHandlerTypeDisplayName,
        IReadOnlyList<string> resolvedArgumentNames,
        string indent)
    {
        builder.Append(indent);
        builder.Append("var ");
        builder.Append(registrationVariablePrefix);
        builder.Append(" = typeof(");
        builder.Append(openHandlerTypeDisplayName);
        builder.Append(").MakeGenericType(");
        for (var index = 0; index < resolvedArgumentNames.Count; index++)
        {
            if (index > 0)
                builder.Append(", ");

            builder.Append(resolvedArgumentNames[index]);
        }

        builder.AppendLine(");");
    }

    private static string AppendRuntimeTypeReferenceResolution(
        StringBuilder builder,
        RuntimeTypeReferenceSpec runtimeTypeReference,
        string variableBaseName,
        ICollection<string> reflectedArgumentNames,
        string indent)
    {
        if (!string.IsNullOrWhiteSpace(runtimeTypeReference.TypeDisplayName))
            return $"typeof({runtimeTypeReference.TypeDisplayName})";

        if (runtimeTypeReference.ArrayElementTypeReference is not null)
            return AppendArrayRuntimeTypeReferenceResolution(
                builder,
                runtimeTypeReference,
                variableBaseName,
                reflectedArgumentNames,
                indent);

        if (runtimeTypeReference.GenericTypeDefinitionReference is not null)
            return AppendConstructedGenericRuntimeTypeReferenceResolution(
                builder,
                runtimeTypeReference,
                variableBaseName,
                reflectedArgumentNames,
                indent);

        return AppendReflectionRuntimeTypeReferenceResolution(
            builder,
            runtimeTypeReference,
            variableBaseName,
            reflectedArgumentNames,
            indent);
    }

    /// <summary>
    ///     发射数组类型引用的运行时重建表达式。
    /// </summary>
    /// <param name="builder">生成源码构造器。</param>
    /// <param name="runtimeTypeReference">数组类型引用描述。</param>
    /// <param name="variableBaseName">用于递归生成变量名的稳定前缀。</param>
    /// <param name="reflectedArgumentNames">需要空值检查的反射解析变量集合。</param>
    /// <param name="indent">当前生成语句的缩进。</param>
    /// <returns>数组类型表达式。</returns>
    private static string AppendArrayRuntimeTypeReferenceResolution(
        StringBuilder builder,
        RuntimeTypeReferenceSpec runtimeTypeReference,
        string variableBaseName,
        ICollection<string> reflectedArgumentNames,
        string indent)
    {
        var elementExpression = AppendRuntimeTypeReferenceResolution(
            builder,
            runtimeTypeReference.ArrayElementTypeReference!,
            $"{variableBaseName}Element",
            reflectedArgumentNames,
            indent);

        return runtimeTypeReference.ArrayRank == 1
            ? $"{elementExpression}.MakeArrayType()"
            : $"{elementExpression}.MakeArrayType({runtimeTypeReference.ArrayRank})";
    }

    /// <summary>
    ///     发射已构造泛型类型引用的运行时重建表达式。
    /// </summary>
    /// <param name="builder">生成源码构造器。</param>
    /// <param name="runtimeTypeReference">已构造泛型类型引用描述。</param>
    /// <param name="variableBaseName">用于递归生成变量名的稳定前缀。</param>
    /// <param name="reflectedArgumentNames">需要空值检查的反射解析变量集合。</param>
    /// <param name="indent">当前生成语句的缩进。</param>
    /// <returns>已构造泛型类型表达式。</returns>
    private static string AppendConstructedGenericRuntimeTypeReferenceResolution(
        StringBuilder builder,
        RuntimeTypeReferenceSpec runtimeTypeReference,
        string variableBaseName,
        ICollection<string> reflectedArgumentNames,
        string indent)
    {
        var genericTypeDefinitionExpression = AppendRuntimeTypeReferenceResolution(
            builder,
            runtimeTypeReference.GenericTypeDefinitionReference!,
            $"{variableBaseName}GenericDefinition",
            reflectedArgumentNames,
            indent);
        var genericArgumentExpressions = new string[runtimeTypeReference.GenericTypeArguments.Length];
        for (var argumentIndex = 0;
             argumentIndex < runtimeTypeReference.GenericTypeArguments.Length;
             argumentIndex++)
        {
            genericArgumentExpressions[argumentIndex] = AppendRuntimeTypeReferenceResolution(
                builder,
                runtimeTypeReference.GenericTypeArguments[argumentIndex],
                $"{variableBaseName}GenericArgument{argumentIndex}",
                reflectedArgumentNames,
                indent);
        }

        return $"{genericTypeDefinitionExpression}.MakeGenericType({string.Join(", ", genericArgumentExpressions)})";
    }

    /// <summary>
    ///     发射命名类型的运行时反射查找语句，并返回后续服务类型构造应引用的变量名。
    /// </summary>
    /// <param name="builder">生成源码构造器。</param>
    /// <param name="runtimeTypeReference">反射查找类型引用描述。</param>
    /// <param name="variableBaseName">生成代码中的反射变量名。</param>
    /// <param name="reflectedArgumentNames">需要空值检查的反射解析变量集合。</param>
    /// <param name="indent">当前生成语句的缩进。</param>
    /// <returns>生成代码中的反射变量名。</returns>
    private static string AppendReflectionRuntimeTypeReferenceResolution(
        StringBuilder builder,
        RuntimeTypeReferenceSpec runtimeTypeReference,
        string variableBaseName,
        ICollection<string> reflectedArgumentNames,
        string indent)
    {
        reflectedArgumentNames.Add(variableBaseName);
        builder.Append(indent);
        builder.Append("var ");
        builder.Append(variableBaseName);
        if (string.IsNullOrWhiteSpace(runtimeTypeReference.ReflectionAssemblyName))
        {
            builder.Append(" = registryAssembly.GetType(\"");
            builder.Append(EscapeStringLiteral(runtimeTypeReference.ReflectionTypeMetadataName!));
            builder.AppendLine("\", throwOnError: false, ignoreCase: false);");
        }
        else
        {
            builder.Append(" = ResolveReferencedAssemblyType(\"");
            builder.Append(EscapeStringLiteral(runtimeTypeReference.ReflectionAssemblyName!));
            builder.Append("\", \"");
            builder.Append(EscapeStringLiteral(runtimeTypeReference.ReflectionTypeMetadataName!));
            builder.AppendLine("\");");
        }

        return variableBaseName;
    }

    private static void AppendReflectionHelpers(StringBuilder builder)
    {
        builder.AppendLine(
            "    private static global::System.Type? ResolveReferencedAssemblyType(string assemblyIdentity, string typeMetadataName)");
        builder.AppendLine("    {");
        builder.AppendLine("        var assembly = ResolveReferencedAssembly(assemblyIdentity);");
        builder.AppendLine(
            "        return assembly?.GetType(typeMetadataName, throwOnError: false, ignoreCase: false);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine(
            "    private static global::System.Reflection.Assembly? ResolveReferencedAssembly(string assemblyIdentity)");
        builder.AppendLine("    {");
        builder.AppendLine("        global::System.Reflection.AssemblyName targetAssemblyName;");
        builder.AppendLine("        try");
        builder.AppendLine("        {");
        builder.AppendLine(
            "            targetAssemblyName = new global::System.Reflection.AssemblyName(assemblyIdentity);");
        builder.AppendLine("        }");
        builder.AppendLine("        catch");
        builder.AppendLine("        {");
        builder.AppendLine("            return null;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine(
            "        foreach (var assembly in global::System.AppDomain.CurrentDomain.GetAssemblies())");
        builder.AppendLine("        {");
        builder.AppendLine(
            "            if (global::System.Reflection.AssemblyName.ReferenceMatchesDefinition(targetAssemblyName, assembly.GetName()))");
        builder.AppendLine("                return assembly;");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        try");
        builder.AppendLine("        {");
        builder.AppendLine(
            "            return global::System.Reflection.Assembly.Load(targetAssemblyName);");
        builder.AppendLine("        }");
        builder.AppendLine("        catch");
        builder.AppendLine("        {");
        builder.AppendLine("            return null;");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
    }

    private static string EscapeStringLiteral(string value)
    {
        return value.Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }
}
