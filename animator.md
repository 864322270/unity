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

# AnimatorCondition
AnimatorCondition在Animator中 当你选中一个过渡箭头时，在 Inspector 面板中会看到条件设置界面 每个条件包含三个设置项 Parameter Condition Threshold 这个就是AnimatorCondition
属性： 
1. m_ConditionMode 条件模式枚举
2. m_ConditionEvent 参数名称字符串
3. m_EventTreshold 	数值阈值
4. m_DeprecatedExitTime

---

# AnimatorState
animatorstate实际上就是打开animator中的小方块<br>
属性：
1. 动画相关 motion 状态关联的动画剪辑或混合树 speed 动画播放速度倍率 cycleOffset 动画循环偏移量 time 动画时间偏移
2. 镜像和IK参数 mirror 是否镜像播放动画 iKOnFeet 是否启用脚部IK riteDefaultValues 是否写入默认值到动画曲线
3. 参数化控制 speedParameter  mirrorParameter cycleOffsetParameter timeParameter 都是一些参数<br>
与AnimatorStateTransition中的参数不同 AnimatorState中的参数是控制具体属性的 例如 speedParameter控制的就是speed 如果想让他生效 还要设置对应的 speedParameterActive为true 这些在animator中都可以设置
4. tag 状态标签（类似于gameobject的tag 方便快速区分）  name <br>
这些参数大部分都可以在animator编辑器面板中看到
5. m_UserList 管理对象间依赖关系的核心机制。它维护了一个双向连接的用户列表 当 AnimatorState 的属性发生变化时，通过 m_UserList.SendMessage(kDidModifyMotion) 通知所有依赖对象
6. m_Transitions 保存从当前状态到其他状态的所有转换条件 m_StateMachineBehaviours 保存附加到当前状态的自定义脚本 （简称smb）是一个很重要的类后面会讲 <br>

方法： 
1. 因为这也是编辑器类 所以大部分方法都是一些简单的get set方法<br>

AnimatorState 和 AnimatorStateInfo 有什么区别： 
1. 类似animationstate 和 AnimationClip的关系 AnimationClip 是编辑器类 而AnimatorStateInfo是运行时结构体 
2. AnimatorState负责定义状态应该是什么样的 AnimatorStateInfo负责告诉你在运行时状态实际是什么样的
3. AnimatorStateInfo 的构建确实需要依靠 AnimatorState，但不是 1:1 直接生成
4. AnimatorState → StateConstant → AnimatorStateInfo  一个 AnimatorState 对应一个 StateConstant，但一个 StateConstant 可以生成多个 AnimatorStateInfo

---
# AnimatorStateMachine
AnimatorStateMachine 是Unity动画系统中状态机的核心类 在animator中 整个后面的窗口可以理解为一个 AnimatorStateMachine 这也是一个仅在编辑器下才能使用的类<br>
属性： 
1. m_DefaultState 存储状态机的默认状态 m_ChildStates 存储状态机包含的所有子状态 m_ChildStateMachines 存储状态机包含的所有子状态机 
2. m_AnyStateTransitions  存储从AnyState到其他状态的过渡 m_EntryTransitions 存储进入状态机时的过渡 m_StateMachineTransitions 存储状态机之间的过渡关系
3. 坐标相关 m_AnyStatePosition 在编辑器中AnyState节点的位置 m_EntryPosition 在编辑器中入口节点的位置 m_ExitPosition 在编辑器中出口节点的位置 m_ParentStateMachinePosition 在父状态机中的位置
4. m_StateMachineBehaviours 存储附加到状态机的行为脚本 m_UserList 管理依赖此状态机的对象
m_ChildStates 和 m_ChildStates有什么区别 
~~~
AnimatorController (根状态机)
├── m_ChildStates
│   ├── Idle (AnimatorState)
│   ├── Walk (AnimatorState)
│   └── Run (AnimatorState)
└── m_ChildStateMachines
    ├── Combat (AnimatorStateMachine)
    │   ├── m_ChildStates
    │   │   ├── Attack (AnimatorState)
    │   │   └── Defend (AnimatorState)
    │   └── m_ChildStateMachines (空)
    └── Locomotion (AnimatorStateMachine)
        ├── m_ChildStates
        │   ├── Idle (AnimatorState)
        │   └── Move (AnimatorState)
        └── m_ChildStateMachines (空)
