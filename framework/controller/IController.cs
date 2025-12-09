using GWFramework.framework.command;
using GWFramework.framework.events;
using GWFramework.framework.model;
using GWFramework.framework.query;
using GWFramework.framework.system;
using GWFramework.framework.utility;

namespace GWFramework.framework.controller;

/// <summary>
/// 控制器接口，定义了控制器需要实现的所有功能契约
/// 该接口继承了多个框架核心接口，用于支持控制器的各种能力
/// 包括架构归属、命令发送、系统获取、模型获取、事件注册、查询发送和工具获取等功能
/// </summary>
public interface IController : ICanSendCommand, ICanGetSystem, ICanGetModel,
    ICanRegisterEvent, ICanSendQuery, ICanGetUtility;
