using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LightBDD.Notification;
using LightBDD.Results;
using LightBDD.Results.Implementation;

namespace LightBDD.Execution.Implementation
{
    internal class ScenarioExecutor : IScenarioExecutor
    {
        private readonly IProgressNotifier _progressNotifier;
        public event Action<IScenarioResult> ScenarioExecuted;

        public ScenarioExecutor(IProgressNotifier progressNotifier)
        {
            _progressNotifier = progressNotifier;
        }

        [DebuggerStepThrough]
        public void Execute(Scenario scenario, IEnumerable<IStep> steps)
        {
            _progressNotifier.NotifyScenarioStart(scenario.Name, scenario.Label);
            var stepsToExecute = PrepareSteps(scenario, steps);

            var watch = new Stopwatch();
            var scenarioStartTime = DateTimeOffset.UtcNow;
            try
            {
                ExecutionContext.Instance = new ExecutionContext(_progressNotifier, stepsToExecute.Length);
                watch.Start();
                ExecuteSteps(stepsToExecute);
            }
            finally
            {
                watch.Stop();
                ExecutionContext.Instance = null;
                var result = new ScenarioResult(scenario.Name, stepsToExecute.Select(s => s.GetResult()), scenario.Label, scenario.Categories)
                .SetExecutionStart(scenarioStartTime)
                .SetExecutionTime(watch.Elapsed);

                if (ScenarioExecuted != null)
                    ScenarioExecuted.Invoke(result);

                _progressNotifier.NotifyScenarioFinished(result);
            }
        }
		public Task ExecuteAsync(Scenario scenario, IEnumerable<IAsyncStep> steps) {
            _progressNotifier.NotifyScenarioStart(scenario.Name, scenario.Label);
            var stepsToExecute = PrepareSteps(scenario, steps);

            var watch = new Stopwatch();
            var scenarioStartTime = DateTimeOffset.UtcNow;
			ExecutionContext.Instance = new ExecutionContext(_progressNotifier, stepsToExecute.Length);
			watch.Start();
			return ExecuteStepsAsync(stepsToExecute).ContinueWith(t => {
                watch.Stop();
                ExecutionContext.Instance = null;
                var result = new ScenarioResult(scenario.Name, stepsToExecute.Select(s => s.GetResult()), scenario.Label, scenario.Categories)
                .SetExecutionStart(scenarioStartTime)
                .SetExecutionTime(watch.Elapsed);

                if (ScenarioExecuted != null)
                    ScenarioExecuted.Invoke(result);

                _progressNotifier.NotifyScenarioFinished(result);
				return t;
			});
		}

        [DebuggerStepThrough]
        private T[] PrepareSteps<T>(Scenario scenario, IEnumerable<T> steps)
        {
            try
            {
                return steps.ToArray();
            }
            catch (Exception e)
            {
                var result = new ScenarioResult(scenario.Name, new IStepResult[0], scenario.Label, scenario.Categories)
                    .SetFailureStatus(e);

                if (ScenarioExecuted != null)
                    ScenarioExecuted.Invoke(result);

                _progressNotifier.NotifyScenarioFinished(result);
                throw;
            }
        }

        private void ExecuteSteps(IStep[] stepsToExecute)
        {
            foreach (var step in stepsToExecute)
                step.Invoke(ExecutionContext.Instance);
        }

		private Task ExecuteStepsAsync(IAsyncStep[] stepsToExecute) {
			if (!stepsToExecute.Any())
				return CompletedTask();
			return stepsToExecute.First().Invoke(ExecutionContext.Instance).ContinueWith(t => {
				if (t.Status != TaskStatus.RanToCompletion)
					return t;
				return ExecuteStepsAsync(stepsToExecute.Skip(1).ToArray());
			});
        }

		private static Task CompletedTask() => FromResult(0);
		private static Task<T> FromResult<T>(T value) {
			var tcs = new TaskCompletionSource<T>();
			tcs.SetResult(value);
			return tcs.Task;
		}
	}
}