using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FubuCore.Binding;
using FubuMVC.Core;
using FubuMVC.Core.Runtime;
using Spark;

namespace FubuMVC.Spark.Rendering
{
    public class NestedOutput
    {
        private Func<TextWriter> _writer;

        public void SetWriter(Func<TextWriter> writer)
        {
            _writer = writer;
        }
        public TextWriter Writer { get { return _writer(); } }

        public bool IsActive()
        {
            return _writer != null;
        }
    }

    public class SparkItemDescriptors
    {
        public SparkItemDescriptors(SparkViewDescriptor viewDescriptor, SparkViewDescriptor partialDescriptor)
        {
            ViewDescriptor = viewDescriptor;
            PartialDescriptor = partialDescriptor;
        }

        public SparkViewDescriptor ViewDescriptor { get; private set; }
        public SparkViewDescriptor PartialDescriptor { get; private set; }
    }

    public interface IViewEngine
    {
        ISparkViewEntry GetViewEntry();
        ISparkViewEntry GetPartialViewEntry();
    }

    public class ViewEngine : IViewEngine
    {
        private readonly IDictionary<int, ISparkViewEntry> _cache;
        private readonly SparkItemDescriptors _descriptors;
        private readonly ISparkViewEngine _engine;

        public ViewEngine(IDictionary<int, ISparkViewEntry> cache, SparkItemDescriptors descriptors, ISparkViewEngine engine)
        {
            _cache = cache;
            _engine = engine;
            _descriptors = descriptors;
        }

        public ISparkViewEntry GetViewEntry()
        {
            var entry = getEntry(_descriptors.ViewDescriptor);
            return entry;
        }
        public ISparkViewEntry GetPartialViewEntry()
        {
            var entry = getEntry(_descriptors.PartialDescriptor);
            return entry;   
        }

        private ISparkViewEntry getEntry(SparkViewDescriptor descriptor)
        {
            ISparkViewEntry entry;
            var key = descriptor.GetHashCode();

            _cache.TryGetValue(key, out entry);
            if (entry == null || !entry.IsCurrent())
            {
                entry = _engine.CreateEntry(descriptor);
                lock (_cache)
                {
                    _cache[key] = entry;
                }
            }
            return entry;
        }

    }

    public interface IViewFactory
    {
        ISparkView GetView();
        ISparkView GetPartialView();
    }
    public class ViewFactory : IViewFactory
    {
        private readonly IViewEngine _viewEngine;
        private readonly IEnumerable<ISparkViewModification> _modifications;

        public ViewFactory(IViewEngine viewEngine, IEnumerable<ISparkViewModification> modifications)
        {
            _modifications = modifications;
            _viewEngine = viewEngine;
        }

        public ISparkView GetView()
        {
            var view = _viewEngine.GetViewEntry().CreateInstance();
            applyModifications(view);
            return view;
        }

        public ISparkView GetPartialView()
        {
            var view = _viewEngine.GetPartialViewEntry().CreateInstance();
            applyModifications(view);
            return view;
        }

        private void applyModifications(ISparkView view)
        {
            _modifications
               .Where(m => m.Applies(view))
               .Each(m => m.Modify(view));
        }
    }

    public interface IViewRenderer
    {
        void Render();
    }
    public class PartialViewRenderer : IViewRenderer
    {
        private readonly IViewFactory _viewFactory;
        private readonly NestedOutput _nestedOutput;
        private readonly IOutputWriter _outputWriter;

        public PartialViewRenderer(IViewFactory viewFactory, NestedOutput nestedOutput, IOutputWriter outputWriter)
        {
            _viewFactory = viewFactory;
            _outputWriter = outputWriter;
            _nestedOutput = nestedOutput;
        }

        public void Render()
        {
            var partial = (SparkViewBase)_viewFactory.GetPartialView();
            var writer = new StringWriter();
            partial.Output = writer;
            _nestedOutput.SetWriter(() => partial.Output);
            partial.RenderView(writer);
            _outputWriter.WriteHtml(writer);
        }
    }
    public class NestedViewRenderer : IViewRenderer
    {
        private readonly IViewFactory _viewFactory;
        private readonly NestedOutput _nestedOutput;

        public NestedViewRenderer(IViewFactory viewFactory, NestedOutput nestedOutput)
        {
            _viewFactory = viewFactory;
            _nestedOutput = nestedOutput;
        }

        public void Render()
        {
            var partial = _viewFactory.GetPartialView();
            partial.RenderView(_nestedOutput.Writer);
        }
    }
    public class DefaultViewRenderer : IViewRenderer
    {
        private readonly IViewFactory _viewFactory;
        private readonly NestedOutput _nestedOutput;
        private readonly IOutputWriter _outputWriter;

        public DefaultViewRenderer(IViewFactory viewFactory, NestedOutput nestedOutput, IOutputWriter outputWriter)
        {
            _viewFactory = viewFactory;
            _outputWriter = outputWriter;
            _nestedOutput = nestedOutput;
        }

        public void Render()
        {
            var view = (SparkViewBase)_viewFactory.GetView();
            var writer = new StringWriter();
            view.Output = writer;
            _nestedOutput.SetWriter(() => view.Output);
            view.RenderView(writer);
            _outputWriter.WriteHtml(writer);
        }
    }

    public interface IRenderStrategy
    {
        bool Applies();
        void Invoke();
    }
    public class NestedRenderStrategy : IRenderStrategy
    {
        private readonly NestedOutput _nestedOutput;
        private readonly IViewRenderer _viewRenderer;

        public NestedRenderStrategy(NestedOutput nestedOutput, IViewRenderer viewRenderer)
        {
            _viewRenderer = viewRenderer;
            _nestedOutput = nestedOutput;
        }

        public bool Applies()
        {
            return _nestedOutput.IsActive();
        }

        public void Invoke()
        {
            _viewRenderer.Render();
        }
    }
    public class AjaxRenderStrategy : IRenderStrategy
    {
        private readonly IViewRenderer _viewRenderer;
        private readonly IRequestData _requestData;

        public AjaxRenderStrategy(IViewRenderer viewRenderer, IRequestData requestData)
        {
            _viewRenderer = viewRenderer;
            _requestData = requestData;
        }

        public bool Applies()
        {
            return _requestData.IsAjaxRequest();
        }

        public void Invoke()
        {
            _viewRenderer.Render();
        }
    }
    public class DefaultRenderStrategy : IRenderStrategy
    {
        private readonly IViewRenderer _viewRenderer;

        public DefaultRenderStrategy(IViewRenderer viewRenderer)
        {
            _viewRenderer = viewRenderer;
        }

        public bool Applies()
        {
            return true;
        }

        public void Invoke()
        {
            _viewRenderer.Render();
        }
    }
}