#button

public class Button : Selectable, IPointerClickHandler, ISubmitHandler <br>
IPointerClickHandler 处理鼠标/触摸点击 ISubmitHandler：处理键盘提交（如回车键）<br>
~~~
public virtual void OnSubmit(BaseEventData eventData)
{
    Press();
    // if we get set disabled during the press
    // don't run the coroutine.
    if (!IsActive() || !IsInteractable())
        return;
    DoStateTransition(SelectionState.Pressed, false);
    StartCoroutine(OnFinishSubmit());
}
private IEnumerator OnFinishSubmit()
{
    var fadeTime = colors.fadeDuration;
    var elapsedTime = 0f;
    while (elapsedTime < fadeTime)
    {
        elapsedTime += Time.unscaledDeltaTime;
        yield return null;
    }
    DoStateTransition(currentSelectionState, false);
}
~~~
为什么 OnFinishSubmit<br>
当用户通过键盘导航（如按回车键）点击按钮时 立即触发：OnSubmit被调用，按钮立即执行Press() 视觉反馈：按钮切换到Pressed状态 如果立即恢复原状态，用户看不到按下效果<br>

navigation
这个参数主要是设置在没有鼠标点击的操作 主要是适配一些手柄 xbox相关的点击键位移动的逻辑<br>
FindSelectable <br>
public Selectable FindSelectable(Vector3 dir)<br>
1. 方向向量标准化和坐标转换 将世界方向向量转换为当前对象的本地坐标系 计算当前对象边缘上的搜索起始点 
2. 遍历所有可选择对象 跳过不可交互或导航模式为None的对象
3. 计算目标对象位置 
4. 评分算法
~~~ csharp
float score = dot / myVector.sqrMagnitude;
分子 dot: 方向一致性评分
当目标完全在搜索方向上时，dot = |myVector|，得分为1
当目标与搜索方向垂直时，dot = 0，得分为0
当目标在搜索方向后方时，dot < 0，已被跳过
分母 myVector.sqrMagnitude: 距离惩罚
距离越远，分母越大，得分越低
使用平方距离避免开方运算，提高性能
综合效果: score = 方向一致性 / 距离²
~~~
如果沿y轴旋转180 灰改变他的左右的
