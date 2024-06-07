// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Reflection;

namespace Files.App.Helpers;

public static class DependencyObjectHelpers
{
    public static T FindChild<T>(DependencyObject startNode) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(startNode);
        for (var i = 0; i < count; i++)
        {
            var current = VisualTreeHelper.GetChild(startNode, i);
            if (current.GetType().Equals(typeof(T)) || current.GetType().GetTypeInfo().IsSubclassOf(typeof(T)))
            {
                var asType = (T)current;
                return asType;
            }
            var retVal = FindChild<T>(current);
            if (retVal is not null)
            {
                return retVal;
            }
        }
        return null!;
    }

    public static T FindChild<T>(DependencyObject startNode, Func<T, bool> predicate) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(startNode);
        for (var i = 0; i < count; i++)
        {
            var current = VisualTreeHelper.GetChild(startNode, i);
            if (current.GetType().Equals(typeof(T)) || current.GetType().GetTypeInfo().IsSubclassOf(typeof(T)))
            {
                var asType = (T)current;
                if (predicate(asType))
                {
                    return asType;
                }
            }
            var retVal = FindChild(current, predicate);
            if (retVal is not null)
            {
                return retVal;
            }
        }
        return null!;
    }

    public static IEnumerable<T> FindChildren<T>(DependencyObject startNode) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(startNode);
        for (var i = 0; i < count; i++)
        {
            var current = VisualTreeHelper.GetChild(startNode, i);
            if (current.GetType().Equals(typeof(T)) || current.GetType().GetTypeInfo().IsSubclassOf(typeof(T)))
            {
                var asType = (T)current;
                yield return asType;
            }
            foreach (var item in FindChildren<T>(current))
            {
                yield return item;
            }
        }
    }

    public static T FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        T parent = null!;
        if (child is null)
        {
            return parent;
        }
        var CurrentParent = VisualTreeHelper.GetParent(child);
        while (CurrentParent is not null)
        {
            if (CurrentParent is T t)
            {
                parent = t;
                break;
            }
            CurrentParent = VisualTreeHelper.GetParent(CurrentParent);
        }
        return parent;
    }
}