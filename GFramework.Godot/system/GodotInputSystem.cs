// using Godot;
//
// namespace GFramework.Godot.system;
//
// /// <summary>
// /// Godot引擎专用的输入系统实现
// /// </summary>
// public class GodotInputSystem : AbstractInputSystem
// {
//     private InputMap _inputMap;
//     private string _configPath;
//
//     public override void Init()
//     {
//         base.Init();
//         
//         _inputMap = new InputMap();
//         _configPath = "user://input_config.json";
//         
//         // 添加一些默认的输入动作
//         AddDefaultActions();
//         
//         // 尝试加载用户配置
//         LoadConfiguration();
//     }
//
//     public override void Destroy()
//     {
//         // 保存配置
//         SaveConfiguration();
//         base.Destroy();
//     }
//
//     /// <summary>
//     /// 添加默认输入动作
//     /// </summary>
//     private void AddDefaultActions()
//     {
//         _inputMap.AddAction(new InputAction("move_left", InputActionType.Axis, "Left"));
//         _inputMap.AddAction(new InputAction("move_right", InputActionType.Axis, "Right"));
//         _inputMap.AddAction(new InputAction("move_up", InputActionType.Axis, "Up"));
//         _inputMap.AddAction(new InputAction("move_down", InputActionType.Axis, "Down"));
//         _inputMap.AddAction(new InputAction("jump", InputActionType.Button, "Space"));
//         _inputMap.AddAction(new InputAction("attack", InputActionType.Button, "LeftMouse"));
//         _inputMap.AddAction(new InputAction("interact", InputActionType.Button, "E"));
//     }
//
//     /// <inheritdoc />
//     public override void SaveConfiguration()
//     {
//         try
//         {
//             var configData = new Dictionary<string, string[]>();
//             foreach (var action in _inputMap.GetAllActions())
//             {
//                 configData[action.Name] = action.CurrentBindings;
//             }
//             
//             var json = Json.Stringify(configData);
//             File.WriteAllText(ProjectSettings.GlobalizePath(_configPath), json);
//         }
//         catch (Exception ex)
//         {
//             GD.PrintErr($"Failed to save input configuration: {ex.Message}");
//         }
//     }
//
//     /// <inheritdoc />
//     public override void LoadConfiguration()
//     {
//         try
//         {
//             if (!File.Exists(ProjectSettings.GlobalizePath(_configPath)))
//             {
//                 // 配置文件不存在，使用默认配置
//                 return;
//             }
//             
//             var json = File.ReadAllText(ProjectSettings.GlobalizePath(_configPath));
//             var parsed = Json.ParseString(json);
//             if (parsed is not Core.Godot.Collections.Dictionary dict)
//             {
//                 GD.PrintErr("Invalid input configuration file");
//                 return;
//             }
//             
//             foreach (var key in dict.Keys)
//             {
//                 var action = _inputMap.GetAction(key.AsString());
//                 if (action != null && dict[key] is Core.Godot.Collections.Array array)
//                 {
//                     var bindings = new string[array.Count];
//                     for (int i = 0; i < array.Count; i++)
//                     {
//                         bindings[i] = array[i].AsString();
//                     }
//                     action.SetBindings(bindings);
//                 }
//             }
//         }
//         catch (Exception ex)
//         {
//             GD.PrintErr($"Failed to load input configuration: {ex.Message}");
//         }
//     }
//
//     /// <inheritdoc />
//     protected override bool CheckKeyPressed(string keyCode)
//     {
//         // 根据Godot的输入系统检查按键状态
//         return Input.IsPhysicalKeyPressed(GodotKeyMapper.GetKeyFromString(keyCode)) ||
//                Input.IsMouseButtonPressed(GodotKeyMapper.GetMouseButtonFromString(keyCode));
//     }
//
//     /// <inheritdoc />
//     public override void Update(double delta)
//     {
//         UpdateInputStates();
//     }
//
//     protected override void RegisterAssets()
//     {
//         throw new NotImplementedException();
//     }
// }
//
// /// <summary>
// /// Godot按键映射辅助类
// /// </summary>
// public static class GodotKeyMapper
// {
//     private static readonly Dictionary<string, Key> KeyMap = new()
//     {
//         { "Left", Key.Left },
//         { "Right", Key.Right },
//         { "Up", Key.Up },
//         { "Down", Key.Down },
//         { "Space", Key.Space },
//         { "Enter", Key.Enter },
//         { "Escape", Key.Escape },
//         { "A", Key.A },
//         { "B", Key.B },
//         { "C", Key.C },
//         { "D", Key.D },
//         { "E", Key.E },
//         { "F", Key.F },
//         { "G", Key.G },
//         { "H", Key.H },
//         { "I", Key.I },
//         { "J", Key.J },
//         { "K", Key.K },
//         { "L", Key.L },
//         { "M", Key.M },
//         { "N", Key.N },
//         { "O", Key.O },
//         { "P", Key.P },
//         { "Q", Key.Q },
//         { "R", Key.R },
//         { "S", Key.S },
//         { "T", Key.T },
//         { "U", Key.U },
//         { "V", Key.V },
//         { "W", Key.W },
//         { "X", Key.X },
//         { "Y", Key.Y },
//         { "Z", Key.Z },
//         { "0", Key.Key0 },
//         { "1", Key.Key1 },
//         { "2", Key.Key2 },
//         { "3", Key.Key3 },
//         { "4", Key.Key4 },
//         { "5", Key.Key5 },
//         { "6", Key.Key6 },
//         { "7", Key.Key7 },
//         { "8", Key.Key8 },
//         { "9", Key.Key9 }
//     };
//
//     private static readonly Dictionary<string, MouseButton> MouseButtonMap = new()
//     {
//         { "LeftMouse", MouseButton.Left },
//         { "RightMouse", MouseButton.Right },
//         { "MiddleMouse", MouseButton.Middle }
//     };
//
//     public static Key GetKeyFromString(string keyString)
//     {
//         return KeyMap.GetValueOrDefault(keyString, Key.None);
//     }
//
//     public static MouseButton GetMouseButtonFromString(string mouseButtonString)
//     {
//         return MouseButtonMap.GetValueOrDefault(mouseButtonString, MouseButton.None);
//     }
// }