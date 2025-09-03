//animationEvent
//属性 time 时间
//     functionName 方法名字
//     stringParameter floatParameter intParameter objectReferenceParameter 方法需要参数
//     mutable AnimationState* stateSender;mutable AnimatorStateInfo* animatorStateInfo;mutable AnimatorClipInfo* animatorClipInfo; 事件相关参数
//方法 重写==和 >逻辑
//方法 FireEventTo static bool FireEventTo(MonoBehaviour& behaviour, AnimationEvent& event, AnimationState* state, AnimatorStateInfo* animatorStateInfo, AnimatorClipInfo* animatorClipInfo, ScriptingMethodPtr method)
//     触发事件 1.检测物体是否为空 2.设置事件状态 3.检测参数数据是否正确  SetupInvokeArgument函数 4. 接收事件返回值 并确认是否打印日志 5.事件状态设置成空 6.返回true 
//方法 FireEvent bool FireEvent(AnimationEvent& event, Unity::Component& animation, AnimationState* state, AnimatorStateInfo* animatorStateInfo, AnimatorClipInfo* animatorClipInfo)
//     触发事件 1.检测物体激活状态 2.获取物体上所有组件继承MonoBehaviour 3.获取要触发的函数指针 ScriptingMethodPtr methodPtr = Scripting::FindMethodCached(behaviour.GetClass(), event.functionName.c_str());
//     4.if (!methodPtr.IsNull() && FireEventTo(behaviour, event, state, animatorStateInfo, animatorClipInfo, methodPtr)) sent = true;
//     5.如果事件发送成功返回true 否则输出警告 时间是否为空 和物体名字 6 返回true
//方法 SetupInvokeArgument static bool SetupInvokeArgument(ScriptingMethodPtr method, AnimationEvent& event, ScriptingArguments& parameters)
//     1.看看这个方法是否需要参数 2不需要参数直接返回true 3.大于一个参数返回false 4.获取第一个参数 和第一个参数类型 ScriptingTypePtr typeOfFirstArgument = scripting_method_get_nth_argumenttype(method, 0); ScriptingClassPtr classOfFirstArgument = scripting_class_from_type(typeOfFirstArgument);
//     5.如果是float int string 添加到参数列表中 返回true 6.如果参数是委托 转化成 MonoAnimationEvent 返回true 7.如果参数是 unityobject 或者是子类 转化成object指针 如果不是空 添加到参数列表返回空 如果是继承monobehaviour的 转化成MonoBehaviour指针 
//     判断是否是空 判断这个类是否继承参数所需要的类 是添加进去 返回true 如果又不是继承monobehaviour的又不是继承unityobject 转换成c++脚本指针 判断是否继承自参数的类 是返回空 都不是返回false