~~~
反复提到了ChildAnimatorState ChildAnimatorStateMachine 他们是什么 和 AnimatorStateMachine AnimatorState有什么关系 ： 
1. 如图所示
~~~
AnimatorStateMachine
├── m_ChildStates (ChildAnimatorState[])
│   ├── ChildAnimatorState
│   │   └── m_State → AnimatorState (实际状态)
│   └── ChildAnimatorState
│       └── m_State → AnimatorState (实际状态)
└── m_ChildStateMachines (ChildAnimatorStateMachine[])
    ├── ChildAnimatorStateMachine
    │   └── m_StateMachine → AnimatorStateMachine (子状态机)
    └── ChildAnimatorStateMachine
        └── m_StateMachine → AnimatorStateMachine (子状态机)
~~~
2. ChildAnimatorState是AnimatorState的包装器，用于在状态机中管理状态 容器类，不直接播放动画 存储状态在状态机中的位置和引用 
3. ChildAnimatorStateMachine AnimatorStateMachine的包装器，用于在状态机中管理子状态机 容器类，不直接播放动画 存储子状态机在父状态机中的位置和引用
4. AnimatorState 是实际的动画状态，直接播放动画 播放AnimationClip或BlendTree
5. 为什么需要包装器 ChildAnimatorState：在编辑器中显示为矩形节点，可以拖拽移动 ChildAnimatorStateMachine：在编辑器中显示为圆角矩形，可以展开/折叠
6. 这四个都是编辑器专用 在运行时转化成StateMachineConstant 和 StateConstant<br>

