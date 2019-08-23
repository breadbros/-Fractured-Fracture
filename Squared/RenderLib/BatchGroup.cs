﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace Squared.Render {
    public class BatchGroup : ListBatch<Batch>, IBatchContainer {
        /// <summary>
        /// Set a name for the batch to aid debugging;
        /// </summary>
        public string Name;

        /// <summary>
        /// A material set view transforms are pushed to.
        /// </summary>
        public DefaultMaterialSet MaterialSet;

        /// <summary>
        /// A view transform that is pushed for the duration of the batch group.
        /// </summary>
        public ViewTransform? ViewTransform;

        public OcclusionQuery OcclusionQuery;

        Action<DeviceManager, object> _Before, _After;
        private object _UserData;

        public override void Prepare (PrepareContext context) {
            BatchCombiner.CombineBatches(_DrawCalls, context.BatchesToRelease);

            _DrawCalls.Sort(Frame.BatchComparer);
            context.PrepareMany(_DrawCalls);

            OnPrepareDone();
        }

        public override void Issue (DeviceManager manager) {
            manager.BatchGroupStack.Push(this);

            if (OcclusionQuery != null)
                OcclusionQuery.Begin();

            if (ViewTransform.HasValue)
                MaterialSet?.PushViewTransform(ref ViewTransform);

            if (_Before != null)
                _Before(manager, _UserData);

            try {
                using (var b = _DrawCalls.GetBuffer(false)) {
                    for (int i = 0; i < b.Count; i++)
                        if (b.Data[i] != null)
                            b.Data[i].IssueAndWrapExceptions(manager);
                }
            } finally {
                if (_After != null)
                    _After(manager, _UserData);
                if (OcclusionQuery != null)
                    OcclusionQuery.End();

                base.Issue(manager);

                if (ViewTransform.HasValue)
                    MaterialSet?.PopViewTransform();

                manager.BatchGroupStack.Pop();
            }
        }

        public static RenderTargetBatchGroup ForRenderTarget (
            IBatchContainer container, int layer, AutoRenderTarget renderTarget, 
            Action<DeviceManager, object> before = null, Action<DeviceManager, object> after = null, 
            object userData = null, 
            DefaultMaterialSet materialSet = null, ViewTransform? viewTransform = null, 
            string name = null, bool? ignoreInvalidTargets = null
        ) {
            if (container == null)
                throw new ArgumentNullException("container");
            else if (renderTarget?.IsDisposed == true)
                throw new ObjectDisposedException("renderTarget");

            var result = container.RenderManager.AllocateBatch<RenderTargetBatchGroup>();

            result.Initialize(container, layer, before, after, userData, materialSet, viewTransform);
            result.SingleAuto = renderTarget;
            result.Single = null;
            result.Multiple = null;
            result.Name = name;
            result.IgnoreInvalidRenderTargets = ignoreInvalidTargets ?? result.IgnoreInvalidRenderTargets;
            result.CaptureStack(0);

            return result;
        }

        public static RenderTargetBatchGroup ForRenderTarget (
            IBatchContainer container, int layer, RenderTarget2D renderTarget, 
            Action<DeviceManager, object> before = null, Action<DeviceManager, object> after = null, 
            object userData = null, 
            DefaultMaterialSet materialSet = null, ViewTransform? viewTransform = null, 
            string name = null, bool? ignoreInvalidTargets = null
        ) {
            if (container == null)
                throw new ArgumentNullException("container");
            else if (renderTarget?.IsDisposed == true)
                throw new ObjectDisposedException("renderTarget");

            var result = container.RenderManager.AllocateBatch<RenderTargetBatchGroup>();

            result.Initialize(container, layer, before, after, userData, materialSet, viewTransform);
            result.Single = renderTarget;
            result.SingleAuto = null;
            result.Multiple = null;
            result.Name = name;
            result.IgnoreInvalidRenderTargets = ignoreInvalidTargets ?? result.IgnoreInvalidRenderTargets;
            result.CaptureStack(0);

            return result;
        }

        public static RenderTargetBatchGroup ForRenderTargets (
            IBatchContainer container, int layer, RenderTarget2D[] renderTargets, 
            Action<DeviceManager, object> before = null, Action<DeviceManager, object> after = null, 
            object userData = null, 
            DefaultMaterialSet materialSet = null, ViewTransform? viewTransform = null, 
            string name = null, bool? ignoreInvalidTargets = null
        ) {
            var bindings = new RenderTargetBinding[renderTargets.Length];
            return ForRenderTargets(
                container, layer, bindings,
                before, after, userData,
                materialSet, viewTransform,
                name, ignoreInvalidTargets
            );
        }

        public static RenderTargetBatchGroup ForRenderTargets (
            IBatchContainer container, int layer, RenderTargetBinding[] renderTargets, 
            Action<DeviceManager, object> before = null, Action<DeviceManager, object> after = null, 
            object userData = null, 
            DefaultMaterialSet materialSet = null, ViewTransform? viewTransform = null, 
            string name = null, bool? ignoreInvalidTargets = null
        ) {
            if (container == null)
                throw new ArgumentNullException("container");

            if ((renderTargets == null) || (renderTargets.Length == 0))
                return ForRenderTarget(container, layer, (RenderTarget2D)null, before, after, userData, name: name);

            foreach (var binding in renderTargets)
                if (binding.RenderTarget?.IsDisposed == true)
                    throw new ObjectDisposedException("renderTargets[i]");

            var result = container.RenderManager.AllocateBatch<RenderTargetBatchGroup>();

            result.Initialize(container, layer, before, after, userData, materialSet, viewTransform);
            result.Single = null;
            result.SingleAuto = null;
            result.Multiple = renderTargets;
            result.Name = name;
            result.IgnoreInvalidRenderTargets = ignoreInvalidTargets ?? result.IgnoreInvalidRenderTargets;
            result.CaptureStack(0);

            return result;
        }

        public static BatchGroup New (
            IBatchContainer container, int layer, 
            Action<DeviceManager, object> before = null, Action<DeviceManager, object> after = null,
            object userData = null, 
            DefaultMaterialSet materialSet = null, ViewTransform? viewTransform = null, 
            string name = null
        ) {
            if (container == null)
                throw new ArgumentNullException("container");

            var result = container.RenderManager.AllocateBatch<BatchGroup>();
            result.Initialize(container, layer, before, after, userData, materialSet, viewTransform);
            result.Name = name;
            result.CaptureStack(0);

            return result;
        }

        public void Initialize (
            IBatchContainer container, int layer, 
            Action<DeviceManager, object> before, Action<DeviceManager, object> after, 
            object userData, DefaultMaterialSet materialSet = null, ViewTransform? viewTransform = null, 
            bool addToContainer = true
        ) {
            base.Initialize(container, layer, null, addToContainer);

            if (viewTransform.HasValue && (materialSet == null))
                throw new ArgumentException("No view transform can be applied without a material set");

            RenderManager = container.RenderManager;
            _Before = before;
            _After = after;
            _UserData = userData;
            MaterialSet = materialSet;
            ViewTransform = viewTransform;
            IsReleased = false;
            OcclusionQuery = null;
        }

        public RenderManager RenderManager {
            get;
            private set;
        }

        new public bool IsReleased { get; private set; }        

        new public void Add (Batch batch) {
            if (batch == null)
                throw new ArgumentNullException("batch");
            if (batch.Container != null)
                throw new InvalidOperationException("This batch is already in another container.");

            batch.Container = this;
            base.Add(ref batch);
        }

        protected override void OnReleaseResources () {
            IsReleased = true;
            OcclusionQuery = null;

            for (int i = 0, c = _DrawCalls.Count; i < c; i++) {
                var batch = _DrawCalls[i];
                if (batch != null)
                    batch.ReleaseResources();
            }

            _DrawCalls.Clear();

            base.OnReleaseResources();
        }

        public override string ToString () {
            return string.Format("{4} {0} #{1} {2} material={3}", Name, Index, StateString, Material, (this is RenderTargetBatchGroup) ? "RT Batch" : "Batch");
        }
    }

    public class RenderTargetBatchGroup : BatchGroup {
#if DEBUG
        public bool IgnoreInvalidRenderTargets = false;
#else
        public bool IgnoreInvalidRenderTargets = true;
#endif

        internal RenderTarget2D Single;
        internal AutoRenderTarget SingleAuto;
        internal RenderTargetBinding[] Multiple;

        public override void Issue (DeviceManager manager) {
            var single = Single;
            if (SingleAuto != null)
                single = SingleAuto.Get();

            var isValid = true;

            if (Multiple != null) {
                foreach (var rt in Multiple)
                    if (!AutoRenderTarget.IsRenderTargetValid(rt.RenderTarget))
                        isValid = false;

                if (!isValid) {
                    if (IgnoreInvalidRenderTargets) {
                        MarkAsIssued();
                        return;
                    } else
                        throw new ObjectDisposedException("Invalid render target for batch group " + Name);
                }

                manager.PushRenderTargets(Multiple);
            } else {
                if (single != null && (!AutoRenderTarget.IsRenderTargetValid(single)))
                    isValid = false;

                if (!isValid) {
                    if (IgnoreInvalidRenderTargets) {
                        MarkAsIssued();
                        return;
                    } else
                        throw new ObjectDisposedException("Invalid render target for batch group " + Name);
                }

                manager.PushRenderTarget(single);
            }

            try {
                base.Issue(manager);
            } finally {
                manager.PopRenderTarget();
            }
        }
    }
}
