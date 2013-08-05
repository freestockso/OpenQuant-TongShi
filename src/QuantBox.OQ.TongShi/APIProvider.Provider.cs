﻿using NLog;
using NLog.Config;
using SmartQuant;
using SmartQuant.Providers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace QuantBox.OQ.TongShi
{
    public partial class APIProvider : IProvider, IDisposable
    {
        private ProviderStatus status;
        private bool isConnected;

        public event EventHandler Connected;
        public event EventHandler Disconnected;
        public event ProviderErrorEventHandler Error;

        private bool disposed;

        private static readonly Logger mdlog = LogManager.GetLogger("TongShi.M");
        private static readonly Logger htlog = LogManager.GetLogger("TongShi.H");

        // Use C# destructor syntax for finalization code.
        ~APIProvider()
        {
            // Simply call Dispose(false).
            Dispose(false);
        }

        public APIProvider()
        {
            try
            {
                LogManager.Configuration = new XmlLoggingConfiguration(@"Bin/QuantBox.nlog");
            }
            catch (Exception ex)
            {
                mdlog.Warn(ex.Message);
            }

            BarFactory = new SmartQuant.Providers.BarFactory();
            status = ProviderStatus.Unknown;
            SmartQuant.Providers.ProviderManager.Add(this);
        }

        //Implement IDisposable.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Free other state (managed objects).
                }
                // Free your own state (unmanaged objects).
                // Set large fields to null.
                Shutdown();
                disposed = true;
            }
            //base.Dispose(disposing);
        }

        #region IProvider
        [Category(CATEGORY_INFO)]
        public byte Id//不能与已经安装的插件ID重复
        {
            get { return 60; }
        }

        [Category(CATEGORY_INFO)]
        public string Name
        {
            get { return "TongShi"; }//不能与已经安装的插件Name重复
        }

        [Category(CATEGORY_INFO)]
        public string Title
        {
            get { return "QuantBox TongShi Provider"; }
        }

        [Category(CATEGORY_INFO)]
        public string URL
        {
            get { return "www.quantbox.cn"; }
        }

        [Category(CATEGORY_INFO)]
        [Description("插件版本信息")]
        public string Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        [Category(CATEGORY_STATUS)]
        public bool IsConnected
        {
            get { return isConnected; }
        }

        [Category(CATEGORY_STATUS)]
        public ProviderStatus Status
        {
            get { return status; }
        }

        public void Connect(int timeout)
        {
            Connect();
            ProviderManager.WaitConnected(this, timeout);
        }

        public void Connect()
        {
            //_Connect();
            InitStockService();
            try
            {
                mdlog.Info("正在启动默认通视接口……");
                if (StockService != null)
                    StockService.Init();
            }
            catch(Exception ex)
            {
                mdlog.Warn(ex.Message);
                try
                {
                    StockService.Init("Stock.dll",2000);
                }
                catch (Exception ex)
                {
                }
            }
        }

        public void Disconnect()
        {
            //_Disconnect();
            if (StockService != null)
                StockService.Quit();

            ChangeStatus(ProviderStatus.Disconnected);
            isConnected = false;
            EmitDisconnectedEvent();
        }

        public void Shutdown()
        {
            Disconnect();

            //SettingsChanged();

            //timerConnect.Elapsed -= timerConnect_Elapsed;
            //timerDisconnect.Elapsed -= timerDisconnect_Elapsed;
            //timerAccount.Elapsed -= timerAccount_Elapsed;
            //timerPonstion.Elapsed -= timerPonstion_Elapsed;
        }

        public event EventHandler StatusChanged;

        private void ChangeStatus(ProviderStatus status)
        {
            this.status = status;
            EmitStatusChangedEvent();
        }

        private void EmitStatusChangedEvent()
        {
            if (StatusChanged != null)
            {
                StatusChanged(this, EventArgs.Empty);
            }
        }

        private void EmitConnectedEvent()
        {
            if (Connected != null)
            {
                Connected(this, EventArgs.Empty);
            }
        }

        private void EmitDisconnectedEvent()
        {
            if (Disconnected != null)
            {
                Disconnected(this, EventArgs.Empty);
            }
        }

        private void EmitError(int id, int code, string message)
        {
            if (Error != null)
                Error(new ProviderErrorEventArgs(new ProviderError(Clock.Now, this, id, code, message)));
        }
        #endregion
    }
}
