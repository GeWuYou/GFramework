using System.Runtime.CompilerServices;
using GFramework.Core.Abstractions.Logging;

[assembly: TypeForwardedTo(typeof(LoggerFactoryResolver))]
[assembly: TypeForwardedTo(typeof(CommandBase<,>))]
[assembly: TypeForwardedTo(typeof(QueryBase<,>))]
[assembly: TypeForwardedTo(typeof(RequestBase<,>))]
[assembly: TypeForwardedTo(typeof(NotificationBase<>))]
