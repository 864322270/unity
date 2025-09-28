#GraphicRaycaster 

如果想要接受点击就要继承BaseRaycaster类 并且实现raycast方法 和camera<br>
blockingobject 这个参数的几个选项就是 none 都不遮挡 2d带有collider2d的物体会遮挡 3d同理 all就是2d 3d都遮挡<br>
ignoreReversedGraphics 是否处理反转图片
~~~csharp
if (ignoreReversedGraphics)
{
    // 检查图形是否面向摄像机
    var dir = go.transform.rotation * Vector3.forward;
    appendGraphic = Vector3.Dot(Vector3.forward, dir) > 0;
}
~~~
raycast 函数<br>
1. 检测有无canvas 且从GraphicRegistry获取该Canvas上所有注册的Graphic组件如果没有Graphic组件，直接返回
2. 支持多显示器
~~~ csharp
int displayIndex;
var currentEventCamera = eventCamera;
if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || currentEventCamera == null)
    displayIndex = canvas.targetDisplay;
else
    displayIndex = currentEventCamera.targetDisplay;
var eventPosition = Display.RelativeMouseAt(eventData.position);
if (eventPosition != Vector3.zero)
{
    int eventDisplayIndex = (int)eventPosition.z;
    if (eventDisplayIndex != displayIndex)
        return; // 忽略其他显示器的输入
}
else
{
    eventPosition = eventData.position;
}
~~~
3. 坐标转换  将屏幕坐标转换为视口坐标（0-1范围） 处理不同Canvas渲染模式的坐标转换
4. 阻挡检测 
~~~ csharp
if (blockingObjects == BlockingObjects.ThreeD || blockingObjects == BlockingObjects.All)
{
    if (ReflectionMethodsCache.Singleton.raycast3D != null)
    {
        var hits = ReflectionMethodsCache.Singleton.raycast3DAll(ray, distanceToClipPlane, (int)m_BlockingMask);
        if (hits.Length > 0)
            hitDistance = hits[0].distance;
    }
}
~~~
5. ui元素检测<br>
获取所有继承graphic的组件<br>
~~~csharp
if (graphic.depth == -1 || !graphic.raycastTarget || graphic.canvasRenderer.cull)
    continue;
~~~
解释一下 depth ==-1 表示这个Graphic组件还没有被Canvas处理过，因此实际上没有被绘制。  非活跃的UI元素 通常也是-1 没有被Canvas处理的Graphic不会实际渲染 对它们进行射线检测是浪费性能的<br>
矩形包含检测 检查点击位置是否在UI元素矩形内<br>
深度检测 检查是否在摄像机远裁剪面内<br>
调用graphic的Raycast 实际上会调用每个组件从写的IsRaycastLocationValid方法<br>
按深度排序<br>
添加到结果列表<br>
6. 结果处理和距离计算
处理反向检测<br>
剔除掉在摄像机后面的元素<br>
阻挡检测if (distance >= hitDistance) continue; 前面2d 3d阻挡相关会赋值 不然不会设置<br>
创建结果
~~~csharp
var castResult = new RaycastResult
{
    gameObject = go,
    module = this,
    distance = distance,
    screenPosition = eventPosition,
    index = resultAppendList.Count,
    depth = m_RaycastResults[index].depth,
    sortingLayer = canvas.sortingLayerID,
    sortingOrder = canvas.sortingOrder,
    worldPosition = ray.origin + ray.direction * distance,
    worldNormal = -transForward
};
resultAppendList.Add(castResult);
~~~



#BaseRaycaster 

虚方法Raycast  这个函数是每帧调用的
~~~
Unity Update循环
    ↓
EventSystem.Update() (每帧调用)
    ↓
m_CurrentInputModule.Process() (当前输入模块处理)
    ↓
PointerInputModule.Process() (处理鼠标/触摸输入)
    ↓
eventSystem.RaycastAll() (执行射线检测)
    ↓
各个Raycaster.Raycast() (具体检测实现)
~~~
OnCanvasHierarchyChanged  OnTransformParentChanged <br>
当Canvas层次结构或Transform父级发生变化时 清除rootRaycaster缓存，确保获取正确的根Raycaster


~~~
Canvas (Main) - 有GraphicRaycaster
├── Image (A) - 注册到Main Canvas
└── Canvas (Child) - 没有GraphicRaycaster
    ├── Image (B) - 注册到Child Canvas ❌ 无法检测
    └── Image (C) - 注册到Child Canvas ❌ 无法检测

修改后：
Canvas (Main) - 有GraphicRaycaster
├── Image (A) - 注册到Main Canvas
├── Image (B) - 注册到Main Canvas ✅ 可以检测
└── Image (C) - 注册到Main Canvas ✅ 可以检测
~~~
~~~ csharp
// 在Graphic.CacheCanvas中
gameObject.GetComponentsInParent(false, list); // 获取[Child Canvas, Main Canvas]

// 遍历父级Canvas，找到第一个活跃的
for (int i = 0; i < list.Count; ++i)
{
    if (list[i].isActiveAndEnabled) // Child Canvas没有GraphicRaycaster，仍然活跃
    {
        m_Canvas = list[i]; // 注册到Child Canvas
        break;
    }
}
~~~
取消Child Canvas后，所有UI元素都会注册到Main Canvas，Main Canvas的GraphicRaycaster就能检测到所有元素，包括Image B上的Button。
这就是为什么在Unity UI中，Canvas的层次结构直接影响事件检测的原因。每个Graphic都会注册到最近的活跃Canvas，而GraphicRaycaster只检测自己Canvas下的Graphics
