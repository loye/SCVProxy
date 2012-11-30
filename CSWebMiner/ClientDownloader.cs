using System.IO;
using System.Web;

namespace SCVProxy.CSWebMiner
{
    public class ClientDownloader : IHttpHandler
    {
        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            if (request.HttpMethod == "GET")
            {
                string fullName = HttpContext.Current.Server.MapPath(@"~\client\bin.zip");
                if (File.Exists(fullName))
                {
                    response.ContentType = "application/zip";
                    response.AddHeader("Content-Disposition", "filename=client.zip");
                    response.WriteFile(fullName);
                    response.End();
                    return;
                }
            }
            response.StatusCode = 404;
            response.End();
        }
    }
}
