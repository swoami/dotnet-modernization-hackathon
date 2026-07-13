using System.ServiceModel;

namespace ContosoInsurance.Services
{
    [ServiceContract]
    public interface IClaimScoringService
    {
        [OperationContract]
        int ScoreClaim(int claimId);

        [OperationContract]
        string GetModelVersion();
    }
}
