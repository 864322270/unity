#animationevent

参数: 

1. 触发时间 回调函数名字 消息发送选项 
2. 时间参数 类似int float objec等
3. AnimationState AnimatorStateInfo AnimatorClipInfo 具体后面会介绍
4. messageOptions 等于0 表示需要接受者 ！=0表示不需要接受者 （什么是需要接受者 通过方法名字寻找函数 如果需要接受者 但是没有找到方法会报错 ）
~~~ 代码
    var e = new AnimationEvent();
    e.time = 0.5f;
    e.functionName = "OnFootstep";
    e.stringParameter = "left";
    // 需要接收者：messageOptions==0（默认就是0）
    e.messageOptions = 0; // RequireReceiver
~~~ 
方法：
1. SetupInvokeArgument 检测参数设置是否正确 
2. FireEventTo 具体 MonoBehaviour 发送事件
    
FireEventTo: 
- 检查脚本实例与方法有效。
- 在 event 上填充上下文指针stateSender/animatorStateInfo/animatorClipInfo 都是上面传下来的
- 组装 ScriptingInvocation，调用 SetupInvokeArgument 打包参数。
- 置“禁止立即销毁”的执行限制，调用脚本方法（允许协程返回值处理）
- 恢复执行限制，清空 event 上的上下文指针
- 返回：true 表示本组件处理成功
1. FireEvent 事件派发总入口

FireEvent：
- 拿到绑定的 GameObject，若未激活（或编辑器预览对象）直接返回 false/不派发。
- 遍历 GameObject 上的所有 MonoBehaviour 组件：通过 functionName 查找方法指针，存在则调用 FireEventTo
- 若未发送成功 构造错误信息并报错

#AnimationState 

Legacy Animation 组件里，每个正在被播放（或可播放）的 AnimationClip 对应一个 AnimationState，记录时间、速度、权重、包裹模式、事件索引等运行时信息，并负责淡入淡出、事件触发、混合掩码等逻辑。
animation state是什么时候初始化的： 
1.  组件创建时  场景加载时（kDidLoadFromDisk） 代码创建对象时（kInstantiateOrCreateFromCodeAwakeFromLoad）对象激活时（kActivateAwakeFromLoad）
2.  添加动画剪辑时（AddClip）
3.  播放动画时（Play/CrossFade）
4.  总结 GameObject创建/激活 Animation组件AwakeFromLoad 检查m_PlayAutomatically自动播放（animation会讲） 如果需要播放，调用Play() Play()检查是否需要重建状态 调用RebuildInternalState()  为每个AnimationClip创建AnimationState 调用AnimationState::Init()初始化 添加到AnimationManager 开始播放动画
5.  AnimationState 不是一开始就创建，而是在需要时才创建  每个 AnimationClip 对应一个 AnimationState  所有 AnimationState 由 AnimationManager 统一管理 只有在真正需要播放动画时才会创建状态对象


参数:
1. 时间相关 m_Time m_WrappedTime m_LastGlobalTime
2. 变化相关 m_Weight/m_WeightTarget/m_WeightDelta/m_FadeBlend/m_IsFadingOut/m_StopWhenFadedOut 当前权重、目标权重、每秒变化量、是否淡入淡出中、是否正在淡出、到达目标后是否停止
   C#通过 Animation.CrossFade()、Animation.Blend() 等方法间接控制 需要自定义控制时，使用协程或手动检查权重变化
3. m_Layer/m_BlendMode(kBlend/kAdditive) 状态所在层、混合模式（普通或叠加）
4. m_WrapMode/m_CachedRange(first,bestEnd) 包裹模式（Default/Once/Loop/PingPong/ClampForever）；缓存的时间范围（通常是剪辑时长）
5. m_AnimationEventState/m_AnimationEventIndex/m_HasAnimationEvent 事件索引状态机（HasEvent/Search/NotFound/PausedOnEvent）、当前事件索引、是否存在事件
6. m_Name/m_ParentName 状态名与父名
   
方法： 
1. GetLength 根据曲线的设置返回长度并缓存 
2. UpdateAnimationState 更新一帧并触发事件 UpdateAnimationState(double globalTime, Unity::Component& animationComponent)

UpdateAnimationState：
- 根据 globalTime 与 m_LastGlobalTime 推进时间
- 根据m_WrapMode 如有事件、且跨越了事件时间点，调用 FireEvents(...) 执行
- 处理淡入淡出

3. FireEvents FireEvents(const float deltaTime, float newWrappedTime, bool forward, Unity::Component& animation, const float beginTime, const float offsetTime, const bool reverseOffsetTime)
负责在指定时间范围内触发动画事件，处理事件索引状态机。

FireEvents：
- 在当前步长内按方向（正放/反放/乒乓）遍历事件，命中即通过 FireEvent(...) 分发到 MonoBehaviour
- 维护 m_AnimationEventIndex 与 m_AnimationEventState（Search/HasEvent/NotFound/PausedOnEvent），保证下一次触发正确。

~~~ m_AnimationEventState
enum
{
    kAnimationEventState_HasEvent = 0,      // 有有效事件
    kAnimationEventState_Search,            // 需要搜索事件
    kAnimationEventState_NotFound,          // 没有找到事件
    kAnimationEventState_PausedOnEvent,     // 在事件上暂停
};
~~~ 
4. SetupUnstoppedState() 和 CleanupUnstoppedState() 这两个函数解决了一个特殊的动画停止问题：
   当动画播放到最后一帧并停止时，如果直接停止，会导致：
   - 权重变为0  最后一帧不会对最终结果产生影响
   - 时间被重置 - 最后一帧的姿势信息丢失
   - 视觉跳跃 - 物体突然"跳回"到初始位置
   - 将"逻辑停止"和"视觉停止"分开 确保最后一帧能够被正确采样  使用极小权重而不是0，避免完全消失 采样完成后恢复真实状态
~~~
// 假设有一个电梯上升的动画
// 动画长度：5秒
// 最后一帧：电梯在顶层位置

// 没有SetupUnstoppedState的情况：
// 5秒后动画停止 → 权重变为0 → 电梯突然跳回底层

// 有SetupUnstoppedState的情况：
// 5秒后动画停止 → 临时保持最后一帧状态 → 采样系统应用最后一帧 → 电梯保持在顶层
~~~

5.UpdateFading 动画权重淡入淡出的核心更新函数，负责处理两种类型的权重过渡 1自动淡出：当动画到达停止时间时自动开始淡出 2 手动淡入淡出：通过 SetWeightTarget() 设置的权重过渡
~~~
AnimationState runState = anim["Run"];
runState.weight = 0.0f;
runState.SetWeightTarget(1.0f, 0.5f);  // 0.5秒内淡入到权重1.0
// 开始淡出
runState.SetWeightTarget(0.0f, 1.0f);  // 1秒内淡出到权重0.0
~~~
