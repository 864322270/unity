#Image 

方法： 
1. OnPopulateMesh 渲染时按类型分别生成网格；无 Sprite 时退回父类四边形
2. IsRaycastLocationValid 判断点击是否命中（GraphicRaycaster + EventSystem）<br>
EventSystem 的输入模块采集指针位置，调用场景内的 Raycaster。<br>
GraphicRaycaster对目标 Canvas 内所有启用的 Graphic 组件（如 Image）做屏幕点检测：<br>
要求 Graphic.raycastTarget == true（Inspector 的“Raycast Target”）。<br>
要求屏幕点在其 RectTransform 内。<br>
若实现了 ICanvasRaycastFilter，会进一步调用 IsRaycastLocationValid 二次过滤。<br>
命中对象按绘制深度排序，触发顶层对象的事件接口（如 IPointerClickHandler，Button 会收到点击）<br>
~~~ csharp
if (alphaHitTestMinimumThreshold <= 0)
    return true;
if (alphaHitTestMinimumThreshold > 1)
    return false;
if (activeSprite == null)
    return true;
Vector2 local;
if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out local))
    return false;
Rect rect = GetPixelAdjustedRect();
// Convert to have lower left corner as reference point.
local.x += rectTransform.pivot.x * rect.width;
local.y += rectTransform.pivot.y * rect.height;
local = MapCoordinate(local, rect);
// Convert local coordinates to texture space.
Rect spriteRect = activeSprite.textureRect;
float x = (spriteRect.x + local.x) / activeSprite.texture.width;
float y = (spriteRect.y + local.y) / activeSprite.texture.height;
try
{
    return activeSprite.texture.GetPixelBilinear(x, y).a >= alphaHitTestMinimumThreshold;
}
catch (UnityException e)
{
    Debug.LogError("Using alphaHitTestMinimumThreshold greater than 0 on Image whose sprite texture cannot be read. " + e.Message + " Also make sure to disable sprite packing for this sprite.", this);
    return true;
}
~~~
解释一下 alphaHitTestMinimumThreshold是可以直接赋值的 
~~~ csharp
var img = GetComponent<Image>();
img.alphaHitTestMinimumThreshold = 0f; // <=0 都等效于“总是命中”
~~~
这个值默认是0 就是点击到这个图片就会触发 这个值的意义 我点击到的地方的alpha要大于这个值才算点击到 就是try里的计算 例如 alphaHitTestMinimumThreshold = 0.5 如果我点击的地方的透明度是0.1 那么不会触发点击事件<br>
另外反转图片也是接受点击的 如果反转图片发现点击无效那可以看看Graphic Raycaster 中的 ignore reversed graphics 是否勾选<br>
属性： 
1. overrideSprite 作为"覆盖 Sprite"（override sprite），用于临时替换显示 临时性数据，不序列化，运行时动态设置<br>
由于和替换sprite的性能相同 官方说更实用与临时图片的替换 我几年的经验也没有用上他 不是很懂有什么用
2. useSpriteMesh 在 Texture Importer 中，Sprite Mesh Type 设置为 Tight 适用于 不规则形状的图标（如星形、心形） 有透明边缘的图片 优点：只渲染实心部分，节省 GPU 资源 缺点：顶点数更多，CPU 计算开销大 useSpriteMesh 只影响渲染，不影响点击检测<br>

说下什么哪些操作会引起网格重建吧 什么操作会引起材质重建<br>
网格重建
1. 改变image的类型 image.type = Image.Type.Sliced;  
2. 改变填充相关属性 image.fillMethod = Image.FillMethod.Radial360; 
3. 改变显示相关属性 改变宽高比保持 image.preserveAspect  image.fillCenter = false
4. 改变transform<br>
材质重建（SetMaterialDirty）
1. 改变材质 image.material = newMaterial; <br>
只触发布局重建（SetLayoutDirty）
1. 尺寸计算相关 canvas.referencePixelsPerUnit = 200;  // 改变参考像素密度<br>
触发全部重建（SetAllDirty）
1. 改变 Sprite image.sprite = newSprite;    
2. 改变射线检测 image.raycastTarget = false;




#RawImage
简单说下区别吧 
RawImage是Unity UI系统中用于直接显示纹理(Texture2D)的组件，与Image组件的主要区别是 Image：只能显示Sprite（图集的一部分） RawImage：可以直接显示任何Texture2D<br>
RawImage 有单独的drawcall image如果是图集可以减少drawcall<br>
image 更适合处理ui元素 而 rawimage更适合处理 背景临时图形 rt图等<br>
内存上 rawimage消耗的更多 基础UI开销 + 纹理内存 纹理内存 = width × height × 4字节(RGBA)
