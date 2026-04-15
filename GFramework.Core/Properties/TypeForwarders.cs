using System.Runtime.CompilerServices;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Cqrs.Command;
using GFramework.Core.Cqrs.Notification;
using GFramework.Core.Cqrs.Query;
using GFramework.Core.Cqrs.Request;

[assembly: TypeForwardedTo(typeof(LoggerFactoryResolver))]
[assembly: TypeForwardedTo(typeof(CommandBase<,>))]
[assembly: TypeForwardedTo(typeof(QueryBase<,>))]
[assembly: TypeForwardedTo(typeof(RequestBase<,>))]
[assembly: TypeForwardedTo(typeof(NotificationBase<>))]
