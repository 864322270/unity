# Animator

---
# AnimatorStateTransition
AnimatorStateTransition 是 Mecanim 状态机中连接两个状态（或状态到子状态机）的过渡对象，定义何时、如何从一个状态切换到另一个状态。<br>
Mecanim会在后面解释是什么 <br>
在unity中 打开animator面板 可以看到Entry到new Animation之间有一个箭头 这个箭头就是AnimatorStateTransition<br>
AnimatorStateTransition 只在编辑器中存在，用于创建和编辑过渡 数据在构建时被编译到 StateMachineConstant 中 过 EvaluateStateMachine 函数使用编译后的数据<br>
属性： 
1. vector<AnimatorCondition> m_Conditions  过渡条件列表，每个条件包含 m_ConditionMode：条件类型（If/IfNot/Greater/Less/Equals/NotEqual）m_ConditionEvent：参数名 m_EventTreshold：阈值
2. m_DstState/m_DstStateMachine：目标状态或子状态机
3. m_Solo/m_Mute：调试用的独奏/静音标志 m_IsExit：是否为退出过渡

解释一下这两个参数：<br>
m_Mute 临时禁用某个过渡，让它不会被执行 即使过渡条件满足，也不会触发状态切换<br>
m_Solo 只启用当前过渡，禁用同一源状态的其他所有过渡 当启用 Solo 时，只有这个过渡会被考虑，其他过渡被临时忽略 <br>
这几个参数都可以在animator设置
~~~ csharp
// 调试示例：测试从 Idle 到 Walk 的过渡
var idleToWalkTransition = GetTransition("Idle", "Walk");
// 静音其他可能干扰的过渡
idleToWalkTransition.SetMute(false);  // 启用目标过渡
otherTransitions.SetMute(true);       // 静音其他过渡
// 或者使用 Solo 模式
idleToWalkTransition.SetSolo(true);   // 只考虑这个过渡
~~~
4. m_TransitionDuration m_TransitionOffset m_ExitTime m_CanTransitionToSelf 过渡持续时间（默认0.1秒） 过渡偏移（默认0.0）退出时间点（默认0.9，即动画90%时）是否允许过渡到自身
5. m_HasExitTime m_HasFixedDuration m_InterruptionSource m_OrderedInterruption  是否启用退出时间条件 是否使用固定持续时间 中断源（None/Current/Next/CurrentThenNext/NextThenCurrent）是否有序中断<br>
实际上这些参数都可以在animator中设置<br>
![alt text](image-1.png)<br>
可以参考官方文档的解释 如果你不愿意看可以看我的<br>
~~~ csharp
var transition = animatorController.layers[0].stateMachine.defaultState.transitions[0];
transition.duration = 0.5f;  // 0.5秒的混合时间
// 快速切换 vs 平滑过渡
transition.duration = 0.0f;  // 立即切换
transition.duration = 1.0f;  // 1秒平滑过渡
transition.offset = 0.2f;  // 设置目标动画的起始偏移 从目标动画的20%处开始播放
transition.exitTime = 0.8f;  // 动画播放到80%时才能过渡
transition.hasExitTime = false;// 立即过渡（不等待动画完成）
transition.hasFixedDuration = true;// 固定持续时间（不受动画长度影响）
transition.duration = 0.3f;  // 总是0.3秒
transition.hasFixedDuration = false;// 相对持续时间（基于动画长度）
transition.duration = 0.5f;  // 动画长度的50%
transition.canTransitionToSelf = true;// 允许状态过渡到自己
transition.canTransitionToSelf = false;// 禁止自过渡（避免无限循环）
~~~
~~~ csharp 示例自过度
var attackToAttack = GetTransition("Attack", "Attack");
attackToAttack.canTransitionToSelf = true;
attackToAttack.AddCondition(AnimatorConditionMode.Equals, "AttackPressed", 1);
attackToAttack.duration = 0.1f;// 玩家连续按攻击键时，Attack状态可以过渡到自己
~~~
解释一下中断源： m_InterruptionSource
- None 无中断：像"死亡动画"，一旦开始就必须完整播放
- Current 当前状态可中断：像"走路过渡"，正在过渡时可以被"跳跃"打断
- Next 目标状态可中断 像"跑步过渡"，目标跑步状态可以被"攻击"打断
- CurrentThenNext 优先考虑当前状态的中断，如果当前状态不能被中断，再考虑目标状态的中断 优先考虑当前状态的中断能力，适合需要快速响应当前状态变化的场景
- NextThenCurrent 优先考虑目标状态是否可以被中断，如果不能，再考虑当前攻击状态 优先考虑目标状态的中断能力，适合需要快速响应目标状态变化的场景<br>
~~~ csharp
var controller = animator.runtimeAnimatorController as AnimatorController;
var stateMachine = controller.layers[0].stateMachine;
// 第一段攻击（当前状态优先中断）
var idleToAttack1 = GetTransition(stateMachine, "Idle", "Attack1");
idleToAttack1.interruptionSource = TransitionInterruptionSource.CurrentThenNext;
idleToAttack1.duration = 0.1f;
idleToAttack1.AddCondition(AnimatorConditionMode.Equals, "AttackPressed", 1);
// 第二段攻击（目标状态优先中断）
var attack1ToAttack2 = GetTransition(stateMachine, "Attack1", "Attack2");
attack1ToAttack2.interruptionSource = TransitionInterruptionSource.NextThenCurrent;
attack1ToAttack2.duration = 0.1f;
attack1ToAttack2.AddCondition(AnimatorConditionMode.Equals, "AttackPressed", 1);
// 第三段攻击（目标状态优先中断）
var attack2ToAttack3 = GetTransition(stateMachine, "Attack2", "Attack3");
attack2ToAttack3.interruptionSource = TransitionInterruptionSource.NextThenCurrent;
attack2ToAttack3.duration = 0.1f;
attack2ToAttack3.AddCondition(AnimatorConditionMode.Equals, "AttackPressed", 1);
// 攻击到Idle（当前状态优先中断）
var attackToIdle = GetTransition(stateMachine, "Attack1", "Idle");
attackToIdle.interruptionSource = TransitionInterruptionSource.CurrentThenNext;
attackToIdle.duration = 0.2f;
attackToIdle.AddCondition(AnimatorConditionMode.Equals, "AttackFinished", 1);
~~~
玩家连续攻击时，优先考虑目标攻击状态是否可以被中断，实现流畅连击<br>
攻击结束时，优先考虑当前攻击状态是否可以被中断，快速回到Idle<br>
方法： <br>
全是一些基础的get set函数 不解释了 还有一些获取名字之类的辅助函数 原因我认为应该在开头 

---

# AnimatorTransition
这个类就是简单的AnimatorStateTransition<br>
区别： 
1. AnimatorTransition 基础的状态切换 只有条件判断，没有时间控制 适用即时切换、条件驱动
2. AnimatorStateTransition 复杂的动画过渡 完整的时间控制、混合、中断管理 适用平滑过渡、复杂状态机
---

# AnimatorState