//animationState
//属性 m_Weight 混合权重（0-1） m_Time 真实时间 m_LastGlobalTime 上一帧的全局时间 m_WrappedTime 根据WrapMode函数处理后的时间 
//     m_Curves 曲线可以在unity中设置 
//     m_Speed 速度 m_StopTime停止时间 m_Layer 层级 (作用：把基础循环动作放低层（layer 0），把临时/覆盖性动作（如表情、手势、上身动作）放更高层，实现并行与覆盖。 不同 layer 的状态可以并行播放，同一 layer 的状态互斥 混合顺序与权重累积)
//     m_FadeBlend m_Enabled m_StopWhenFadedOut m_AutoCleanup m_OwnsCurves m_IsFadingOut m_ShouldCleanup m_HasAnimationEvent m_IsClone m_AnimationEventState 一些状态记录
//     m_AnimationEventState 枚举 状态有 kAnimationEventState_HasEvent 有有效事件 kAnimationEventState_Search 在寻找事件 kAnimationEventState_NotFound 没有有效事件 kAnimationEventState_PausedOnEvent 事件已经触发并暂停 后面用状态代指这个参数
//     m_AnimationEventIndex 事件索引 m_WrapMode enum { Default = 0, Once = 1, Loop = 2, PingPong = 4, ClampForever = 8 } unity animation中WrapMode参数 可以在unity面板中设置 m_BlendMode; ///< enum { Blend = 0, Additive = 1 }
//方法 FireEvents bool AnimationState::FireEvents(const float deltaTime, float newWrappedTime, bool forward, Unity::Component& animation, const float beginTime, const float offsetTime, const bool reverseOffsetTime)
//     修改播放方向从新计算m_time
//     1.获取所有事件 2.如果当前状态等于kAnimationEventState_Search 设置当前时间 根据动画播放方向 前就找最小的大于当前时间的事件 反之亦然 设置m_AnimationEventIndex 并且把状态设置为kAnimationEventState_HasEvent 如果没找到就把状态设置成kAnimationEventState_NotFound
//     3.更新速度和时间 4.循环判断 4.1如果m_AnimationEventIndex <0或者超出最大 并且时间不符合需求 推出 4.2 执行animationEvent 中FireEvent函数 
//     4.3 如果当前状态等于kAnimationEventState_Search 更新旧的方向和新的播放方向 如果方向不同 并且时间一致 判断播放速度是否等于0 说明暂停 m_AnimationEventState改为 kAnimationEventState_PausedOnEvent 速度不等于0 根据方向更改m_AnimationEventIndex 并且m_AnimationEventState = kAnimationEventState_HasEvent;
//     4.4 根据方向更新 m_AnimationEventIndex 5.返回true 
//方法 SetSpeed void AnimationState::SetSpeed(float speed)
//     1.如果状态等于kAnimationEventState_PausedOnEvent 判断方向并且不等于0则 1.1判断m_WrapMode == kWrapModePingPong 一堆计算逻辑 1.2 更新状态为kAnimationEventState_HasEvent
//     2.状态不等于kAnimationEventState_PausedOnEvent 如果新方向和老方向不相等 把状态改成 kAnimationEventState_Search 3.调用 SetupStopTime函数
//方法 UpdateAnimationState bool AnimationState::UpdateAnimationState(double globalTime, Unity::Component& animationComponent)
//     1.更新时间并判断动画速度 2.速度不为零的情况下根据m_WrapMode的模式设置各种事件在满足条件的情况下 FireEvents 3.调用并返回UpdateFading函数
//方法 UpdateFading bool AnimationState::UpdateFading(float deltaTime) 自动触发基于时间的淡出 处理常规权重渐变


//animationClip
//属性 m_WrapMode 同上面 AnimationEvents  m_Events事件  m_AnimationStates 状态列表 m_MuscleClip 核心动画具体看后面
//以下属性是只读属性 m_QuaternionCurves m_EulerCurves m_PositionCurves m_ScaleCurves m_PPtrCurves   这些属性可以点击animationclip 在unity Inspector中查看 其中没有说明的属性在m_MuscleClip中
//从前到后面一次是 有x条使用四元数格式存储的旋转曲线 有x条使用欧拉角格式存储的旋转曲线 有x条位移曲线 有x条缩放曲线  有x条对象引用曲线
//编辑器下属性 m_EditorCurves m_EulerEditorCurves m_EditModeEvents 保留一份编辑模式下的事件设置副本 以便我们能在播放模式后安全地恢复事件设置
//方法 GetRange std::pair<float, float> AnimationClip::GetRange() 返回animationclip长度
//     1.默认设置从-无穷到+无穷 2 如果存在缓存 返回缓存 3遍历所有曲线获取时间范围 4 考虑动画事件的时间 5 缓存结果以提高性能
//方法 FireAnimationEvents void FireAnimationEvents(AnimationClipEventInfo* info, Unity::Component& source)
//     1.获取动画事件列表 获取时间相关数据 没有变化直接返回 2 正常播放 循环处理 将时间时间转换到实际时间轴 在时间内触发事件 3 如果是反向播放 就倒着执行第二部
//方法 SetEvents void AnimationClip::SetEvents(const AnimationEvent* events, int size, bool sort)//最好一次性添加多个事件 
//     1.添加到新的events中并排序 2.同步编辑器事件到m_EditModeEvents 3.通知动画系统已经修改 4setdirty
//方法 SetRuntimeEvents void AnimationClip::SetRuntimeEvents(const AnimationEvents& events)
//     1.获取老的动画长度 2.根据时间排序 3.清除缓存从新计算动画长度 4.如果长度变化较大触发动画系统重建 5.变化不大只通知事件更新
//方法 AddRuntimeEvent void AnimationClip::AddRuntimeEvent(AnimationEvent& event)     //这是Unity动画系统中用于在运行时动态添加动画事件的核心函数
//     1.获取老的动画长度 2.根据时间长度判断插入位置 3.清除缓存从新计算动画长度 4.如果长度变化较大触发动画系统重建 5.变化不大只通知事件更新
//对比 SetRuntimeEvents 和SetEvents  SetRuntimeEvents 不适合在编辑器状态下使用 在代码中有编辑器检查 在1.用户在Animation窗口中添加/删除/修改事件 2.通过Inspector面板修改动画事件 3.导入动画资源时设置事件 4.脚本修改 5.资源导入和序列化 会触发SetEvents 3setevents会同步编辑器数据
//方法 InitClipMuscleAdditivePose void InitClipMuscleAdditivePose(mecanim::animation::ClipMuscleConstant& constant, mecanim::animation::ClipMuscleConstant& additiveConstant, UnityEngine::Animation::AnimationSetBindings* clipsBindings, float time, RuntimeBaseAllocator& allocator)
//     看不太懂 猜测处理加载动画的？
//     还有一些load set get clear 编辑器 等函数 都是一些设置基础参数 曲线 欧拉角等就不多叙述了


