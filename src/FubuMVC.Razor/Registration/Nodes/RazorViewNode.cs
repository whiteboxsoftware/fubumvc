﻿using System;
using FubuMVC.Core.Registration.Nodes;
using FubuMVC.Core.Registration.ObjectGraph;
using FubuMVC.Core.View.Rendering;
using FubuMVC.Razor.Rendering;
using FubuMVC.Razor.RazorModel;

namespace FubuMVC.Razor.Registration.Nodes
{
    public class RazorViewNode : OutputNode<RenderRazorBehavior>, IMayHaveInputType
    {
        private readonly IRazorDescriptor _descriptor;

        public RazorViewNode(IRazorDescriptor descriptor)
        {
            _descriptor = descriptor;
        }

        protected override void configureObject(ObjectDef def)
        {
            var renderer = def.DependencyByType(typeof (IViewRenderer), typeof (ViewRenderer));

            var viewEntrySource = renderer.DependencyByType(typeof (IRenderAction), typeof (RenderAction))
                .DependencyByType(typeof (IViewFactory), typeof (ViewFactory));

            viewEntrySource.DependencyByValue(_descriptor);
        }

        public override string Description
        {
            get { return string.Format("Razor [{0}]", _descriptor.RelativePath()); }
        }

        public Type InputType()
        {
            return _descriptor.ViewModel;
        }
    }
}