方法： 
1. 同 AnimatorState 大多数都是一些 get set add 方法 还有部分 check rename方法 
2. BuildRuntimeAsset 
BuildRuntimeAsset(TOSVector& tos, AnimatorControllerLayerVector& layers, const AnimationClipPPtrVector& animationClips, RuntimeBaseAllocator& alloc) <br>
解释一下 TOSVector TOSVector& tos 是“字符串表（Table Of Strings）”的构建容器。构建运行时常量时，所有用到的名字/路径/标签/参数名等字符串都会通过 ProccessString(tos, "...") 被“实参表化”：去重收集进同一个字符串表，并返回一个 uint32 的ID。随后，状态机的各类常量用这些ID而不是原始字符串，达到体积更小、对比更快、可序列化且稳定可复现的目的。<br>
BuildRuntimeAsset ：顾名思义就是把编辑器的数据结构转换成运行时的数据结构
- 收集所有状态 把默认状态设置为第一个 提高查找效率
- 收集所有状态机信息
- 为每个状态机创建entry节点 包括处理solo/mute逻辑 过度常量等 创建过渡到默认状态条件
- 为每个状态机创建exit节点 如果没有节点到exit就使用默认 有就构建状态机的过度
- 构建状态常量 对每个状态执行 处理状态过渡(BuildTransitionConstant) 处理动画数据 (clip 调用 CreateBlendTreeConstant blendtree 调用 BlendTree::BuildRuntimeAsset) 创建状态常量(CreateStateConstant) 构建 Any State 过渡(BuildTransitionConstant)
- 创建最终状态机常量(CreateStateMachineConstant)
- 返回的 StateMachineConstant 包含：
m_StateConstantArray: 所有状态的常量数组
m_AnyStateTransitionConstantArray: Any State 过渡常量数组
m_SelectorStateConstantArray: 选择器状态常量数组（Entry/Exit 节点）
m_DefaultState: 默认状态索引
m_SynchronizedLayerCount: 同步层数量<br>
3. BuildTransitionConstant (AnimatorStateTransition& transition, AnimatorState const* state,StateInfoVector const& allState, StateMachineInfoVector const& allStateMachines, TOSVector& tos, RuntimeBaseAllocator& alloc)
BuildTransitionConstant: 把编辑器里的一个 AnimatorStateTransition（含条件、目标、时长、打断设置等）编译为运行时可用的 TransitionConstant，供 mecanim 状态机快速评估使用。 
- 如果是animatorState 找到在allstate中的索引 如果是 AnimatorStateMachine 在 allStateMachines中找到索引 如果找不到返回报错 给出错误信息 否则dstStateIndex = (dstStateMachineIndex * 2) + mecanim::statemachine::s_SelectorStateEncodeKey;<br>
dstStateIndex 是allstate中的索引 dstStateMachineIndex是allStateMachines 
解释一下这句话 因为entry和exit是伪节点 他们是在AnimatorStateMachine声明的 而不是AnimatorState 简单把dstStateIndex表示我要去哪里 这个哪里可能是普通状态 也可能是子状态机的entry和exit这样的节点 为了不和普通状态混淆 Unity把“选择器节点(Entry/Exit)”放到一个单独的“编号区”，这个区的编号都加了同一个大偏移量：s_SelectorStateEncodeKey。<br>
这句代码的语义就是“把过渡的目的地设为‘子状态机的 Entry 节点’”，并且“放到选择器编号空间”里，避免和普通状态索引冲突。<br>
最后再给出一个例子
~~~
假设有三个子状态机，索引为：Locomotion=0，Combat=1，UI=2
它们的 Entry 节点在选择器区的编号：
Locomotion Entry: 0*2 + key = 0 + key
Combat Entry: 1*2 + key = 2 + key
UI Entry: 2*2 + key = 4 + key
它们的 Exit 节点在选择器区的编号（供对比理解）：
Locomotion Exit: 0*2+1 + key = 1 + key
Combat Exit: 1*2+1 + key = 3 + key
UI Exit: 2*2+1 + key = 5 + key
~~~
-  若前面仍未得到目标索引 如果过渡是 “To Exit”，将目标编码为“父状态机的 Exit 选择器索引”：((parentIndex * 2) + 1) + s_SelectorStateEncodeKey。 否则（既不是状态、也不是子状态机、也不是 Exit）→ 非法目标：报错并返回空
-  把编辑器层的 Condition 列表编译为 ConditionConstant 数组（内部会用 tos 生成参数名ID） 调用 BuildConditionConstants函数 如果既没有条件、又没有 Exit Time，则给出警告：该过渡在运行时会被忽略（避免无条件“永久真”的过渡）
-  统一字符串ID（使用 tos）
-  生成 TransitionConstant 调用CreateTransitionConstant函数
1. BuildSelectorTransitionConstant
把“选择器过渡”编译成运行时的 SelectorTransitionConstant。选择器过渡指两类<br>从 Entry 节点指向某个状态/子状态机（进入） 从子状态机的 Exit 节点指回父级（退出） 
- 判断过渡类型
 如果是到“状态”（AnimatorStateTransition 或 AnimatorTransition 的 GetDstState()）：用 GetStateIndex(...) 得到“普通状态索引”<br>
 如果是到“子状态机”：把目标编码为“子状态机 Entry 选择器索引” dst = dstStateMachineIndex * 2 + s_SelectorStateEncodeKey（偶数槽位表示 Entry）<br>
 如果是 “Exit”：把目标编码为“父状态机 Exit 选择器索引”dst = (parentIndex * 2 + 1) + s_SelectorStateEncodeKey（奇数槽位表示 Exit）<br>
 上面解释过公式<br>
- 目标无效则返回 0（忽略该过渡）
- 用 BuildConditionConstants 将条件编译为运行时条件常量
- 调用 CreateSelectorTransitionConstant 生成选择器过渡常量<br>

问 BuildSelectorTransitionConstant 和 BuildTransitionConstant 有什么区别？<br>
- 两者都在“编译animator箭头”，只是BuildTransitionConstant 处理“状态层面的过渡” BuildSelectorTransitionConstant 处理“Entry/Exit/子状态机出入口等选择器相关的过渡”。
- 解释一下普通箭头和选择器箭头 普通箭头（状态→状态、Any State→状态/子状态机/Exit） 选择器箭头（Entry→目标、子状态机的 Exit→父级/目标、状态机间箭头）
- 一般情况下 普通箭头在animator中是白色的 选择器箭头是灰色的
5. BuildConditionConstants<br>
- 将编辑器中的过渡条件列表（ConditionVector）编译为运行时的条件常量数组（ConditionConstant*），供状态机运行时快速评估过渡条件<br>
- 循环遍历 conditionsVector 将满足条件的condition 生成一个eventid 调用CreateConditionConstant<br>
`CreateConditionConstant  CreateSelectorTransitionConstant CreateTransitionConstant`这几个函数后面会说
---

