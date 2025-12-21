using GFramework.Core.architecture;
using Godot;

namespace GFramework.Godot.architecture;

public interface IGodotArchitectureExtension<T> where T : Architecture<T>, new()
{
    Node Node { get; }
    void OnAttach(Architecture<T> architecture);
    void OnDetach();
}
