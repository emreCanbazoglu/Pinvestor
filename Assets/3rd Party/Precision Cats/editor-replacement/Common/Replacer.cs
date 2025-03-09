#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using SystemPrefs;
using PrecisionCats;

namespace EditorReplacement
{
    public abstract class Replacer<RT, TDT, TNT> : SortedSystem<RT>, IComparable<RT>
        where RT : Replacer<RT, TDT, TNT>
        where TDT : TreeDef<RT, TDT, TNT>
        where TNT : TreeNode<RT, TDT, TNT>
    {
        public abstract RT DeepCopy();

        /// <summary>
        /// This is called for every replacer. You should do nothing to the tree if the tree does not match what your replacer should replace. If you do want to wrap the tree, then call tree.AddWrapper(this);
        /// </summary>
        public abstract void Replace(TDT treeDef);

        /// <summary>
        /// Unlike Replace() which is called for every replacer, PostProcess() is called only for the replacers in the tree's nodes.
        /// </summary>
        public virtual void PostProcess(TNT node, TDT treeDef) { }
    }
}
#endif