# statemachine 
和大写的StateMachine不同 大写的 StateMachine是editor使用的 小写的 statemachine 是运行时使用的 <br>

## TransitionConstant
属性 ： 
1. m_ConditionConstantCount 过渡条件的数量 m_ConditionConstantArray条件常量数组，包含所有需要满足的条件
2. m_DestinationState 目标状态的索引 可以是普通状态索引，也可以是选择器节点索引（Entry/Exit）
3. m_FullPathID m_ID m_UserID 过渡的完整路径ID（用于调试和日志） 过渡的唯一ID（用于内部识别） 用户可见的过渡名称ID
4. m_TransitionDuration 过渡持续时间 m_TransitionOffset 过渡偏移时间 m_ExitTime 退出时间  m_HasExitTime 是否使用退出时间作为过渡条件 m_HasFixedDuration 是否使用固定持续时间
5. m_InterruptionSource 过渡中断源类型 m_OrderedInterruption 是否按顺序处理中断（影响多个过渡的优先级） m_CanTransitionToSelf 是否允许状态过渡到自己
<br> 这些参数看着眼熟把 实际上和AnimatorStateTransition中的参数都差不多 <br>
CreateTransitionConstant :
~~~ csharp
TransitionConstant * transitionConstant = arAlloc.Construct<TransitionConstant>();
transitionConstant->m_ConditionConstantArray = arAlloc.ConstructArray<OffsetPtr<ConditionConstant> >(aConditionConstantCount);
transitionConstant->m_ConditionConstantCount = aConditionConstantCount
transitionConstant->m_TransitionDuration = aTransitionDuration;
transitionConstant->m_TransitionOffset = aTransitionOffset;
transitionConstant->m_ExitTime = aExitTime;
transitionConstant->m_HasExitTime = aHasExitTime;
transitionConstant->m_HasFixedDuration = aHasFixedDuration;
transitionConstant->m_InterruptionSource = transitionInterruptionSource;
transitionConstant->m_OrderedInterruption = orderedInterruption;
transitionConstant->m_CanTransitionToSelf = aCanTransitionToSelf;
transitionConstant->m_ID = aID;
transitionConstant->m_FullPathID = aFullPathID;
transitionConstant->m_UserID = aUserID
transitionConstant->m_DestinationState = aDestinationState;
~~~
这几个create函数都一个样 后面就不解释了 <br>

---
## ConditionConstant
属性 ： 
1. m_ConditionMode 条件模式 大于小于等于 truefalse之类的 
2. m_EventID 指向参数数组中的参数索引
3. m_EventThreshold 事件阈值 简单说下 例子 速度大于2.5 这个2.5就是这个参数 
4. m_ExitTime - 退出时间 <br>
其实和AnimatorCondition中的参数差不多 <br>
CreateConditionConstant也就是简单的赋值不解释了 
---

## SelectorTransitionConstant
属性 ： 
1. m_Destination 目标索引
2. m_ConditionConstantCount 条件数量
3. m_ConditionConstantArray 条件数组
其实和 AnimatorTransition 中的参数差不多 <br>
CreateSelectorTransitionConstant 也就是简单的赋值不解释了 
---

## SelectorStateConstant
运行时“选择器状态”常量，专用于 Entry/Exit 节点<br>
属性 ： 
1. m_TransitionConstantCount 过渡数量
2. m_TransitionConstantArray 过渡数组（每个元素是一个 SelectorTransitionConstant，含条件与目标索引）
3. m_FullPathID 该选择器节点的完整路径字符串ID（用于调试/标识）
4. m_IsEntry true 表示 Entry 选择器；false 表示 Exit 选择器
与 SelectorTransitionConstant的关系 <br>
SelectorStateConstant 是“容器”（选择器节点本身） 其 m_TransitionConstantArray 中的每一项是具体的“箭头规则”（目标索引 + 条件集合）就是SelectorTransitionConstant<br>
CreateSelectorStateConstant 也就是简单的赋值不解释了 


