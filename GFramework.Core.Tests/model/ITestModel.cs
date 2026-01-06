using GFramework.Core.Abstractions.model;

namespace GFramework.Core.Tests.model;

public interface ITestModel : IModel
{
    int GetCurrentXp { get; }
}