//Animation
//属性 枚举 CullingType kCulling_AlwaysAnimate kCulling_BasedOnRenderers 总是更新动画 仅在有可见渲染器时更新
//     枚举 PlayMode kStopSameLayer kStopAll 播放新动画时停止同层其他动画 播放新动画时停止所有层的动画
//     枚举 QueueMode CompleteOthers PlayNow 等其他动画完成后再播放 立即打断并播放
//     m_WrapMode 包裹模式（Default/Once/Loop/PingPong/ClampForever） m_PlayAutomatically: 是否在激活时自动播放默认动画 m_AnimatePhysics: 是否在FixedUpdate（物理步）更新 m_CullingType: 剔除策略（见上） m_DirtyMask: 脏标记位掩码
//     m_AnimationManagerNode: 用于加入AnimationManager链表的节点 m_Animation: 默认（主）AnimationClip m_Animations: 挂在组件上的Clip列表
//方法 IsPlaying() IsPlaying(name)  IsPlaying(const core::string& clip) IsPlayingLayer(int layer) 判断是否在播放
//方法 Stop() Stop(const core::string& name) Stop(AnimationState& state) 停止所有动画 停止指定名字的（如果是克隆的判断父亲的名字 ）
//方法 Rewind() Rewind(const core::string& name) Rewind(AnimationState& state) 将动画时间回到起点，不改变播放/权重等其它状态。
//方法 Play(const core::string& name, PlayMode playMode) Play(AnimationState& fadeIn, PlayMode playMode) Play(PlayMode mode)
//    1.如果playMode == kPlayQueuedDeprecated 需要排队 调用QueueCrossFade 否则调用CrossFade 不传名字就播放默认
//方法 Blend() Blend(const core::string& name, float targetWeight, float time)//把指定状态“混到”目标权重，不强制别人降权。常用于多状态叠加。
//方法 CrossFade() CrossFade(AnimationState& playState, float time, PlayMode mode, bool clearQueuedAnimations) 对“同层”做交叉淡入淡出：目标状态淡入到权重1，其他同层状态淡出到0（或立即Stop）
//方法 UpdateAnimation UpdateAnimation(double time)
//     1.对齐每层的时间 2.为“刚停”的状态准备临时缓存 ALLOC_TEMP_AUTO(stoppedAnimations, m_AnimationStates.size()); 申请一块临时数组，用来记录本帧从“播放态→停止态”的状态指针，后续做善后。
//     3.逐个 AnimationState 更新 如果是激活状态 调用上面的UpdateAnimationState 若返回 true 表示“刚停止”（到达末尾或被停止）“刚停止”且不需要立刻清理的，先记录到 stoppedAnimations[] stoppedAnimations[stoppedAnimationCount++] = &state;
//     4.如果该状态本帧会对结果有贡献（权重/遮罩等决定），置 needsUpdate=true，意味着稍后需要混合采样。
//     5.更新m_DirtyMask 6.如果 state.ShouldAutoCleanupNow() 已播放完且应当自动销毁 m_AnimationStates 列表移除 m_AnimationStates.erase(m_AnimationStates.begin() + i); 否则继续循环
//     7.结束循环
//     8.处理已排队的动画 UpdateQueuedAnimations(needsUpdate)：若有队列且时机到，会把队列里下一个克隆状态推进到播放；若有变化也会把 needsUpdate 置为 true。
//     9.若 stoppedAnimationCount>0 对每个记录的状态调用 SetupUnstoppedState()，一般用于在采样前把“停止”转成一个可在本帧继续被正确混合的稳定形态 并把 needsUpdate=true，确保稍后会执行采样混合
//     10.SampleInternal() 内部会重建活跃状态集合、做权重归一、执行 BlendOptimized/Generic/Additive 路径，并应用曲线到目标对象，同时在其中触发 AnimationEvent
//     11.对“刚停止”的那些状态再调用 CleanupUnstoppedState()
//方法 UpdateQueuedAnimations UpdateQueuedAnimations(bool& needsUpdate) 这个函数会启动已经到达开始时机的“排队动画 启动时一律使用队列项里的 fadeTime 作为淡入时长。也就是说，即便当前正在播放的动画会更早结束，我们仍然按 fadeTime 去把新动画淡入。选择多长的淡入时长由用户自己控制。
//     1.遍历所有等待的动画 如果qa.mode == kStopAll(停止所有动画 当播放新动画时会停止所有层的动画) if (allQueueTime < 0) allQueueTime存储所有动画的剩余播放时间 负值表示尚未计算或需要重新计算 调用GetQueueTimes 具体参考下面
//     2.如果不等于就比较当前动画层或者lastLayerQueueTime < 0 在调用GetQueueTimes函数
//     3.startNow = fadeTime >= lastLayerQueueTime 解释下这句话 fadeTime：排队动画的淡入时长（从 QueuedAnimation.fadeTime 获取） allQueueTime：所有播放中动画的最大剩余时长（通过 GetQueueTimes 计算得出）
//     当前播放状态：Walk 动画还剩 0.5 秒结束 Run 动画还剩 1.0 秒结束 allQueueTime = max(0.5, 1.0) = 1.0 秒  排队动画：- Attack 动画，fadeTime = 1.5 秒 判断：1.5 >= 1.0 → startNow = true → 立即开始 Attack
//     fadetime是什么 这个参数控制动画切换的平滑程度，值越大切换越平滑，值越小切换越快速 可以通过unity脚本设置 CrossFade(name, fadeTime) Blend(name, targetWeight, fadeTime) QueueCrossFade(name, fadeTime, queueMode, playMode)
//     4.如果startNow CrossFade这个动画 从等待队列中移除这个动画 重置 allQueueTime 和 lastLayerQueueTime 5.否则处理下一个动画
//方法 GetQueueTimes GetQueueTimes(const Animation::AnimationStates& states, const int targetLayer, float& allQueueTime, float& layerQueueTime) allQueueTime 用于返回所有动画的剩余时间 layerQueueTime返回指定层的剩余时间
//     1.遍历所有animationstate 并处理所有启动的动画 获取层级 如果state的wrapmode != kWrapModeDefault && wrapMode != kWrapModeClamp 那就是循环 重复播放的 allQueueTime等于无穷 如果由当前层级的一并设置为无穷
//     2. 等于kWrapModeDefault kWrapModeClamp的计算时间（动画长度-当前时间） allQueueTime等所有的中最大的 由当前层级 更新layerQueueTime 计算逻辑同 allQueueTime
//方法 SampleInternal SampleInternal()负责将动画数据应用到目标对象上
//     1.验证绑定曲线 曲线相关我看不太懂就不解释了 
//     2.有脏标记 重建所有状态（添加/删除动画时） 重新排序状态（层变化时）清楚脏标记 
//     3.重建绑定状态掩码，确定哪些状态影响哪些曲线  通用混合路径 加法混合 在基础动画之上叠加额外效果 通知 Unity 的变换系统有对象发生了变化
//方法 BlendAdditive() BlendOptimized() BlendGeneric() 这三个函数都是混合动画的把大概 我也看不太懂 
//     剩下一些看名字就懂得函数  1.物理和剔除相关 SetAnimatePhysics GetAnimatePhysics SetCullingType  GetCullingType
//     2.生命周期管理 AddToManager RemoveFromManager AwakeFromLoad
//     3.剪辑管理 AddClip RemoveClip GetClipCount
//CorssFade Blend QueueCrossFade 这三个函数有什么区别 
//     1.CrossFade 交叉淡入淡出，实现动画的平滑切换 同一层只保留一个主导动画 调用后立即开始切换  主动停止或淡出其他动画 默认清除同层的排队动画
//     2.Blend 权重混合，在现有动画基础上叠加效果 多个动画可以同时播放 不停止其他动画调用后立即开始权重变化 不涉及排队机制
//     3.QueueCrossFade 排队执行交叉淡入淡出 不立即执行，而是排队等待 根据当前动画状态决定何时开始 支持动画序列的编排 
 
