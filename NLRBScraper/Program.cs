using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using System.Net;
using AngleSharp.Scripting;
using System.Web;
using System.Reflection.Metadata;
using System.Net.Http;
using AngleSharp.Js;
using Jint.Parser;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net.Http.Json;

namespace NLRBScraper
{
    internal class Program
    {
        static string fromDate = DateTime.Today.Subtract(TimeSpan.FromDays(7)).ToString("yyyy-MM-dd");
        static string toDate = DateTime.Today.ToString("yyyy-MM-dd");
        static string baseHref = "https://www.nlrb.gov";
        static string url = "https://www.nlrb.gov/advanced-search";
        static string cookieName = "nlrb-dl-sessid";
        //static CookieContainer cookieContainer = new CookieContainer();
        static IConfiguration configuration;
        static IBrowsingContext context;
        static void Main(string[] args)
        {
            //using var handler = new HttpClientHandler() { CookieContainer = cookieContainer };
            //using var httpClient = new HttpClient(handler);
            //var retInit = GetInitial(httpClient).Result;
            //Pass2(httpClient, retInit).Wait();
            //Console.WriteLine("Hello, World!");
            configuration = Configuration.Default.WithDefaultLoader().WithDefaultCookies().WithJs();
            context = BrowsingContext.New(configuration);

            //var token = CreateCookie();// Guid.NewGuid().ToString();
            var token = CookieGenerator.CreateCookie();
            context.SetCookie(new Url(baseHref), $"{cookieName}={token}");

            Console.WriteLine("Creating browsing context...");
            var retInit = GetInitialBrowsingContext().Result;
            var valCookie = ReturnCookies();

            Console.WriteLine($"Sending file request for {fromDate} to {toDate}");
            var dnld = Pass1BrowsingContext(retInit).Result;

            GetDownloadDetails().Wait();
            StartDownloads(dnld).Wait();
            Console.WriteLine("Finished");
            Console.WriteLine("Press any key to exit...");
            if (!Console.IsInputRedirected && Console.KeyAvailable)
            {
                Console.ReadKey();
            }


        }
        //static string CreateCookie()
        //{
        //    string token = "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx";
        //    token = Regex.Replace(token, "[xy]", (match) =>
        //    {
        //        int r = new Random().Next(16);
        //        int v = match.Value == "x" ? r : (r & 0x3 | 0x8);
        //        return v.ToString("x");
        //    });
        //    return token;
        //}
        static Dictionary<string,string> ReturnCookies() => context.GetCookie(new Url(baseHref)).Split(';').Select(x => x.Trim().Split('=')).ToDictionary(x => x[0], x => x[1]);
        static async Task<(string formBuildId, string formId)> GetInitialBrowsingContext() {
            (string formBuildId, string formId) elForms = new();
            var document = await context.OpenAsync(url);
            string[] formFields = { "form_build_id", "form_id" };
            //var formBuildId = document.All.Where(x => x.HasAttribute("name") && x.Attributes["name"].Value=="form_build_id").ToList();// .DocumentElement.Qu .QuerySelector("form_build_id");
            var formName = "nlrb-foia-report-type-form";
            var form = document.All.FirstOrDefault(x => x.Id == formName);
            if (form is not null)
            {
                var els = form.QuerySelectorAll<IHtmlInputElement>("*")
                    .Where(x => x.Attributes["name"] is not null && formFields.Contains(x.Attributes["name"].Value))
                    .ToList();
                elForms.formBuildId = els.FirstOrDefault(x => x.Attributes["name"].Value == "form_build_id")?.Attributes["value"]?.Value;
                elForms.formId = els.FirstOrDefault(x => x.Attributes["name"].Value == "form_id")?.Attributes["value"]?.Value;
            }
            return elForms;
        }
        static async Task<(string urlDnld, string cacheid, string typeofreport)> Pass1BrowsingContext((string formBuildId, string formId) input)
        {
            (string urlDnld, string cacheid, string typeofreport) retVal = new();
            //using var httpClient = new HttpClient();
            var builder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(builder.Query);
            //foia_report_type=cases_and_decisions&search_term=&&from_date=2019-01-01&to_date=2024-07-03&case_status=-1&case_type=-1&&&&sort_by=date_filed&order_by=desc&items_per_page=20&submit=Search&reset=Reset&form_build_id=form-SmrK3JEafoCLMV_7_FNKn9bWskTrDj4cquUcruMMo88&form_id=nlrb_foia_search_form&op=Search
            query["foia_report_type"] = "cases_and_decisions";
            query["search_term"] = null;
            query["from_date"] = fromDate;// "2019-01-01";
            query["to_date"] = toDate;// DateTime.Today.ToString("yyyy-MM-dd");
            query["case_status"] = "-1";
            query["case_type"] = "-1";
            query["sort-by"] = "date_filed";
            query["order_by"] = "desc";
            query["items_per_page"] = "20";
            query["submit"] = "Search";
            query["reset"] = "Reset";
            query["form_build_id"] = input.formBuildId;
            query["form_id"] = input.formId;
            query["op"] = "Search";
            builder.Query = query.ToString();
            string urlFinal = builder.ToString();

            try
            {
                var document = await context.OpenAsync(urlFinal).WaitUntilAvailable();
                //document.ExecuteScript("getCookie");

                var cookies = ReturnCookies();
                if (!cookies.ContainsKey(cookieName))
                {
                    var token = Guid.NewGuid().ToString();//.Replace("-", "").Substring(0, 32);
                    cookies.Add(cookieName, token);
                    var newCookie = String.Join(';', cookies.Select(x=>$"{x.Key}={x.Value}"));
                    context.SetCookie(new Url(baseHref), newCookie);
                    var c = context.GetCookie(new Url(baseHref));
                    var newCookies = ReturnCookies();
                }
                
                
                

                var dnld = document.QuerySelectorAll<IHtmlAnchorElement>("*").Where(x => x.Attributes["id"] is not null && x.Attributes["id"].Value == "ads-download-button").FirstOrDefault();
                if (dnld is not null)
                {
                    retVal.urlDnld = dnld.Href;
                    retVal.cacheid = dnld.GetAttribute("data-cacheid");
                    retVal.typeofreport = dnld.GetAttribute("data-typeofreport");
                    //cookies = ReturnCookies();

                    
                }
                
            }

            catch (Exception ex)
            {

                throw;
            }
            return retVal;
        }

