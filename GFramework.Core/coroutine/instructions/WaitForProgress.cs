using GFramework.Core.Abstractions.coroutine;

namespace GFramework.Core.coroutine.instructions;

/// <summary>
/// 带进度回调的等待指令
/// </summary>
public class WaitForProgress : IYieldInstruction
{
    private readonly double _duration;
    private readonly Action<float> _onProgress;
    private double _elapsed;
    private bool _progressCompleted;

    /// <summary>
    /// 初始化等待进度指令
    /// </summary>
    /// <param name="duration">总持续时间（秒）</param>
    /// <param name="onProgress">进度回调，参数为0-1之间的进度值</param>
    public WaitForProgress(double duration, Action<float> onProgress)
    {
        if (duration <= 0) 
            throw new ArgumentException("Duration must be positive", nameof(duration));
        
        _duration = duration;
        _onProgress = onProgress ?? throw new ArgumentNullException(nameof(onProgress));
        _elapsed = 0;
        _progressCompleted = false;
    }

    /// <summary>
    /// 更新方法
    /// </summary>
    /// <param name="deltaTime">时间增量</param>
    public void Update(double deltaTime)
    {
        if (_progressCompleted)
            return;

        _elapsed += deltaTime;
        
        // 计算进度并回调
        if (_elapsed >= _duration)
        {
            _elapsed = _duration;
            _progressCompleted = true;
            _onProgress(1.0f);
        }
        else
        {
            var progress = (float)(_elapsed / _duration);
            _onProgress(progress);
        }
    }

    /// <summary>
    /// 获取等待是否已完成
    /// </summary>
    public bool IsDone => _progressCompleted;
}