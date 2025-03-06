using FastLane.Workers.JsonTemplates;
using FastLane.Workers.Contracts;
using System.Text.Json;
using FastLane.Workers.Models;

namespace FastLane.Workers.Bucket.Services;

public class WorkerContainer
{
	public WorkerInstanceExecutionStatus Status { get; set; }
	public JsonElement Input { get; set; }
	public JsonElement Result { get; set; }
	public IWorker Worker { get; set; }
	public Guid WorkerInstanceId { get; set; }
	public string WorkerTypeName { get; set; }
	public DateTime startTime { get; set; }
	public DateTime finishTime { get; set; }
	public CancellationTokenSource cancellationTokenSource { get; set; }
	private ILogger<WorkerContainer> _logger { get; set; }

	public WorkerContainer(IWorker worker
		, ILogger<WorkerContainer> logger)
	{
		_logger = logger;
		Status = WorkerInstanceExecutionStatus.NotStarted;
		Input = new JsonElement();
		Result = new JsonElement();
		Worker = worker;
		WorkerInstanceId = Guid.NewGuid();
		WorkerTypeName = worker?.GetType().FullName;

		cancellationTokenSource = new CancellationTokenSource();
	}

	public async Task RunAsync(JsonElement? input, CancellationToken cancellationToken)
	{
		JsonDocument JsonDocResult = null;

		try
		{
			startTime = DateTime.UtcNow;
			Status = WorkerInstanceExecutionStatus.Running;
			var output = await Worker.ExecuteAsync(input, cancellationToken);
			
			if (!output.HasValue)
			{
				JsonDocument doc = JsonDocument.Parse(WorkerResultTemplates.EmptyResult);
				output = doc.RootElement.Clone();
			}
			Result = output.Value;
			Status = WorkerInstanceExecutionStatus.FinishedSuccessfully;
		}
		catch (AggregateException aggregateEx) when (aggregateEx.InnerExceptions.Any(e=>e is OperationCanceledException))
		{
			_logger.LogInformation($"Aborted execution of Worker with instance Id:{WorkerInstanceId}, Type: {Worker.GetType()}");
			Status = WorkerInstanceExecutionStatus.Aborted;
			string messageTemplate = string.Format(WorkerResultTemplates.NotCompletedResult, aggregateEx.Message);
			JsonDocResult = JsonDocument.Parse(messageTemplate);
			Result = JsonDocResult.RootElement.Clone();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, $"Exception while executing Worker with instance Id:{WorkerInstanceId}, Type: {Worker.GetType()}" );
			Status = WorkerInstanceExecutionStatus.Failed;

			string messageTemplate = string.Format(ExceptionResultTemplates.DefaultException, ex.Message, ex.ToString());
			JsonDocResult = JsonDocument.Parse(messageTemplate);
			Result = JsonDocResult.RootElement.Clone();
		}
		finally 
		{ 
			JsonDocResult?.Dispose();
		}

		finishTime = DateTime.UtcNow;
	}
}
