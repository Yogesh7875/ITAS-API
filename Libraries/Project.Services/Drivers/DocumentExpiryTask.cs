using Project.Services.Tasks;

namespace Project.Services.Drivers;

/// <summary>Run daily. Register it as a ScheduleTask row (Type = this class's full name).</summary>
public partial class DocumentExpiryTask : IScheduleTask
{
    private readonly IDriverOnboardingService _onboardingService;

    public DocumentExpiryTask(IDriverOnboardingService onboardingService)
    {
        _onboardingService = onboardingService;
    }

    async System.Threading.Tasks.Task IScheduleTask.ExecuteAsync()
    {
        await _onboardingService.MarkExpiredDocumentsAsync();
    }
}