//AnimationManager
//属性 m_Animations m_FixedAnimations 都是动画列表 根据m_AnimatePhysics 决定加入那个
//方法 InitializeClass InitializeClass()
//     1.创建全局 gAnimationManager，注册到 PlayerLoop 两个阶段调用 Update。
//     2.注册 FixedUpdate 阶段的 LegacyFixedAnimationUpdate 与 PreLateUpdate 阶段的 LegacyAnimationUpdate
//     3.如果是编辑器模式下 注册一个Domain Reload回调函数在Unity编辑器进行域重载（Domain Reload）之前执行。
//方法 BeforeDomainReload BeforeDomainReload() 重载前清理所有动画对象的脚本引用，防止悬挂引用导致的问题
//     1.对清理所有动画对象的脚本引用 （原因）在Unity编辑器中，当脚本重新编译时：旧的应用域被销毁，所有托管对象（C#对象）变成无效 C++对象仍然存在，但它们持有的C#对象引用变成悬挂指针 如果不清理这些引用，当C++代码尝试访问这些引用时会导致崩溃
//方法 Update Update（）动画系统的核心更新循环 负责驱动所有已注册的动画对象进行更新。
//     1.获取当前帧的时间戳 2.使用fixtime 就遍历m_FixedAnimations 反之则m_Animations 调用UpdateAnimation （注释补充）在 UpdateAnimation 过程中，可能会触发 AnimationEvent这些事件可能会销毁动画对象导致链表节点被删除 SafeIterator 确保在遍历过程中即使节点被删除也不会崩溃