看到这可能有点不理解 用一个简单的例子直观说下
~~~ csharp
    [MenuItem("Assets/Create/Animator/Build Example Controller (Fixed)")]
	public static void Build()
	{
		// 1) 新建 AnimatorController
		AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath("Assets/RootController.controller");
		AnimatorStateMachine rootSM = controller.layers[0].stateMachine;
		// 2) 参数
		controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
		controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
		controller.AddParameter("AttackTrigger", AnimatorControllerParameterType.Trigger);
		controller.AddParameter("AttackPressed", AnimatorControllerParameterType.Bool);
		controller.AddParameter("DefendPressed", AnimatorControllerParameterType.Bool);
		// 3) 根状态
		AnimatorState idle   = rootSM.AddState("Idle");
		AnimatorState walk   = rootSM.AddState("Walk");
		AnimatorState run    = rootSM.AddState("Run");
		AnimatorState attack = rootSM.AddState("Attack");
		AnimatorState death  = rootSM.AddState("Death");
		rootSM.defaultState = idle;
		// 4) 子状态机
		AnimatorStateMachine locomotionSM = rootSM.AddStateMachine("Locomotion");
		AnimatorStateMachine combatSM     = rootSM.AddStateMachine("Combat");
		// 4.1) Locomotion 子状态机状态
		AnimatorState idleL = locomotionSM.AddState("IdleL");
		AnimatorState walkL = locomotionSM.AddState("WalkL");
		AnimatorState runL  = locomotionSM.AddState("RunL");
		locomotionSM.defaultState = idleL;
		// 4.2) Combat 子状态机状态
		AnimatorState attackC = combatSM.AddState("AttackC");
		AnimatorState defendC = combatSM.AddState("DefendC");
		combatSM.defaultState = attackC;
		// 5) 根 Entry（灰箭头）- 使用正确的重载
		{
			// Entry -> Death （IsDead == true）
			AnimatorTransition eDeath = rootSM.AddEntryTransition(death);
			eDeath.AddCondition(AnimatorConditionMode.If, 0f, "IsDead");
			// Entry -> Locomotion（Speed > 3）
			AnimatorTransition eLocoRun = rootSM.AddEntryTransition(locomotionSM);
			eLocoRun.AddCondition(AnimatorConditionMode.Greater, 3f, "Speed");
			// Entry -> Locomotion（Speed > 0.1）
			AnimatorTransition eLocoWalk = rootSM.AddEntryTransition(locomotionSM);
			eLocoWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
			// Entry -> Idle（默认）
			rootSM.AddEntryTransition(idle);
		}
		// 6) Any State（白箭头）：AttackTrigger → Attack（无 ExitTime，时长0.2）
		{
			AnimatorStateTransition anyToAttack = rootSM.AddAnyStateTransition(attack);
			anyToAttack.hasExitTime = false;
			anyToAttack.duration = 0.2f;
			anyToAttack.interruptionSource = TransitionInterruptionSource.None;
			anyToAttack.orderedInterruption = true;
			anyToAttack.AddCondition(AnimatorConditionMode.If, 0f, "AttackTrigger");
		}
		// 7) 根状态间白箭头：Idle ↔ Walk（基于 Speed）
		{
			AnimatorStateTransition idleToWalk = idle.AddTransition(walk);
			idleToWalk.hasExitTime = false;
			idleToWalk.duration = 0.15f;
			idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
			AnimatorStateTransition walkToIdle = walk.AddTransition(idle);
			walkToIdle.hasExitTime = false;
			walkToIdle.duration = 0.15f;
			walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
		}
		// 8) 根状态间白箭头：Walk ↔ Run（基于 Speed）
		{
			AnimatorStateTransition walkToRun = walk.AddTransition(run);
			walkToRun.hasExitTime = false;
			walkToRun.duration = 0.2f;
			walkToRun.AddCondition(AnimatorConditionMode.Greater, 3f, "Speed");
			AnimatorStateTransition runToWalk = run.AddTransition(walk);
			runToWalk.hasExitTime = false;
			runToWalk.duration = 0.2f;
			runToWalk.AddCondition(AnimatorConditionMode.Less, 3f, "Speed");
		}
		// 9) Attack → Locomotion（HasExitTime=true, ExitTime=0.8, Duration=0.1）
		{
            AnimatorStateTransition atkToLoc = attack.AddTransition(locomotionSM);
			atkToLoc.hasExitTime = true;
			atkToLoc.exitTime = 0.8f;
			atkToLoc.duration = 0.1f;
		}
		// 10) Locomotion Entry（灰箭头）
		{
			AnimatorTransition e1 = locomotionSM.AddEntryTransition(runL);
			e1.AddCondition(AnimatorConditionMode.Greater, 3f, "Speed");
			AnimatorTransition e2 = locomotionSM.AddEntryTransition(walkL);
			e2.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
			// 默认 Entry -> IdleL
			locomotionSM.AddEntryTransition(idleL);
		}
		// 11) RunL → Exit（灰箭头，无条件）
		{
			AnimatorStateTransition toExit = runL.AddTransition(runL);   // 先创建一个过渡实例
			toExit.destinationState = null;          // 置空状态目标
			toExit.isExit = true;                    // 标记为 Exit 过渡
			toExit.hasExitTime = false;
			toExit.duration = 0f;
		}
		// 12) Combat Entry（灰箭头）：AttackPressed → AttackC；DefendPressed → DefendC；默认 → AttackC
		{
			AnimatorTransition c1 = combatSM.AddEntryTransition(attackC);
			c1.AddCondition(AnimatorConditionMode.If, 0f, "AttackPressed");
			AnimatorTransition c2 = combatSM.AddEntryTransition(defendC);
			c2.AddCondition(AnimatorConditionMode.If, 0f, "DefendPressed");
			// 默认 Entry -> AttackC
			combatSM.AddEntryTransition(attackC);
		}
		// 13) Combat Exit（灰箭头）：示例——AttackC 到 Exit（播完退出）
		{
			AnimatorStateTransition sToExit = attackC.AddTransition(attackC); // 兼容写法
			sToExit.destinationState = null;
			sToExit.isExit = true;
			sToExit.hasExitTime = true;
			sToExit.exitTime = 0.9f;
			sToExit.duration = 0.05f;
		}
		EditorUtility.DisplayDialog("Animator", "  ", "OK");
	}
