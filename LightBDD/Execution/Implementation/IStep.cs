using System.Threading.Tasks;
using LightBDD.Results;

namespace LightBDD.Execution.Implementation
{
    internal interface IStepInfo
    {
        IStepResult GetResult();
        void Comment(ExecutionContext context, string comment);
    }
    internal interface IStep : IStepInfo
    {
        void Invoke(ExecutionContext context);
    }
    internal interface IAsyncStep : IStepInfo
    {
        Task Invoke(ExecutionContext context);
    }
}