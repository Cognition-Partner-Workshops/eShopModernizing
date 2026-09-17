using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace eShopLegacyWebForms
{
    public partial class Site_Mobile : System.Web.UI.MasterPage
    {
        private const string AntiXsrfTokenKey = "__AntiXsrfToken";
        private const string AntiXsrfViewStateKey = "_AntiXsrfToken";
        private string _antiXsrfTokenValue;

        protected void Page_Init(object sender, EventArgs e)
        {
            if (Session != null && Session.SessionID != null)
            {
                Page.ViewStateUserKey = Session.SessionID;
            }

            var requestCookie = Request.Cookies[AntiXsrfTokenKey];
            Guid requestCookieGuidValue;
            if (requestCookie != null && Guid.TryParse(requestCookie.Value, out requestCookieGuidValue))
            {
                _antiXsrfTokenValue = requestCookie.Value;
            }
            else
            {
                _antiXsrfTokenValue = Guid.NewGuid().ToString("N");

                var responseCookie = new HttpCookie(AntiXsrfTokenKey)
                {
                    HttpOnly = true,
                    Value = _antiXsrfTokenValue
                };
                if (FormsAuthentication.RequireSSL && Request.IsSecureConnection)
                {
                    responseCookie.Secure = true;
                }
                Response.Cookies.Set(responseCookie);
            }

            Page.PreLoad += Page_PreLoad;
        }

        protected void Page_PreLoad(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                ViewState[AntiXsrfViewStateKey] = _antiXsrfTokenValue;
            }
            else if ((string)ViewState[AntiXsrfViewStateKey] != _antiXsrfTokenValue)
            {
                throw new InvalidOperationException("Validation of Anti-XSRF token failed.");
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {

        }
    }
}