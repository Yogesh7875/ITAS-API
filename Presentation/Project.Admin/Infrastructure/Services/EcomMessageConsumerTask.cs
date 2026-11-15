using Amazon.SQS.Model;
using Project.Core;
using ServiceTask = Project.Services.Tasks;

namespace Project.Admin.Infrastructure.Services;

/// <summary>
/// Represents POS message consumer task
/// </summary>
public class EcomMessageConsumerTask : ServiceTask.IScheduleTask
{
    #region Fields

    private readonly IHttpContextAccessor _httpContextAccessor;
    #endregion

    #region Constructor

    public EcomMessageConsumerTask(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;

    }

    #endregion

    #region Utilities

    protected async Task ProcessReceivedEcomMessagesAsync(ReceiveMessageResponse response)
    {
        if (response == null)
            throw new ArgumentNullException(nameof(response));
        //if (_sqsClientFactory == null)
        //    throw new ArgumentNullException(nameof(_sqsClientFactory));

        foreach (var message in response.Messages)
        {
            var group = message.Attributes["MessageGroupId"];
            if (string.IsNullOrWhiteSpace(group))
                continue;
            if (string.IsNullOrEmpty(message.Body))
                continue;

            switch (group)
            {
                case SQSGroup.ECOMTOCENTRALINSERTCUSTOMER:
                    {
                        await ProcessMessageWithAutoDelete(async () =>
                        {
                            //var model = JsonConvert.DeserializeObject<CustomerModel>(message.Body);
                            //model.IsFromEcom = true;
                            //model.EcomId = model.Id;
                            //model.Id = 0;
                            //await _customerFactory.InsertCustomer(model);
                        }, message.ReceiptHandle, group, message.Body);
                        break;

                    }

                default:
                    break;

            }

        }

    }


    /// <summary>
    /// handle msg and delete after completion
    /// </summary>
    /// <param name="processAction"></param>
    /// <param name="receiptHandle"></param>
    /// <param name="group"></param>
    /// <param name="messageBody"></param>
    /// <returns></returns>
    private async Task ProcessMessageWithAutoDelete(Func<Task> processAction, string receiptHandle, string group, string messageBody)
    {
        try
        {
            await processAction();
        }
        catch (Exception)
        {
            //var failedSqsModel = new FailedSqsLog { MessageBody = messageBody, Exception = ex.ToString(), Group = group, CreatedOn = DateTime.Now };
            //await _sQSLogService.InsertFailedSqsLogAsync(failedSqsModel);
        }
        finally
        {
            //await _sqsClientFactory.DeleteMessagesFromPosAsync(receiptHandle);
        }
    }


    #endregion

    #region Methods

    public async Task ExecuteAsync()
    {
        //var response = await _sqsClientFactory.ReceiveMessagesFromEcomAsync(numberOfMessage: 1);

        //if ((response.HttpStatusCode != HttpStatusCode.OK) || (response.Messages.Count == 0))
        //{
        //    return;
        //}

        //await ProcessReceivedEcomMessagesAsync(response);
    }

    #endregion
}
