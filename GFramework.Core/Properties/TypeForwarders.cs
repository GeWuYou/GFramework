using System.Runtime.CompilerServices;
using GFramework.Core.Abstractions.Logging;
using GFramework.Cqrs.Command;
using GFramework.Cqrs.Notification;
using GFramework.Cqrs.Query;
using GFramework.Cqrs.Request;

[assembly: TypeForwardedTo(typeof(LoggerFactoryResolver))]
[assembly: TypeForwardedTo(typeof(CommandBase<,>))]
[assembly: TypeForwardedTo(typeof(QueryBase<,>))]
[assembly: TypeForwardedTo(typeof(RequestBase<,>))]
[assembly: TypeForwardedTo(typeof(NotificationBase<>))]