//animator
//属性 m_IsInitialized 是否初始化完成 m_Visible是否有相关渲染器可见 m_CullingMode: 剔除模式（不剔除/限制写回/完全剔除） m_UpdateMode: 更新时间源（Normal/AnimatePhysics/UnscaledTime）m_AnimatorActivePasses: 当前处于哪些内部评估阶段的位标记
//     m_Controller: 当前RuntimeAnimatorController m_ControllerPlayable: 控制器的Playable包装 m_ControllerPlayableCache/m_ControllerPlayableCacheLayerCount: 控制器Playable缓存及其层数 
//     m_ApplyRootMotion: 是否应用根运动 m_Speed: 全局播放速度  m_LogWarnings: 是否记录警告 m_FireEvents: 是否触发动画事件 m_HasAnimationEvents 是否存在动画事件 m_KeepAnimatorControllerStateOnDisable: 禁用时是否保留控制器状态 m_HasTransformHierarchy: 是否存在可遍历的Transform层级（优化相关）
//     m_AvatarPlayback: Avatar回放控制结构 m_RecorderMode: 录制模式（离线/回放/录制）m_PlaybackDeltaTime: 回放步长 m_PlaybackTime: 回放时间（查询）
//方法 SetRuntimeAnimatorController SetRuntimeAnimatorController(RuntimeAnimatorController* controller)
//     1.如果是同一个直接返回 2.获取新的和旧的控制器并比较 如果playable为空或者 avatar还没初始化 fullRebind等于true（需要重绑定）3.更新控制器
//     4.如果需要重新绑定 清掉之前备份的“默认属性值” 做一次完整 Rebind 5.不需要重新绑定 把控制器默认值写回（不打脏）清空旧控制器的依赖用户关系 把当前 Animator 作为 AOC 的“用户”注册 依赖跟踪 资源变更可通知 把AOC应用到现有控制器Playable 替换片段映射
//     5.setdirty
//方法 Rebind Rebind(bool writeDefaultValues /* = true */)
//     1.加一段性能采样，便于 Profiler 里看到 Rebind 的耗时 2.只有在允许写默认值时执行下面两步；注释提示：从资源加载唤醒时一般不写默认值，避免覆盖场景里已存在的数值  WriteDefaultValuesNoDirty 清空当前控制器的 Playable 实例
//     3.CreateObject 重新创建 Animator 的运行时对象与依赖 收集该 Animator 影响的渲染器并注册可见性回调；据此决定剔除策略



