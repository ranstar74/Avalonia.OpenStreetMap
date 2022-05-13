using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Metadata;

namespace Avalonia.OpenStreetMap.Control.Map;

/// <summary>
/// Base class for controls that can contain multiple children.
/// </summary>
/// <remarks>
/// Controls can be added to a <see cref="TemplatedPanel"/> by adding them to its <see cref="Children"/>
/// collection. All children are layed out to fill the TemplatedPanel.
/// </remarks>
public class TemplatedPanel : TemplatedControl, IPanel, IChildIndexProvider
{
    /// <summary>
    /// Initializes static members of the <see cref="TemplatedPanel"/> class.
    /// </summary>
    static TemplatedPanel()
    {
        
    }

    private EventHandler<ChildIndexChangedEventArgs> _childIndexChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplatedPanel"/> class.
    /// </summary>
    public TemplatedPanel()
    {
        Children.CollectionChanged += ChildrenChanged;
    }

    /// <summary>
    /// Gets the children of the <see cref="TemplatedPanel"/>.
    /// </summary>
    [Content]
    public Controls.Controls Children { get; } = new();

    event EventHandler<ChildIndexChangedEventArgs> IChildIndexProvider.ChildIndexChanged
    {
        add => _childIndexChanged += value;
        remove => _childIndexChanged -= value;
    }
    
    /// <summary>
    /// Marks a property on a child as affecting the parent TemplatedPanel's arrangement.
    /// </summary>
    /// <param name="properties">The properties.</param>
    protected static void AffectsParentArrange<TTemplatedPanel>(params AvaloniaProperty[] properties)
        where TTemplatedPanel : class, IPanel
    {
        foreach (var property in properties)
        {
            property.Changed.Subscribe(AffectsParentArrangeInvalidate<TTemplatedPanel>);
        }
    }

    /// <summary>
    /// Marks a property on a child as affecting the parent TemplatedPanel's measurement.
    /// </summary>
    /// <param name="properties">The properties.</param>
    protected static void AffectsParentMeasure<TTemplatedPanel>(params AvaloniaProperty[] properties)
        where TTemplatedPanel : class, IPanel
    {
        foreach (var property in properties)
        {
            property.Changed.Subscribe(AffectsParentMeasureInvalidate<TTemplatedPanel>);
        }
    }

    /// <summary>
    /// Called when the <see cref="Children"/> collection changes.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event args.</param>
    protected virtual void ChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        List<Controls.Control> controls;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                controls = e.NewItems.OfType<Controls.Control>().ToList();
                LogicalChildren.InsertRange(e.NewStartingIndex, controls);
                VisualChildren.InsertRange(e.NewStartingIndex, e.NewItems.OfType<Visual>());
                break;

            case NotifyCollectionChangedAction.Move:
                LogicalChildren.MoveRange(e.OldStartingIndex, e.OldItems.Count, e.NewStartingIndex);
                VisualChildren.MoveRange(e.OldStartingIndex, e.OldItems.Count, e.NewStartingIndex);
                break;

            case NotifyCollectionChangedAction.Remove:
                controls = e.OldItems.OfType<Controls.Control>().ToList();
                LogicalChildren.RemoveAll(controls);
                VisualChildren.RemoveAll(e.OldItems.OfType<Visual>());
                break;

            case NotifyCollectionChangedAction.Replace:
                for (var i = 0; i < e.OldItems.Count; ++i)
                {
                    var index = i + e.OldStartingIndex;
                    var child = (IControl)e.NewItems[i];
                    LogicalChildren[index] = child;
                    VisualChildren[index] = child;
                }

                break;

            case NotifyCollectionChangedAction.Reset:
                throw new NotSupportedException();
        }

        _childIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs());
        InvalidateMeasureOnChildrenChanged();
    }

    private protected virtual void InvalidateMeasureOnChildrenChanged()
    {
        InvalidateMeasure();
    }

    private static void AffectsParentArrangeInvalidate<TTemplatedPanel>(AvaloniaPropertyChangedEventArgs e)
        where TTemplatedPanel : class, IPanel
    {
        var control = e.Sender as IControl;
        var TemplatedPanel = control?.VisualParent as TTemplatedPanel;
        TemplatedPanel?.InvalidateArrange();
    }

    private static void AffectsParentMeasureInvalidate<TTemplatedPanel>(AvaloniaPropertyChangedEventArgs e)
        where TTemplatedPanel : class, IPanel
    {
        var control = e.Sender as IControl;
        var TemplatedPanel = control?.VisualParent as TTemplatedPanel;
        TemplatedPanel?.InvalidateMeasure();
    }

    int IChildIndexProvider.GetChildIndex(ILogical child)
    {
        return child is IControl control ? Children.IndexOf(control) : -1;
    }

    public bool TryGetTotalCount(out int count)
    {
        count = Children.Count;
        return true;
    }
}