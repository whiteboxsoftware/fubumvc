using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FubuMVC.Core.Registration.Nodes;

namespace FubuMVC.Core.Registration
{
    /// <summary>
    /// Implementations of this contract inspect a given <see cref="TypePool"/>
    /// and provide any number of <see cref="ActionCall"/> instances.
    /// </summary>
    public interface IActionSource
    {
        IEnumerable<ActionCall> FindActions(Assembly applicationAssembly);
    }

    public class ActionSources
    {
        private readonly IList<IActionSource> _sources = new List<IActionSource>();
 
        public void AddSource(IActionSource source)
        {
            _sources.Add(source);
        }

        public IEnumerable<IActionSource> AllSources()
        {
            return _sources.Any() ? _sources : new IActionSource[] { new EndpointActionSource() };
        } 
    }
}