using System;
using System.IO;
using System.Web.UI;
using ContosoInsurance.Common.Config;
using ContosoInsurance.Common.Logging;

namespace ContosoInsurance.Web
{
    public partial class Upload : Page
    {
        protected void SubmitBtn_Click(object sender, EventArgs e)
        {
            if (!FileUploadCtrl.HasFile)
            {
                StatusLabel.Text = "No file selected.";
                return;
            }

            var root = ConfigHelper.GetSetting("ClaimDocumentsRoot", @"C:\ClaimsFiles");
            var maxBytes = ConfigHelper.GetInt("MaxUploadBytes", 10 * 1024 * 1024);

            if (FileUploadCtrl.PostedFile.ContentLength > maxBytes)
            {
                StatusLabel.Text = "File too large.";
                return;
            }

            var claimId = ClaimIdBox.Text.Trim();
            var folder = Path.Combine(root, claimId);
            Directory.CreateDirectory(folder);

            // LEGACY: filename taken directly from client — path traversal risk.
            var target = Path.Combine(folder, FileUploadCtrl.FileName);
            FileUploadCtrl.PostedFile.SaveAs(target);

            AppLogger.Info("Saved claim document " + target);
            StatusLabel.Text = "Uploaded to " + target;
        }
    }
}
