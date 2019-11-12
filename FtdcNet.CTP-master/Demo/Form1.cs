using CTP;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Demo
{
    public partial class Form1 : Form
    {
        FtdcMdAdapter DataApi;
        FtdcTdAdapter TraderApi;

        int iRequestID = 0;
        
        ThostFtdcDepthMarketDataField lastData = new ThostFtdcDepthMarketDataField();
        double douLastPrice;
        public Form1()
        {
            InitializeComponent();

            this.Disposed += Form1_Disposed;
        }

        void Form1_Disposed(object sender, EventArgs e)
        {
            if (DataApi != null)
                DataApi.Dispose();
            if (TraderApi != null)
                TraderApi.Dispose();
        }

        void DataApi_OnRtnEvent(object sender, OnRtnEventArgs e)
        {
            //Console.WriteLine("DataApi_OnRtnEvent " + e.EventType.ToString());

            var fld = Conv.P2S<ThostFtdcDepthMarketDataField>(e.Param);



            //string sData = "当前交易日:{0} 当前时间:{1}  最后修改时间:{2} 成交量：{3} 卖1价格:{4} 卖1数量:{5} 买1价格:{6} 买1数量:{7} 本次结算价：{8} 成交数量:{9} ";
            //sData = string.Format(sData, fld.TradingDay, fld.UpdateTime, fld.UpdateMillisec, vol, fld.AskPrice1, fld.AskVolume1, fld.BidPrice1, fld.BidVolume1, fld.SettlementPrice, fld.Volume);



            int vol = fld.Volume - lastData.Volume;
            string sLose = "";

            if (lastData.ActionDay == null)
            {
                lastData = fld;
                return;
            }
            //Console.WriteLine("{0}.{1:D3} {2} {3}", fld.UpdateTime, fld.UpdateMillisec, fld.InstrumentID, fld.LastPrice);
            //if (vol > 0)
            //{
            //双开 空换 多换 空平 多平
            string sData2 = "当前交易日:{0} 当前时间:{1} 当前价格：{2} 成交：{3} {4}";

            //多开
            double douLose;
            ConsoleColor tColor = ConsoleColor.White;

            int intDirect = 0;
            if (fld.LastPrice > lastData.AskPrice1 || fld.LastPrice > lastData.BidPrice1)
            {
                intDirect = 1;
                tColor = ConsoleColor.Red;
            }
            else if (fld.LastPrice < lastData.AskPrice1 || fld.LastPrice < lastData.BidPrice1)
            {
                intDirect = -1;
                tColor = ConsoleColor.Green;
            }
            else {
                tColor = ConsoleColor.White;
            }
            if (vol > 0) {
                Console.ForegroundColor = tColor;
                //Console.WriteLine(vol);
            }

            //else {
            //    if (fld.LastPrice > fld.AskPrice1)
            //    {
            //        intDirect = 1;
            //    }
            //    else if (fld.LastPrice < fld.BidPrice1)
            //    {
            //        intDirect = -1;
            //    }
            //    else {
            //        intDirect = 0; //middle
            //    }
            //}
            double xx = 0; double yy = 0;
 
            if (intDirect == 1)
            {
                //仓位变化 
                /*
                 * 
                16 16 双开 - 成交：16  空平:16 多开:0 多平:16 空开:0
                14 -10 空平 - 成交：14  空平:-12 多开:2 多平:-12 空开:2
                8 0 多换 成交：8  空平:-4 多开:4 多平:-4 空开:4

                 * */
                douLose = fld.OpenInterest - lastData.OpenInterest;
                #region 上涨
                //1.多开 多平 空开  空平  

                if (douLose != 0)
                {
                    calCtp(vol, douLose, ref xx, ref yy);
                }
                else {
                    xx = vol;
                    yy = vol;
                }

                
                sLose += " 空平:" + xx;
                sLose += " 多开:" + yy;

                #endregion
            }
            if (intDirect == -1)
            {
                /*
                 46  -14 多平
                10 0 空换
                2 -2 双平
                 */
                #region 下跌
                douLose = fld.OpenInterest - lastData.OpenInterest;

                if (douLose != 0)
                {
                    calCtp(vol, douLose, ref xx, ref yy);
                }
                else {
                    xx = vol;
                    yy = vol;
                }

           

                sLose += " 多平:" + xx;
                sLose += " 空开:" + yy;
                #endregion

            }
           
            if(vol > 0)
                Console.WriteLine(string.Format(sData2,
                            fld.TradingDay, fld.UpdateTime, fld.LastPrice,
                            vol, sLose)
                        );
        

        //if (fld.OpenInterest < lastData.OpenInterest)
        //{

        //    sLose = " 平仓: " + (fld.OpenInterest - lastData.OpenInterest );
        //}
        //else if (fld.OpenInterest == lastData.OpenInterest)
        //{

        //    sLose = " 换手: " + (lastData.Volume - fld.Volume);
        //}
        //else
        //{
        //    sLose = " 增仓: " + (lastData.Volume - fld.Volume);
        //}
        //if ( vol > 0 )
        //Console.WriteLine(string.Format(sData2,
        //            fld.TradingDay, fld.UpdateTime, fld.LastPrice,
        //            vol, sLose)

        //        );
        //}
        lastData = fld;
            douLastPrice = fld.LastPrice;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sum">总成交量</param>
        /// <param name="absSum">仓位变动</param>
        /// <param name="xx">平仓</param>
        /// <param name="yy">开仓</param>
        void calCtp(double sum, double absSum, ref double xx, ref double yy )
        {
            //sum 21  absum -5
            double intStart;
            xx = -1;
            yy = -1;

            intStart = (sum * -1);
            for (double x = intStart; x <= sum; x++)
            {
               
                for (double y = intStart; y <= sum; y++)
                {
                    if (
                        (Math.Abs(x) + y == sum)
                        && (x + y == absSum)
                        )
                    {
                        //
                        xx = x;
                        yy = y;
                        break;
                        Console.WriteLine(string.Format("x={0}   y={1}", x, y));
                    }
                }
            }
        }
        void DataApi_OnRspEvent(object sender, OnRspEventArgs e)
        {
            Console.WriteLine("DataApi_OnRspEvent " + e.EventType.ToString());
            bool err = IsError(e.RspInfo, e.EventType.ToString());

            switch (e.EventType)
            {
                case EnumOnRspType.OnRspUserLogin:
                    if (!err)
                        Console.WriteLine("登录成功");
                    break;
                case EnumOnRspType.OnRspSubMarketData:
                    {
                        var f = Conv.P2S<ThostFtdcSpecificInstrumentField>(e.Param);
                        Console.WriteLine("订阅成功:" + f.InstrumentID);
                    }
                    break;
                case EnumOnRspType.OnRspUnSubMarketData:
                    {
                        var f = Conv.P2S<ThostFtdcSpecificInstrumentField>(e.Param);
                        Console.WriteLine("退订成功:" + f.InstrumentID);
                    }
                    break;
            }
        }

        void DataApi_OnFrontEvent(object sender, OnFrontEventArgs e)
        {
            switch (e.EventType)
            {
                case EnumOnFrontType.OnFrontConnected:
                    {
                        var req = new ThostFtdcReqUserLoginField();
                        req.BrokerID = this.txtBrokerID.Text;
                        req.UserID = this.txtUserID.Text;
                        req.Password = this.txtPasswd.Text;
                        int iResult = DataApi.ReqUserLogin(req, ++iRequestID);
                    }
                    break;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Console.WriteLine("Platform is {0}", Environment.Is64BitProcess ? "x64" : "x86");
            Console.WriteLine("TradeApi version {0}", FtdcTdAdapter.GetApiVersion());
            Console.WriteLine("DataApi  version {0}", FtdcMdAdapter.GetApiVersion());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (DataApi == null)
            {
                if (CheckInput() == false)
                    return;

                DataApi = new FtdcMdAdapter("", false, false);
                DataApi.OnFrontEvent += DataApi_OnFrontEvent;
                DataApi.OnRspEvent += DataApi_OnRspEvent;
                DataApi.OnRtnEvent += DataApi_OnRtnEvent;

                DataApi.RegisterFront(txtFrontMD.Text);
                DataApi.Init();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            List<string> inst = new List<string>();
            inst.Add("i2001");
            //inst.Add("ag1912");
            //inst.Add("j1909");
            //inst.Add("SR909");
            if (DataApi != null)
                DataApi.SubscribeMarketData(inst.ToArray());
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (DataApi != null)
            {
                DataApi.Dispose();
                DataApi = null;
                Console.WriteLine("Disconnected.");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (DataApi != null)
            {
                DataApi.UnSubscribeMarketData(new string[] { "ag1912" });
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (TraderApi == null)
            {
                if (CheckInput() == false)
                    return;

                TraderApi = new FtdcTdAdapter("");
                TraderApi.OnFrontEvent += TraderApi_OnFrontEvent;
                TraderApi.OnRspEvent += TraderApi_OnRspEvent;
                TraderApi.OnRtnEvent += TraderApi_OnRtnEvent;
                TraderApi.OnErrRtnEvent += TraderApi_OnErrRtnEvent;
                TraderApi.SubscribePublicTopic(EnumTeResumeType.THOST_TERT_QUICK);
                TraderApi.SubscribePrivateTopic(EnumTeResumeType.THOST_TERT_QUICK);
                TraderApi.RegisterFront(txtFrontTD.Text);
                TraderApi.Init();
            }
        }

        bool IsError(ThostFtdcRspInfoField rspinfo, string source)
        {
            if (rspinfo != null && rspinfo.ErrorID != 0)
            {
                Console.WriteLine(rspinfo.ErrorMsg + ", 来源 " + source);
                return true;
            }
            return false;
        }

        void TraderApi_OnErrRtnEvent(object sender, OnErrRtnEventArgs e)
        {
            Console.WriteLine("=====> " + e.EventType);
        }

        void TraderApi_OnRtnEvent(object sender, OnRtnEventArgs e)
        {
            Console.WriteLine("=====> " + e.EventType);
        }

        void TraderApi_OnRspEvent(object sender, OnRspEventArgs e)
        {
            bool err = IsError(e.RspInfo, e.EventType.ToString());

            switch (e.EventType)
            {
                case EnumOnRspType.OnRspAuthenticate:
                    if (err)
                    {
                        Console.WriteLine("认证失败!!!");
                    }
                    else
                    {
                        Console.WriteLine("认证成功!!!");
                        if (chkSubmitUserSystemInfo.Checked)
                        {
                            RegSystemInfo();
                        }
                        ReqUserLogin();
                    }

                    break;
                case EnumOnRspType.OnRspUserLogin:
                    if (err)
                    {
                        Console.WriteLine("登录失败");
                    }
                    else
                    {
                        Console.WriteLine("登录成功");
                        var fld = Conv.P2S<ThostFtdcRspUserLoginField>(e.Param);
                        Console.WriteLine("TradingDay is " + fld.TradingDay);
                        Console.WriteLine("CTP Version " + FtdcTdAdapter.GetApiVersion());

                        ThostFtdcSettlementInfoConfirmField req = new ThostFtdcSettlementInfoConfirmField();
                        req.BrokerID = this.txtBrokerID.Text;
                        req.InvestorID = this.txtUserID.Text;
                        TraderApi.ReqSettlementInfoConfirm(req, ++this.iRequestID);
                    }
                    break;
                case EnumOnRspType.OnRspQryInstrument:
                    if (e.Param != IntPtr.Zero)
                    {
                        var fld = Conv.P2S<ThostFtdcInstrumentField>(e.Param);
                        Console.WriteLine("=====> {0}, {1},  isLast {2}", e.EventType, fld.InstrumentID, e.IsLast);
                    }
                    break;
            }
        }

        void TraderApi_OnFrontEvent(object sender, OnFrontEventArgs e)
        {
            switch (e.EventType)
            {
                case EnumOnFrontType.OnFrontConnected:
                    {
                        if (chkAuthenticate.Checked)
                        {
                            var req = new ThostFtdcReqAuthenticateField();
                            req.BrokerID = txtBrokerID.Text;
                            req.UserID = txtUserID.Text;
                            req.AppID = txtAppID.Text;
                            req.AuthCode = txtAuthCode.Text;

                            TraderApi.ReqAuthenticate(req, ++iRequestID);
                        }
                        else
                        {
                            ReqUserLogin();
                        }
                    }
                    break;
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (TraderApi != null)
            {
                TraderApi.Dispose();
                TraderApi = null;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (TraderApi != null)
            {
                var req = new ThostFtdcQryInstrumentField();
                req.InstrumentID = "";
                req.ExchangeID = "";
                TraderApi.ReqQryInstrument(req, ++this.iRequestID);
            }
        }

        private bool CheckInput()
        {
            if (string.IsNullOrWhiteSpace(txtBrokerID.Text) || string.IsNullOrWhiteSpace(txtFrontTD.Text) || string.IsNullOrWhiteSpace(txtFrontMD.Text))
            {
                MessageBox.Show(this, "请输入Broker相关项", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtUserID.Text) || string.IsNullOrWhiteSpace(txtPasswd.Text))
            {
                MessageBox.Show(this, "请输入用户名和密码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (chkAuthenticate.Checked)
            {
                if (string.IsNullOrWhiteSpace(txtAppID.Text) || string.IsNullOrWhiteSpace(txtAuthCode.Text))
                {
                    MessageBox.Show(this, "已启用App认证，请输入AppID和AuthCode", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            return true;
        }

        void ReqUserLogin()
        {
            var req = new ThostFtdcReqUserLoginField();
            req.BrokerID = this.txtBrokerID.Text;
            req.UserID = this.txtUserID.Text;
            req.Password = this.txtPasswd.Text;
            int iResult = TraderApi.ReqUserLogin(req, ++iRequestID);
        }

        void RegSystemInfo()
        {
            byte[] buffer = new byte[512];
            int nLen = 0;
            int res = FtdcTdAdapter.CTP_GetSystemInfo(buffer, ref nLen);
            if (res != 0 || nLen == 0)
            {
                Console.WriteLine("CTP_GetSystemInfo() 失败, 错误代码 {0}, nLen = {1}", res, nLen);
                return;
            }

            var field = new ThostFtdcUserSystemInfoField();
            field.BrokerID = txtBrokerID.Text;
            field.UserID = txtUserID.Text;
            Array.Copy(buffer, 0, field.ClientSystemInfo, 0, nLen);

            field.ClientPublicIP = "127.0.0.1";
            field.ClientIPPort = 65535;
            field.ClientLoginTime = "11:28:28";
            field.ClientAppID = "Q7";
            int result = TraderApi.RegisterUserSystemInfo(field);
            if (result == 0)
                Console.WriteLine("RegisterUserSystemInfo() 成功");
            else
                Console.WriteLine("RegisterUserSystemInfo() 失败, 错误代码 {0}", result);

            /*  RegisterUserSystemInfo 错误代码 
             0 正确
            -1 字段长度不对
            -2 非CTP采集的终端信息
            -3 当前终端类型非多对多中继
            -5 字段中存在非法字符或者长度超限
            -6 采集结果字段错误
             */
        }
    }
}
