using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

using Xamarin.Forms;

namespace XamarinFormsClient.Core
{
    //[XamlCompilation(XamlCompilationOptions.Skip)]
    public partial class MainPage : ContentPage
    {
        OidcClient _client;
        LoginResult _result;

        Lazy<HttpClient> _apiClient = new Lazy<HttpClient>(() => new HttpClient());

        public MainPage()
        {
            InitializeComponent();

            Login.Clicked += Login_Clicked;
            CallApi.Clicked += CallApi_Clicked;

            var browser = DependencyService.Get<IBrowser>();

            var options = new OidcClientOptions
            {
                Authority = "https://moonbrook.area52.local/IdentityServer4",
                ClientId = "interactive.public",
                Scope = "openid profile api1 offline_access",
                RedirectUri = "xamarinformsclients://callback",
                Browser = browser,

                ResponseMode = OidcClientOptions.AuthorizeResponseMode.Redirect
            };

            _client = new OidcClient(options);
            _apiClient.Value.BaseAddress = new Uri("https://moonbrook.area52.local/IdentityServer4Api/");
        }

        private async void Login_Clicked(object sender, EventArgs e)
        {
            try
            {
                _result = await _client.LoginAsync(new LoginRequest());

                if (_result.IsError)
                {
                    OutputText.Text = _result.Error;
                    return;
                }

                var sb = new StringBuilder(128);
                foreach (var claim in _result.User.Claims)
                {
                    sb.AppendFormat("{0}: {1}\n", claim.Type, claim.Value);
                }

                sb.AppendFormat("\n{0}: {1}\n", "refresh token", _result?.RefreshToken ?? "none");
                sb.AppendFormat("\n{0}: {1}\n", "access token", _result.AccessToken);

                OutputText.Text = sb.ToString();

                _apiClient.Value.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_result?.TokenResponse.TokenType ?? "Bearer", _result?.AccessToken ?? "");
            }
            catch (Exception ex)
            {
                OutputText.Text = ex.ToString();
            }
        }

        private async void CallApi_Clicked(object sender, EventArgs e)
        {
            try
            {
                var result = await _apiClient.Value.GetAsync("identity");

                if (result.IsSuccessStatusCode)
                {
                    OutputText.Text = JArray.Parse(await result.Content.ReadAsStringAsync()).ToString();
                }
                else
                {
                    OutputText.Text = result.ReasonPhrase;
                }
            }
            catch (Exception ex)
            {
                OutputText.Text = ex.ToString();
            }
        }
    }
}