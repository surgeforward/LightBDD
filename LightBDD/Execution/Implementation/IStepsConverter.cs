using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LightBDD.Execution.Implementation
{
    internal interface IStepsConverter
    {
        IEnumerable<IStep> Convert(IEnumerable<Action> steps);
        IEnumerable<IAsyncStep> Convert(IEnumerable<Func<Task>> steps);
        IEnumerable<IStep> Convert<TContext>(TContext context, IEnumerable<Action<TContext>> steps);
        IEnumerable<IAsyncStep> Convert<TContext>(TContext context, IEnumerable<Func<TContext, Task>> steps);
        IEnumerable<IStep> Convert<TContext>(TContext context, IEnumerable<Expression<Action<StepType,TContext>>> steps);
        IEnumerable<IStep> Convert(IEnumerable<Expression<Action<StepType>>> steps);
    }
}