~~~
~~~
运行时常量结构（StateMachineConstant 视角）
StateMachineConstant (Root)
├─ m_StateConstantArray
│  ├─ StateConstant(Idle)                     ┐
│  ├─ StateConstant(Walk)                     │  ← 普通白箭头：TransitionConstant
│  ├─ StateConstant(Run)                      │
│  ├─ StateConstant(Attack)                   │
│  ├─ StateConstant(Death)                    ┘
│  ├─ StateConstant(Locomotion.IdleL)
│  ├─ StateConstant(Locomotion.WalkL)
│  ├─ StateConstant(Locomotion.RunL)
│  ├─ StateConstant(Combat.AttackC)
│  └─ StateConstant(Combat.DefendC)
│
├─ m_AnyStateTransitionConstantArray
│  └─ TransitionConstant(AnyState → Attack)
│       - 条件：If(AttackTrigger)
│       - 时序：duration=0.2
│       - 中断：CurrentThenNext, ordered=true
│
├─ m_SelectorStateConstantArray                 ← 选择器灰箭头（Entry/Exit）
│  ├─ [Root.Entry] SelectorStateConstant(isEntry=true)
│  │   ├─ SelectorTransitionConstant(If(IsDead))         → Death
│  │   ├─ SelectorTransitionConstant(Greater(Speed,3))   → Locomotion.Entry(编码索引)
│  │   ├─ SelectorTransitionConstant(Greater(Speed,0.1)) → Locomotion.Entry(编码索引)
│  │   └─ SelectorTransitionConstant(默认)               → Idle
│  │
│  ├─ [Root.Exit] SelectorStateConstant(isEntry=false)
│  │   └─ SelectorTransitionConstant(默认)               → 父层（若存在）
│  │
│  ├─ [Locomotion.Entry] SelectorStateConstant
│  │   ├─ (Speed>3)   → RunL
│  │   ├─ (Speed>0.1) → WalkL
│  │   └─ (默认)      → IdleL
│  │
│  ├─ [Locomotion.Exit] SelectorStateConstant
│  │   └─ (默认)      → 回 Root（父层预设）
│  │
│  ├─ [Combat.Entry] SelectorStateConstant
│  │   ├─ (AttackPressed) → AttackC
│  │   ├─ (DefendPressed) → DefendC
│  │   └─ (默认)          → AttackC
│  │
│  └─ [Combat.Exit] SelectorStateConstant
│      └─ (默认)          → 回 Root（父层）
│
└─ m_DefaultState = Idle
~~~
~~~
Editor 直观图（Animator 面板逻辑布局）
Root (StateMachine)
├─ States //AnimatorState[]
│  ├─ Idle  ← default
│  ├─ Walk
│  ├─ Run
│  ├─ Attack
│  └─ Death
│
├─ SubStateMachines //AnimatorStateMachine[]
│  ├─ Locomotion //AnimatorStateMachine
│  │   ├─ (Entry) --(Speed>3)--> RunL
│  │   │           (Speed>0.1)--> WalkL
│  │   │           (default)  --> IdleL
│  │   ├─ IdleL  ← default
│  │   ├─ WalkL
│  │   ├─ RunL --(灰箭头 Exit)-->
│  │   └─ (Exit)
│  │
│  └─ Combat //AnimatorStateMachine
│      ├─ (Entry) --(AttackPressed)--> AttackC
│      │           (DefendPressed)--> DefendC
│      │           (default)      --> AttackC
│      ├─ AttackC --(hasExitTime=0.9,duration=0.05 灰箭头 Exit)-->
│      ├─ DefendC
│      └─ (Exit)
│
├─ (Entry) //AnimatorTransition
│   ├─ -- If(IsDead)        --> Death
│   ├─ -- Speed>3           --> Locomotion (进入其 Entry)
│   ├─ -- Speed>0.1         --> Locomotion (进入其 Entry)
│   └─ -- default           --> Idle
│
├─ Any State  // AnimatorStateTransition
│   └─ -- If(AttackTrigger) --> Attack   [duration=0.2, interrupt=None]
│
├─ Transitions (white)  //AnimatorStateTransition
│   ├─ Idle --(Speed>0.1, dur=0.15)--> Walk
│   ├─ Walk --(Speed<0.1, dur=0.15)--> Idle
│   ├─ Walk --(Speed>3,   dur=0.2 )--> Run
│   └─ Run  --(Speed<3,   dur=0.2 )--> Walk
│
└─ Attack --(hasExitTime=0.8, dur=0.1, white)--> Locomotion (进入其 Entry)
~~~

