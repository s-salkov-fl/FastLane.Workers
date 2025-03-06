namespace FastLane.Workers.JsonTemplates;

public class WorkerResultTemplates
{
	public const string EmptyResult = "{{}}";
	public const string NotCompletedResult = "{{\"ExceptionMessage\": \"Worker execution was stopped before completion task\",\"WorkerTypeName\": \"{0}\"}}";
}
