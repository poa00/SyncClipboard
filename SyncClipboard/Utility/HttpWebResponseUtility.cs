﻿using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;

namespace SyncClipboard
{
    public class HttpWebResponseUtility
    {
        private static readonly string DefaultUserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
        //private static CookieCollection savedCookies = null;

        private static void SaveCookies(CookieCollection cookies)
        {
            //savedCookies = cookies;
        }

        public static string GetText(string url, int timeout, string AuthHeader)
        {
            HttpWebRequest request = CreateHttpRequest(url, "GET", timeout, AuthHeader);
            HttpWebResponse response = AnalyseHttpResponse((HttpWebResponse)request.GetResponse());

            StreamReader objStrmReader = new StreamReader(response.GetResponseStream());
            string text = objStrmReader.ReadToEnd();

            objStrmReader.Close();
            response.Close();

            return text;
        }

        public static void PutText(string url, string text, int timeout, string AuthHeader)
        {
            HttpWebRequest request = CreateHttpRequest(url, "PUT", timeout, AuthHeader);

            byte[] postBytes = Encoding.UTF8.GetBytes(text);
            request.ContentLength = Encoding.UTF8.GetBytes(text).Length;

            Stream reqStream = request.GetRequestStream();
            reqStream.Write(postBytes, 0, postBytes.Length);
            reqStream.Close();

            HttpWebResponse response = AnalyseHttpResponse((HttpWebResponse)request.GetResponse());
            response.Close();
        }

        public static HttpWebRequest CreateHttpRequest(string url, string httpMethod, int timeout, string authHeader)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException("url");
            }

            SetSecurityProtocol(url);
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = httpMethod;
            request.UserAgent = DefaultUserAgent;
            request.Timeout = timeout;

            if (authHeader != null)
            {
                request.Headers.Add(authHeader);
            }
            request.CookieContainer = new CookieContainer();
            // if (useCookies && savedCookies != null)
            // {
            //     request.CookieContainer.Add(savedCookies);
            // }
            return request;
        }

        public static void PutImage(string url, Image image, int timeout, string authHeader)
        {
            HttpWebRequest request = HttpWebResponseUtility.CreateHttpRequest(url, "PUT", Config.TimeOut, authHeader);
            request.ContentType = "application/x-bmp";

            MemoryStream mstream = new MemoryStream();
            image.Save(mstream, System.Drawing.Imaging.ImageFormat.Bmp);
            byte[] byteData = new Byte[mstream.Length];
    
            mstream.Position = 0;
            mstream.Read(byteData, 0, byteData.Length);
            mstream.Close();

            Stream reqStream = request.GetRequestStream();
            reqStream.Write(byteData, 0, byteData.Length);
            reqStream.Close();

            HttpWebResponse response = AnalyseHttpResponse((HttpWebResponse)request.GetResponse());
            response.Close();
        }

        public static void SetSecurityProtocol(string url)
        {
            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            }
        }

        private static HttpWebResponse AnalyseHttpResponse(HttpWebResponse response)
        {
            HttpStatusCode code = response.StatusCode;
            string codeMessage = response.StatusDescription;
            if (response.StatusCode < System.Net.HttpStatusCode.OK || response.StatusCode >= System.Net.HttpStatusCode.Ambiguous)
            {
                SaveCookies(new CookieCollection());
                response.Close();
                throw new WebException(response.StatusCode.GetHashCode().ToString());
            }
            SaveCookies(response.Cookies);
            return response;
        }
    } 
}