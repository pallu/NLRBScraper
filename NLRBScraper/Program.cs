using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using System.Net;
using System.Web;

namespace NLRBScraper
{
    internal class Program
    {
        static string baseHref = "https://www.nlrb.gov";
        static string url = "https://www.nlrb.gov/advanced-search";
        static CookieContainer cookieContainer = new CookieContainer();
        static void Main(string[] args)
        {
            using var handler = new HttpClientHandler() { CookieContainer = cookieContainer };
            using var httpClient = new HttpClient(handler);
            var retInit = GetInitial(httpClient).Result;
            Pass2(httpClient, retInit).Wait();
            Console.WriteLine("Hello, World!");
        }

        static async Task<(string formBuildId, string formId)> GetInitial(HttpClient httpClient)
        {
            (string formBuildId, string formId) elForms = new();
            //using var httpClient = new HttpClient();
            var httpResponseMessage = await httpClient.GetAsync(url);
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                var body = await httpResponseMessage.Content.ReadAsStringAsync();
                IConfiguration configuration = Configuration.Default;
                IBrowsingContext context = BrowsingContext.New(configuration);
                IDocument document = await context.OpenAsync(req => req.Content(body));
                string[] formFields = { "form_build_id", "form_id" };
                //var formBuildId = document.All.Where(x => x.HasAttribute("name") && x.Attributes["name"].Value=="form_build_id").ToList();// .DocumentElement.Qu .QuerySelector("form_build_id");
                var formName = "nlrb-foia-report-type-form";
                var form = document.All.FirstOrDefault(x => x.Id == formName);
                if(form is not null)
                {
                    var els = form.QuerySelectorAll<IHtmlInputElement>("*")
                        .Where(x => x.Attributes["name"] is not null && formFields.Contains(x.Attributes["name"].Value))
                        .ToList();
                    elForms.formBuildId = els.FirstOrDefault(x => x.Attributes["name"].Value == "form_build_id")?.Attributes["value"]?.Value;
                    elForms.formId = els.FirstOrDefault(x => x.Attributes["name"].Value == "form_id")?.Attributes["value"]?.Value;
                }
                //return body;
            }
            return elForms;
        }
        static async Task Pass1(HttpClient httpClient, (string formBuildId, string formId) input)
        {
            //using var httpClient = new HttpClient();
            var formVariables = new List<KeyValuePair<string, string>>();
            formVariables.Add(new ("foia_report_type", "cases_and_decisions"));
            formVariables.Add(new ("form_build_id", input.formBuildId));
            formVariables.Add(new("form_id", input.formId));
            var formContent = new FormUrlEncodedContent(formVariables);
            try
            {
                var httpResponseMessage = await httpClient.PostAsync(url, formContent);
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    var body = await httpResponseMessage.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }
        static async Task Pass2(HttpClient httpClient, (string formBuildId, string formId) input)
        {
            //using var httpClient = new HttpClient();
            var builder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(builder.Query);
            //foia_report_type=cases_and_decisions&search_term=&&from_date=2019-01-01&to_date=2024-07-03&case_status=-1&case_type=-1&&&&sort_by=date_filed&order_by=desc&items_per_page=20&submit=Search&reset=Reset&form_build_id=form-SmrK3JEafoCLMV_7_FNKn9bWskTrDj4cquUcruMMo88&form_id=nlrb_foia_search_form&op=Search
            query["foia_report_type"] = "cases_and_decisions";
            query["search_term"] = null;
            query["from_date"] = "2019-01-01";
            query["to_date"] = DateTime.Today.ToString("yyyy-MM-dd");
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
                var response = await httpClient.GetAsync(urlFinal);
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    IConfiguration configuration = Configuration.Default;
                    IBrowsingContext context = BrowsingContext.New(configuration);
                    IDocument document = await context.OpenAsync(req => req.Content(body).Address(new Url(baseHref)));
                    var dnld = document.QuerySelectorAll<IHtmlAnchorElement>("*").Where(x => x.Attributes["id"] is not null && x.Attributes["id"].Value == "ads-download-button").FirstOrDefault();
                    if(dnld is not null)
                    {
                        var href = dnld.Href;
                        var resp2 = await httpClient.GetAsync(href);
                        if (resp2.IsSuccessStatusCode) 
                        { 
                            
                        }
                    }
                }
            }
            
            catch (Exception ex)
            {

                throw;
            }
        }
    }
}
