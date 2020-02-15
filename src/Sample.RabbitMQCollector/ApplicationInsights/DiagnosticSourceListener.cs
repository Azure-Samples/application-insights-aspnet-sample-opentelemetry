using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sample.RabbitMQCollector.ApplicationInsights
{
    internal class DiagnosticSourceListener : IObserver<KeyValuePair<string, object>>
    {
        public DiagnosticSourceListener()
        {
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(KeyValuePair<string, object> value)
        {
            if (Activity.Current == null)
            {
                //CollectorEventSource.Log.NullActivity(value.Key);
                return;
            }

            try
            {
                if (value.Key.EndsWith("Start"))
                {
                    OnStartActivity(Activity.Current, value.Value);
                }
                else if (value.Key.EndsWith("Stop"))
                {
                    this.OnStopActivity(Activity.Current, value.Value);
                }
                else if (value.Key.EndsWith("Exception"))
                {
                    this.OnException(Activity.Current, value.Value);
                }
                else
                {
                    this.OnCustom(value.Key, Activity.Current, value.Value);
                }
            }
            catch (Exception)
            {
                //CollectorEventSource.Log.UnknownErrorProcessingEvent(this.handler?.SourceName, value.Key, ex);
            }
        }

        protected virtual void OnCustom(string key, Activity current, object value)
        {        
        }

        protected virtual void OnException(Activity current, object value)
        {
        }

        protected virtual void OnStopActivity(Activity current, object value)
        {
        }

        protected virtual void OnStartActivity(Activity current, object value)
        {
        }
    }
}
