
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using AutoMapper;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OAuth;
using Newtonsoft.Json.Linq;
using Smart.App_Start;
using Smart.Data;
using Smart.Models;
using Smart.Models.Entities;
using Smart.Repository;
using Smart.Results;
using Smart.Service;
using Smart.ViewModels;

namespace Smart.Controllers
{
    [RoutePrefix("api/Account")]
    public class AccountController : ApiController
    {
        private readonly IUserService _userService;

        private UserRepository _repo = null;

        private IAuthenticationManager Authentication
        {
            get { return Request.GetOwinContext().Authentication; }
        }

        public AccountController(IUserService service)
        {
            this._userService = service;
            _repo = new UserRepository();
        }

        // POST api/Account/Register
        [AllowAnonymous]
        [Route("Register")]
        //public async Task<IHttpActionResult> Register(UserViewModel userModel)
        public IHttpActionResult Register(UserViewModel userModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result = _userService.Add(userModel);

            IHttpActionResult errorResult = GetErrorResult(result);

            if (errorResult != null)
            {
                return errorResult;
            }

            return Ok();
        }

        // GET api/Account/ExternalLogin
        [OverrideAuthentication]
        [HostAuthentication(DefaultAuthenticationTypes.ExternalCookie)]
        [AllowAnonymous]
        [Route("ExternalLogin", Name = "ExternalLogin")]
        public async Task<IHttpActionResult> GetExternalLogin(string provider, string error = null)
        {
            string redirectUri = string.Empty;

            if (error != null)
            {
                return BadRequest(Uri.EscapeDataString(error));
            }

            if (!User.Identity.IsAuthenticated)
            {
                return new ChallengeResult(provider, this);
            }

            var redirectUriValidationResult = ValidateClientAndRedirectUri(this.Request, ref redirectUri);

            if (!string.IsNullOrWhiteSpace(redirectUriValidationResult))
            {
                return BadRequest(redirectUriValidationResult);
            }

            ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);

            if (externalLogin == null)
            {
                return InternalServerError();
            }

            if (externalLogin.LoginProvider != provider)
            {
                Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);
                return new ChallengeResult(provider, this);
            }

            IdentityUser user = await _repo.FindAsync(new UserLoginInfo(externalLogin.LoginProvider, externalLogin.ProviderKey));

            bool hasRegistered = user != null;

            redirectUri = string.Format("{0}#external_access_token={1}&provider={2}&haslocalaccount={3}&external_user_name={4}",
                                            redirectUri,
                                            externalLogin.ExternalAccessToken,
                                            externalLogin.LoginProvider,
                                            hasRegistered.ToString(),
                                            externalLogin.UserName);