说了这么多 BuildRuntimeAsset 什么时候会被调用呢
- BuildRuntimeAsset是“编辑器侧编译”步骤，用来把 AnimatorStateMachine 编译成运行时常量
- 你修改 Animator（状态、过渡、条件、Entry/Exit 等）并保存/应用时，控制器需要重建内部常量。
- 进入 Play Mode、加载场景且该 AnimatorController 被用到时，编辑器端需要确保有最新的编译产物。
- 构建 Player/打 AssetBundle 时，管线会对 AnimatorController 进行编译打包。
- 通过导入/再导入 AnimatorController 资产（Importer）时。

---
上面全是editor和怎么从editor转换到运行时的 下面说的就是运行时相关的部分了 <br>
---
  
## StateMachineInput
1. m_DeltaTime本帧步进时间
2. m_AnimationSet 该层可用的动画集合（Clip 索引、BlendTree 节点等）
3. m_Speed 层速度缩放（Layer 级别）。与状态自身的 m_Speed、参数化速度共同决定最终播放速率。
4. m_Values 参数值表（float/int/bool/trigger 的即时快照）。条件检查（ConditionConstant）通过它按 EventID 读取参数值。
5. m_SynchronizedLayerTimingWeightArray 同步层的时间权重数组
6. m_GotoStateInfo 主动跳转请求（如 Play/CrossFade 注入的“临时过渡/跳转”）
7. m_FirstEval 首帧评估状态机标志：kWaitForTick / kFirstEval / kFirstEvalCompleted。
8. m_StateMachineBehaviourPlayer SMB 事件派发器。Evaluate 内检查是否禁用（IsDisabled）以及在状态切换/更新时触发 OnStateEnter/Exit/Update/Move/IK。
## StateMachineOutput
本帧评估的输出，提供给上层可播放节点（如 AnimationStateMachineMixerPlayable）与回调系统。<br>
1. m_CurrentStateMessage / m_NextStateMessage / m_InterruptedStateMessage: 本帧要派发的状态机消息（用于 SMB：OnStateEnter/Exit/Move/IK 等）
2. m_Playables: 指向本层输出的 PlayableInstanceData
3. m_IKOnFeet: 是否启用脚部 IK
4. m_HasPosePlayable: 是否产生姿势输出
5. m_InterruptedTransition: 是否发生“被中断”的过渡
6. m_EndTransition: 本帧是否结束过渡
## StateMachineMemory
该层状态机的“可变运行时内存”，跨帧保存当前/下一状态、过渡与时间等上下文，EvaluateStateMachine 每帧都会读写。<br>
1. 同步层信息 m_SynchronizedLayerCount  同步层数量 m_SynchronizedLayerAutoWeightArray  同步层的自动权重数组（OffsetPtr<float>），用于层间时间/权重同步
2. 运行状态索引 m_CurrentStateIndex: 当前播放的状态索引 m_NextStateIndex: 目标/下一状态索引（处于过渡中时有效）  m_ExitStateIndex: Exit 路由时的状态索引缓存（用于从子状态机退出的返回指向） m_InterruptedStateIndex: 被中断的“原目标”或“原当前”状态索引（用于恢复/统计）
3. 当前生效的过渡信息 m_TransitionIndex: 正在生效的过渡在数组中的下标（-1 表示无过渡） m_TransitionSourceStateIndex: 该过渡的来源状态索引（AnyState/Source/Next 等场景会用到）m_TransitionType: 过渡类型掩码（kNormal/kEntry/kExit），帮助运行时区分来源于普通、Entry、Exit 的流向
4. 上一帧各状态时间 m_CurrentStatePreviousTime: 上一帧“当前状态”的标准化时间/或局部时间缓存 m_NextStatePreviousTime: 上一帧“下一状态”的时间缓存 m_InterruptedStatePreviousTime: 上一帧“被中断状态”的时间缓存 m_ExitStatePreviousTime: 上一帧“Exit 路由状态”的时间缓存
5. 状态时长（秒或基于剪辑长度的基准） m_CurrentStateDuration: 当前状态的有效时长（用于 ExitTime、时间推进） m_NextStateDuration: 下一状态的有效时长 m_NextStateBaseDuration: 下一状态的“基础时长”（中断逻辑下用于恢复/对齐） m_ExitStateDuration: Exit 路由状态的有效时长 m_InterruptedStateDuration: 被中断的状态时长
6. 速度修正（与 StateConstant/参数化速度结合） m_CurrentStateSpeedModifier: 当前状态速度缩放（>1 加速，<1 减速） m_NextStateSpeedModifier: 下一状态速度缩放 m_ExitStateSpeedModifier: Exit 路由状态速度缩放 m_InterruptedStateSpeedModifier: 被中断状态速度缩放
7. 过渡计时 m_TransitionStartTime: 启动过渡的全局时刻（用于累计） m_TransitionTime: 当前过渡已进行时间（秒） m_TransitionDuration: 本次过渡总时长（秒）m_TransitionOffset: 本次过渡偏移（开始时的 offset，配合 Fixed/Normalized 使用）
8. 运行标志（核心状态机布尔位）m_InInterruptedTransition: 当前是否处于“被中断”的过渡（影响播放/采样混合） m_InTransition: 是否正在进行普通过渡（Current→Next 混合） m_InDynamicTransition: 是否是动态/临时过渡（如 CrossFade 注入的 runtime 过渡） m_ActiveGotoState: 是否存在主动跳转（GotoState）请求（外部 API/内部路由触发） m_FixedTransition: 过渡是否使用固定时长（非按剪辑归一化） m_CleanAfterTransition: 过渡结束后是否需要清理缓存/复位标志 m_ResetPlayableGraph: 是否需要在本帧后重置/重构可播放图（结构变化时设置）
## StateMachineWorkspace
 存放当帧条件检查、混合树过渡、触发器复位等的临时数据
1. m_StateWorkspace: 指向“状态级”的工作区（每个状态采样、混合时的临时缓存）
2. m_ValuesConstant: 参数常量表（用于参数索引映射）
3. m_TriggerResetArray: 本帧需要复位的 Trigger 标志数组指针
4. m_MaxBlendedClipCount: 本层最大混合片段数上限（为临时缓冲分配提供界限）

animator的执行顺序
1.  Animator.UpdateGraph
2.  AnimatorControllerPlayable::PrepareFrame/UpdateGraph
3.  statemachine::EvaluateStateMachine
4.  AnimationClipPlayable::PrepareFrame
5.  AnimationClip::FireAnimationEvents
