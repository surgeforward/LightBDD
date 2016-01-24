using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LightBDD.Execution.Exceptions;
using LightBDD.Results;
using LightBDD.Results.Implementation;

namespace LightBDD.Execution.Implementation
{
	internal abstract class BaseStep {
        protected readonly Func<Type, ResultStatus> _mapping;
        protected readonly StepResult _result;
        public IStepResult GetResult() { return _result; }

        public BaseStep(string stepTypeName, string stepName, int stepNumber, Func<Type, ResultStatus> mapping)
        {
            _mapping = mapping;
            _result = new StepResult(stepNumber, new StepName(stepName, stepTypeName), ResultStatus.NotRun);
        }

        public void Comment(ExecutionContext context, string comment)
        {
            _result.AddComment(comment);
            context.ProgressNotifier.NotifyStepComment(_result.Number, context.TotalStepCount, comment);
        }

        public override string ToString()
        {
            return _result.ToString();
        }
	}
	internal class AsyncStep : BaseStep, IAsyncStep {
		private readonly Func<Task> _action;

		public AsyncStep(Func<Task> action, string stepTypeName, string stepName, int stepNumber, Func<Type, ResultStatus> mapping)
			: base(stepTypeName, stepName, stepNumber, mapping)
        {
            _action = action;
        }
		public Task Invoke(ExecutionContext context) {
			context.CurrentStep = this;
			context.ProgressNotifier.NotifyStepStart(_result.Name, _result.Number, context.TotalStepCount);
			_result.SetExecutionStart(DateTimeOffset.UtcNow);
			return MeasuredInvoke().ContinueWith(t => {
				var tcs = new TaskCompletionSource<int>();
				tcs.SetResult(0);
				Task result = tcs.Task;
				if (t.Status == TaskStatus.RanToCompletion)
					_result.SetStatus(ResultStatus.Passed);
				else {
					if (null == t.Exception)
						_result.SetStatus(ResultStatus.Failed, "Unknown failure reason");
					var bypass = t.Exception.InnerExceptions.FirstOrDefault(e => e is StepBypassException);
					if (null != bypass)
						_result.SetStatus(ResultStatus.Bypassed, bypass.Message);
					else {
						result = t;
						var e = t.Exception.InnerException;
						_result.SetStatus(_mapping(e.GetType()), e.Message);
					}
				}
				context.CurrentStep = null;
				context.ProgressNotifier.NotifyStepFinished(_result, context.TotalStepCount);
				return result;
			});
		}
        private Task MeasuredInvoke()
        {
            var watch = new Stopwatch();
			_result.SetExecutionStart(DateTimeOffset.UtcNow);
			watch.Start();
			return _action().ContinueWith(t => {
				_result.SetExecutionTime(watch.Elapsed);
				return t;
			});
        }

	}
	[DebuggerStepThrough]
    internal class Step : BaseStep, IStep
    {
        private readonly Action _action;

        public Step(Action action, string stepTypeName, string stepName, int stepNumber, Func<Type, ResultStatus> mapping)
			: base(stepTypeName, stepName, stepNumber, mapping) 
		{ 
            _action = action;
        }

        public void Invoke(ExecutionContext context)
        {
            try
            {
                context.CurrentStep = this;
                context.ProgressNotifier.NotifyStepStart(_result.Name, _result.Number, context.TotalStepCount);
                _result.SetExecutionStart(DateTimeOffset.UtcNow);
                MeasuredInvoke();
                _result.SetStatus(ResultStatus.Passed);
            }
            catch (StepBypassException e)
            {
                _result.SetStatus(ResultStatus.Bypassed, e.Message);
            }
            catch (Exception e)
            {
                _result.SetStatus(_mapping(e.GetType()), e.Message);
                throw;
            }
            finally
            {
                context.CurrentStep = null;
                context.ProgressNotifier.NotifyStepFinished(_result, context.TotalStepCount);
            }
        }


        private void MeasuredInvoke()
        {
            var watch = new Stopwatch();
            try
            {
                _result.SetExecutionStart(DateTimeOffset.UtcNow);
                watch.Start();
                _action();
            }
            finally
            {
                _result.SetExecutionTime(watch.Elapsed);
            }
        }
    }
}