            return Redirect(redirectUri);

        }

        // POST api/Account/RegisterExternal
        [AllowAnonymous]
        [Route("RegisterExternal")]
        public async Task<IHttpActionResult> RegisterExternal(ExternalLoginModels.RegisterExternalBindingModel model)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var verifiedAccessToken = await VerifyExternalAccessToken(model.Provider, model.ExternalAccessToken);
            if (verifiedAccessToken == null)
            {
                return BadRequest("Invalid Provider or External Access Token");
            }

            ApplicationUser user = await _repo.FindAsync(new UserLoginInfo(model.Provider, verifiedAccessToken.user_id));

            bool hasRegistered = user != null;

            if (hasRegistered)
            {
                return BadRequest("External user is already registered");
            }

            user = new ApplicationUser() { UserName = model.UserName };

            IdentityResult result = await _repo.CreateAsync(user);
            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            var info = new ExternalLoginInfo()
            {
                DefaultUserName = model.UserName,
                Login = new UserLoginInfo(model.Provider, verifiedAccessToken.user_id)
            };

            result = await _repo.AddLoginAsync(user.Id, info.Login);
            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            //generate access token response
            var accessTokenResponse = GenerateLocalAccessTokenResponse(model.UserName);

            return Ok(accessTokenResponse);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("ObtainLocalAccessToken")]
        public async Task<IHttpActionResult> ObtainLocalAccessToken(string provider, string externalAccessToken)
        {

            if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(externalAccessToken))
            {
                return BadRequest("Provider or external access token is not sent");
            }

            var verifiedAccessToken = await VerifyExternalAccessToken(provider, externalAccessToken);
            if (verifiedAccessToken == null)
            {
                return BadRequest("Invalid Provider or External Access Token");
            }

            IdentityUser user = await _repo.FindAsync(new UserLoginInfo(provider, verifiedAccessToken.user_id));

            bool hasRegistered = user != null;

            if (!hasRegistered)
            {
                return BadRequest("External user is not registered");
            }

            //generate access token response
            var accessTokenResponse = GenerateLocalAccessTokenResponse(user.UserName);

            return Ok(accessTokenResponse);

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _repo.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Helpers

        private IHttpActionResult GetErrorResult(IdentityResult result)
        {
            if (result == null)
            {
                return InternalServerError();
            }

            if (!result.Succeeded)
            {
                if (result.Errors != null)
                {
                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }

                if (ModelState.IsValid)
                {
                    // No ModelState errors are available to send, so just return an empty BadRequest.
                    return BadRequest();
                }

                return BadRequest(ModelState);
            }

            return null;
        }

        private string ValidateClientAndRedirectUri(HttpRequestMessage request, ref string redirectUriOutput)
        {

            Uri redirectUri;

            var redirectUriString = GetQueryString(Request, "redirect_uri");

            if (string.IsNullOrWhiteSpace(redirectUriString))
            {
                return "redirect_uri is required";
            }

            bool validUri = Uri.TryCreate(redirectUriString, UriKind.Absolute, out redirectUri);

            if (!validUri)
            {
                return "redirect_uri is invalid";
            }

            var clientId = GetQueryString(Request, "client_id");

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return "client_Id is required";
            }

            var client = _repo.FindClient(clientId);

            if (client == null)
            {
                return string.Format("Client_id '{0}' is not registered in the system.", clientId);
            }
            String leftPart = redirectUri.GetLeftPart(UriPartial.Authority);
            if (!string.Equals(client.AllowedOrigin, "*") && !string.Equals(client.AllowedOrigin, leftPart, StringComparison.OrdinalIgnoreCase))
            {
                return string.Format("The given URL is not allowed by Client_id '{0}' configuration.", clientId);
            }

            redirectUriOutput = redirectUri.AbsoluteUri;

            return string.Empty;

        }

        private string GetQueryString(HttpRequestMessage request, string key)
        {
            var queryStrings = request.GetQueryNameValuePairs();

            if (queryStrings == null) return null;

            var match = queryStrings.FirstOrDefault(keyValue => string.Compare(keyValue.Key, key, true) == 0);

            if (string.IsNullOrEmpty(match.Value)) return null;

            return match.Value;
        }

        private async Task<ExternalLoginModels.ParsedExternalAccessToken> VerifyExternalAccessToken(string provider, string accessToken)
        {
            ExternalLoginModels.ParsedExternalAccessToken parsedToken = null;

            var verifyTokenEndPoint = "";

            if (provider == "Facebook")
            {
                //You can get it from here: https://developers.facebook.com/tools/accesstoken/
                //More about debug_tokn here: http://stackoverflow.com/questions/16641083/how-does-one-get-the-app-access-token-for-debug-token-inspection-on-facebook
                var appToken = "185013055265833|ukQttTvv4-wZR9bszWYCcPLQL8s";
                verifyTokenEndPoint = string.Format("https://graph.facebook.com/debug_token?input_token={0}&access_token={1}", accessToken, appToken);
            }
            else if (provider == "Google")
            {
                verifyTokenEndPoint = string.Format("https://www.googleapis.com/oauth2/v1/tokeninfo?access_token={0}", accessToken);
            }
            else
            {
                return null;
            }

            var client = new HttpClient();
            var uri = new Uri(verifyTokenEndPoint);
            var response = await client.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();

                dynamic jObj = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(content);

                parsedToken = new ExternalLoginModels.ParsedExternalAccessToken();

                if (provider == "Facebook")
                {
                    parsedToken.user_id = jObj["data"]["user_id"];
                    parsedToken.app_id = jObj["data"]["app_id"];

                    if (!string.Equals(StartUp.facebookAuthOptions.AppId, parsedToken.app_id, StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }
                }
                else if (provider == "Google")
                {
                    parsedToken.user_id = jObj["user_id"];
                    parsedToken.app_id = jObj["audience"];

                    if (!string.Equals(StartUp.googleAuthOptions.ClientId, parsedToken.app_id, StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }

                }

            }

            return parsedToken;
        }

        private JObject GenerateLocalAccessTokenResponse(string userName)
        {

            var tokenExpiration = TimeSpan.FromDays(1);

            ClaimsIdentity identity = new ClaimsIdentity(OAuthDefaults.AuthenticationType);

            identity.AddClaim(new Claim(ClaimTypes.Name, userName));
            identity.AddClaim(new Claim("role", "user"));

            var props = new AuthenticationProperties()
            {
                IssuedUtc = DateTime.UtcNow,
                ExpiresUtc = DateTime.UtcNow.Add(tokenExpiration),
            };

            var ticket = new AuthenticationTicket(identity, props);

            var accessToken = StartUp.OAuthBearerOptions.AccessTokenFormat.Protect(ticket);

            JObject tokenResponse = new JObject(
                                        new JProperty("userName", userName),
                                        new JProperty("access_token", accessToken),
                                        new JProperty("token_type", "bearer"),
                                        new JProperty("expires_in", tokenExpiration.TotalSeconds.ToString()),
                                        new JProperty(".issued", ticket.Properties.IssuedUtc.ToString()),
                                        new JProperty(".expires", ticket.Properties.ExpiresUtc.ToString())
        );

            return tokenResponse;
        }

        private class ExternalLoginData
        {
            public string LoginProvider { get; set; }
            public string ProviderKey { get; set; }
            public string UserName { get; set; }
            public string ExternalAccessToken { get; set; }

            public static ExternalLoginData FromIdentity(ClaimsIdentity identity)
            {
                if (identity == null)
                {
                    return null;
                }

                Claim providerKeyClaim = identity.FindFirst(ClaimTypes.NameIdentifier);

                if (providerKeyClaim == null || String.IsNullOrEmpty(providerKeyClaim.Issuer) || String.IsNullOrEmpty(providerKeyClaim.Value))
                {
                    return null;
                }

                if (providerKeyClaim.Issuer == ClaimsIdentity.DefaultIssuer)
                {
                    return null;
                }

                return new ExternalLoginData
                {
                    LoginProvider = providerKeyClaim.Issuer,
                    ProviderKey = providerKeyClaim.Value,
                    UserName = identity.FindFirstValue(ClaimTypes.Name),
                    ExternalAccessToken = identity.FindFirstValue("ExternalAccessToken"),
                };
            }
        }

        #endregion
    }
}









