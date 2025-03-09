#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace EditorReplacement
{
    public enum WrapType { Root, Defaults }

    public delegate void OnCreatedNode<RT, TNT>(RT replacer, TNT node);

    public abstract class TreeLocation<TLT, RT, TDT, TNT>
        where TLT : TreeLocation<TLT, RT, TDT, TNT>
        where RT : Replacer<RT, TDT, TNT>
        where TDT : TreeDef<RT, TDT, TNT>
        where TNT : TreeNode<RT, TDT, TNT>
    {
        public readonly int depth;
        public readonly int index;
        public readonly TNT node;
        public readonly TDT treeDef;
        public TLT parent = null;

        public TreeLocation(int depth, int index, TNT node, TDT treeDef, TLT parent)
        {
            this.depth = depth;
            this.index = index;
            this.node = node;
            this.treeDef = treeDef;
            this.parent = parent;
        }

        public static Func<int, int, TNT, TDT, TLT, TLT> Create { get; protected set; }
    }

    public abstract class TreeNode<RT, TDT, TNT> : IEnumerable<RT>
        where RT : Replacer<RT, TDT, TNT>
        where TDT : TreeDef<RT, TDT, TNT>
        where TNT : TreeNode<RT, TDT, TNT>
    {
        /// <summary>
        /// If null, it will use the original type, and this also means the end of a branch. There should be no children to a Node with null replacer.
        /// </summary>
        public RT replacer;
        public List<TNT> children = new List<TNT>();

        protected abstract TNT CreateNode(RT replacer);

        public TNT DeepCopy()
        {
            RT rCopy = replacer == null ? null : replacer.DeepCopy();
            var copy = CreateNode(rCopy);
            DeepCopyTo(copy);
            for (int i = 0; i < children.Count; i++)
                copy.children.Add(children[i].DeepCopy());
            return copy;
        }
        protected virtual void DeepCopyTo(TNT deepCopy) { }

        public TreeNode(RT replacer)
        {
            this.replacer = replacer;
        }
        public TreeNode(RT replacer, params TNT[] children)
        {
            this.replacer = replacer;
            this.children.AddRange(children);
        }
        public TreeNode(RT replacer, IList<TNT> children)
        {
            this.replacer = replacer;
            this.children.AddRange(children);
        }

        public IEnumerator<RT> GetEnumerator()
        {
            if (replacer != null)
            {
                yield return replacer;
                for (int i = 0; i < children.Count; i++)
                {
                    foreach (var node in children[i])
                        yield return node;
                }
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(RT replacer)
        {
            children.Add(CreateNode(replacer));
        }
        public void Insert(int index, RT replacer)
        {
            children.Insert(index, CreateNode(replacer));
        }
        public bool RemoveFirst(RT replacer)
        {
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i].replacer == replacer)
                {
                    children.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
        public bool RemoveLast(RT replacer)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].replacer == replacer)
                {
                    children.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
        public int RemoveEvery(RT replacer)
        {
            int count = 0;
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].replacer == replacer)
                {
                    children.RemoveAt(i);
                    count++;
                }
            }
            return count;
        }

        public void WrapDefaultChildren<T>(T replacer, OnCreatedNode<T, TNT> onCreated = null) where T : RT
        {
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i].replacer == null)
                {
                    var wrap = CreateNode(replacer.DeepCopy());
                    wrap.children.Add(children[i]);
                    children[i] = wrap;
                    if (onCreated != null)
                        onCreated((T)wrap.replacer, wrap);
                }
                else
                {
                    children[i].WrapDefaultChildren(replacer, onCreated);
                }
            }
        }

        internal void ForEachNode(Func<TNT, bool> function, ref bool ended)
        {
            if (function((TNT)this))
            {
                for (int i = 0; i < children.Count && !ended; i++)
                    children[i].ForEachNode(function, ref ended);
            }
            else
                ended = true;
        }
        /// <summary>
        /// Return in function if should continue.
        /// </summary>
        public void ForEachNode(Func<TNT, bool> function)
        {
            bool ended = false;
            ForEachNode(function, ref ended);
        }

        public void RemoveChildren(Func<TNT, bool> function, bool reparentChildren)
        {
            for (int i = 0; i < children.Count; i++)
            {
                var node = children[i];
                if (function(node))
                {
                    if (reparentChildren)
                        children.AddRange(node.children);
                    node.children.Clear();
                    children.RemoveAt(i);
                    i--;
                }
            }
        }
    }

    public abstract class TreeDef<RT, TDT, TNT>
        where RT : Replacer<RT, TDT, TNT>
        where TDT : TreeDef<RT, TDT, TNT>
        where TNT : TreeNode<RT, TDT, TNT>
    {
        protected abstract TNT CreateNode(RT replacer);
        protected abstract TNT CreateNode(RT replacer, List<TNT> children);

        public TreeDef()
        {
            root = CreateNode(null);
        }

        protected TNT root;
        public TNT Root
        {
            get
            {
                return root;
            }
            set
            {
                if (value == null)
                    Debug.LogError("TreeDef.Root cannot be null.");
                else
                    root = value;
            }
        }

        /// <summary>
        /// Return in function if should continue.
        /// </summary>
        public void ForEachNode(Func<TNT, bool> function)
        {
            bool ended = false;
            Root.ForEachNode(function, ref ended);
        }

        public void RemoveNodes(Func<TNT, bool> function, bool reparentChildren)
        {
            if (function(Root))
            {
                if (reparentChildren)
                {
                    Root = CreateNode(null, Root.children);
                    Root.RemoveChildren(function, reparentChildren);
                }
                else
                    Root = CreateNode(null);
            }
            else
                Root.RemoveChildren(function, reparentChildren);
        }

        /// <summary>
        /// It's recommended to instead use Wrap(,wrapType) with a static WrapType field,
        /// because otherwise priority will have little effect in customizing the order between a replacer that wraps the defaults and one that wraps the root,
        /// and it's best for there to be some way to offer customization to the user.
        /// </summary>
        public void WrapRoot(RT replacer)
        {
            Root = CreateNode(replacer.DeepCopy(), new List<TNT>() { Root });
        }
        public void Wrap<T>(T replacer, WrapType wrapType, OnCreatedNode<T, TNT> onCreated = null) where T : RT
        {
            if (wrapType == WrapType.Root || Root.replacer == null)
            {
                WrapRoot(replacer);
                if (onCreated != null)
                    onCreated((T)Root.replacer, Root);
            }
            else
            {
                Root.WrapDefaultChildren(replacer, onCreated);
            }
        }
        public void WrapDefaults<T>(T replacer, OnCreatedNode<T, TNT> onCreated = null) where T : RT
        {
            Wrap(replacer, WrapType.Defaults, onCreated);
        }
    }
}
#endif
