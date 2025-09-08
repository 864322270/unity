[animation.md](https://github.com/user-attachments/files/22198038/animation.md)
# AnimationEvent 

参数: 

1. 触发时间 回调函数名字 消息发送选项 
2. 事件参数 类似int float object等
3. AnimationState AnimatorStateInfo AnimatorClipInfo 具体后面会介绍
4. messageOptions 等于0 表示需要接受者 ！=0表示不需要接受者 （什么是需要接受者 通过方法名字寻找函数 如果需要接受者 但是没有找到方法会报错 ）
~~~ csharp 代码
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
3. FireEvent 事件派发总入口

FireEvent：
- 拿到绑定的 GameObject，若未激活（或编辑器预览对象）直接返回 false/不派发。
- 遍历 GameObject 上的所有 MonoBehaviour 组件：通过 functionName 查找方法指针，存在则调用 FireEventTo
- 若未发送成功 构造错误信息并报错
---
# AnimationState 

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
- 在当前步长内按方向（正放/反放/乒乓）遍历事件，命中即通过 FireEvent(...) 分发到 `MonoBehaviour`
- 维护 m_AnimationEventIndex 与 m_AnimationEventState（Search/HasEvent/NotFound/PausedOnEvent），保证下一次触发正确。

~~~ c++ m_AnimationEventState
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

5.UpdateFading 动画权重淡入淡出的核心更新函数，负责处理两种类型的权重过渡 1 自动淡出：当动画到达停止时间时自动开始淡出 2 手动淡入淡出：通过 SetWeightTarget() 设置的权重过渡
~~~ csharp
AnimationState runState = anim["Run"];
runState.weight = 0.0f;
runState.SetWeightTarget(1.0f, 0.5f);  // 0.5秒内淡入到权重1.0
// 开始淡出
runState.SetWeightTarget(0.0f, 1.0f);  // 1秒内淡出到权重0.0
~~~
---
# AnimationClip

参数:
1. 内存相关 m_ClipAllocator  内存分配器，用于管理AnimationClip的内存分配
2. m_AnimationStates AnimationClip的所有AnimationState列表
3. 基本属性 采样率 是否启用压缩  是否使用高质量曲线（编辑器专用） 包装模式（Default/Once/Loop/PingPong/ClampForever） 是否为Legacy动画 
4. 曲线相关 四元数旋转曲线 欧拉角曲线 位置曲线 缩放曲线 浮点数属性曲线 对象引用曲线 m_Events 动画事件列表
5. 编辑器支持 编辑模式备份、轨道管理、高质量曲线
6. Mecanim支持: 肌肉数据、绑定常量、人形动画相关

方法： 
1. FireAnimationEvents  动画事件触发
FireAnimationEvents： 
- 检查播放方向（正向/反向）
- 处理循环播放时的事件触发
- 使用 m_LoopLastTime 标志跳过特定循环事件
- 调用 FireEvent() 实际触发事件回调
  
2. GetRange 获取动画的最小和最大关键帧时间 
GetRange： 调用时机: 需要动画时间边界时（如事件触发、循环计算）
- 遍历所有曲线类型
- 找到最早和最晚的关键帧时间
- 缓存结果到 m_CachedRange

3. SetEvents  SetRuntimeEvents  AddRuntimeEvent 

SetEvents: 编辑器模式下设置动画事件
- 备份事件到 m_EditModeEvents 调用 ClipWasModifiedAndUpdateMuscleRange() 
- 标记对象为脏

SetRuntimeEvents: 运行时批量替换所有动画事件
- 完全替换现有事件列表 自动排序事件
- 清除缓存范围 通知用户系统（通过消息机制）

AddRuntimeEvent: 运行时添加单个动画事件
- 使用二分查找插入到正确位置（保持排序）
- 只添加一个事件，不影响其他事件 通知用户系统
提一下通知用户系统 代码上显示 需要调用AddObjectUser 方法 但是unity并没有提供 通过反射我也没有拿到 如果真的要监听事件变化 建议开一个携程 监听旧事件和新时间的数量 

~~~ c++
namespace AnimationClipBindings
{
    void Internal_AddObjectUser(AnimationClip& self, ScriptingObjectOfType<Object> user);
    void Internal_RemoveObjectUser(AnimationClip& self, ScriptingObjectOfType<Object> user);
}
~~~

~~~ csharp
AnimationUtility.SetAnimationEvents(clip, newEvents); //SetEvents
// 添加单个事件（调用 AddRuntimeEvent）
AnimationEvent[] newEvents = new AnimationEvent[currentEvents.Length + 1];
currentEvents.CopyTo(newEvents, 0);
newEvents[currentEvents.Length] = newEvent;

// 设置所有事件（调用 SetRuntimeEvents）
clip.events = newEvents;
~~~

animationClip和animationState都可以调用fireevent函数 他们有什么区别吗 ： 
1. animationstate的调用顺序： 
   - c# Animation.Play() 
   - C++ Animation::Play()
   - c++ Animation::UpdateAnimation()
   - c++ AnimationState::UpdateAnimationState()
   - c++ AnimationState::FireEvents()

~~~
Animation anim = GetComponent<Animation>();
anim.Play("Walk"); // 这会触发 AnimationState::FireEvents
~~~

2. animationclip的调用顺序： 
   - C# Animator.SetTrigger() 
   - c++ Animator::SetTrigger()
   - c++ Animator::UpdateWithDelta()
   - c++ AnimatorControllerPlayable::PrepareFrame()
   - c++ AnimatorControllerPlayable::UpdateGraph()
   - c++ AnimationClip::FireAnimationEvents()

~~~
Animator animator = GetComponent<Animator>();
animator.SetTrigger("Walk"); // 这会触发 AnimationClip::FireAnimationEvents
~~~
AnimationClip的FireAnimationEvents 方法会循环遍历所有event触发满足条件的 而AnimationState的FireEvents 会记录当前播放的m_AnimationEventIndex 根据播放方向决定接下来播放哪一个

animationClip 在animation组件中 运行时有什么实际作用吗？
1. Animation 组件运行时，AnimationClip是 数据源 在 Animation 组件里，AnimationClip不“播放”，但提供“播放所需的一切数据”（曲线、事件、长度、wrap 等），由 AnimationState 管时间/权重并用它来采样与触发。

还有一些Editor相关的函数 这里就不介绍了

---
# AnimationManager
AnimationManager 是 Legacy Animation 的全局“更新器”，把 Animation::UpdateAnimation 接入 Unity PlayerLoop（FixedUpdate/PreLateUpdate），并根据是否使用固定步长选择对应的队列来驱动。

参数: 
1. m_FixedAnimations m_Animations 固定帧率动画列表 非固定帧率动画列表 （是否加入固定帧率 可以通过勾选animation组件上的 Animate Physics来设置）

方法： 
1. Update 根据是否使用fixedtime 获取动画列表 对每个动画调用 UpdateAnimation
2. InitializeClass 把update注册到 PlayerLoop
---

# Animation 

animation组件是Legacy动画系统 只能播放“标记为 Legacy 的 AnimationClip 
而 Animator 组件属于 Mecanim 系统，播放“非 Legacy 的 AnimationClip 
会在后面讲解 Legacy 和 Mecanim的区别

参数： 
1.  m_WrapMode: 组件级默认 WrapMode（Default/Once/Loop/PingPong/ClampForever）；与 AnimationClip、AnimationState 的 wrap 一起决定最终包裹策略。
2.  m_PlayAutomatically  m_AnimatePhysics 启动时自动播放 是否在（FixedUpdate）下更新 可以通过勾选animation组件上的 Play Automatically Animate Physics 来设置
3.  m_CullingType: 剔除策略（AlwaysAnimate / BasedOnRenderers 等），决定是否在不可见时暂停更新
4.  m_Animation: 默认动画剪辑（Inspector 的 Animation 字段）
5.  m_Queued: 排队的动画（QueuedAnimation 列表），配合 QueueCrossFade/QueuePlay 与 UpdateQueuedAnimations 实现“当前播放完再接某个动画”等逻辑
6.  m_AnimationManagerNode: 加入 AnimationManager 的链表节点，用于统一调度更新
7.  还有些脏标记 层级相关 缓存相关

方法：
1. AddToManager RemoveFromManager 将组件加入到animationManager的链表中
2. Play 播放对应的动画 
3. QueueCrossFade 将克隆的状态排入 m_Queued，由 UpdateQueuedAnimations 在合适时机接管播放
4. Stop()/Stop(name)/Stop(AnimationState&) / Rewind() 停止或回到起始；用于打断与复位
5. UpdateAnimation 一次完整的帧更新，驱动状态机更新、触发事件、处理队列、混合并采样写回
   
   UpdateAnimation: 
   - 同步层时间
   - 更新各 AnimationState 调用 state.UpdateAnimationState(time, *this)
   - 如果 state.ShouldUse()（权重>0、需要参与混合），则 needsUpdate = true
   - 合并并清空 state 的 dirtyMask 到 m_DirtyMask
   - 若 state.ShouldAutoCleanupNow()(淡出结束、自动清理 )删除该状态，标 kLayersDirtyMask，从 m_AnimationStates 擦除并重置活跃缓存大小
   - UpdateQueuedAnimations(needsUpdate)：可能启动新的排队动画并置 needsUpdate = true
   - 恢复刚停的状态 若 stoppedAnimationCount > 0 对收集的每个状态调用 SetupUnstoppedState()，并将 needsUpdate = true
   - 必要时执行采样与混合 若 needsUpdate 为真调用 SampleInternal
   - 清除所有 stoppedAnimationCount的state
   为什么会有stoppedAnimationCount 最简单的理由 不能一面迭代一面删除 需要先做善后再混合 语义上的需要 crossfade需要在最后一帧做一些逻辑 需要在本帧最后才能删除
6. UpdateQueuedAnimations

    UpdateQueuedAnimations： 决定何时启动排队的动画（m_Queued），并以队列项的 fadeTime 作为淡入时长
    - 维护两个“剩余时间”指标 allQueueTime：当前所有正在播放的动画（跨层）到“完成/淡出完”的最大剩余时间 lastLayerQueueTime：目标层内的最大剩余时间
    - 如果是停止所有层 qa.mode == kStopAll（可以参考corssfade 函数第三个参数） startNow = fadeTime >= allQueueTime 解释一下这句话 当前播放状态：Walk 动画还剩 0.5 秒结束 Run 动画还剩 1.0 秒结束 allQueueTime = max(0.5, 1.0) = 1.0 秒  排队动画：- Attack 动画，   fadeTime = 1.5 秒 判断：1.5 >= 1.0 → startNow = true → 立即开始 Attack
    - 否则按层排队 startNow = fadeTime >= lastLayerQueueTime
    - 若应当启动 对目标 state 执行 CrossFade 并从排队动画中移除改动画 设置需要更新标记 重置 lastLayerQueueTime和allQueueTime
7. Sample() / SampleInternal()
   Sample 仅做轻量检查，若有任何需要参与混合的状态，则调用 SampleInternal()。常用于外部强制采样（如编辑器或手动刷新）。
   SampleInternal: 真正执行采样与混合的核心流程：处理脏标记、重建/排序、选择混合路径、写回变换/属性并发送变更通知。
   Sample 遍历 m_AnimationStates，若存在 state.ShouldUse()（权重>0、启用等）则置 needsUpdate = true 合并并清空各 state 的 dirtyMask 到 m_DirtyMask 若需要，调用 SampleInternal
   SampleInternal 若有脏标记 RebuildStateForEverything SortAnimationStates 最后 SendTransformChangedToCachedTransform() 通知 Transform 变更 实际控制transform 就在这里 （SendTransformChangedToCachedTransform） 而材质球组建属性变更则会在RebuildStateForEverything 函数进行更新 SampleInternal() 中完成混合后，通过绑定的写入器把值写回到对应对象/属性。Transform 以外的属性不会调用 QueueChanges()，而是各自的 setter 在内部标记脏并通知渲染管线
   何时调用 ： UpdateAnimation 内，当本帧确实有需要混合的状态或状态变更时调用 外部也可直接调 Sample() 触发一次混合采样（如编辑器拖动时间线、程序化刷新）

层是什么： `这里的层指的是animation的层 而不是animator的层`
 
层（layer）在 Legacy Animation 中是给每个 AnimationState 分组的整数标签，用来
1. 决定播放/交叉淡入时的作用范围（例如 kStopSameLayer 只影响同一层的状态，kStopAll 影响所有层）
2. 影响队列启动判断（UpdateQueuedAnimations 会按“全部层”或“指定层”的剩余时间来决定何时启动）
3. 参与混合顺序（通常高层可以覆盖/叠加低层，配合 MixingTransform 可只影响部分骨骼）

~~~ csharp
public Animation anim;          // 挂在含有 Animation 组件的对象
public AnimationClip walk;      // 全身走路
public AnimationClip shoot;     // 上半身开枪（或挥手等）
public Transform upperBodyRoot; // 上半身的根骨骼（如 Spine 或 Chest）
void Start()
{
    // 准备状态
    anim.AddClip(walk, "Walk");
    anim.AddClip(shoot, "Shoot");
    var walkState  = anim["Walk"];
    var shootState = anim["Shoot"];
    // 设置层：走路在层0；开枪在层1
    walkState.layer  = 0;
    shootState.layer = 1;
    // 混合遮罩：让“Shoot”只影响上半身（避免覆盖腿部走路）
    shootState.AddMixingTransform(upperBodyRoot, true);
    // 循环 & 权重
    walkState.wrapMode  = WrapMode.Loop;
    shootState.wrapMode = WrapMode.Default;
    // 先播放走路（层0）
    anim.Play("Walk");
    // 2秒后触发层1的开枪，只停止同层（层1）内其它状态；不会停止层0的走路
    Invoke(nameof(PlayUpperBodyShoot), 2f);
}
void PlayUpperBodyShoot()
{
    // 这里使用 StopSameLayer，表示只影响 layer == 1 的状态
    anim.CrossFade("Shoot", 0.25f, PlayMode.StopSameLayer);
    // 再演示：将另一个同为层1的状态排队，只会等待层1上正在播放的状态完成
    // var anotherUpperBody = anim["AnotherUpperBody"];
    // anotherUpperBody.layer = 1;
    // anim.QueueCrossFade("AnotherUpperBody", 0.25f, (int)Animation.QueueMode.CompleteOthers, PlayMode.StopSameLayer);
}
~~~
区分于 Animator 的“层”：这里是 Legacy 每个 AnimationState 的 int layer，不是 Animator Controller 的“Layer”面板。Legacy 的层只能通过脚本设置与使用。