/*[System.Web.Mvc.RoutePrefix("api/Account")]
public class AccountController : ApiController
{
    private readonly IUserService _userService;

    // TODO: Need to change this to Sevice-repo structure. Just take away _repo variable and create all the methods in service
    private readonly UserRepository _repo;

    public AccountController(IUserService userService)
    {
        this._userService = userService;
    }

    // POST api/Account/Register
    [System.Web.Mvc.AllowAnonymous]
    [System.Web.Mvc.Route("Register")]
    public IHttpActionResult Register(UserViewModel userViewModel)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        bool result = _userService.Add(userViewModel);

        if (!result)
            return BadRequest(ModelState);

        return Ok();
    }

    private IAuthenticationManager Authentication
    {
        get { return Request.GetOwinContext().Authentication; }
    }

    // GET api/Account/ExternalLogin
    [OverrideAuthentication]
    [HostAuthentication(DefaultAuthenticationTypes.ExternalCookie)]
    [AllowAnonymous]
    [System.Web.Http.HttpGet]
    [Route("ExternalLogin")]//, Name = "ExternalLogin")]
    //public async Task<IHttpActionResult> GetExternalLogin(string provider, string error = null)
    public async Task<IHttpActionResult> GetExternalLogin([FromUri]string provider, string error = null)
    {
        string redirectUri = string.Empty;

        if (error != null)
        {
            return BadRequest(Uri.EscapeDataString(error));
        }

        if (!User.Identity.IsAuthenticated)
        {
            return new ChallengeResult(provider, this);
        }

        var redirectUriValidationResult = ValidateClientAndRedirectUri(this.Request, ref redirectUri);

        if (!string.IsNullOrWhiteSpace(redirectUriValidationResult))
        {
            return BadRequest(redirectUriValidationResult);
        }

        ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);

        if (externalLogin == null)
        {
            return InternalServerError();
        }

        if (externalLogin.LoginProvider != provider)
        {
            Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);
            return new ChallengeResult(provider, this);
        }

        ApplicationUser user = await _userService.FindAsync(new UserLoginInfo(externalLogin.LoginProvider, externalLogin.ProviderKey));

        bool hasRegistered = user != null;

        redirectUri = string.Format("{0}#external_access_token={1}&provider={2}&haslocalaccount={3}&external_user_name={4}",
                                        redirectUri,
                                        externalLogin.ExternalAccessToken,
                                        externalLogin.LoginProvider,
                                        hasRegistered.ToString(),
                                        externalLogin.UserName);

        return Redirect(redirectUri);

    }

    private string ValidateClientAndRedirectUri(HttpRequestMessage request, ref string redirectUriOutput)
    {

        Uri redirectUri;

        var redirectUriString = GetQueryString(Request, "redirect_uri");

        if (string.IsNullOrWhiteSpace(redirectUriString))
        {
            return "redirect_uri is required";
        }

        bool validUri = Uri.TryCreate(redirectUriString, UriKind.Absolute, out redirectUri);

        if (!validUri)
        {
            return "redirect_uri is invalid";
        }

        var clientId = GetQueryString(Request, "client_id");

        if (string.IsNullOrWhiteSpace(clientId))
        {
            return "client_Id is required";
        }

        var client = _repo.FindClient(clientId);

        if (client == null)
        {
            return string.Format("Client_id '{0}' is not registered in the system.", clientId);
        }

        if (!string.Equals(client.AllowedOrigin, redirectUri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
        {
            return string.Format("The given URL is not allowed by Client_id '{0}' configuration.", clientId);
        }

        redirectUriOutput = redirectUri.AbsoluteUri;

        return string.Empty;

    }

    private string GetQueryString(HttpRequestMessage request, string key)
    {
        var queryStrings = request.GetQueryNameValuePairs();

        if (queryStrings == null) return null;

        var match = queryStrings.FirstOrDefault(keyValue => string.Compare(keyValue.Key, key, true) == 0);

        if (string.IsNullOrEmpty(match.Value)) return null;

        return match.Value;
    }

    private JObject GenerateLocalAccessTokenResponse(string userName)
    {

        var tokenExpiration = TimeSpan.FromDays(1);

        ClaimsIdentity identity = new ClaimsIdentity(OAuthDefaults.AuthenticationType);

        identity.AddClaim(new Claim(ClaimTypes.Name, userName));
        identity.AddClaim(new Claim("role", "user"));

        var props = new AuthenticationProperties()
        {
            IssuedUtc = DateTime.UtcNow,
            ExpiresUtc = DateTime.UtcNow.Add(tokenExpiration),
        };

        var ticket = new AuthenticationTicket(identity, props);

        var accessToken = StartUp.OAuthBearerOptions.AccessTokenFormat.Protect(ticket);

        JObject tokenResponse = new JObject(
                                    new JProperty("userName", userName),
                                    new JProperty("access_token", accessToken),
                                    new JProperty("token_type", "bearer"),
                                    new JProperty("expires_in", tokenExpiration.TotalSeconds.ToString()),
                                    new JProperty(".issued", ticket.Properties.IssuedUtc.ToString()),
                                    new JProperty(".expires", ticket.Properties.ExpiresUtc.ToString())
    );

        return tokenResponse;
    }





    // POST api/Account/RegisterExternal
    [AllowAnonymous]
    [Route("RegisterExternal")]
    public async Task<IHttpActionResult> RegisterExternal(ExternalLoginModels.RegisterExternalBindingModel model)
    {

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var verifiedAccessToken = await VerifyExternalAccessToken(model.Provider, model.ExternalAccessToken);
        if (verifiedAccessToken == null)
        {
            return BadRequest("Invalid Provider or External Access Token");
        }

        ApplicationUser user = await _userService.FindAsync(new UserLoginInfo(model.Provider, verifiedAccessToken.user_id));

        bool hasRegistered = user != null;

        if (hasRegistered)
        {
            return BadRequest("External user is already registered");
        }

        user = new ApplicationUser() { UserName = model.UserName };

        IdentityResult result = await _repo.CreateAsync(user);
        if (!result.Succeeded)
        {
            return GetErrorResult(result);
        }

        var info = new ExternalLoginInfo()
        {
            DefaultUserName = model.UserName,
            Login = new UserLoginInfo(model.Provider, verifiedAccessToken.user_id)
        };

        result = await _repo.AddLoginAsync(user.Id, info.Login);
        if (!result.Succeeded)
        {
            return GetErrorResult(result);
        }

        //generate access token response
        var accessTokenResponse = GenerateLocalAccessTokenResponse(model.UserName);

        return Ok(accessTokenResponse);
    }

    [AllowAnonymous]
    [HttpGet]
    [Route("ObtainLocalAccessToken")]
    public async Task<IHttpActionResult> ObtainLocalAccessToken(string provider, string externalAccessToken)
    {

        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(externalAccessToken))
        {
            return BadRequest("Provider or external access token is not sent");
        }

        var verifiedAccessToken = await VerifyExternalAccessToken(provider, externalAccessToken);
        if (verifiedAccessToken == null)
        {
            return BadRequest("Invalid Provider or External Access Token");
        }

        ApplicationUser user = await _userService.FindAsync(new UserLoginInfo(provider, verifiedAccessToken.user_id));

        bool hasRegistered = user != null;

        if (!hasRegistered)
        {
            return BadRequest("External user is not registered");
        }

        //generate access token response
        var accessTokenResponse = GenerateLocalAccessTokenResponse(user.UserName);

        return Ok(accessTokenResponse);

    }

    private IHttpActionResult GetErrorResult(IdentityResult result)
    {
        if (result == null)
        {
            return InternalServerError();
        }
        if (!result.Succeeded)
        {
            if (result.Errors != null)
            {
                foreach (string error in result.Errors)
                {
                    ModelState.AddModelError("", error);
                }
            }
            if (ModelState.IsValid)
            {
                // No ModelState errors are available to send, 
                // so just return an empty BadRequest.
                return BadRequest();
            }
            return BadRequest(ModelState);
        }
        return Ok();
    }

    private async Task<ExternalLoginModels.ParsedExternalAccessToken> VerifyExternalAccessToken(string provider, string accessToken)
    {
        ExternalLoginModels.ParsedExternalAccessToken parsedToken = null;

        var verifyTokenEndPoint = "";

        if (provider == "Facebook")
        {
            //You can get it from here: https://developers.facebook.com/tools/accesstoken/
            //More about debug_tokn here: http://stackoverflow.com/questions/16641083/how-does-one-get-the-app-access-token-for-debug-token-inspection-on-facebook

            var appToken = "185013055265833|ukQttTvv4-wZR9bszWYCcPLQL8s";
            verifyTokenEndPoint = string.Format("https://graph.facebook.com/debug_token?input_token={0}&access_token={1}", accessToken, appToken);
        }
        else if (provider == "Google")
        {
            verifyTokenEndPoint = string.Format("https://www.googleapis.com/oauth2/v1/tokeninfo?access_token={0}", accessToken);
        }
        else
        {
            return null;
        }

        var client = new HttpClient();
        var uri = new Uri(verifyTokenEndPoint);
        var response = await client.GetAsync(uri);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();

            dynamic jObj = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(content);

            parsedToken = new ExternalLoginModels.ParsedExternalAccessToken();

            if (provider == "Facebook")
            {
                parsedToken.user_id = jObj["data"]["user_id"];
                parsedToken.app_id = jObj["data"]["app_id"];

                if (!string.Equals(StartUp.facebookAuthOptions.AppId, parsedToken.app_id, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
            }
            else if (provider == "Google")
            {
                parsedToken.user_id = jObj["user_id"];
                parsedToken.app_id = jObj["audience"];

                if (!string.Equals(StartUp.googleAuthOptions.ClientId, parsedToken.app_id, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

            }

        }

        return parsedToken;
    }

    private class ExternalLoginData
    {
        public string LoginProvider { get; set; }
        public string ProviderKey { get; set; }
        public string UserName { get; set; }
        public string ExternalAccessToken { get; set; }

        public static ExternalLoginData FromIdentity(ClaimsIdentity identity)
        {
            if (identity == null)
            {
                return null;
            }

            Claim providerKeyClaim = identity.FindFirst(ClaimTypes.NameIdentifier);

            if (providerKeyClaim == null || String.IsNullOrEmpty(providerKeyClaim.Issuer) || String.IsNullOrEmpty(providerKeyClaim.Value))
            {
                return null;
            }

            if (providerKeyClaim.Issuer == ClaimsIdentity.DefaultIssuer)
            {
                return null;
            }

            return new ExternalLoginData
            {
                LoginProvider = providerKeyClaim.Issuer,
                ProviderKey = providerKeyClaim.Value,
                UserName = identity.FindFirstValue(ClaimTypes.Name),
                ExternalAccessToken = identity.FindFirstValue("ExternalAccessToken"),
            };
        }
    }
}

}*/