        static async Task GetDownloadDetails()
        {
            Console.WriteLine("Getting download details...");
            try
            {
               
                var builder = new UriBuilder(new Uri(new Uri(baseHref), "/nlrb-downloads/load-user-downloads"));
                var query = HttpUtility.ParseQueryString(builder.Query);
                query["token"] = ReturnCookies()[cookieName];
                builder.Query = query.ToString();
                string urlFinal = builder.ToString();

                var document = await context.OpenAsync(urlFinal).WaitUntilAvailable();
                var dnlddetails = JsonSerializer.Deserialize<DownloadDetails>(document.Text().AsSpan());
            }
            catch (Exception ex)
            {

                //throw;
            }
            
        }
        static async Task StartDownloads((string urlDnld, string cacheid, string typeofreport) dnldDetails)
        {
            Console.WriteLine("Starting download process...");
            var download_token = ReturnCookies()[cookieName];
            //var url = new Uri(new Uri(baseHref), $"/nlrb-downloads/start-download/{dnldDetails.typeofreport}/{dnldDetails.cacheid}/{download_token}");
            var url = $"/nlrb-downloads/start-download/{dnldDetails.typeofreport}/{dnldDetails.cacheid}/{download_token}";

            CookieContainer cookieContainer = new CookieContainer();
            foreach (var item in ReturnCookies())
            {
                cookieContainer.Add(new Uri(baseHref), new Cookie(item.Key, item.Value));
            }

            Console.WriteLine("Initiating download...");
            using var handler = new HttpClientHandler() { CookieContainer = cookieContainer };
            using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(baseHref) };
            var dnld = await httpClient.GetFromJsonAsync<DownloadDetails>(new Uri(new Uri(baseHref), url));

            Console.WriteLine($"Processed: {dnld.data.processed}, Progress: {dnld.data.progress}");
            var urlProgress = $"/nlrb-downloads/progress/{dnld.data.id}";
            while (dnld.data.finished!=1)
            {
                dnld = await httpClient.GetFromJsonAsync<DownloadDetails>(urlProgress);
                Console.WriteLine($"Processed: {dnld.data.processed}, Progress: {dnld.data.progress}");
                Thread.Sleep(2000);
            }
            var dnldUrl = dnld.data.filename;
            var dnldFolder = Path.Combine(AppContext.BaseDirectory, "Downloads");
            if (!Directory.Exists(dnldFolder))
            {
                Directory.CreateDirectory(dnldFolder);
            }
            var fileName = Path.GetFileName(dnld.data.filename);
            var filePath = Path.Combine(dnldFolder, fileName);
            using var stream = await httpClient.GetStreamAsync(dnldUrl);
            using var fs = new FileStream(filePath, FileMode.Create);
            await stream.CopyToAsync(fs);
            Console.WriteLine($"Downloaded file to {filePath}");
        }
        
    }
}
