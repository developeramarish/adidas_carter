﻿using AdidasBot.Model;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AdidasBot
{
    public class Job : INotifyPropertyChanged
    {

        // constructor without handler
        public Job(string pid, string sizeCode, int quantity)
        {
            this.pid = pid;
            this.sizeCode = sizeCode;
            this.quantity = quantity;
            this.handler = new HttpClientHandler();
            this.handler.UseCookies = true;
            this.handler.CookieContainer = new CookieContainer();
            //this.handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            this.client = new HttpClient(this.handler);
            this.userAgent = "Mozilla/5.0 (X11; Linux x86_64; rv:53.0) Gecko/20100101 Firefox/53.0";
            this.client.DefaultRequestHeaders.Add("User-Agent", this.userAgent);
            this.client.DefaultRequestHeaders.Add("Accept", "*/*");
            this.client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
            this.client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            this.client.DefaultRequestHeaders.Add("Referer", "http://www.adidas.co.uk/" + this.pid + ".html");
            this.client.DefaultRequestHeaders.Add("Connection", "close");

            if (Manager.Username == "username")
            {
                throw new Exception();
            }

        }

        // constructor with handler
        public Job(string pid, string sizeCode, int quantity, HttpClientHandler handler)
        {
            this.pid = pid;
            this.sizeCode = sizeCode;
            this.quantity = quantity;
            this.handler = handler;

            //this.skipCaptcha = false;

        }


        #region Properties
        private string pid;

        public string PID
        {
            get { return pid; }
            set { pid = value; }
        }

        private string size;

        public string Size
        {
            get { return size; }
            set { size = value; }
        }

        private string sizeCode;

        public string SizeCode
        {
            get { return sizeCode; }
            set { sizeCode = value; }
        }


        private int quantity;


        public int Quantity
        {
            get { return quantity; }
            set { quantity = value; }
        }

        private Proxy proxy;

        public Proxy Proxy
        {
            get { return proxy; }
            set { proxy = value;
                this.handler.UseProxy = true;
                WebProxy pr = new WebProxy(Proxy.IP + ":" + Proxy.Port, false);
                if (proxy.Username != null && proxy.Password != null) pr.Credentials = new NetworkCredential(proxy.Username, proxy.Password);
                this.handler.Proxy = pr;
                OnPropertyChanged("proxy");
            }
        }

        private string captchaResponse;

        public string CaptchaResponse
        {
            get { return captchaResponse; }
            set { captchaResponse = value; }
        }

        private Account acc;

        public Account Acc
        {
            get { return acc; }
            set { acc = value;
                OnPropertyChanged("Acc");
            }
        }

        private HttpClient client;

        public HttpClient Client
        {
            get { return client; }
            set { client = value; }
        }


        private HttpClientHandler handler;

        public HttpClientHandler Handler
        {
            get { return handler; }
            set { handler = value; }
        }


        private string status;

        public string Status
        {
            get { return status; }
            set { status = value;
                OnPropertyChanged("status");
            }
        }

        //public bool skipCaptcha { get; set; }

        private int retries;

        public int Retries
        {
            get { return retries; }
            set { retries = value;
                OnPropertyChanged("Retries");
            }
        }

        private string userAgent;

        public string UserAgent
        {
            get { return userAgent; }
            set { userAgent = value; }
        }



        #endregion


        #region Data binding stuff
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propName)
        {
            PropertyChangedEventHandler h = PropertyChanged;

            if (h != null)
            {
                h(this, new PropertyChangedEventArgs(propName));
            }

        }
        #endregion


        // Methods
        public async Task<Boolean> addToCart2()
        {
            bool _status = false;
            string _url = Manager.atcUrl + Manager.selectedProfile.Domain.Replace("global.", "") +
                "/on/demandware.store/" + Manager.selectedProfile.InUrlLong + "/" + Manager.selectedProfile.InUrlShort +
                "/" + Manager.atcUrlPart;


            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("layer", "Add To Bag overlay");
            dict.Add("pid", this.pid+"_"+this.sizeCode);
            dict.Add("Quantity", this.quantity.ToString());
            dict.Add("g-recaptcha-response", this.captchaResponse);
            dict.Add("masterPid", this.pid);
            dict.Add("sessionSelectedStoreID", "null");
            dict.Add("ajax", "true");

            var data = new FormUrlEncodedContent(dict);

            // add custom headers here...
            foreach (string key in Manager.customHeaders.Keys)
            {
                Console.WriteLine(key.ToLower());
                if (key.ToLower() == "user-agent")
                {
                    this.userAgent = Manager.customHeaders[key];
                    continue;
                }

                client.DefaultRequestHeaders.Add(key, Manager.customHeaders[key]);

            }


            //client.DefaultRequestHeaders.Add("Origin", "http://www.adidas.com");
            //client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:51.0) Gecko/20100101 Firefox/51.0");
            //client.DefaultRequestHeaders.Add("User-Agent", this.userAgent);
            //client.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
            //client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            //client.DefaultRequestHeaders.Add("Connection", "close");

            using (HttpResponseMessage response = await this.client.PostAsync(_url, data))
            {
                string content = await response.Content.ReadAsStringAsync();
                //Manager.debugSave(this.pid + "_" + this.sizeCode + new Random().Next(1,999999999) + ".html", content);

                if (content.Contains("OUT-OF-STOCK"))
                {
                    this.Status = "OUT-OF-STOCK";

                }
                else if (content.Contains("INVALID_CAPTCHA"))
                {
                    this.Status = "INVALID_CAPTCHA";

                }
                else if (content.Contains("QUANTITY-EXCEEDED"))
                {
                    this.Status = "QUANTITY-EXCEEDED";
                }
                else if(content.Contains("dwfrm_cart_checkoutShortcutPaypal"))
                {
                    //await openCart();
                    this.Status = "OK";
                    _status = true;

                }
                else
                {
                    this.Status = "Something fucked up!";
                }

            }

            return _status;

        }


        public async Task openCart()
        {
            string cartUrl = "http://www." + Manager.selectedProfile.Domain.Replace("global.","") +
                "/on/demandware.store/" + Manager.selectedProfile.InUrlLong + "/" + Manager.selectedProfile.InUrlShort + "/Cart-Show";

            using (HttpResponseMessage response = await this.client.GetAsync(cartUrl))
            {
                string content = await response.Content.ReadAsStringAsync();
                //Manager.debugSave("cart.html", content);
            }
        }



        public override string ToString()
        {
            return this.PID + " " + this.SizeCode + " " +
                this.Quantity + " " + this.Status;
        }

    }
}