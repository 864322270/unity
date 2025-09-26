#mask 

mask中的m_MaskMaterial(遮罩材质) 和 m_UnmaskMaterial(取消遮罩材质)是什么<br>
就是挂在组件上继承Graphic 上的材质球 并且要支持模板缓冲 stencil 否侧会创建一个新的 设置模板参数 具体原理可以去看shader<br>
mask的OnEnable 和OnDisable 会调用 SetMaterialDirty<br>

#RectMask2D

RectMask2D 是“矩形裁剪”：不使用模板缓冲，不创建额外材质；直接用“矩形裁剪框”去裁掉超出区域的 UI。<br>
收集祖先链里的所有 RectMask2D，计算一个“合成裁剪矩形”。<br>
RectMask2D 在 Canvas 空间把所有上层矩形做“交集”，把这块最终矩形通过 CanvasRenderer.EnableRectClipping 下发给子图元，同时对完全不可见的图元做 cull，因此既实现了裁剪，又尽量避免无谓的绘制。<br>
RectMask2D.OnEnable/OnDisable/OnValidate/OnTransformParentChanged/OnCanvasHierarchyChanged 会调用 MaskUtilities.Notify2DMaskStateChanged(this) → 子元素执行 IClippable.RecalculateClipping() → 重新挂接父 RectMask2D，随后 PerformClipping()。<br>
RectMask2D 不会触发 网格还有材质的重建 当它让元素从“被剔除”变为“可见”时，若该元素此前已经被其它改动标记为脏，才会在这一刻执行一次网格/材质重建


#mask和rectmask2d的区别
1. 不使用 Stencil、不产生 Enter/Pop 额外合批代价，通常更省。
2. 只能裁“轴对齐的矩形区域”（在 Canvas 空间），不支持任意形状/软边。
3. 要求元素共面、2D 平面内使用；嵌套时做“矩形求交”。

推荐：
1. 合批与性能：一般场景优先用 RectMask2D。它不改材质、不用模板(push/pop)，更容易与周围元素合批，额外开销更低。 需要“非矩形/不规则形状/软边/精细遮罩层级”时，用 Mask（Stencil）。
2. 为什么 RectMask2D 更利于合批 不改材质 被裁剪的子元素仍使用其原材质/纹理，满足常规合批条件即可跨元素批处理 没有“写模板位”和“Pop 材质”的两次额外 Draw。<br>
mask 进入遮罩：绘制一次“写入模板位”的主材质（可选隐藏 showMaskGraphic=false 避免颜色写入，但 Draw 仍存在）。<br>
离开遮罩：执行一次 Pop（canvasRenderer.hasPopInstruction + popMaterialCount=1）清除模板位。<br>
3. 纯矩形裁剪：滚动列表的 Viewport、面板裁剪、HUD 区域裁剪 子元素共面、在 2D UI 平面内；矩形交集能满足需求 推荐使用rectmask2d
4. 需要非矩形或精细形状遮罩（用 Sprite 形状做遮罩） 需要“镂空/环形/心形”等复杂轮廓 需要softness 推荐使用 mask<br>
优先 RectMask2D：滚动列表、大量子项的界面，能显著减少 Draw，合批更稳。<br>
必要时 Mask：确实要不规则/软边效果再用；减少嵌套层级；设置 showMaskGraphic=false 可减少颜色写入，但 Draw 仍存在。<br>
混用策略：外层用 RectMask2D 做粗裁剪，内层小范围用 Mask 做形状精修，控制层级数量与遮罩范围，综合性能更好。<br>
