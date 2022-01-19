using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Do_Re_Mi_Lyrics.Helper;

public class VisualTreeAdapter : ILinqTree<DependencyObject>
{
    private readonly DependencyObject _item;

    public VisualTreeAdapter(DependencyObject item)
    {
        _item = item;
    }

    public IEnumerable<DependencyObject> Children()
    {
        int childrenCount = VisualTreeHelper.GetChildrenCount(_item);
        for (int i = 0; i < childrenCount; i++)
        {
            yield return VisualTreeHelper.GetChild(_item, i);
        }
    }
}

public interface ILinqTree<out T>
{
    IEnumerable<T> Children();
}

public static class LinqToVisualTree
{
    public static IEnumerable<DependencyObject> Descendants(this DependencyObject item)
    {
        ILinqTree<DependencyObject> adapter = new VisualTreeAdapter(item);
        foreach (DependencyObject child in adapter.Children())
        {
            yield return child;

            foreach (DependencyObject grandChild in child.Descendants())
            {
                yield return grandChild;
            }
        }
    }
}