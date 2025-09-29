
#Graphic

OnRectTransformDimensionsChange 

setalldirty
~~~csharp
public virtual void SetAllDirty()
{
    if (m_SkipLayoutUpdate) //m_SkipLayoutUpdate 如果是纹理相同 只有uv变化 或者颜色变化不影响布局 可以设置为true 
    {
        m_SkipLayoutUpdate = false;
    }
    else
    {
        SetLayoutDirty();
    }
    if (m_SkipMaterialUpdate)// 只改变顶点数据而不改变材质的情况 可以设置为true
    {
        m_SkipMaterialUpdate = false;
    }
    else
    {
        SetMaterialDirty();
    }
    SetVerticesDirty();
}
~~~
RegisterDirtyLayoutCallback<br>
RegisterDirtyVerticesCallback<br>
RegisterDirtyMaterialCallback<br> 
这三个都是简单的注册回调函数 
调用时机在set对应的dirty方法<br> 

SetLayoutDirty<br>
SetMaterialDirty<br>
SetVerticesDirty<br>
如果隐藏就返回 之后把对自己注册到对应的rebuild中 最后调用回调<br>

OnDidApplyAnimationProperties 动画系统改变属性时调用<br>
定点相关的函数就不介绍了 

#canvasupdateregistry
PlayerLoop（UIEvents 阶段）→ CanvasManager::WillRenderCanvases()（C++）<br>
通过 ScriptingInvocation 调 Canvas.SendWillRenderCanvases()（C#）<br>
C# 里 SendWillRenderCanvases() 触发 Canvas.willRenderCanvases 事件 (通常每帧一次；调用 Canvas.ForceUpdateCanvases() 会立即触发一次；编辑器下重绘也可能触发。)<br>
订阅者（如 CanvasUpdateRegistry.PerformUpdate）被调用，进而跑 Layout/Graphic 的重建流程。<br>

核心就是PerformUpdate 函数
~~~csharp
protected CanvasUpdateRegistry()
{
    Canvas.willRenderCanvases += PerformUpdate;
}
~~~
~~~csharp
private void PerformUpdate()
{
    UISystemProfilerApi.BeginSample(UISystemProfilerApi.SampleType.Layout);
    CleanInvalidItems();//清理已销毁或无效的队列元素，避免空引用。
    m_PerformingLayoutUpdate = true;//标记“正在做布局更新”，用于防重入与报错。
    m_LayoutRebuildQueue.Sort(s_SortLayoutFunction);
    for (int i = 0; i <= (int)CanvasUpdate.PostLayout; i++)
    {
        for (int j = 0; j < m_LayoutRebuildQueue.Count; j++)
        {
            var rebuild = instance.m_LayoutRebuildQueue[j];
            try
            {
                if (ObjectValidForUpdate(rebuild))
                    rebuild.Rebuild((CanvasUpdate)i);//三阶段循环: i = Prelayout → Layout → PostLayout，逐阶段对每个元素调用 ICanvasElement.Rebuild(i)（如 LayoutRebuilder.Rebuild）。
            }
            catch (Exception e)
            {
                Debug.LogException(e, rebuild.transform);
            }
        }
    }
    for (int i = 0; i < m_LayoutRebuildQueue.Count; ++i)
        m_LayoutRebuildQueue[i].LayoutComplete();//对本轮队列里的每个元素调用完成回调
    instance.m_LayoutRebuildQueue.Clear();
    m_PerformingLayoutUpdate = false;
    ClipperRegistry.instance.Cull();
    m_PerformingGraphicUpdate = true;//布局完成后做一次裁剪更新（遮罩/裁剪区域刷新）
    for (var i = (int)CanvasUpdate.PreRender; i < (int)CanvasUpdate.MaxUpdateValue; i++)
    {
        for (var k = 0; k < instance.m_GraphicRebuildQueue.Count; k++)
        {
            try
            {
                var element = instance.m_GraphicRebuildQueue[k];
                if (ObjectValidForUpdate(element))
                    element.Rebuild((CanvasUpdate)i);//两阶段循环: i = PreRender → LatePreRender，逐阶段对 m_GraphicRebuildQueue 调用 ICanvasElement.Rebuild(i)（如 Graphic.Rebuild 内部据 m_VertsDirty/m_MaterialDirty 选择 UpdateGeometry/UpdateMaterial）。
            }
            catch (Exception e)
            {
                Debug.LogException(e, instance.m_GraphicRebuildQueue[k].transform);
            }
        }
    }
    for (int i = 0; i < m_GraphicRebuildQueue.Count; ++i)
        m_GraphicRebuildQueue[i].GraphicUpdateComplete();
    instance.m_GraphicRebuildQueue.Clear();
    m_PerformingGraphicUpdate = false;
    UISystemProfilerApi.EndSample(UISystemProfilerApi.SampleType.Layout);
}
~~~
