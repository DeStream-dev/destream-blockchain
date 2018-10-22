using System.Collections.Generic;

namespace NBitcoin
{
    internal static class CircularLinkedList
    {
        public static LinkedListNode<T> NextOrFirst<T>(this LinkedListNode<T> current)
        {
            if (current?.List != null) return current.Next ?? current.List.First;
            return null;
        }

        public static LinkedListNode<T> PreviousOrLast<T>(this LinkedListNode<T> current)
        {
            if (current?.List != null) return current.Previous ?? current.List.Last;
            return null;
        }
    }
}