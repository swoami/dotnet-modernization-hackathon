using System;
using System.ServiceModel;
using System.Web.UI;
using ContosoInsurance.Common.Config;
using ContosoInsurance.Common.Logging;
using ContosoInsurance.Data;
using ContosoInsurance.Services;

namespace ContosoInsurance.Web
{
    public partial class Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var repo = new ClaimsRepository();
            var recent = repo.GetRecent(50);

            // Fire-and-forget WCF call to score any un-scored claims
            foreach (var c in recent)
            {
                if (c.Score.HasValue) continue;
                try
                {
                    var score = CallScoringService(c.ClaimId);
                    repo.UpdateScore(c.ClaimId, score);
                    c.Score = score;
                }
                catch (Exception ex)
                {
                    AppLogger.Error("Scoring failed for claim " + c.ClaimId, ex);
                }
            }

            ClaimsGrid.DataSource = recent;
            ClaimsGrid.DataBind();
        }

        private static int CallScoringService(int claimId)
        {
            var endpoint = ConfigHelper.GetSetting("ClaimScoringEndpoint");
            var binding = new BasicHttpBinding();
            var address = new EndpointAddress(endpoint);
            var factory = new ChannelFactory<IClaimScoringService>(binding, address);
            var channel = factory.CreateChannel();
            try
            {
                return channel.ScoreClaim(claimId);
            }
            finally
            {
                ((IClientChannel)channel).Close();
                factory.Close();
            }
        }
    }
}
