﻿//----------------------------------------------------------------------------------------
//	Copyright © 2006 - 2018 Tangible Software Solutions Inc.
//	This class can be used by anyone provided that the copyright notice remains intact.
//
//	This class provides the ability to initialize and delete array elements.
//----------------------------------------------------------------------------------------
internal static class Arrays
{
    internal static T[] InitializeWithDefaultInstances<T> ( int length ) where T : new()
    {
        T[] array = new T[length];
        for (int i = 0; i < length; i++)
        {
            array[i] = new T();
        }
        return array;
    }

    internal static void DeleteArray<T> ( T[] array ) where T : System.IDisposable
    {
        foreach (T element in array)
        {
            if (element != null)
                element.Dispose();
        }
    }

    internal static int Copy<T> ( T[] sourceArray, int sourceOFfset, T[] destinationArray, int destinationOffset )
    {
        var len = sourceArray.Length;
        Array.Copy(sourceArray, sourceOFfset, destinationArray, destinationOffset, len);
        return len;
    }
}