using FubuCore.Binding;
using FubuMVC.Core;

namespace FubuMVC.Razor.Rendering
{
    public interface IRenderStrategy
    {
        bool Applies();
        void Invoke(IRenderAction action);
    }

    public class DefaultRenderStrategy : IRenderStrategy
    {
        public bool Applies() { return true; }
        public void Invoke(IRenderAction action) { action.Render(); }
    }

    public class AjaxRenderStrategy : IRenderStrategy
    {
        private readonly IRequestData _requestData;		
        public AjaxRenderStrategy(IRequestData requestData)
        {
            _requestData = requestData;
        }

        public bool Applies()
        {
            return _requestData.IsAjaxRequest();
        }

        public void Invoke(IRenderAction action)
        {
            action.RenderPartial();
        }
    }
}