//ClipMuscleConstant
 


//unity是如何触发动画事件的？ 实行顺序是什么
//     1.PlayerLoop 入口（FixedUpdate）在 AnimationManager.InitializeClass 中注册： FixedUpdate: LegacyFixedAnimationUpdate → GetAnimationManager().Update() PreLateUpdate: LegacyAnimationUpdate → GetAnimationManager().Update()
//     2.AnimationManager.Update 若设置animation的Play Automatically 遍历 m_FixedAnimations 否则遍历 m_Animations 对每个 Animation 组件调用 animation.UpdateAnimation(time)
//     3.Animation.UpdateAnimation(time)（Legacy Animation） 同步层时间 SyncLayerTime 推进各 AnimationState：state.UpdateAnimationState(time, this) 处理排队 UpdateQueuedAnimations 若需要更新则 SampleInternal()
//     4.Animation.SampleInternal 重建活跃状态集，做混合 对每个活跃 AnimationState计算该状态上次时间 lastTime 与当前时间 now 如果该状态绑定的 AnimationClip 上有事件，则调用：clip.FireAnimationEvents(info, sourceComponent)并填充数据
//     5.AnimationClip.FireAnimationEvents遍历 clip 的事件列表 根据 lastTime → now 的区间与是否跨圈/反向，决定哪些事件触发 将命中的事件通过消息派发到 sourceComponent（通常是 Animation/Animator 所在的 GameObject 脚本），以 functionName 和参数调用对应方法
//     总结FixedUpdate → AnimationManager.Update → Animation.UpdateAnimation → SampleInternal → AnimationClip.FireAnimationEvents（Legacy）

//animation animationstate animationclip 都含有m_WrapMode 具体执行是根据那个来确定呢 
//     具体逻辑在animation代码中 如果animationclip和animation不相等 优先使用animationclip的 其次使用animation的
//     如果在运行中修改 那么animationstate的修改优先级最高 下一帧立刻执行 修改animation的会更新所有的animationstate 但是如果和animationclip不一样的话 会使用animationclip的 修改animationclip的 会影响当前和未来的 如果不是重新构建animationstate不会生效

//animation Unity面板上参数都是干什么的 
//      animation 当调用play时 如果没有传入name 会播放的动画 
//      animations 存储所有可播放的动画剪辑 每个animationclip在运行时对应一个 AnimationState
//      Play Automatically 当 GameObject 激活时，是否自动播放默认动画
//      Animate Physics 决定动画在哪个更新循环中执行 勾选在 FixedUpdate 中更新（物理步）否则在 PreLateUpdate 中更新（普通帧）
//      Culling Type Always Animate：无论是否可见都更新动画 Based On Renderers：只有当渲染器可见时才更新动画
